using System;
using UnityEditor;
using UnityEngine;

namespace HierarchyPlus
{
    public enum ModifierKey { None = 0, Ctrl = 1, Alt = 2, Shift = 3, Command = 4 }

    public static class GUIHelper
    {
        private static bool CheckMouseButton(Rect rect, int button, EventType type, ModifierKey key)
        {
            var evt = Event.current;
            bool mod = true;
            switch (key)
            {
                case ModifierKey.Ctrl:
                    mod = evt.control;
                    break;
                case ModifierKey.Alt:
                    mod = evt.alt;
                    break;
                case ModifierKey.Shift:
                    mod = evt.shift;
                    break;
                case ModifierKey.Command:
                    mod = evt.command;
                    break;
            }
            if (type == EventType.ScrollWheel)
            {
                if (Mathf.Sign(evt.delta.y) == Mathf.Sign(button) && Mathf.Abs(evt.delta.y) > Mathf.Abs(button))
                    button = evt.button;
                else
                    button = -1;
            }
            //var result = mod && evt.isMouse && evt.type == type && evt.button == button && rect.Contains(evt.mousePosition);
            var result = mod && evt.type == type && evt.button == button && rect.Contains(evt.mousePosition);
            return result;
        }

        public static bool IsMouseDown(Rect rect, int button, ModifierKey key = ModifierKey.None, bool use = true)
        {
            var result = CheckMouseButton(rect, button, EventType.MouseDown, key);
            if (result && use) Event.current.Use();
            return result;
        }

        public static bool IsMouseUp(Rect rect, int button, ModifierKey key = ModifierKey.None, bool use = true)
        {
            var result = CheckMouseButton(rect, button, EventType.MouseUp, key);
            if (result && use) Event.current.Use();
            return result;
        }

        public static int GetMouseWheel(Rect rect, ModifierKey key = ModifierKey.None, bool use = true)
        {
            var result = CheckMouseButton(rect, 1, EventType.ScrollWheel, key);
            if (result)
            {
                if (use) Event.current.Use();
                return 1;
            }
            result = CheckMouseButton(rect, -1, EventType.ScrollWheel, key);
            if (result)
            {
                if (use) Event.current.Use();
                return -1;
            }
            return 0;
        }

        public static void SetGUIColorAlpha(float a)
        {
            var c = GUI.color;
            c.a = a;
            GUI.color = c;
        }

        public static void SetGUIColorFade(float factor)
        {
            var c = GUI.color;
            c.a *= factor;
            GUI.color = c;
        }

        public static void DrawHalfFadeIcon(Rect rect, Texture image, float left, float right)
        {
            var l = GUI.color;
            l.a = left;
            var r = GUI.color;
            r.a = right;
            DrawHalfFadeIcon(rect, image, l, r);
        }

        public static void DrawHalfFadeIcon(Rect rect, Texture image, Color left, Color right)
        {
            var r = rect;
            r.width /= 2;
            var tc = new Rect(0, 0, 0.5f, 1);
            using (new GUIColorTint(left))
                GUI.DrawTextureWithTexCoords(r, image, tc);
            r.x += r.width;
            tc = new Rect(0.5f, 0, 0.5f, 1);
            using (new GUIColorTint(right))
                GUI.DrawTextureWithTexCoords(r, image, tc);
        }

        public static void DrawFadeInMask(Rect rect, Color col)
        {
            var guic = GUI.color;
            GUI.color = col;
            GUI.DrawTexture(rect, Styles.fadeMask);
            GUI.color = guic;
        }

        public static void DrawFadeOutMask(Rect rect, Color col)
        {
            var guic = GUI.color;
            var tc = new Rect(1, 0, -1, 1);
            GUI.color = col;
            GUI.DrawTextureWithTexCoords(rect, Styles.fadeMask, tc);
            GUI.color = guic;
        }

        public static void CheckMouseHover(Rect rect, int index, ref int hoverIndex, Action callback)
        {
            var evt = Event.current;
            if (evt.type == EventType.MouseMove)
            {
                if (rect.Contains(evt.mousePosition))
                {
                    if (index != hoverIndex)
                    {
                        hoverIndex = index;
                        callback();
                    }
                }
                else if (index == hoverIndex)
                {
                    hoverIndex = -1;
                    callback();
                }
            }
        }
    }

