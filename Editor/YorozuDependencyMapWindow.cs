using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yorozu.EditorTool
{
    /// <summary>
    /// 指定したアセット一覧から、依存関係のリストを作成
    /// バンドルで複数のアセットがまとまってる際に依存確認するのに便利
    /// </summary>
    internal class YorozuDependencyMapWindow : EditorWindow
    {
        [MenuItem("Tools/Yorozu/DependencyMap")]
        private static void ShowWindow()
        {
            var window = GetWindow<YorozuDependencyMapWindow>();
            window.titleContent = new GUIContent("DependencyMap");
            window.Show();
        }

        [Serializable]
        internal class Data
        {
            [SerializeField]
            internal string Path;
            /// <summary>
            /// パスの参照があるやつ
            /// </summary>
            [SerializeField]
            internal List<string> References;
        }

        [SerializeField]
        private List<Data> _dependencyMap = new List<Data>();
        internal IEnumerable<Data> DependencyData => _dependencyMap;

        [SerializeField]
        private TreeViewState _state;
        private DependencyTreeView _treeView;

        private void Init()
        {
            if (_state == null)
            {
                _state = new TreeViewState();
            }
            if (_treeView == null)
            {
                _treeView = new DependencyTreeView(this, _state);
            }
        }

        private void OnGUI()
        {
            Init();
            
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Find Dependency from Selection.assetGUIDs", EditorStyles.toolbarButton))
                {
                    var guids = Selection.assetGUIDs;
                    var paths = GetPaths(guids);
                    _dependencyMap = FindDependencies(paths);
                    _treeView.Reload();
                }
            }
            
            var rect = GUILayoutUtility.GetRect(0, 0, position.width, position.height);
            _treeView.OnGUI(rect);
        }

        private List<Data> FindDependencies(IEnumerable<string> paths)
        {
            _dependencyMap = new List<Data>();
            foreach (var path in paths)
            {
                var dependencies = AssetDatabase.GetDependencies(path);
                foreach (var dependency in dependencies)
                {
                    var index = _dependencyMap.FindIndex(d => d.Path == dependency);
                    if (index >= 0)
                    {
                        _dependencyMap[index].References.Add(path);
                    }
                    else
                    {
                        var data = new Data()
                        {
                            Path = dependency,
                            References = new List<string>() {path}
                        };
                        _dependencyMap.Add(data);
                    }
                }
            }

            return _dependencyMap.OrderBy(d => d.Path).ToList();
        }

        /// <summary>
        /// GUIDから全パスを取得
        /// </summary>
        private IEnumerable<string> GetPaths(string[] guids)
        {
            var paths = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .SelectMany(p =>
                {
                    if (!AssetDatabase.IsValidFolder(p))
                        return new[] {p};

                    // ディレクトリだったら中を取得
                    return Directory.GetFiles(p, "*", SearchOption.AllDirectories)
                            .Where(p => !p.EndsWith(".meta"))
                        ;
                })
                .Distinct();

            return paths;
        }
    }

    internal class DependencyTreeView : TreeView
    {
        private YorozuDependencyMapWindow _window;
        
        public DependencyTreeView(YorozuDependencyMapWindow window, TreeViewState state) : base(state)
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

        protected override void DoubleClickedItem(int id)
        {
            var item = FindItem(id, rootItem);

            var obj = AssetDatabase.LoadAssetAtPath<Object>(item.displayName);
            EditorGUIUtility.PingObject(obj);
        }
    }
}