﻿using OneCSharp.AST.Model;
using OneCSharp.MVVM;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Input;

namespace OneCSharp.AST.UI
{
    public interface ISyntaxNodeViewModel
    {
        bool IsVisible { get; set; }
        bool IsTemporallyVisible { get; set; }
        ISyntaxNode Model { get; set; }
        string PropertyBinding { get; set; }
        void Add(ISyntaxNodeViewModel child);
        ISyntaxNodeViewModel Owner { get; set; }
        ObservableCollection<ICodeLineViewModel> Lines { get; }
        bool IsFocused { get; set; }
        bool IsMouseOver { get; set; }
        ICommand KeyDownCommand { get; set; }
        ICommand MouseDownCommand { get; set; }
        ICommand MouseEnterCommand { get; set; }
        ICommand MouseLeaveCommand { get; set; }
        ICommand CtrlCCommand { get; set; }
        ICommand CtrlVCommand { get; set; }
    }
    public abstract class SyntaxNodeViewModel : ISyntaxNodeViewModel, INotifyPropertyChanged
    {
        private bool _isFocused = false;
        private bool _isMouseOver = false;
        private string _propertyBinding = null;
        public event PropertyChangedEventHandler PropertyChanged;
        public SyntaxNodeViewModel()
        {
            KeyDownCommand = new RelayCommand(OnKeyDown);
            MouseDownCommand = new RelayCommand(OnMouseDown);
            MouseEnterCommand = new RelayCommand(OnMouseEnter);
            MouseLeaveCommand = new RelayCommand(OnMouseLeave);
            CtrlCCommand = new RelayCommand(OnCtrlC);
            CtrlVCommand = new RelayCommand(OnCtrlV);
        }
        public SyntaxNodeViewModel(ISyntaxNodeViewModel owner) : this() { Owner = owner; }
        public SyntaxNodeViewModel(ISyntaxNodeViewModel owner, ISyntaxNode model) : this(owner) { Model = model; }
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public ISyntaxNode Model { get; set; }
        public string PropertyBinding
        {
            get { return _propertyBinding; }
            set { _propertyBinding = value; SetVisibility(); }
        }
        public ISyntaxNodeViewModel Owner { get; set; }
        public virtual void Add(ISyntaxNodeViewModel child) { }
        public ObservableCollection<ICodeLineViewModel> Lines { get; } = new ObservableCollection<ICodeLineViewModel>();
        

        
        public bool IsFocused
        {
            get { return _isFocused; }
            set { _isFocused = value; OnPropertyChanged(nameof(IsFocused)); }
        }
        public bool IsMouseOver
        {
            get { return _isMouseOver; }
            set { _isMouseOver = value; OnPropertyChanged(nameof(IsMouseOver)); }
        }
        public ICommand KeyDownCommand { get; set; }
        public ICommand MouseDownCommand { get; set; }
        public ICommand MouseEnterCommand { get; set; }
        public ICommand MouseLeaveCommand { get; set; }
        public ICommand CtrlCCommand { get; set; }
        public ICommand CtrlVCommand { get; set; }

        protected virtual void OnMouseEnter(object parameter)
        {
            IsMouseOver = true;
            if (!(parameter is MouseEventArgs args)) { return; }
            ConceptNodeViewModel concept = this.Ancestor<ConceptNodeViewModel>() as ConceptNodeViewModel;
            if (concept != null)
            {
                concept.ShowOptions();
            }
            //if (IsFocused) return;
            //IsBorderVisible = true;
            //BorderBrush = Brushes.Black;
        }
        protected virtual void OnMouseLeave(object parameter)
        {
            IsMouseOver = false;
            if (!(parameter is MouseEventArgs)) { return; }
            ConceptNodeViewModel concept = this.Ancestor<ConceptNodeViewModel>() as ConceptNodeViewModel;
            if (concept != null)
            {
                concept.HideOptions();
            }
            //if (!IsFocused)
            //{
            //    //IsBorderVisible = false;
            //    //BorderBrush = Brushes.White;
            //}
        }
        protected virtual void OnMouseDown(object parameter)
        {
            IsFocused = true;
            //IsBorderVisible = true;
            //BorderBrush = Brushes.Black;
            //FocusManager.SetFocus(this);
        }
        protected virtual void OnKeyDown(object parameter)
        {
            if (!(parameter is KeyEventArgs args)) return;
            
            if (args.Key == Key.Enter)
            {
                BreakLine(this);
                args.Handled = true;
            }
            else if (args.Key == Key.Back)
            {
                RestoreLine(this);
                args.Handled = true;
            }
            else if (args.Key == Key.Left)
            {
                FocusLeft(this);
                args.Handled = true;
            }
            else if (args.Key == Key.Right)
            {
                FocusRight(this);
                args.Handled = true;
            }
            else if (args.Key == Key.Tab)
            {
                args.Handled = true;
            }
            //MessageBox.Show($"{Keyword}: {args.Key}");
        }
        protected virtual void OnCtrlC(object parameter)
        {

        }
        protected virtual void OnCtrlV(object parameter)
        {

        }