    public class ToggleableGroup : GUI.Scope
    {
        public ToggleableGroup(GUIContent content, ref bool value)
        {
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
            var style = new GUIStyle(EditorStyles.label);
            style.fontStyle = FontStyle.Bold;
            value = EditorGUILayout.ToggleLeft(content, value, style);
            if (value)
            {
                var rect = GUILayoutUtility.GetLastRect();
                rect.yMin = rect.yMax;
                rect.height = 1;
                var start = new Vector2(rect.x, rect.y);
                var end = new Vector2(rect.xMax, rect.y);
                Handles.color = Color.black;
                Handles.DrawLine(start, end);
            }
            EditorGUI.indentLevel++;
        }

        protected override void CloseScope()
        {
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }
    }

    public class MultiProperty : GUI.Scope
    {
        private readonly int _Indent;

        public MultiProperty(GUIContent content)
        {
            _Indent = EditorGUI.indentLevel;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(content);
            EditorGUI.indentLevel = 0;
        }

        protected override void CloseScope()
        {
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel = _Indent;
        }
    }

    public class GUILightSkin : GUI.Scope
    {
        private readonly GUISkin _Skin;

        public GUILightSkin()
        {
            _Skin = GUI.skin;
            GUI.skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
        }

        protected override void CloseScope()
        {
            GUI.skin = _Skin;
        }
    }

    public class GUIDarkSkin : GUI.Scope
    {
        private readonly GUISkin _Skin;

        public GUIDarkSkin()
        {
            _Skin = GUI.skin;
            GUI.skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);
        }

        protected override void CloseScope()
        {
            GUI.skin = _Skin;
        }
    }

    public class HandlesColorTint : GUI.Scope
    {
        private readonly Color _Color;

        public HandlesColorTint(Color col)
        {
            _Color = Handles.color;
            Handles.color = col;
        }

        protected override void CloseScope()
        {
            Handles.color = _Color;
        }
    }

    public class GUIColorTint : GUI.Scope
    {
        private readonly Color _Color;
        private readonly Color _ContentColor;

        public GUIColorTint(Color col)
        {
            _Color = GUI.color;
            _ContentColor = GUI.contentColor;
            GUI.color = col;
        }

        public GUIColorTint(bool value)
        {
            _Color = GUI.color;
            _ContentColor = GUI.contentColor;
            GUI.contentColor = value ? Styles.iconColor : Styles.iconDisabledColor;
        }

        public GUIColorTint(float alpha)
        {
            _Color = GUI.color;
            _ContentColor = GUI.contentColor;
            var col = _Color;
            col.a = alpha;
            GUI.color = col;
        }

        public GUIColorTint(Color col, float alpha)
        {
            _Color = GUI.color;
            _ContentColor = GUI.contentColor;
            col.a = alpha;
            GUI.color = col;
        }

        protected override void CloseScope()
        {
            GUI.color = _Color;
            GUI.contentColor = _ContentColor;
        }
    }

    public class TintIconButton : GUI.Scope
    {
        private readonly Color _Color;
        private readonly Color _IconColor;
        private readonly GUIContent _IconContent;

        public GUIContent content;
        public GUIStyle style;

        public TintIconButton(GUIContent gc, Color col)
        {
            _Color = GUI.contentColor;
            _IconColor = col;
            content = new GUIContent(gc);
            content.image = null;
            _IconContent = new GUIContent(gc);
            _IconContent.text = string.Empty;

            style = new GUIStyle("button");
            style.alignment = TextAnchor.MiddleLeft;
            style.fixedHeight = 21;
            style.padding.left = 23;
        }

        protected override void CloseScope()
        {
            var style = new GUIStyle("button");
            style.alignment = TextAnchor.MiddleLeft;
            style.fixedHeight = 21;
            style.normal.background = null;

            GUI.contentColor = _IconColor;
            GUI.Label(GUILayoutUtility.GetLastRect(), _IconContent, style);
            GUI.contentColor = _Color;
        }
    }

}
