﻿using OneCSharp.AST.Model;
using OneCSharp.MVVM;
using System.Collections.ObjectModel;

namespace OneCSharp.AST.UI
{
    public sealed class KeywordNode : SyntaxNode
    {   
        private string _keyword = string.Empty;
        public KeywordNode(ISyntaxNode owner) : base(owner) { }
        public KeywordNode(ISyntaxNode owner, Concept model) : base(owner, model) { }
        public string Keyword
        {
            get { return _keyword; }
            set { _keyword = value; OnPropertyChanged(nameof(Keyword)); }
        }
        public ObservableCollection<MenuItemViewModel> ContextMenu { get; } = new ObservableCollection<MenuItemViewModel>();
    }
}