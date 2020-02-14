﻿using OneCSharp.MVVM;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace OneCSharp.AST.UI
{
    public sealed class KeywordNodeViewModel : SyntaxNodeViewModel
    {
        private string _keyword = string.Empty;
        private bool _isContextMenuEnabled = false;
        public KeywordNodeViewModel(ISyntaxNodeViewModel owner) : base(owner) { }
        public string Keyword
        {
            get { return _keyword; }
            set { _keyword = value; OnPropertyChanged(nameof(Keyword)); }
        }
        public bool IsContextMenuEnabled
        {
            get { return _isContextMenuEnabled; }
            set { _isContextMenuEnabled = value; OnPropertyChanged(nameof(IsContextMenuEnabled)); }
        }
        public ObservableCollection<MenuItemViewModel> ContextMenu { get; } = new ObservableCollection<MenuItemViewModel>();
    }
}