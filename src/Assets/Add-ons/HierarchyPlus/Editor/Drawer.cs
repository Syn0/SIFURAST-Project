using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;

namespace HierarchyPlus
{
    [InitializeOnLoad]
    public static class HierarchyDrawer
    {
        private const int kBaseOffset = 16;

        public static EditorWindow hierarchyWindow { get { return TreeViewInject.hierarchyWindow; } }
        public static Action handleCommandEvent;

        private static readonly Type kHierarchyWindowType;
        private static readonly Color kPlayModeTint;

        private static bool s_WindowHovered;
        private static bool s_WindowFocused;
        private static bool s_ItemHovered;
        private static bool s_SeparatorHovered;
        private static GameObject s_LastSelectionBase;
        private static GameObject s_LastActiveGameObject;
        private static int s_LastSelectionCount;
        private static Rect s_LastHoveredRect = new Rect();
        private static GameObject s_HierarchBaseGameObject;

        private static Rect s_ItemRect;
        private static float s_ItemMaxWidth;
        private static GameObject s_GameObject;

        private static Dictionary<FunctionType, FunctionButton> s_FunctionTable;
        private static Dictionary<int, string> s_PersistentTable = new Dictionary<int, string>();

        static HierarchyDrawer()
        {
            var assembly = typeof(EditorWindow).Assembly;
            kHierarchyWindowType = assembly.GetType("UnityEditor.SceneHierarchyWindow");
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
#if UNITY_2018_1_OR_NEWER
            EditorApplication.hierarchyChanged += HierarchyWindowChanged;
#else
            EditorApplication.hierarchyWindowChanged += HierarchyWindowChanged;
#endif
            EditorApplication.update += EditorUpdate;
            EditorApplication.modifierKeysChanged += EditorModifierKeysChanged;
#if UNITY_2017_2_OR_NEWER
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
#else
            EditorApplication.playmodeStateChanged += PlaymodeStateChanged;
#endif
            SetupFunctionTable();

            var binding = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
            var prefcolor = assembly.GetType("UnityEditor.HostView").GetField("kPlayModeDarken", binding).GetValue(null);
            kPlayModeTint = (Color)prefcolor.GetType().GetProperty("Color", binding).GetValue(prefcolor, null);
        }

        private static void EditorUpdate()
        {
            Profiler.BeginSample("HierarchyPlus.EditorUpdate");
            if (!Prefs.HierarchyItemDrawer) return;

            var currentWindow = EditorWindow.focusedWindow;
            s_WindowFocused = currentWindow != null && currentWindow.GetType() == kHierarchyWindowType;

            currentWindow = EditorWindow.mouseOverWindow;
            var hovered = currentWindow != null && currentWindow.GetType() == kHierarchyWindowType;
            if (!hovered)
            {
                s_LastHoveredRect = new Rect();
                if (s_WindowHovered) EditorApplication.RepaintHierarchyWindow();
            }
            s_WindowHovered = hovered;

            if (s_HierarchBaseGameObject != null)
            {
                TreeViewInject.SetExpandedRecursive(s_HierarchBaseGameObject, false);
                s_HierarchBaseGameObject = null;
                EditorApplication.RepaintHierarchyWindow();
            }

            CheckSelection();
            Profiler.EndSample();
        }

        private static bool SelectionChanged()
        {
            if (s_LastActiveGameObject != Selection.activeGameObject || s_LastSelectionCount != Selection.gameObjects.Length)
            {
                s_LastActiveGameObject = Selection.activeGameObject;
                s_LastSelectionCount = Selection.gameObjects.Length;
                return true;
            }
            return false;
        }

