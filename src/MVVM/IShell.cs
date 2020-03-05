﻿namespace OneCSharp.MVVM
{
    public interface IShell
    {
        string AppCatalogPath { get; }
        string ModulesCatalogPath { get; }
        IService GetService<IService>();
        void AddMenuItem(MenuItemViewModel menuItem);
        void AddTreeNode(TreeNodeViewModel treeNode);
        void AddTabItem(string header, object content);
        void RemoveTabItem(TabViewModel tab);
        void ShowStatusBarMessage(string message);
    }
}