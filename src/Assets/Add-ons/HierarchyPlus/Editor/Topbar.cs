using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HierarchyPlus
{
    public static class Topbar
    {
        private const float kWindowRectYOffset = 17;
        private const float kTopbarY = -6;
        private const float kTopbarHeight = 30;
        private const float kTopbarWidth = 6 * 16 + 12;

        private static GUIContent s_RecentGameObjectContent = new GUIContent(string.Empty, "Recent GameObject");
        private static GUIContent s_RecentSceneContent = new GUIContent(string.Empty, "Recent Scene");
        private static GUIContent s_SelectionContent = new GUIContent(string.Empty, "Selection");
        private static GUIContent s_FavoriteContent = new GUIContent(string.Empty, "Favorite");
        private static GUIContent s_NoteContent = new GUIContent(string.Empty, "Note");
        private static GUIContent s_SettingContent = EditorGUIUtility.IconContent("_Popup");

        private static RecentGameObject s_RecentGameObject = new RecentGameObject();
        private static List<Scene> s_RecentScene = new List<Scene>();

        static Topbar()
        {
            s_RecentGameObjectContent.image = EditorGUIUtility.ObjectContent(null, typeof(GameObject)).image;
            s_RecentSceneContent.image = EditorGUIUtility.ObjectContent(null, typeof(SceneAsset)).image;
            s_FavoriteContent.image = EditorGUIUtility.Load("Favorite Icon") as Texture2D;
            s_NoteContent.image = EditorGUIUtility.ObjectContent(null, typeof(TextAsset)).image;
            s_SettingContent.tooltip = "Quick Setting";
            Selection.selectionChanged += SelectionChanged;
#if UNITY_2018_1_OR_NEWER
            EditorApplication.hierarchyChanged += HierarchyWindowChanged;
#else
            EditorApplication.hierarchyWindowChanged += HierarchyWindowChanged;
#endif
        }

        private static void SelectionChanged()
        {
            var go = Selection.activeGameObject;
            if (go == null || !go.scene.isLoaded) return;
            if (s_RecentGameObject.current != go)
                s_RecentGameObject.Add(go);
        }

        private static void HierarchyWindowChanged()
        {
            var scenes = Utility.GetAllLoadedScene();
            var added = scenes.Except(s_RecentScene);
            s_RecentScene = scenes.ToList();
            if (added.Any())
            {
                foreach (var scene in added)
                    ProjectData.instance.recentScene.Add(scene);
                ProjectData.Update();
            }
            s_RecentGameObject.LoadSceneRecentGameObject();
        }

        public static void Draw(Rect windowRect)
        {
            if (!Prefs.HierarchyTopbar) return;
            if (Event.current.type != EventType.Repaint) return;
            OnGUI(windowRect);
        }

        public static void HandleEvent(Rect windowRect)
        {
            if (!Prefs.HierarchyTopbar) return;
            if (Event.current.type == EventType.Repaint) return;
            OnGUI(windowRect);
            var rect = GetRect(windowRect);
            if (Event.current.isMouse && rect.Contains(Event.current.mousePosition))
                Event.current.Use();
        }

        public static void OnGUI(Rect windowRect)
        {
            var rect = GetRect(windowRect);

            using (new GUILightSkin())
            using (new GUIColorTint(Prefs.svTopbarColor))
                GUI.Box(rect, GUIContent.none, "flow overlay header upper right");

            rect.yMin -= kTopbarY;
            rect.height = 16;
            DrawButton(rect);
        }

        private static Rect GetRect(Rect windowRect)
        {
            var rect = new Rect(windowRect);
            rect.xMax -= Prefs.tbRightMargin;
            rect.xMin = rect.xMax - kTopbarWidth;
            rect.yMin += kWindowRectYOffset + kTopbarY;
            rect.height = kTopbarHeight;
            rect = GUIUtility.ScreenToGUIRect(rect);
            return rect;
        }

        private static void DrawButton(Rect drawRect)
        {
            var rect = new Rect(drawRect);
            rect.x += 8;
            rect.y--;
            rect.width = 16;
            int wheel = GUIHelper.GetMouseWheel(rect);
            if (wheel != 0)
                SelectRecentGameObject(s_RecentGameObject.currentIndex + wheel);
            if (GUI.Button(rect, s_RecentGameObjectContent, Styles.iconOnly))
                PopupRecentGameObject(rect);

            rect.x += 16;
            if (GUI.Button(rect, s_RecentSceneContent, Styles.iconOnly))
                PopupRecentScene(rect);

            rect.x += 16;
            var r = new Rect(rect);
            r.position += 2 * Vector2.one;
            r.size -= 4 * Vector2.one;
            var v = GUI.Toggle(r, true, s_SelectionContent, "flow overlay box");
            if (!v) PopupSelectionList(rect);

            rect.x += 16;
            if (GUI.Button(rect, s_FavoriteContent, Styles.iconOnly))
                AllInOneButton.PopupFavoriteList(rect);

            rect.x += 16;
            if (GUI.Button(rect, s_NoteContent, Styles.iconOnly))
                AllInOneButton.PopupNoteList(rect);

            rect.x += 16;
            r = new Rect(rect);
            r.y++;
            if (GUI.Button(r, s_SettingContent, Styles.iconOnly))
                PopupQuickSetting(rect);
        }

        private static void PopupRecentGameObject(Rect drawRect)
        {
            var menu = new GenericMenu();
            if (s_RecentGameObject.count == 0)
                menu.AddDisabledItem(new GUIContent("Empty"));
            for (int i = 0; i < s_RecentGameObject.count; i++)
            {
                var index = i;
                if (s_RecentGameObject[i] != null)
                {
                    var label = (i + 1) + ". " + s_RecentGameObject[i].name;
                    menu.AddItem(new GUIContent(label), i == s_RecentGameObject.currentIndex, () => { SelectRecentGameObject(index); });
                }
                else
                    menu.AddDisabledItem(new GUIContent((i + 1) + ". " + "None"));
            }
            menu.DropDown(drawRect);
        }

        private static void SelectRecentGameObject(int index)
        {
            if (s_RecentGameObject.count == 0) return;
            s_RecentGameObject.currentIndex = index;
            if (s_RecentGameObject.current == null) return;
            Selection.selectionChanged -= SelectionChanged;
            Selection.activeGameObject = s_RecentGameObject.current;
            Selection.selectionChanged += SelectionChanged;
        }

        private static void PopupRecentScene(Rect drawRect)
        {
            var menu = new GenericMenu();
            var rs = ProjectData.instance.recentScene;
            rs.RemoveInvalid();
            var count = rs.count;
            if (count == 0)
                menu.AddDisabledItem(new GUIContent("Empty"));
            for (int i = 0; i < count; i++)
            {
                var index = i;
                var name = rs[i];
                if (!string.IsNullOrEmpty(name))
                {
                    var label = (i + 1) + ". " + name;
                    label = label.Replace('_', ' ');
                    menu.AddItem(new GUIContent(label), false, () => { SelectRecentScene(index); });
                }
                else
                    menu.AddDisabledItem(new GUIContent((i + 1) + ". " + "None"));
            }
            menu.DropDown(drawRect);
        }

        private static void SelectRecentScene(int index)
        {
            ProjectData.instance.recentScene.LoadScene(index);
        }


        private static void PopupSelectionList(Rect drawRect)
        {
            var menu = new GenericMenu();
            var sl = DataObject.GetSelectionList();
            for (int i = 0; i < 10; i++)
            {
                var save = i;
                var label = "Save/Selection " + (i + 1);
                if (sl[i].Any())
                    label = "Save/" + (i + 1) + ". " + sl[i][0].name;
                if (sl[i].Count > 1)
                    label += " + " + (sl[i].Count - 1) + " more";
                menu.AddItem(new GUIContent(label), false, () => { SaveSelectionList(save); });
            }
            menu.AddSeparator(string.Empty);
            for (int i = 0; i < 10; i++)
            {
                var load = i;
                var label = "Selection " + (i + 1);
                if (sl[i].Any())
                    label = (i + 1) + ". " + sl[i][0].name;
                if (sl[i].Count > 1)
                    label += " + " + (sl[i].Count - 1) + " more";
                if (sl[i].Any())
                    menu.AddItem(new GUIContent(label), false, () => { LoadSelectionList(load); });
                else
                    menu.AddDisabledItem(new GUIContent(label));
            }
            menu.DropDown(drawRect);
        }

        private static void SaveSelectionList(int index)
        {
            var sel = Selection.gameObjects.OfType<GameObject>().Where(go => go.scene.isLoaded);
            sel = sel.OrderByDescending(go => go == Selection.activeGameObject);
            DataObject.SetSelectionList(index, sel.ToList());
        }

        private static void LoadSelectionList(int index)
        {
            var sl = DataObject.GetSelectionList();
            Selection.activeGameObject = sl[index][0];
            Selection.objects = sl[index].Cast<UnityEngine.Object>().ToArray();
        }

        private static void PopupQuickSetting(Rect drawRect)
        {
            var popupRect = new Rect(drawRect);
            popupRect.position = GUIUtility.GUIToScreenPoint(popupRect.position);

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Preferences"), false, () => { PopupPreferences(popupRect); });
            menu.AddSeparator(string.Empty);
            menu.AddItem(Prefs.sceneViewHighlighterContent, Prefs.SceneViewHighlighter, () => Prefs.SceneViewHighlighter = !Prefs.SceneViewHighlighter);
            menu.AddItem(Prefs.hierarchyItemDrawerContent, Prefs.HierarchyItemDrawer, () => Prefs.HierarchyItemDrawer = !Prefs.HierarchyItemDrawer);
            menu.AddItem(Prefs.hierarchyTopbarContent, Prefs.HierarchyTopbar, () => Prefs.HierarchyTopbar = !Prefs.HierarchyTopbar);
            menu.AddItem(Prefs.functionButtonContent, Prefs.FunctionButton, () => Prefs.FunctionButton = !Prefs.FunctionButton);
            menu.AddSeparator(string.Empty);
            GeneralSettingList(menu, "General/");
            FunctionButtonList(menu, "Function Button/");
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Select Data Object"), false, () => Selection.activeObject = UnityEngine.Object.FindObjectOfType<HierarchyData>());
            menu.DropDown(drawRect);
        }

        private static void PopupPreferences(Rect drawRect)
        {
            HierarchyDrawer.handleCommandEvent = () =>
            {
                if (Event.current.commandName.Equals("Preferences"))
                {
                    Event.current.Use();
                    var popup = new PrefsPopup();
                    PopupWindow.Show(GUIUtility.ScreenToGUIRect(drawRect), popup);
                }
            };
            HierarchyDrawer.hierarchyWindow.SendEvent(EditorGUIUtility.CommandEvent("Preferences"));
        }

        private static void GeneralSettingList(GenericMenu menu, string sub)
        {
            var prefs = new string[] { "hCustomize", "hLeftActive", "hChildrenCount", "hBackToParent", "hShadowLabel", "hDrawTree", "fbDrawMask" };
            foreach (var item in prefs.Select(i => Prefs.GetPrefsItem(i)))
            {
                var pi = item;
                var value = (bool)item.Field.GetValue(null);
                menu.AddItem(new GUIContent(sub + item.Desc.Text), value, () =>
                {
                    pi.Field.SetValue(null, !value);
                    Prefs.SaveAllPrefs();
                });
            }
            var mode = Prefs.hLevelHighlighter;
            menu.AddItem(new GUIContent(sub + "Level Highlighter"), mode != (int)HighlightPosition.None, () =>
             {
                 if (mode == (int)HighlightPosition.None)
                     Prefs.hLevelHighlighter = Prefs.hLevelHighlighterLastMode;
                 else
                 {
                     Prefs.hLevelHighlighterLastMode = Prefs.hLevelHighlighter;
                     Prefs.hLevelHighlighter = (int)HighlightPosition.None;
                 }
                 Prefs.SaveAllPrefs(prefix: "hLevelHighlighter");
             });
        }

        private static void FunctionButtonList(GenericMenu menu, string sub)
        {
            int prefix = 1;
            foreach (var item in Prefs.GetRawFunctionList())
            {
                var fti = item;
                var name = item.Type.GetLabel();
                if (item.Type == FunctionType.Separator)
                {
                    name += " (" + prefix + ")";
                    prefix++;
                }
                menu.AddItem(new GUIContent(sub + name), item.active, () =>
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                        fti.PlayMode = !fti.PlayMode;
                    else
                        fti.Enabled = !fti.Enabled;
                    Prefs.SaveSeparatorPrefs();
                    Prefs.SaveFunctionButtonPrefs();
                });
            }
        }
    }

    public class RecentGameObject
    {
        private class Entry
        {
            public GameObject gameObject;
            public DateTime timestamp = DateTime.Now;
            public Entry(GameObject go) { gameObject = go; }
            public Entry(GameObject go, long t) { gameObject = go; timestamp = new DateTime(t); }
        }

        private class EntryList
        {
            public List<Entry> list;
            public EntryList() { list = new List<Entry>(); }
            public EntryList(List<Entry> el) { list = el; }
            public void Add(GameObject go)
            {
                var remove = list.Any(e => e.gameObject == null);
                if (remove || list.Count >= kListMax)
                {
                    if (remove)
                        list = list.Where(e => e.gameObject != null).ToList();
                    else
                        list.RemoveAt(0);
                }
                var entry = new Entry(go);
                list.Add(entry);
                var rl = DataObject.GetRecentGameObjects(go.scene);
                rl.Clear();
                rl.AddRange(list.Select(e => new RecentGameObjectItem { gameObject = e.gameObject, timestamp = e.timestamp.Ticks }));
                if (Prefs.tbRecentDirty)
                    Utility.MarkSceneDirty(go.scene);
            }
        }

        public const int kListMax = 10;

        public GameObject this[int index] { get { return m_List[index].gameObject; } }
        public int count { get { return m_List.Count; } }
        public GameObject current { get { return GetCurrentGameObject(); } }
        public int currentIndex { get { return m_CurrentIndex; } set { SetCurrentIndex(value); } }

        private Dictionary<Scene, EntryList> m_SceneEntryList = new Dictionary<Scene, EntryList>();
        private List<Entry> m_List = new List<Entry>();
        private int m_CurrentIndex;

        public RecentGameObject()
        {
            LoadSceneRecentGameObject();
        }

        public void LoadSceneRecentGameObject()
        {
            foreach (var scene in Utility.GetAllLoadedScene())
            {
                if (!m_SceneEntryList.ContainsKey(scene))
                {
                    var list = DataObject.GetRecentGameObjects(scene);
                    m_SceneEntryList[scene] = new EntryList(list.Select(i => new Entry(i.gameObject, i.timestamp)).ToList());
                }
            }
            RebuildList();
        }

        public void Add(GameObject go)
        {
            if (go == null) return;
            RemoveItemAfterCurrent();
            GetEntryList(go.scene).Add(go);
            RebuildList();
        }

        private GameObject GetCurrentGameObject()
        {
            if (m_List.Count == 0) return null;
            SetCurrentIndex(m_CurrentIndex);
            return m_List[m_CurrentIndex].gameObject;
        }

        private void SetCurrentIndex(int index)
        {
            if (index < 0)
                index = 0;
            if (index >= m_List.Count)
                index = m_List.Count - 1;
            m_CurrentIndex = index;
        }

        private EntryList GetEntryList(Scene scene)
        {
            if (m_SceneEntryList.ContainsKey(scene))
                return m_SceneEntryList[scene];
            m_SceneEntryList[scene] = new EntryList();
            return m_SceneEntryList[scene];
        }

        private void RemoveItemAfterCurrent()
        {
            SetCurrentIndex(m_CurrentIndex);
            if (m_List.Count == 0) return;
            foreach (var el in m_SceneEntryList.Values)
                el.list = el.list.Where(e => e.timestamp <= m_List[m_CurrentIndex].timestamp).ToList();
        }

        private void RebuildList()
        {
            var el = m_SceneEntryList.Values.SelectMany(i => i.list);
            el = el.Where(e => e.gameObject != null).OrderByDescending(i => i.timestamp).Take(kListMax);
            m_List = el.Reverse().ToList();
            m_CurrentIndex = m_List.Count - 1;
        }
    }
}