        private static void CheckSelection()
        {
            var selection = Selection.objects;
            if (s_WindowFocused)
            {
                s_LastActiveGameObject = Selection.activeGameObject;
                s_LastSelectionCount = Selection.gameObjects.Length;
                return;
            }

            if (!SelectionChanged()) return;
            var objects = Enumerable.Empty<UnityEngine.Object>();
            var scenes = selection.Where(i => i is GameObject).Cast<GameObject>().Where(i => i.scene.isLoaded).Select(i => i.scene).Distinct();
            objects = objects.Concat(DataObject.GetHierarchyAllItems(scenes.ToArray()).Where(i => !i.selectable).Select(i => (UnityEngine.Object)i.gameObject));
            Selection.objects = selection.Except(objects).ToArray();

            var go = Selection.activeGameObject;
            if (go != null && go.scene.isLoaded && go != s_LastSelectionBase)
            {
                while (go != null)
                {
                    if (DataObject.GetHierarchyItem(go).selectionBase && DataObject.GetHierarchyItem(go).selectable)
                    {
                        Selection.activeGameObject = go;
                        s_LastSelectionBase = go;
                        break;
                    }
                    go = go.transform.parent != null ? go.transform.parent.gameObject : null;
                }
            }

            go = Selection.activeGameObject;
            if (go != null && go.scene.isLoaded)
            {
                while (go != null)
                {
                    if (DataObject.GetHierarchyItem(go).hierarchyBase)
                    {
                        s_HierarchBaseGameObject = go;
                        break;
                    }
                    go = go.transform.parent != null ? go.transform.parent.gameObject : null;
                }
            }
        }

        private static void EditorModifierKeysChanged()
        {
            if (!Prefs.HierarchyItemDrawer) return;
            bool repaint = false;
            if ((ModifierKey)Prefs.hChildrenCountModifier != ModifierKey.None) repaint = true;
            if ((ModifierKey)Prefs.hBackToParentModifier != ModifierKey.None) repaint = true;
            if ((ModifierKey)Prefs.hShadowLabelModifier != ModifierKey.None) repaint = true;
            if ((ModifierKey)Prefs.fbTagLayerInvert != ModifierKey.None) repaint = true;
            if ((ModifierKey)Prefs.fbTagLayer2Invert != ModifierKey.None) repaint = true;
            if (repaint) EditorApplication.RepaintHierarchyWindow();
        }

#if UNITY_2017_2_OR_NEWER
        private static void PlayModeStateChanged(PlayModeStateChange state)
        {
            PlaymodeStateChanged();
        }
#endif

        private static void PlaymodeStateChanged()
        {
            if (EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                var e = DataObject.GetHierarchyAllItems().Where(i => i.persistentData).SelectMany(i => i.gameObject.GetComponents<Component>());
                s_PersistentTable = e.Where(i => i != null).ToDictionary(k => k.GetInstanceID(), v => EditorJsonUtility.ToJson(v));
            }
            else if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                foreach (var item in s_PersistentTable)
                {
                    var obj = EditorUtility.InstanceIDToObject(item.Key);
                    if (obj != null)
                        EditorJsonUtility.FromJsonOverwrite(item.Value, obj);
                }
                if (s_PersistentTable.Any())
                    InternalEditorUtility.RepaintAllViews();
                s_PersistentTable.Clear();
            }
        }

        private static void HierarchyWindowChanged()
        {
            if (!Prefs.HierarchyItemDrawer) return;
            //Debug.Log("HierarchyWindowChanged");
        }

        private static void SetupFunctionTable()
        {
            s_FunctionTable = Enumerable.Range(0, Enum.GetNames(typeof(FunctionType)).Length).Cast<FunctionType>().ToDictionary(i => i, i => i.GetFunction());
        }

        private static void CheckHierarchyWindow()
        {
            var window = TreeViewInject.LastInteractedHierarchyWindow();
            if (window != null)
            {
                if (!window.wantsMouseMove)
                    window.wantsMouseMove = true;
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    window.autoRepaintOnSceneChange = false;
                else if (!window.autoRepaintOnSceneChange)
                    window.autoRepaintOnSceneChange = true;
            }
        }

        private static void HierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            TreeViewInject.Init();

            var evt = Event.current;
            if (evt.Equals(Event.KeyboardEvent("\\")))
            {
                evt.Use();
                Prefs.HierarchyItemDrawer = !Prefs.HierarchyItemDrawer;
            }
            if (!Prefs.HierarchyItemDrawer && !Prefs.HierarchyTopbar)
                return;

            CheckHierarchyWindow();
            s_GameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            //EditorGUIUtility.SetIconSize(new Vector2(16, 16));

