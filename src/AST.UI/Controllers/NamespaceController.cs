﻿using OneCSharp.AST.Model;
using OneCSharp.Core.Model;
using OneCSharp.MVVM;
using System;
using System.Windows.Media.Imaging;

namespace OneCSharp.AST.UI
{
    public sealed class NamespaceController : IController
    {
        private readonly IModule _module;
        public NamespaceController(IModule module)
        {
            _module = module;
        }
        public void BuildTreeNode(Entity model, out TreeNodeViewModel treeNode)
        {
            treeNode = new TreeNodeViewModel()
            {
                NodeText = model.Name,
                NodePayload = model,
                NodeIcon = new BitmapImage(new Uri(Module.NAMESPACE_PUBLIC))
            };
            BuildContextMenu(treeNode);
        }
        public void BuildContextMenu(TreeNodeViewModel treeNode)
        {
            treeNode.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Add namespace...",
                MenuItemPayload = treeNode,
                MenuItemCommand = new RelayCommand(AddNamespace),
                MenuItemIcon = new BitmapImage(new Uri(Module.ADD_NAMESPACE)),
            });
            treeNode.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Add concept...",
                MenuItemPayload = treeNode,
                MenuItemCommand = new RelayCommand(AddConcept),
                MenuItemIcon = new BitmapImage(new Uri(Module.ADD_VARIABLE)),
            });
        }

        private void AddNamespace(object parameter)
        {
            InputStringDialog dialog = new InputStringDialog();
            _ = dialog.ShowDialog();
            if (dialog.Result == null) { return; }

            TreeNodeViewModel treeNode = (TreeNodeViewModel)parameter;

            Namespace child = new Namespace()
            {
                Name = (string)dialog.Result
            };
            if (treeNode.NodePayload is Language language)
            {
                child.Owner = language;
                language.Namespaces.Add(child);
            }
            else if (treeNode.NodePayload is Namespace parent)
            {
                child.Owner = parent;
                parent.Namespaces.Add(child);
            }

            IController controller = _module.GetController<Namespace>();
            controller.BuildTreeNode(child, out TreeNodeViewModel childNode);

            _module.Persist(child.Owner);
            treeNode.TreeNodes.Add(childNode);
        }

        private void AddConcept(object parameter)
        {
            InputStringDialog dialog = new InputStringDialog();
            _ = dialog.ShowDialog();
            if (dialog.Result == null) { return; }

            TreeNodeViewModel treeNode = (TreeNodeViewModel)parameter;

            Concept child = new Concept()
            {
                Name = (string)dialog.Result
            };
            if (treeNode.NodePayload is Namespace parent)
            {
                child.Owner = parent;
                parent.DataTypes.Add(child);
            }

            IController controller = _module.GetController<Concept>();
            controller.BuildTreeNode(child, out TreeNodeViewModel childNode);
            
            _module.Persist(child);
            treeNode.TreeNodes.Add(childNode);
        }
    }
}