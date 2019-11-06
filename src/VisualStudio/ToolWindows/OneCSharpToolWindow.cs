﻿namespace OneCSharp.VisualStudio
{
    using global::OneCSharp.OQL.Model;
    using global::OneCSharp.OQL.UI;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using OneCSharp.Metadata;
    using OneCSharp.OQL.UI.Services;
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("0a01d6af-6211-40c0-ab52-9d433d3512b1")]
    public class OneCSharpToolWindow : ToolWindowPane//, IVsWindowFrameNotify2
    {
        private const string TOOL_WINDOW_CAPTION = "ONE-C-SHARP CODE EDITOR";
        
        private IOneCSharpCodeEditor _codeEditor;
        public OneCSharpToolWindow() : base(null)
        {
            this.Caption = TOOL_WINDOW_CAPTION;
            this.Content = new TextBlock() { Text = TOOL_WINDOW_CAPTION }; //new ProcedureView(new ProcedureViewModel(new Procedure()));
        }
        public void EditSyntaxNode(IOneCSharpCodeEditorConsumer consumer, ISyntaxNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (consumer == null) throw new ArgumentNullException(nameof(consumer));

            //(OneCSharpCodeProvider)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SOneCSharpCodeProvider));
            OneCSharpCodeProvider codeProvider = (OneCSharpCodeProvider)this.GetService(typeof(SOneCSharpCodeProvider));
            if (codeProvider == null)
            {
                _ = MessageBox.Show(
                "Failed to get OneCSharp code provider!",
                TOOL_WINDOW_CAPTION,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
                return;
            }

            _codeEditor = codeProvider.GetCodeEditor(node);
            _codeEditor.Save += consumer.SaveSyntaxNode;
            this.Content = codeProvider.GetCodeEditorView(_codeEditor);
        }
        //public int OnClose(ref uint pgrfSaveOptions)
        //{
        //    if (_codeEditor == null) return VSConstants.S_OK;

        //    if (!_codeEditor.IsModified)
        //    {
        //        _codeEditor = null;
        //        return VSConstants.S_OK;
        //    }

        //    MessageBoxResult result = MessageBox.Show(
        //        "Code has been modified. Do you want to save changes ?",
        //        TOOL_WINDOW_CAPTION,
        //        MessageBoxButton.YesNo,
        //        MessageBoxImage.Warning);
            
        //    if (result == MessageBoxResult.Yes)
        //    {
        //        return VSConstants.E_ABORT;
        //    }
        //    _codeEditor = null;
        //    return VSConstants.S_OK;
        //}
    }
}