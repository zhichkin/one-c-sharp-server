﻿using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;

namespace OQL
{
    [Export(typeof(IVsTextViewCreationListener))]
    [Name("one-c-sharp completion handler")]
    [ContentType(OneCSharp.ContentType)]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal class TestCompletionHandlerProvider : IVsTextViewCreationListener
    {
        [Import] internal IVsEditorAdaptersFactoryService AdapterService = null;
        [Import] internal ICompletionBroker CompletionBroker { get; set; }
        [Import] internal SVsServiceProvider ServiceProvider { get; set; }
        [Import] internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }
        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            ITextView textView = AdapterService.GetWpfTextView(textViewAdapter);
            if (textView == null)
                return;

            Func<TestCompletionCommandHandler> createCommandHandler = delegate () { return new TestCompletionCommandHandler(textViewAdapter, textView, this); };
            textView.Properties.GetOrCreateSingletonProperty(createCommandHandler);
        }
    }
    internal class TestCompletionCommandHandler : IOleCommandTarget
    {
        private IOleCommandTarget m_nextCommandHandler;
        private ITextView m_textView;
        private TestCompletionHandlerProvider m_provider;
        private ICompletionSession m_session;

        private TextViewController _controller;

        internal TestCompletionCommandHandler(IVsTextView textViewAdapter, ITextView textView, TestCompletionHandlerProvider provider)
        {
            m_textView = textView;
            m_provider = provider;
            _controller = new TextViewController(m_textView.TextBuffer);

            //add the command to the command chain
            textViewAdapter.AddCommandFilter(this, out m_nextCommandHandler);
        }
        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            //return VSConstants.S_OK;
            return m_nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ITrackingSpan trackingSpan = GetCodeContext();

            if (trackingSpan == null
                && (nCmdID == (uint)VSConstants.VSStd2KCmdID.BACKSPACE
                || nCmdID == (uint)VSConstants.VSStd2KCmdID.DELETE))
            {
                return VSConstants.S_OK;
            }

            if (trackingSpan != null
                //&& pguidCmdGroup == VSConstants.VSStd2K
                && (nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR
                || nCmdID == (uint)VSConstants.VSStd2KCmdID.BACKSPACE
                || nCmdID == (uint)VSConstants.VSStd2KCmdID.DELETE
                || nCmdID == (uint)VSConstants.VSStd2KCmdID.END))
            {
                return VSConstants.S_OK;
            }

            if (VsShellUtilities.IsInAutomationFunction(m_provider.ServiceProvider))
            {
                return m_nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }
            //make a copy of this so we can look at it after forwarding some commands
            uint commandID = nCmdID;
            char typedChar = char.MinValue;
            //make sure the input is a char before getting it
            if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
            {
                typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
            }

            //check for a commit character
            if (nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN
                || nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB
                || (char.IsWhiteSpace(typedChar) || char.IsPunctuation(typedChar)))
            {
                //check for a selection
                if (m_session != null && !m_session.IsDismissed)
                {
                    //if the selection is fully selected, commit the current session
                    if (m_session.SelectedCompletionSet.SelectionStatus.IsSelected)
                    {
                        m_session.Commit();
                        //also, don't add the character to the buffer
                        return VSConstants.S_OK;
                    }
                    else
                    {
                        //if there is no selection, dismiss the session
                        m_session.Dismiss();
                    }
                }
            }

            //pass along the command so the char is added to the buffer
            int retVal = m_nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            bool handled = false;
            if (!typedChar.Equals(char.MinValue) && char.IsLetterOrDigit(typedChar))
            {
                if (m_session == null || m_session.IsDismissed) // If there is no active session, bring up completion
                {
                    this.TriggerCompletion();
                    m_session.Filter();
                }
                else    //the completion session is already active, so just filter
                {
                    m_session.Filter();
                }
                handled = true;
            }
            else if (commandID == (uint)VSConstants.VSStd2KCmdID.BACKSPACE   //redo the filter if there is a deletion
                || commandID == (uint)VSConstants.VSStd2KCmdID.DELETE)
            {
                if (m_session != null && !m_session.IsDismissed)
                    m_session.Filter();
                handled = true;
            }
            if (handled) return VSConstants.S_OK;
            return retVal;
        }
        private void OnSessionDismissed(object sender, EventArgs e)
        {
            m_session.Dismissed -= this.OnSessionDismissed;
            m_session = null;
        }
        private bool TriggerCompletion()
        {
            //the caret must be in a non-projection location 
            SnapshotPoint? caretPoint =
            m_textView.Caret.Position.Point.GetPoint(
            textBuffer => (!textBuffer.ContentType.IsOfType("projection")), PositionAffinity.Predecessor);
            if (!caretPoint.HasValue)
            {
                return false;
            }

            m_session = m_provider.CompletionBroker.CreateCompletionSession
         (m_textView,
                caretPoint.Value.Snapshot.CreateTrackingPoint(caretPoint.Value.Position, PointTrackingMode.Positive),
                true);

            //subscribe to the Dismissed event on the session 
            m_session.Dismissed += this.OnSessionDismissed;
            m_session.Start();

            return true;
        }

        private ITrackingSpan GetCodeContext()
        {
            SnapshotPoint? caretPoint = m_textView.Caret.Position.BufferPosition;
            if (caretPoint.HasValue)
            {
                return _controller.GetTrackingSpan(caretPoint.Value);
            }
            return null;
        }
    }
}