            s_ItemRect = new Rect(selectionRect);
            var rect = new Rect(s_ItemRect);
            rect.xMin = 0;
            s_ItemHovered = rect.Contains(evt.mousePosition);

            switch (evt.type)
            {
                case EventType.ExecuteCommand:
                    if (handleCommandEvent != null)
                    {
                        handleCommandEvent.Invoke();
                        handleCommandEvent = null;
                    }
                    break;
                case EventType.Layout:
                    return;
                case EventType.MouseDrag:
                case EventType.DragUpdated:
                case EventType.DragPerform:
                case EventType.DragExited:
                case EventType.MouseMove:
                    if (!Prefs.HierarchyItemDrawer)
                        return;
                    if (Prefs.hBackToParent)
                    {
                        if (rect.Contains(evt.mousePosition) && rect != s_LastHoveredRect)
                        {
                            s_LastHoveredRect = rect;
                            EditorApplication.RepaintHierarchyWindow();
                        }
                        if (!rect.Contains(evt.mousePosition) && rect == s_LastHoveredRect)
                        {
                            s_LastHoveredRect = new Rect();
                            EditorApplication.RepaintHierarchyWindow();
                        }
                    }
                    if (Prefs.GetSeparatorList().Any())
                    {
                        if (s_SeparatorHovered != IsMouseInSeparator(evt.mousePosition))
                            EditorApplication.RepaintHierarchyWindow();
                        s_SeparatorHovered = IsMouseInSeparator(evt.mousePosition);
                    }
                    return;
                case EventType.KeyDown:
                    if (evt.keyCode == (KeyCode)Prefs.hCustomizeKey)
                    {
                        evt.Use();
                        GameObject[] list = Selection.gameObjects.Where(o => o.scene.isLoaded).ToArray();
                        if (list.Length == 0 && rect.Contains(evt.mousePosition))
                            list = new[] { s_GameObject };
                        if (list.Length > 0)
                        {
                            var popupRect = new Rect();
                            popupRect.position = evt.mousePosition;
                            handleCommandEvent = () =>
                            {
                                if (Event.current.commandName.Equals("QuickCustomize"))
                                {
                                    Event.current.Use();
                                    QuickCustomize.Show(popupRect, list);
                                }
                            };
                            hierarchyWindow.SendEvent(EditorGUIUtility.CommandEvent("QuickCustomize"));
                            Selection.objects = new UnityEngine.Object[] { };
                        }
                    }
                    break;
            }

