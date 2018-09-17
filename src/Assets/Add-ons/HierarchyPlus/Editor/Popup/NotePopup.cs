using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace HierarchyPlus
{
    [InitializeOnLoad]
    public static class SceneViewSticky
    {
        private static Tool s_CurrentTools;
        private static bool s_MouseDragging;
        private static bool s_MouseDragged;
        private static Vector2 s_MouseDragPosition = new Vector2(-1, -1);

        static SceneViewSticky()
        {
            SceneView.onSceneGUIDelegate += SceneGUIDelegate;
        }

        private static void SceneGUIDelegate(SceneView sceneView)
        {
            var go = Selection.activeGameObject;
            if (go == null || !go.scene.isLoaded) return;
            var note = DataObject.GetHierarchyItem(go).noteData;
            if (note == null || note.IsDefault() || !note.showInSceneView) return;

            var box = new GUIStyle("box");
            box.border = new RectOffset(2, 9, 9, 2);
            box.normal.background = Resources.Load<Texture2D>("note");
            box.normal.textColor = note.stickyTextColor;
            box.fontSize = note.stickyTextSize;
            box.wordWrap = true;
            box.richText = true;
            box.alignment = TextAnchor.UpperLeft;
            box.padding.right = 10;

            var content = new GUIContent(note.title + (note.maximized && !string.IsNullOrEmpty(note.content) ? "\n" + note.content : string.Empty));
            var size = box.CalcSize(content);
            if (size.x > note.stickyWidth) size.x = note.stickyWidth;
            size.y = box.CalcHeight(content, size.x);

            var pos = HandleUtility.WorldToGUIPoint(go.transform.position);
            pos.x -= size.x / 2;
            pos += note.offset;
            var rect = new Rect(pos, size);

            Handles.BeginGUI();
            var evt = Event.current;
            var cid = GUIUtility.GetControlID(FocusType.Passive, rect);

            if (evt.type == EventType.MouseDown)
            {
                if (evt.button != 2 && rect.Contains(evt.mousePosition))
                {
                    evt.Use();
                    s_CurrentTools = Tools.current;
                    GUIUtility.hotControl = cid;
                    if (!s_MouseDragging)
                    {
                        s_MouseDragging = true;
                        s_MouseDragged = false;
                        s_MouseDragPosition = evt.mousePosition;
                    }
                }
            }
            if (evt.type == EventType.MouseDrag && s_MouseDragging)
            {
                evt.Use();
                s_MouseDragged = true;
                note.offset += evt.mousePosition - s_MouseDragPosition;
                s_MouseDragPosition = evt.mousePosition;
                sceneView.Repaint();
            }
            if (evt.type == EventType.MouseUp && s_MouseDragging)
            {
                evt.Use();
                s_MouseDragging = false;
                if (!s_MouseDragged)
                {
                    if (evt.button == 0) note.maximized = !note.maximized;
                    if (evt.button == 1) note.showInSceneView = !note.showInSceneView;
                }
                GUIUtility.hotControl = 0;
                Tools.current = s_CurrentTools;
            }

            using (new GUIColorTint(note.stickyColor))
                GUI.Box(rect, content, box);
            Handles.EndGUI();
        }
    }

    public class NotePopup : PopupWindowContent
    {
        private GameObject m_GameObject;
        private NoteData m_NoteData;

        private int m_ToolbarIndex = 0;
        private Vector2 m_ContentScrollPos;

        public NotePopup(GameObject go)
        {
            m_GameObject = go;
            m_NoteData = DataObject.GetHierarchyItem(go).noteData;
            if (m_NoteData.IsDefault()) m_ToolbarIndex = 1;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(300, 300);
        }

        public override void OnGUI(Rect rect)
        {
            string[] tb = new[] { "Note", "Edit", "Options" };
            m_ToolbarIndex = GUILayout.Toolbar(m_ToolbarIndex, tb, EditorStyles.toolbarButton);

            switch (m_ToolbarIndex)
            {
                case 0:
                    ShowNote();
                    break;
                case 1:
                    ShowEdit();
                    break;
                case 2:
                    ShowOption();
                    break;
            }
        }

        private void UpdateData()
        {
            DataObject.GetHierarchyItem(m_GameObject).noteData = m_NoteData;
            Utility.MarkSceneDirty(m_GameObject.scene);
        }

        private void ShowNote()
        {
            var label = new GUIStyle(EditorStyles.label);
            label.fontSize = m_NoteData.stickyTextSize;
            label.wordWrap = true;
            label.richText = true;
            EditorGUILayout.LabelField(m_NoteData.title, label);
            m_ContentScrollPos = EditorGUILayout.BeginScrollView(m_ContentScrollPos, GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField(m_NoteData.content, label, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            GUILayout.Space(4);
        }

        private void ShowEdit()
        {
            var textArea = new GUIStyle(EditorStyles.textArea);
            textArea.wordWrap = true;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Title");
            m_NoteData.title = EditorGUILayout.TextField(m_NoteData.title);
            EditorGUILayout.LabelField("Content");
            m_ContentScrollPos = EditorGUILayout.BeginScrollView(m_ContentScrollPos, GUILayout.ExpandHeight(true));
            m_NoteData.content = EditorGUILayout.TextArea(m_NoteData.content, textArea, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            GUILayout.Space(4);
            if (EditorGUI.EndChangeCheck()) UpdateData();
        }

        private void ShowOption()
        {
            EditorGUI.BeginChangeCheck();
            m_NoteData.color = EditorGUILayout.ColorField("Icon Color Tint", m_NoteData.color);
            m_NoteData.tooltip = EditorGUILayout.Toggle("Show Note in Tooltip", m_NoteData.tooltip);
            m_NoteData.showInSceneView = EditorGUILayout.Toggle("SceneView Sticky", m_NoteData.showInSceneView);
            m_NoteData.stickyWidth = EditorGUILayout.IntField("Sticky Width", m_NoteData.stickyWidth);
            m_NoteData.stickyColor = EditorGUILayout.ColorField("Sticky Color", m_NoteData.stickyColor);
            m_NoteData.stickyTextColor = EditorGUILayout.ColorField("Sticky Text Color", m_NoteData.stickyTextColor);
            m_NoteData.stickyTextSize = EditorGUILayout.IntField("Sticky Text Size", m_NoteData.stickyTextSize);
            if (GUILayout.Button("Reset Sticky Position"))
                m_NoteData.offset = new Vector2(0, 10);
            if (EditorGUI.EndChangeCheck())
            {
                UpdateData();
                InternalEditorUtility.RepaintAllViews();
            }
        }
    }
}