        public void BreakLine(ISyntaxNodeViewModel node)
        {
            for (int current = 0; current < Lines.Count; current++)
            {
                ICodeLineViewModel line = Lines[current];
                int position = line.Nodes.IndexOf(node);
                if (position == -1 || position == 0) continue; // position == 0 means no empty line allowed
                if (line.Nodes.Count == 1) return; // no empty line allowed

                ICodeLineViewModel newLine = new CodeLineViewModel(this);
                while (position != line.Nodes.Count)
                {
                    newLine.Nodes.Add(line.Nodes[position]);
                    line.Nodes.RemoveAt(position);
                }
                Lines.Insert(++current, newLine);
            }
            //if (node is KeywordViewModel)
            //{
            //    FocusManager.SetFocus((KeywordViewModel)node);
            //}
        }
        public void RestoreLine(ISyntaxNodeViewModel node)
        {
            if (Lines.Count == 0 || Lines.Count == 1) return;

            for (int current = 1; current < Lines.Count; current++)
            {
                ICodeLineViewModel line = Lines[current];
                int position = line.Nodes.IndexOf(node);
                if (position != 0) continue; // only first item can restore line

                ICodeLineViewModel restoringLine = Lines[--current];
                while (position != line.Nodes.Count)
                {
                    restoringLine.Nodes.Add(line.Nodes[position]);
                    line.Nodes.RemoveAt(position);
                }
                Lines.RemoveAt(++current);
            }
            //if (item is KeywordViewModel)
            //{
            //    FocusManager.SetFocus((KeywordViewModel)item);
            //}
        }
        public void FocusLeft(ISyntaxNodeViewModel node)
        {
            for (int current = 0; current < Lines.Count; current++)
            {
                ICodeLineViewModel line = Lines[current];
                int position = line.Nodes.IndexOf(node);
                if (position == -1) continue;
                if (position == 0) return;

                //if (line.Nodes[position - 1] is KeywordViewModel)
                //{
                //    FocusManager.SetFocus((KeywordViewModel)line.Nodes[position - 1]);
                //}
            }
        }
        public void FocusRight(ISyntaxNodeViewModel node)
        {
            for (int current = 0; current < Lines.Count; current++)
            {
                ICodeLineViewModel line = Lines[current];
                int position = line.Nodes.IndexOf(node);
                if (position == -1) continue;
                if (position == line.Nodes.Count - 1) return;

                //if (line.Nodes[position + 1] is KeywordViewModel)
                //{
                //    FocusManager.SetFocus((KeywordViewModel)line.Nodes[position + 1]);
                //}
            }
        }



        private bool _isVisible = true;
        private bool _isTemporallyVisible = false;
        public bool IsVisible
        {
            get { return _isVisible; }
            set { _isVisible = value; OnPropertyChanged(nameof(IsVisible)); }
        }
        private void SetVisibility()
        {
            if (string.IsNullOrWhiteSpace(PropertyBinding)) return;

            Type metadata = Owner.Model.GetType();
            PropertyInfo property = metadata.GetProperty(PropertyBinding);
            if (property == null) return;

            if (property.IsOptional())
            {
                IOptional optional = (IOptional)property.GetValue(Owner.Model);
                IsVisible = optional.HasValue;
                _isTemporallyVisible = false;
            }
            else
            {
                IsVisible = true;
                _isTemporallyVisible = false;
            }
        }
        public virtual bool IsTemporallyVisible
        {
            get { return _isTemporallyVisible; }
            set
            {
                if (IsVisible && !_isTemporallyVisible) { return; }
                _isTemporallyVisible = value;
                IsVisible = _isTemporallyVisible;
                OnPropertyChanged(nameof(IsTemporallyVisible));
            }
        }
    }
}