            Profiler.BeginSample("HierarchyPlus.HierarchyWindowItemOnGUI");
            try
            {
                if (Prefs.HierarchyItemDrawer && s_GameObject != null)
                {
                    DrawBackground();
                    if (Prefs.hCustomize) DrawHighlight();
                    DrawChildCount();
                    if (Prefs.hDrawTree) DrawTree();
                    DrawHierarchyLevel();
                    DrawShadowLabel();
                    DrawParentButton();
                    if (Prefs.FunctionButton) DrawItemMask();
                    DrawSeparator();
                    DrawActiveButton();
                    if (Prefs.FunctionButton) DrawFunctionList();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            Profiler.EndSample();
        }

        private static bool IsMouseInSeparator(Vector2 pos)
        {
            float offset = 0;
            foreach (var item in Prefs.GetSeparatorList())
            {
                offset += item.Offset;
                var x2 = s_ItemRect.xMax - offset;
                offset += item.Show ? Separator.kWidth : 0;
                var x1 = s_ItemRect.xMax - offset;
                if (item.Movable && pos.x >= x1 && pos.x <= x2)
                    return true;
            }
            return false;
        }

        private static List<float> GetSeparatorOffsets()
        {
            float offset = 0;
            var offsets = new List<float>();
            foreach (var item in Prefs.GetSeparatorList())
            {
                offset += item.Offset;
                offsets.Add(offset);
                offset += item.Show ? Separator.kWidth : 0;
            }
            return offsets;
        }

        private static void DrawHighlight()
        {
            var data = DataObject.GetHierarchyItem(s_GameObject).customData;
            if (!data.enabled) return;
            if (TreeViewInject.IsRenaming(s_GameObject)) return;

            var label = new GUIStyle("PR Label");
            label.normal.textColor = data.textColor;
            label.padding.left = 0;
            if (data.bold) label.fontStyle = FontStyle.Bold;

            var rect = new Rect(s_ItemRect);
            rect.xMin = 0f;
            if (TreeViewInject.IsItemSelected(s_GameObject))
            {
                if (Event.current.type == EventType.Repaint)
                    label.Draw(rect, false, false, true, TreeViewInject.HasFocus());
                label.normal.textColor = label.onActive.textColor;
            }
            if (Event.current.type == EventType.Repaint)
                if (!TreeViewInject.IsItemSelected(s_GameObject))
                {
                    EditorGUI.DrawRect(rect, Styles.normalColor);
                    DrawBackground();
                    EditorGUI.DrawRect(rect, data.highlight);
                }

            int lvl = FindHierarchyLevel(s_GameObject);
            rect = GetHierarchyLevelRect(lvl + 1);
            rect.xMax = s_ItemRect.xMax;
            GUI.Label(rect, s_GameObject.name, label);

            if (Event.current.type == EventType.Repaint)
                if (TreeViewInject.IsExpandable(s_GameObject))
                {
                    var expanded = TreeViewInject.IsExpanded(s_GameObject);
                    var foldout = new GUIStyle("IN Foldout");
                    rect = GetHierarchyLevelRect(lvl);
                    foldout.Draw(rect, false, false, expanded, s_WindowFocused);
                }
        }

        private static void DrawItemMask()
        {
            float width = 0;
            int index = 0;
            var offsets = GetSeparatorOffsets();
            var comps = s_GameObject.GetComponents<Component>();
            var last = FunctionType.Separator;
            Profiler.BeginSample("HierarchyPlus.Layout");
            foreach (var item in Prefs.GetFunctionList())
            {
                s_FunctionTable[item.Type].SetAligned(last == FunctionType.Separator);
                s_FunctionTable[item.Type].SetComponent(comps);
                last = item.Type;

                if (item.Type != FunctionType.Separator)
                {
                    var space = index < offsets.Count ? offsets[index] - width : 0;
                    width += s_FunctionTable[item.Type].Layout(s_GameObject, width, space);
                }
                else
                {
                    if (s_ItemRect.xMax - offsets[index] < 50)
                    {
                        var diff = offsets[index] - (s_ItemRect.xMax - 50);
                        offsets[index] = s_ItemRect.xMax - 50;
                        item.SeparatorConfig.Offset -= (int)diff;
                        Prefs.SaveSeparatorPrefs();
                    }
                    var separator = (s_FunctionTable[item.Type] as ISeparator);
                    separator.SetSeparator(index, item.SeparatorConfig);
                    width = offsets[index] + s_FunctionTable[item.Type].Layout(s_GameObject, offsets[index]);
                    index++;
                }
            }
            Profiler.EndSample();
            s_ItemMaxWidth = s_ItemRect.xMax - width;
            if (Prefs.fbDrawMask)
                DrawBackgroundMask(s_ItemMaxWidth - 30, 35);
        }

        private static void DrawFunctionList()
        {
            int index = 0;
            var offsets = GetSeparatorOffsets();
            offsets.Add(s_ItemRect.xMax);
            Profiler.BeginSample("HierarchyPlus.Draw");
            foreach (var item in Prefs.GetFunctionList())
            {
                if (item.Type == FunctionType.Separator)
                {
                    var separator = (s_FunctionTable[item.Type] as ISeparator);
                    separator.SetSeparator(index, item.SeparatorConfig);
                    s_FunctionTable[item.Type].Draw(s_GameObject, s_ItemRect);
                    index++;
                }
                else
                {
                    var rect = new Rect(s_ItemRect);
                    rect.xMin = s_ItemRect.xMax - offsets[index];
                    if (rect.xMin < s_ItemRect.xMax - s_FunctionTable[item.Type].Offset - s_FunctionTable[item.Type].Width)
                    {
                        s_FunctionTable[item.Type].Draw(s_GameObject, rect);
                    }
                    else if (rect.xMin < s_ItemRect.xMax - s_FunctionTable[item.Type].Offset)
                    {
                        GUI.BeginGroup(rect);
                        rect.position = Vector2.zero;
                        s_FunctionTable[item.Type].Draw(s_GameObject, rect);
                        GUI.EndGroup();
                    }
                }
            }
            Profiler.EndSample();
        }

        private static void DrawActiveButton()
        {
            var data = DataObject.GetHierarchyItem(s_GameObject).customData;
            var comp = s_GameObject.GetMainComponent();
            var image = EditorGUIUtility.ObjectContent(comp, comp.GetType()).image;
            if (Prefs.hCustomize && data.enabled && data.icon != null)
                image = data.icon;
            var rect = new Rect(s_ItemRect);
            rect.x = 0;
            rect.width = rect.height;

            if (!Prefs.hLeftActive)
            {
                if (Event.current.type == EventType.Repaint)
                    if (Prefs.hCustomize && data.enabled && data.icon != null)
                        using (new GUIColorTint(data.enabled ? data.iconTint : Color.white))
                            GUI.DrawTexture(rect, image);
                return;
            }

            if (GUIHelper.IsMouseUp(rect, 1))
                AllInOneButton.ShowAioMenu(s_GameObject, rect);

            BatchAction.Prepare();
            EditorGUI.BeginChangeCheck();
            if (Prefs.hLeftActiveIcon)
            {
                var p = s_GameObject.transform.parent;
                float left = s_GameObject.activeSelf || (p != null && s_GameObject.activeSelf && !p.gameObject.activeInHierarchy) ? 1f : 0.4f;
                float right = s_GameObject.activeInHierarchy ? 1f : 0.4f;
                if (Event.current.type == EventType.Repaint)
                    using (new GUIColorTint(Prefs.hCustomize && data.icon != null && data.enabled ? data.iconTint : Color.white))
                        GUIHelper.DrawHalfFadeIcon(rect, image, left, right);
                GUI.Toggle(rect, s_GameObject.activeSelf, GUIContent.none, Styles.emptyStyle);
            }
            else
            {
                using (new GUIColorTint(s_GameObject.activeInHierarchy ? 1f : 0.4f))
                    GUI.Toggle(rect, s_GameObject.activeSelf, GUIContent.none);
            }
            if (EditorGUI.EndChangeCheck())
                GeneralButton.BatchToggle(FunctionType.Active, s_GameObject);
        }

        private static void DrawTree()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (TreeViewInject.HasSearchFilter()) return;

            var go = s_GameObject;
            int lvl = FindHierarchyLevel(go);
            if (lvl == 0) return;
            lvl--;
            var rect = GetHierarchyLevelRect(lvl);
            var l1 = new Vector2(rect.x + 7, rect.yMin);
            var l2 = new Vector2(rect.x + 7, rect.yMax);
            if (TreeViewInject.IsLastChild(go))
                l2.y = rect.y + 8;
            using (new HandlesColorTint(Styles.EnhanceColor(Styles.GetCycleColor(lvl + 1, 1f))))
                Handles.DrawLine(l1, l2);
            l1 = new Vector2(rect.x + 7, rect.y + 8);
            l2 = new Vector2(rect.xMax, rect.y + 8);
            using (new HandlesColorTint(Styles.EnhanceColor(Styles.GetCycleColor(lvl + 1, 1f))))
                Handles.DrawLine(l1, l2);

            while (lvl > 0)
            {
                go = go.transform.parent.gameObject;
                lvl--;
                rect = GetHierarchyLevelRect(lvl);
                if (TreeViewInject.IsLastChild(go)) continue;
                l1 = new Vector2(rect.x + 7, rect.yMin);
                l2 = new Vector2(rect.x + 7, rect.yMax);
                using (new HandlesColorTint(Styles.EnhanceColor(Styles.GetCycleColor(lvl + 1, 1f))))
                    Handles.DrawLine(l1, l2);
            }
        }

