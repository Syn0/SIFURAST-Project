using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HierarchyPlus
{
    public class QuickCustomize : PopupWindowContent
    {
        public enum ColorType { iconTint, highlight, textColor }

        private GameObject[] m_GameObjects;
        private EditorWindow m_Parent;

        public static void Show(Rect rect, GameObject[] list)
        {
            var popup = new QuickCustomize();
            popup.m_GameObjects = list;
            PopupWindow.Show(rect, popup);
        }

        private QuickCustomize() { }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(240, 192);
        }

        public override void OnGUI(Rect rect)
        {
            GUILayout.Space(3);
            var colors = new Color[] { Prefs.colCustomize1, Prefs.colCustomize2, Prefs.colCustomize3, Prefs.colCustomize4, Prefs.colCustomize5,
                Prefs.colCustomize6, Prefs.colCustomize7, Prefs.colCustomize8, Prefs.colCustomize9, Prefs.colCustomize10 };

            EditorGUILayout.HelpBox("Icon", MessageType.None, true);
            var r = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
            r.width /= 10;
            for (int i = 0; i < 5; i++)
            {
                ImageButton(r, i);
                r.x += r.width;
            }
            r.width *= 5;
            r.xMin += 4;
            var icon = EditorGUI.ObjectField(r, null, typeof(Texture2D), true) as Texture2D;
            if (icon != null)
            {
                var list = ProjectData.instance.recentIcon;
                if (!list.Contains(icon))
                {
                    list.Insert(0, icon);
                    if (list.Count >= 5)
                        list.RemoveAt(5);
                }
                foreach (var go in m_GameObjects)
                {
                    var data = DataObject.GetHierarchyItem(go).customData;
                    if (!data.enabled)
                    {
                        data.iconTint = Color.white;
                        data.highlight = Color.clear;
                        data.textColor = Color.black;
                    }
                    data.enabled = true;
                    data.icon = icon;
                    DataObject.GetHierarchyItem(go).customData = data;
                    Utility.MarkSceneDirty(go.scene);
                }
                EditorApplication.RepaintHierarchyWindow();
            }

            EditorGUILayout.HelpBox("Icon Color Tint", MessageType.None, true);
            r = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
            r.width /= 10;
            foreach (var c in colors)
            {
                ColorButton(r, c, ColorType.iconTint);
                r.x += r.width;
            }

            EditorGUILayout.HelpBox("Highlight Color", MessageType.None, true);
            r = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
            r.width /= 10;
            foreach (var c in colors)
            {
                ColorButton(r, c, ColorType.highlight);
                r.x += r.width;
            }

            EditorGUILayout.HelpBox("Text Color", MessageType.None, true);
            r = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
            r.width /= 10;
            foreach (var c in colors)
            {
                ColorButton(r, c, ColorType.textColor);
                r.x += r.width;
            }

            GUILayout.Space(4);
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Bold"))
                {
                    foreach (var go in m_GameObjects)
                    {
                        var data = DataObject.GetHierarchyItem(go).customData;
                        if (!data.enabled)
                        {
                            data.iconTint = Color.white;
                            data.highlight = Color.clear;
                            data.textColor = Color.black;
                        }
                        data.enabled = true;
                        data.bold = !data.bold;
                        DataObject.GetHierarchyItem(go).customData = data;
                        Utility.MarkSceneDirty(go.scene);
                    }
                    EditorApplication.RepaintHierarchyWindow();
                }
                if (GUILayout.Button("Clear"))
                {
                    foreach (var go in m_GameObjects)
                    {
                        var data = DataObject.GetHierarchyItem(go).customData;
                        data.enabled = false;
                        DataObject.GetHierarchyItem(go).customData = data;
                        Utility.MarkSceneDirty(go.scene);
                    }
                    EditorApplication.RepaintHierarchyWindow();
                }
            }
        }

        private void ImageButton(Rect rect, int index)
        {
            GUI.Box(rect, GUIContent.none);
            if (ProjectData.instance.recentIcon.Count > index)
            {
                var icon = ProjectData.instance.recentIcon[index];
                if (icon != null)
                    GUI.DrawTexture(rect, icon);
                if (GUI.Button(rect, GUIContent.none, "label"))
                {
                    foreach (var go in m_GameObjects)
                    {
                        var data = DataObject.GetHierarchyItem(go).customData;
                        if (!data.enabled)
                        {
                            data.iconTint = Color.white;
                            data.highlight = Color.clear;
                            data.textColor = Color.black;
                        }
                        data.enabled = true;
                        data.icon = icon;
                        DataObject.GetHierarchyItem(go).customData = data;
                        Utility.MarkSceneDirty(go.scene);
                    }
                    EditorApplication.RepaintHierarchyWindow();
                }
            }
        }

        private void ColorButton(Rect rect, Color color, ColorType t)
        {
            EditorGUI.DrawRect(rect, color);
            if (GUI.Button(rect, GUIContent.none, "colorPickerBox"))
            {
                foreach (var go in m_GameObjects)
                {
                    var data = DataObject.GetHierarchyItem(go).customData;
                    if (!data.enabled)
                    {
                        data.iconTint = Color.white;
                        data.highlight = Color.clear;
                        data.textColor = Color.black;
                    }
                    data.enabled = true;
                    DataObject.GetHierarchyItem(go).customData = data;
                    switch (t)
                    {
                        case ColorType.iconTint:
                            data.iconTint = color;
                            break;
                        case ColorType.highlight:
                            data.highlight = color;
                            break;
                        case ColorType.textColor:
                            data.textColor = color;
                            break;
                    }
                    Utility.MarkSceneDirty(go.scene);
                }
                EditorApplication.RepaintHierarchyWindow();
            }
        }
    }

    public class ColorPopup : PopupWindowContent
    {
        public enum ColorType { iconTint, highlight, textColor }

        private ColorType m_ColorType;
        private CustomData m_CustomData;
        private EditorWindow m_Parent;

        public static void Show(Rect rect, GameObject go, ColorType type, EditorWindow parent)
        {
            var popup = new ColorPopup();
            popup.m_ColorType = type;
            popup.m_Parent = parent;
            popup.m_CustomData = DataObject.GetHierarchyItem(go).customData;
            PopupWindow.Show(rect, popup);
        }

        private ColorPopup() { }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(184, 40);
        }

        public override void OnGUI(Rect rect)
        {
            GUILayout.Space(3);
            using (new GUILayout.HorizontalScope())
            {
                ColorButton(EditorGUILayout.GetControlRect(GUILayout.Width(32)), Prefs.colCustomize1);
                ColorButton(EditorGUILayout.GetControlRect(GUILayout.Width(32)), Prefs.colCustomize2);
                ColorButton(EditorGUILayout.GetControlRect(GUILayout.Width(32)), Prefs.colCustomize3);
                ColorButton(EditorGUILayout.GetControlRect(GUILayout.Width(32)), Prefs.colCustomize4);
                ColorButton(EditorGUILayout.GetControlRect(GUILayout.Width(32)), Prefs.colCustomize5);
            }
            using (new GUILayout.HorizontalScope())
            {
                ColorButton(EditorGUILayout.GetControlRect(GUILayout.Width(32)), Prefs.colCustomize6);
                ColorButton(EditorGUILayout.GetControlRect(GUILayout.Width(32)), Prefs.colCustomize7);
                ColorButton(EditorGUILayout.GetControlRect(GUILayout.Width(32)), Prefs.colCustomize8);
                ColorButton(EditorGUILayout.GetControlRect(GUILayout.Width(32)), Prefs.colCustomize9);
                ColorButton(EditorGUILayout.GetControlRect(GUILayout.Width(32)), Prefs.colCustomize10);
            }
        }

        private void ColorButton(Rect rect, Color color)
        {
            EditorGUI.DrawRect(rect, color);
            if (GUI.Button(rect, GUIContent.none, "colorPickerBox"))
            {
                switch (m_ColorType)
                {
                    case ColorType.iconTint:
                        m_CustomData.iconTint = color;
                        break;
                    case ColorType.highlight:
                        m_CustomData.highlight = color;
                        break;
                    case ColorType.textColor:
                        m_CustomData.textColor = color;
                        break;
                }
                editorWindow.Close();
                m_Parent.Repaint();
            }
        }
    }

    public class CustomPopup : PopupWindowContent
    {
        private GameObject m_GameObject;
        private CustomData m_CustomData;

        public CustomPopup(GameObject go)
        {
            m_GameObject = go;
            m_CustomData = DataObject.GetHierarchyItem(go).customData;
            m_CustomData.enabled = true;
            DataObject.GetHierarchyItem(m_GameObject).customData = m_CustomData;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(200, 290);
        }

        private bool m_ApplyIcon;
        private bool m_ApplyIconTint;
        private bool m_ApplyHighlight;
        private bool m_ApplyTextColor;
        private bool m_ApplyBold;
        private bool m_Selected;
        private bool m_Children;

        public override void OnGUI(Rect rect)
        {
            var ro = new RectOffset(130, 50, 0, 0);
            EditorGUI.BeginChangeCheck();
            m_CustomData.enabled = EditorGUILayout.ToggleLeft(m_GameObject.name, m_CustomData.enabled);
            m_CustomData.icon = EditorGUILayout.ObjectField(m_CustomData.icon, typeof(Texture2D), true) as Texture2D;

            var cr = EditorGUILayout.GetControlRect();
            if (GUI.Button(ro.Remove(cr), GUIContent.none, "StaticDropdown"))
                ColorPopup.Show(ro.Remove(cr), m_GameObject, ColorPopup.ColorType.iconTint, editorWindow);
            m_CustomData.iconTint = EditorGUI.ColorField(cr, "Icon Color Tint", m_CustomData.iconTint);

            cr = EditorGUILayout.GetControlRect();
            if (GUI.Button(ro.Remove(cr), GUIContent.none, "StaticDropdown"))
                ColorPopup.Show(ro.Remove(cr), m_GameObject, ColorPopup.ColorType.highlight, editorWindow);
            m_CustomData.highlight = EditorGUI.ColorField(cr, "Highlight Color", m_CustomData.highlight);

            cr = EditorGUILayout.GetControlRect();
            if (GUI.Button(ro.Remove(cr), GUIContent.none, "StaticDropdown"))
                ColorPopup.Show(ro.Remove(cr), m_GameObject, ColorPopup.ColorType.textColor, editorWindow);
            m_CustomData.textColor = EditorGUI.ColorField(cr, "Text Color", m_CustomData.textColor);

            m_CustomData.bold = EditorGUILayout.Toggle("Bold", m_CustomData.bold);
            if (EditorGUI.EndChangeCheck())
            {
                DataObject.GetHierarchyItem(m_GameObject).customData = m_CustomData;
                Utility.MarkSceneDirty(m_GameObject.scene);
                EditorApplication.RepaintHierarchyWindow();
            }

            EditorGUILayout.HelpBox("Custom Color", MessageType.None, true);
            EditorGUI.BeginChangeCheck();
            using (new GUILayout.HorizontalScope())
            {
#if UNITY_2018_1_OR_NEWER
                Prefs.colCustomize1 = EditorGUILayout.ColorField(GUIContent.none, Prefs.colCustomize1, false, true, false, GUILayout.Width(32));
                Prefs.colCustomize2 = EditorGUILayout.ColorField(GUIContent.none, Prefs.colCustomize2, false, true, false, GUILayout.Width(32));
                Prefs.colCustomize3 = EditorGUILayout.ColorField(GUIContent.none, Prefs.colCustomize3, false, true, false, GUILayout.Width(32));
                Prefs.colCustomize4 = EditorGUILayout.ColorField(GUIContent.none, Prefs.colCustomize4, false, true, false, GUILayout.Width(32));
                Prefs.colCustomize5 = EditorGUILayout.ColorField(GUIContent.none, Prefs.colCustomize5, false, true, false, GUILayout.Width(32));
#else
                Prefs.colCustomize1 = EditorGUILayout.ColorField(GUIContent.none, Prefs.colCustomize1, false, true, false, null, GUILayout.Width(32));
                Prefs.colCustomize2 = EditorGUILayout.ColorField(GUIContent.none, Prefs.colCustomize2, false, true, false, null, GUILayout.Width(32));
                Prefs.colCustomize3 = EditorGUILayout.ColorField(GUIContent.none, Prefs.colCustomize3, false, true, false, null, GUILayout.Width(32));
                Prefs.colCustomize4 = EditorGUILayout.ColorField(GUIContent.none, Prefs.colCustomize4, false, true, false, null, GUILayout.Width(32));
                Prefs.colCustomize5 = EditorGUILayout.ColorField(GUIContent.none, Prefs.colCustomize5, false, true, false, null, GUILayout.Width(32));
#endif
            }
            using (new GUILayout.HorizontalScope())
            {
#if UNITY_2018_1_OR_NEWER
                Prefs.colCustomize6 = EditorGUILayout.ColorField(GUIContent.none, Prefs.colCustomize6, false, true, false, GUILayout.Width(32));
                Prefs.colCustomize7 = EditorGUILayout.ColorField(GUIContent.none, Prefs.colCustomize7, false, true, false, GUILayout.Width(32));
                Prefs.colCustomize8 = EditorGUILayout.ColorField(GUIContent.none, Prefs.colCustomize8, false, true, false, GUILayout.Width(32));
                Prefs.colCustomize9 = EditorGUILayout.ColorField(GUIContent.none, Prefs.colCustomize9, false, true, false, GUILayout.Width(32));
                Prefs.colCustomize10 = EditorGUILayout.ColorField(GUIContent.none, Prefs.colCustomize10, false, true, false, GUILayout.Width(32));
#else
                Prefs.colCustomize6 = EditorGUILayout.ColorField(GUIContent.none, Prefs.colCustomize6, false, true, false, null, GUILayout.Width(32));
                Prefs.colCustomize7 = EditorGUILayout.ColorField(GUIContent.none, Prefs.colCustomize7, false, true, false, null, GUILayout.Width(32));
                Prefs.colCustomize8 = EditorGUILayout.ColorField(GUIContent.none, Prefs.colCustomize8, false, true, false, null, GUILayout.Width(32));
                Prefs.colCustomize9 = EditorGUILayout.ColorField(GUIContent.none, Prefs.colCustomize9, false, true, false, null, GUILayout.Width(32));
                Prefs.colCustomize10 = EditorGUILayout.ColorField(GUIContent.none, Prefs.colCustomize10, false, true, false, null, GUILayout.Width(32));
#endif
            }
            if (EditorGUI.EndChangeCheck())
                Prefs.SaveAllPrefs(prefix: "col");

            EditorGUILayout.HelpBox("Copy To", MessageType.None, true);
            using (new GUILayout.HorizontalScope())
            {
                m_Selected = GUILayout.Toggle(m_Selected, "Selected", GUILayout.Width(80));
                GUILayout.FlexibleSpace();
                m_Children = GUILayout.Toggle(m_Children, "Children", GUILayout.Width(80));
                GUILayout.FlexibleSpace();
            }
            using (new GUILayout.HorizontalScope())
            {
                m_ApplyIcon = GUILayout.Toggle(m_ApplyIcon, "Icon", GUILayout.Width(80));
                GUILayout.FlexibleSpace();
                m_ApplyIconTint = GUILayout.Toggle(m_ApplyIconTint, "Color Tint", GUILayout.Width(80));
                GUILayout.FlexibleSpace();
            }
            using (new GUILayout.HorizontalScope())
            {
                m_ApplyHighlight = GUILayout.Toggle(m_ApplyHighlight, "Highlight", GUILayout.Width(80));
                GUILayout.FlexibleSpace();
                m_ApplyTextColor = GUILayout.Toggle(m_ApplyTextColor, "Text Color", GUILayout.Width(80));
                GUILayout.FlexibleSpace();
            }
            m_ApplyBold = GUILayout.Toggle(m_ApplyBold, "Bold");
            if (GUILayout.Button("Copy", GUILayout.Width(50)))
            {
                var all = m_Selected ? Selection.gameObjects.AsEnumerable() : new[] { m_GameObject }.AsEnumerable();
                if (m_Children)
                    all = all.SelectMany(go => go.GetChildrenList(true));
                foreach (var go in all)
                {
                    if (go == m_GameObject) continue;
                    var data = DataObject.GetHierarchyItem(go).customData;
                    if (data == null) continue;
                    data.enabled = m_CustomData.enabled;
                    if (m_ApplyIcon) data.icon = m_CustomData.icon;
                    if (m_ApplyIconTint) data.iconTint = m_CustomData.iconTint;
                    if (m_ApplyHighlight) data.highlight = m_CustomData.highlight;
                    if (m_ApplyTextColor) data.textColor = m_CustomData.textColor;
                    if (m_ApplyBold) data.bold = m_CustomData.bold;
                    DataObject.GetHierarchyItem(go).customData = data;
                }
                Utility.MarkSceneDirty(m_GameObject.scene);
                EditorApplication.RepaintHierarchyWindow();
                editorWindow.Close();
            }
        }
    }
}