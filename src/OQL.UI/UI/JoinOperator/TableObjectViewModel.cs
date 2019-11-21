﻿using OneCSharp.OQL.Model;

namespace OneCSharp.OQL.UI
{
    public sealed class TableObjectViewModel : SyntaxNodeViewModel
    {
        public TableObjectViewModel(ISyntaxNodeViewModel parent, TableObject model) : base(parent, model)
        {
            InitializeViewModel();
        }
        public override void InitializeViewModel() { }
        public string FullName { get { return ((TableObject)Model).FullName; } }
    }
}
