using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HierarchyPlus
{
    public interface ISetAligned
    {
        void SetAligned(bool aligned);
    }

    public class PrefabButton : FunctionButton, ISetAligned
    {
        public override float Width { get { return _Width; } }
        public override float Offset { get { return _Offset; } }

        private float _Offset;
        private float _Width;
        private bool _Aligned;

        public void SetAligned(bool aligned)
        {
            _Aligned = aligned;
        }

        public override float Layout(GameObject go, float offset, float expand = 0)
        {
            _Offset = offset;
            _Width = kButtonWidth;
            if (Prefs.fbPrefabHideDefault && PrefabUtility.GetPrefabType(go) == PrefabType.None)
                _Width = 0;
            return _Width;
        }

        public override float Draw(GameObject go, Rect itemRect)
        {
            if (_Width == 0) return 0;
            var rect = new Rect(itemRect);
            rect.x = rect.xMax - _Offset - _Width;
            rect.width = _Width;
            var tint = GUI.color;

            var menu = new GenericMenu();
            var pt = PrefabUtility.GetPrefabType(go);
            if (pt == PrefabType.None)
            {
                DrawPrefabGroupLastRow(rect, go);
                if (Prefs.fbPrefabHideGameObject)
                    return _Width;

                var icon = IconCache.GetIcon(typeof(GameObject));
                var oc = Utility.TempContent(tex: icon, tooltip: go.name);
                if (GUI.Button(rect, oc, Styles.iconOnly) && !EditorApplication.isPlaying)
                    CreatePrefab(go);
                return _Width;
            }

            var prefab = PrefabUtility.GetPrefabParent(go);
            var gc = Utility.TempContent();
            gc.text = string.Empty;
            if (prefab == null)
                gc.image = IconCache.GetIcon(PrefabUtility.GetPrefabObject(go));
            else
                gc.image = IconCache.GetIcon(AssetDatabase.GetAssetPath(prefab));
            gc.tooltip = AssetDatabase.GetAssetPath(prefab);

            if (pt == PrefabType.MissingPrefabInstance)
            {
                gc.tooltip = "Missing Prefab";
                tint = new Color(1, 0.4f, 0.4f, 1);
                menu.AddItem(new GUIContent("Reconnect Prefab"), false, () =>
                {
                    var path = EditorUtility.OpenFilePanel("Select Prefab", "Assets", "prefab");
                    if (path.StartsWith(Application.dataPath))
                        path = "Assets" + path.Substring(Application.dataPath.Length);
                    var pf = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (pf != null)
                        PrefabUtility.ConnectGameObjectToPrefab(go, pf);
                });
                menu.AddItem(new GUIContent("Disconnect Prefab"), false, () => PrefabUtility.DisconnectPrefabInstance(go));
            }
            else if (pt == PrefabType.DisconnectedPrefabInstance || pt == PrefabType.DisconnectedModelPrefabInstance)
            {
                gc.tooltip += "\n(Disconnected)";
                tint = new Color(0.8f, 0.6f, 0.4f, 0.6f);
                if (Event.current.type == EventType.Repaint)
                    GUI.DrawTexture(rect, gc.image);
                menu.AddItem(new GUIContent("Reconnect Prefab"), false, () => PrefabUtility.ReconnectToLastPrefab(go));
            }
            else if (!EditorApplication.isPlaying)
            {
                bool changed = false;
                prefab = PrefabUtility.GetPrefabParent(go);
                var rootprefab = PrefabUtility.GetPrefabParent(PrefabUtility.FindPrefabRoot(go));
                var childobj = go.GetChildrenList().Skip(1).Select(i => PrefabUtility.GetPrefabParent(i)).ToArray();
                var ignore = Prefs.fbPrefabIgnoreChanged.Split(',');

                string change = "";
                var childchanged = new HashSet<UnityEngine.Object>();

                var list = PrefabUtility.GetPropertyModifications(go);
                if (list != null && list.Length != 0)
                {
                    foreach (var pm in list)
                    {
                        if (ignore.Contains(pm.propertyPath)) continue;
                        if (pm.target == null) continue;

                        if (pm.target is GameObject)
                        {
                            var obj = pm.target as GameObject;
                            if (childobj.Contains(obj))
                                childchanged.Add(obj);
                            if (obj != prefab) continue;
                            change += pm.propertyPath + "\n";
                        }

                        if (pm.target is Component)
                        {
                            var comp = pm.target as Component;
                            if (childobj.Contains(comp.gameObject))
                                childchanged.Add(comp.gameObject);
                            if (comp.gameObject != prefab) continue;
                            if (comp.gameObject == rootprefab)
                            {
                                if (pm.propertyPath.StartsWith("m_LocalPosition")) continue;
                                if (pm.propertyPath.StartsWith("m_LocalRotation")) continue;
                                if (pm.propertyPath.StartsWith("m_LocalEulerAnglesHint")) continue;
                            }
                            change += pm.propertyPath + "\n";
                        }
                        changed = true;
                    }
                }
                if (changed)
                {
                    gc.tooltip = (gc.tooltip + "\n(Changed)\n" + change).TrimEnd();

                    tint = new Color(1f, 0.4f, 0.8f, 1);
                    if (PrefabUtility.GetPrefabType(go) != PrefabType.ModelPrefabInstance)
                    {
                        menu.AddItem(new GUIContent("Apply Prefab"), false, () =>
                        {
                            var root = PrefabUtility.FindPrefabRoot(go);
                            PrefabUtility.ReplacePrefab(root, PrefabUtility.GetPrefabParent(root), ReplacePrefabOptions.ConnectToPrefab);
                        });
                    }
                    menu.AddItem(new GUIContent("Revert Prefab"), false, () => PrefabUtility.RevertPrefabInstance(go));
                    menu.AddItem(new GUIContent("Disconnect Prefab"), false, () => PrefabUtility.DisconnectPrefabInstance(go));

                }
                if (!changed)
                {
                    if (childchanged.Any())
                    {
                        gc.tooltip += "\n(" + childchanged.Count + " Children Changed)";
                        if (Event.current.type == EventType.Repaint)
                            GUIHelper.DrawHalfFadeIcon(rect, gc.image, Color.white, new Color(1f, 0.4f, 0.8f, 1));
                        gc.image = null;
                    }
                    menu.AddItem(new GUIContent("Disconnect Prefab"), false, () => PrefabUtility.DisconnectPrefabInstance(go));
                    var e = go.GetChildrenList().Where(i => childchanged.Contains(PrefabUtility.GetPrefabParent(i)));
                    if (e.Any()) menu.AddSeparator("");
                    foreach (var g in e)
                    {
                        var gameobject = g;
                        menu.AddItem(new GUIContent(g.name), false, () => Selection.activeGameObject = gameobject);
                    }
                }
            }

            DrawPrefabGroup(rect, go, pt);

            using (new GUIColorTint(tint))
                if (GUI.Button(rect, gc, Styles.iconOnly) && !EditorApplication.isPlaying)
                    menu.DropDown(rect);

            return _Width;
        }

        public static void CreatePrefab(GameObject go)
        {
            var type = typeof(ProjectWindowUtil);
            var method = type.GetMethod("GetActiveFolderPath", BindingFlags.NonPublic | BindingFlags.Static);
            var path = (string)method.Invoke(type, new object[] { });
            path = AssetDatabase.GenerateUniqueAssetPath(path + "/" + go.name + ".prefab");
            var o = PrefabUtility.CreatePrefab(path, go, ReplacePrefabOptions.ConnectToPrefab);
            ProjectWindowUtil.ShowCreatedAsset(o);
        }

        private static Dictionary<GameObject, float> GroupParent = new Dictionary<GameObject, float>();
        private static HashSet<GameObject> GroupLast = new HashSet<GameObject>();

        private void DrawPrefabGroup(Rect rect, GameObject go, PrefabType pt)
        {
            if (!_Aligned) return;
            if (Prefs.fbPrefabHideDefault) return;
            if (!Prefs.fbPrefabGroupOutline) return;
            if (TreeViewInject.HasSearchFilter()) return;
            if (pt == PrefabType.None) return;

            var disconnect = (pt == PrefabType.DisconnectedPrefabInstance || pt == PrefabType.DisconnectedModelPrefabInstance);
            var parent = disconnect ? PrefabUtility.FindRootGameObjectWithSameParentPrefab(go) : PrefabUtility.FindPrefabRoot(go);
            if (parent == null) return;
            var row = TreeViewInject.GetRow(parent);
            if (row >= 0 && row < TreeViewInject.GetRowCount() && !GroupParent.ContainsKey(parent))
            {
                var row2 = TreeViewInject.GetLastChildRowUnder(row);
                var last = parent;
                for (int i = row; i <= row2; i++)
                {
                    var obj = TreeViewInject.GetGameObject(i);
                    if (obj == null) continue;
                    var prefab = disconnect ? PrefabUtility.FindRootGameObjectWithSameParentPrefab(obj) : PrefabUtility.FindPrefabRoot(obj);
                    if (prefab != null && prefab == parent)
                        last = obj;
                }
                if (Prefs.fbPrefabGroupOutlineSingle || parent != last)
                {
                    GroupParent[parent] = rect.yMin;
                    GroupLast.Add(last);
                }
            }

            if (GroupLast.Contains(go))
                DrawGroupOverlay(rect, parent);
            GroupLast.Remove(go);
            DrawPrefabGroupLastRow(rect, go);
        }

        private void DrawPrefabGroupLastRow(Rect rect, GameObject go)
        {
            if (!_Aligned) return;
            if (Prefs.fbPrefabHideDefault) return;
            if (!Prefs.fbPrefabGroupOutline) return;
            if (TreeViewInject.HasSearchFilter()) return;

            var row1 = TreeViewInject.GetRow(go);
            var row2 = TreeViewInject.GetLastRowVisible();
            float drawheight = GUIUtility.GUIToScreenPoint(rect.position).y + 16;
            rect.yMax += 18;
            //if (drawheight >= HierarchyDrawer.hierarchyWindow.position.yMax || row1 >= row2)
            if (drawheight >= HierarchyDrawer.hierarchyWindow.position.yMax)
            {
                foreach (var kv in GroupParent.ToList())
                    DrawGroupOverlay(rect, kv.Key);
                GroupLast.Clear();
            }
            if (row1 >= row2)
            {
                GroupParent.Clear();
                GroupLast.Clear();
            }
        }

        private void DrawGroupOverlay(Rect rect, GameObject go)
        {
            var ro = new RectOffset(1, 1, 1, 1);
            rect.yMin = GroupParent[go];
            //int row = TreeViewInject.GetRow(go);
            //if (row < 0) row = 0;
            //using (new GUIColorTint(Styles.EnhanceColor(Styles.GetCycleColor(row, 1f))))
            GUI.Label(ro.Remove(rect), GUIContent.none, "TL SelectionButton PreDropGlow");
            GroupParent.Remove(go);
        }
    }
}