        private static void DrawHierarchyLevel()
        {
            if (TreeViewInject.HasSearchFilter()) return;
            if (Prefs.hLevelHighlighter == (int)HighlightPosition.None)
                return;

            int lvl = FindHierarchyLevel(s_GameObject);
            if (lvl == 0 && !Prefs.hLevelHighlighterDrawFirstLevel)
                return;
            var col = Styles.EnhanceColor(Styles.GetCycleColor(lvl, 0.2f));
            var rect = new Rect(s_ItemRect);

            if (Prefs.hLevelHighlighter == (int)HighlightPosition.All)
                rect.x = 0;
            if (Prefs.hLevelHighlighter == (int)HighlightPosition.Start)
                rect = GetHierarchyLevelRect(0);
            if (Prefs.hLevelHighlighter == (int)HighlightPosition.Foldout)
                rect = GetHierarchyLevelRect(lvl);
            if (Prefs.hLevelHighlighter == (int)HighlightPosition.Label)
            {
                rect = GetHierarchyLevelRect(lvl + 1);
                rect.x--;
            }
            rect.width = Prefs.hLevelHighlighterLength * 10;
            if (Prefs.hSeparator) rect.yMax--;
            if (Event.current.type == EventType.Repaint)
                GUIHelper.DrawFadeOutMask(rect, col);
        }

