using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Yorozu.EditorTool
{
    internal class DependencyTreeView : TreeView
    {
        private DependencyMapWindow _window;
        
        public DependencyTreeView(DependencyMapWindow window, TreeViewState state) : base(state)
        {
            _window = window;
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(0, -1, "Root");
            var count = 0;
            foreach (var data in _window.DependencyData)
            {
                var asset = AssetDatabase.LoadAssetAtPath<Object>(data.Path);
                var item = new TreeViewItem(asset.GetInstanceID())
                {
                    displayName = data.Path,
                    icon = AssetDatabase.GetCachedIcon(data.Path) as Texture2D
                };
                foreach (var reference in data.References)
                {
                    // 自分への参照なら無視
                    if (reference == data.Path)
                        continue;
                            
                    var child = new TreeViewItem(++count)
                    {
                        displayName = reference,
                        icon = AssetDatabase.GetCachedIcon(reference) as Texture2D
                    };
                    item.AddChild(child);
                }
                if (item.hasChildren)
                    root.AddChild(item);
            }
            
            SetupDepthsFromParentsAndChildren(root);
            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            // 空対応
            if (!root.hasChildren) 
                root.AddChild(new TreeViewItem(1, 0));

            return base.BuildRows(root);
        }

        protected override void DoubleClickedItem(int id)
        {
            var item = FindItem(id, rootItem);

            var obj = AssetDatabase.LoadAssetAtPath<Object>(item.displayName);
            EditorGUIUtility.PingObject(obj);
        }
    }
}