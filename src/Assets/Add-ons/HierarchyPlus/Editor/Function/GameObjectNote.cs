using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace HierarchyPlus
{
    public class GameObjectNote : FunctionButton
    {
        public override float Width { get { return _Width; } }
        public override float Offset { get { return _Offset; } }

        private float _Offset;
        private float _Width;

        private NoteData _NoteData;

        public override float Layout(GameObject go, float offset, float expand = 0)
        {
            _Offset = offset;
            _Width = kButtonWidth;
            _NoteData = DataObject.GetHierarchyItem(go).noteData;
            if (Prefs.fbNoteShowText)
            {
                var label = new GUIStyle("label");
                label.richText = true;
                var w = label.CalcSize(Utility.TempContent(_NoteData.title)).x;
                if (string.IsNullOrEmpty(_NoteData.title))
                    w = 0;
                w = Mathf.Min(w, Prefs.fbNoteTextWidth);
                if (!Prefs.fbNoteAutoWidth)
                    w = Prefs.fbNoteTextWidth;
                _Width += w;
                if (Prefs.fbNoteExpandWidth)
                    _Width = expand;
            }
            if (Prefs.fbNoteHideDefault && _NoteData.IsDefault())
                _Width = 0;
            return _Width;
        }

        public override float Draw(GameObject go, Rect itemRect)
        {
            if (_Width == 0) return 0;
            var rect = new Rect(itemRect);
            rect.x = rect.xMax - _Offset - _Width;
            rect.width = _Width;

            var gc = Utility.TempContent();
            gc.image = EditorGUIUtility.ObjectContent(null, typeof(TextAsset)).image;
            gc.text = string.Empty;
            gc.tooltip = _NoteData.IsDefault() ? "Add Note" : !_NoteData.tooltip ? string.Empty : string.IsNullOrEmpty(_NoteData.content) ? _NoteData.title : _NoteData.content;
            gc.tooltip = Regex.Replace(gc.tooltip, @"</?b>|</?i>|</?size(=\d+)?>|<color=\w+>|<color=#[0-9a-fA-F]+>|</color>", string.Empty);

            var label = new GUIStyle("label");
            label.richText = true;

            if (Prefs.fbNoteShowText && Prefs.fbNoteExpandWidth)
            {
                GUI.BeginGroup(rect);
                rect.position = Vector2.zero;
            }
            rect.width = kButtonWidth;
            using (new GUIColorTint(_NoteData.color, _NoteData.IsDefault() ? 0.5f : 1.0f))
                if (GUI.Button(rect, gc, Styles.iconOnly))
                    ShowEditNote(rect, go);

            rect.x += kButtonWidth;
            rect.width = _Width - kButtonWidth;
            gc.image = null;
            gc.text = _NoteData.title;
            if (GUI.Button(rect, gc, label))
                ShowEditNote(rect, go);
            if (Prefs.fbNoteShowText && Prefs.fbNoteExpandWidth) GUI.EndGroup();

            return _Width;
        }

        public static void EditNote(Rect rect, GameObject go)
        {
            var popup = new NotePopup(go);
            PopupWindow.Show(rect, popup);
        }

        private void ShowEditNote(Rect rect, GameObject go)
        {
            var popupRect = new Rect(rect);
            popupRect.position = GUIUtility.GUIToScreenPoint(popupRect.position);

            HierarchyDrawer.handleCommandEvent = () =>
            {
                if (Event.current.commandName.Equals("EditNote"))
                {
                    Event.current.Use();
                    EditNote(GUIUtility.ScreenToGUIRect(popupRect), go);
                }
            };
            HierarchyDrawer.hierarchyWindow.SendEvent(EditorGUIUtility.CommandEvent("EditNote"));
        }
    }
}