        private static void DrawChildCount()
        {
            if (TreeViewInject.HasSearchFilter()) return;
            if (!Prefs.hChildrenCount) return;

            bool total = Prefs.hChildrenCountTotal;
            if ((ModifierKey)Prefs.hChildrenCountModifier != ModifierKey.None)
                total = Utility.CheckModifierKey((ModifierKey)Prefs.hChildrenCountModifier) ? !total : total;
            int count = total ? s_GameObject.GetChildrenList().Count() - 1 : s_GameObject.transform.childCount;
            if (count == 0) return;

            var data = DataObject.GetHierarchyItem(s_GameObject).customData;
            var label = new GUIStyle("PR Label");
            label.padding.left = 0;
            if (data.bold) label.fontStyle = FontStyle.Bold;
            var go_width = label.CalcSize(Utility.TempContent(s_GameObject.name)).x;
            var size = Styles.miniLabel.CalcSize(Utility.TempContent(count.ToString()));

            var color = Styles.GetCycleColor(2, 1.0f);
            color = Styles.EnhanceColor(color, TreeViewInject.IsItemSelected(s_GameObject));
            int lvl = FindHierarchyLevel(s_GameObject);
            var rect = GetHierarchyLevelRect(lvl + 1);
            rect.x += go_width + 1;
            rect.yMin = rect.yMax - size.y + 2;
            rect.height = size.y;
            rect.width = size.x;
            using (new GUIColorTint(color))
                GUI.Label(rect, count.ToString(), Styles.miniLabel);
        }

        private static void DrawShadowLabel()
        {
            if (TreeViewInject.HasSearchFilter()) return;

            int lvl = FindHierarchyLevel(s_GameObject);
            int minlvl = Prefs.hShadowLabelLevel;
            if (!Prefs.hShadowLabel || lvl - minlvl < 0) return;
            //if (Prefs.hBackToParent && s_ItemHovered) return;
            if ((ModifierKey)Prefs.hShadowLabelModifier != ModifierKey.None)
            {
                var mod = Utility.CheckModifierKey((ModifierKey)Prefs.hShadowLabelModifier);
                var inv = Prefs.hShadowLabelModifierHide;
                if (mod && inv) return;
                if (!mod && !inv) return;
            }

            var rect = new Rect(s_ItemRect);
            rect.x = 13 + 1 + (lvl - minlvl) * Prefs.hShadowLabelIndent;
            rect.xMax = 13 + lvl * 14;

            using (new GUIColorTint(0.3f))
                GUI.Label(rect, s_GameObject.name);

            lvl = FindHierarchyLevel(s_GameObject);
            var col = Styles.EnhanceColor(Styles.GetCycleColor(lvl, 0.2f));
            var len = Prefs.hShadowLabelHighlighterLength * 10;
            if (len < rect.width)
                rect.width = len;
            if (Prefs.hSeparator) rect.yMax--;
            if (Event.current.type == EventType.Repaint)
                if (Prefs.hLevelHighlighter != (int)HighlightPosition.All && Prefs.hLevelHighlighter != (int)HighlightPosition.Start)
                    GUIHelper.DrawFadeOutMask(rect, col);
        }

        private static void DrawParentButton()
        {
            if (TreeViewInject.HasSearchFilter()) return;
            if (!Prefs.hBackToParent || !s_ItemHovered) return;
            if ((ModifierKey)Prefs.hBackToParentModifier != ModifierKey.None)
            {
                var mod = Utility.CheckModifierKey((ModifierKey)Prefs.hBackToParentModifier);
                var inv = Prefs.hBackToParentModifierHide;
                if (mod && inv) return;
                if (!mod && !inv) return;
            }

            int lvl = FindHierarchyLevel(s_GameObject);
            var rect = GetHierarchyLevelRect(lvl + 1);
            rect.x -= 14;
            rect.y += 1;
            rect.height = 14;
            rect.width = rect.height;

            var icon = Resources.Load("parent") as Texture;
            var trans = s_GameObject.transform;
            while (trans.parent != null)
            {
                lvl--;
                rect.x -= 14;
                trans = trans.parent;
                if (rect.xMax < s_ItemMaxWidth)
                {
                    if (Prefs.hBackToParentDrawDot)
                    {
                        if (Event.current.type == EventType.Repaint)
                            using (new HandlesColorTint(Styles.EnhanceColor(Styles.GetCycleColor(lvl + 1, 1f))))
                                Handles.DrawSolidArc(rect.center, new Vector3(0, 0, 1), new Vector3(1, 0, 0), 360, 2.6f);
                        if (GUI.Button(rect, Utility.TempContent(string.Empty, tooltip: trans.name), Styles.iconOnly))
                            TreeViewInject.SetExpandedRecursive(trans.gameObject, false);
                    }
                    else
                    {
                        using (new GUIColorTint(Styles.GetCycleColor(lvl, 1)))
                            if (GUI.Button(rect, Utility.TempContent(tex: icon, tooltip: trans.name), Styles.iconOnly))
                                TreeViewInject.SetExpandedRecursive(trans.gameObject, false);
                    }
                }
            }
        }

        private static Rect GetHierarchyLevelRect(int lvl)
        {
            if (TreeViewInject.HasSearchFilter()) lvl = 0;
            var rect = new Rect(s_ItemRect);
            rect.xMin = kBaseOffset + 14 * lvl;
            rect.width = 14;
            return rect;
        }

        private static int FindHierarchyLevel(GameObject go)
        {
            int lvl = 0;
            var tf = go.transform.parent;
            while (tf != null)
            {
                lvl++;
                tf = tf.parent;
            }
            return lvl;
        }

        private static void DrawBackground()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (!Prefs.hRowShading) return;
            var rect = new Rect(s_ItemRect);
            rect.xMin = 0f;
            if (Styles.forceProSkin)
                EditorGUI.DrawRect(rect, Styles.normalColor);
            if ((rect.y / rect.height) % 2 > 0f)
            {
                if (!TreeViewInject.IsItemSelected(s_GameObject))
                    EditorGUI.DrawRect(rect, Styles.overlayColor);
            }
        }

        private static void DrawSeparator()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (!Prefs.hSeparator) return;
            var rect = new Rect(s_ItemRect);
            rect.xMin = 0f;
            rect.y = rect.yMax - 1;
            rect.height = 1;
            if (Prefs.hSeparator) EditorGUI.DrawRect(rect, Styles.lineColor);
        }

        private static void DrawBackgroundMask(float x, float w)
        {
            if (Event.current.type != EventType.Repaint) return;
            var rect = new Rect(s_ItemRect);
            var col = Styles.normalColor;
            if (Prefs.hRowShading && (rect.y / rect.height) % 2 > 0f)
                col = Styles.altColor;
            col.a = 1;
            var mask = col;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                mask *= kPlayModeTint;

            rect.x = x;
            rect.width = w;
            GUIHelper.DrawFadeInMask(rect, mask);
            rect.xMin = x + w;
            rect.xMax = s_ItemRect.xMax;
            EditorGUI.DrawRect(rect, col);
        }
    }
}