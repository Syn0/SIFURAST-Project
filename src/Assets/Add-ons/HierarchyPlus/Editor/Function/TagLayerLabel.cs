using System;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace HierarchyPlus
{
    public class TagLayerLabel : FunctionButton
    {
        public override float Width { get { return _Width; } }
        public override float Offset { get { return _Offset; } }

        private float _Offset;
        private float _Width;
        private float _LayerWidth;
        private float _TagWidth;
        private bool _ShowLayer;
        private bool _ShowTag;
        private string _Layer;
        private string _LayerLabel;
        private string _TagLabel;
        private GUIStyle _Style;

        public override float Layout(GameObject go, float offset, float expand = 0)
        {
            _Offset = offset;
            _Style = new GUIStyle(Styles.miniLabel);
            _Style.font = EditorStyles.miniFont;
            _Style.fontStyle = FontStyle.Bold;

            _Layer = LayerMask.LayerToName(go.layer);
            if (_Layer == null) _Layer = string.Empty;
            _LayerLabel = _Layer.ConvertTagLayerLabel((TagLayerLabelOption)Prefs.fbTagLayerLabelOption, Prefs.fbTagLayerChop, Prefs.fbTagLayerChopTo);
            _TagLabel = go.tag.ConvertTagLayerLabel((TagLayerLabelOption)Prefs.fbTagLayerLabelOption, Prefs.fbTagLayerChop, Prefs.fbTagLayerChopTo);

            if (!Prefs.fbTagLayerSmallFont)
            {
                _Style.font = EditorStyles.boldFont;
                _LayerWidth = _LayerLabel.CalcWidth(_Style) + 1;
                _TagWidth = _TagLabel.CalcWidth(_Style) + 1;
            }
            else
            {
                _LayerWidth = _LayerLabel.CalcWidth(_Style);
                _TagWidth = _TagLabel.CalcWidth(_Style);
            }
            _LayerWidth += 2;
            _TagWidth += 2;

            var op = (TagLayerOption)Prefs.fbTagLayerOption;
            if (NeedInvert())
            {
                if (op == TagLayerOption.Tag) op = TagLayerOption.Layer;
                else if (op == TagLayerOption.Layer) op = TagLayerOption.Tag;
                else if (op == TagLayerOption.TagOrLayer) op = TagLayerOption.LayerOrTag;
                else if (op == TagLayerOption.LayerOrTag) op = TagLayerOption.TagOrLayer;
            }

            _ShowTag = !Prefs.fbTagLayerHideDefault || go.tag != "Untagged";
            _ShowLayer = !Prefs.fbTagLayerHideDefault || go.layer != 0;
            switch (op)
            {
                case TagLayerOption.Tag:
                    _ShowLayer = false;
                    break;
                case TagLayerOption.Layer:
                    _ShowTag = false;
                    break;
                case TagLayerOption.TagOrLayer:
                    _ShowLayer = go.tag != "Untagged" ? false : go.layer != 0;
                    _ShowTag = _ShowLayer ? false : _ShowTag;
                    break;
                case TagLayerOption.LayerOrTag:
                    _ShowTag = go.layer != 0 ? false : go.tag != "Untagged";
                    _ShowLayer = _ShowTag ? false : _ShowLayer;
                    break;
            }

            if (!_ShowTag && !_ShowLayer) return 0;
            _Width = (_ShowLayer ? _LayerWidth : _TagWidth) + 1;
            if (_ShowLayer && _ShowTag) _Width = _LayerWidth + _TagWidth + 2;
            return _Width;
        }

        public override float Draw(GameObject go, Rect itemRect)
        {
            if (!_ShowTag && !_ShowLayer) return 0;

            var rect = new Rect(itemRect);
            rect.y += 2;
            rect.height -= 4;

            var labelColor = ((GUIStyle)"PR Label").normal.textColor;

            BatchAction.Prepare();
            if (_ShowLayer)
            {
                rect.x = rect.xMax - _Offset - _LayerWidth;
                rect.width = _LayerWidth - 1;
                _Offset += _LayerWidth;

                var layercolor = Styles.GetCycleColor(go.layer, 1.0f);
                layercolor = go.layer == 0 ? Styles.iconDisabledColor : Styles.EnhanceColor(layercolor);
                layercolor = Prefs.fbTagLayerNoColorTint ? labelColor : layercolor;
                if (Event.current.type == EventType.Repaint)
                    using (new GUIColorTint(layercolor))
                        EditorGUI.DrawRect(rect, Color.white);
                using (new GUIColorTint(Styles.normalColor))
                    if (GUI.Button(rect, Utility.TempContent(text: _LayerLabel, tooltip: _Layer), _Style))
                        LayerPopup(rect, go);
            }
            if (_ShowTag)
            {
                rect.x = rect.xMax - _Offset - _TagWidth;
                rect.width = _TagWidth - 1;

                int idx = Array.IndexOf(InternalEditorUtility.tags, go.tag);
                var tagcolor = Styles.GetCycleColor(idx, 1.0f);
                tagcolor = idx == 0 ? Styles.iconDisabledColor : Styles.EnhanceColor(tagcolor);
                tagcolor = Prefs.fbTagLayerNoColorTint ? labelColor : tagcolor;
                using (new GUIColorTint(tagcolor))
                    if (GUI.Button(rect, Utility.TempContent(text: _TagLabel, tooltip: go.tag), _Style))
                        TagPopup(rect, go);
            }
            return _Width;
        }

        private static bool NeedInvert()
        {
            if (Prefs.fbTagLayerOption == (int)TagLayerOption.Both) return false;
            return Utility.CheckModifierKey((ModifierKey)Prefs.fbTagLayerInvert);
        }

        public static void TagPopup(GenericMenu menu, GameObject go, string sub)
        {
            foreach (var t in InternalEditorUtility.tags)
            {
                var tag = t;
                menu.AddItem(new GUIContent(sub + t), go.tag == t, () => BatchTag(go, tag));
            }
            menu.AddSeparator(sub);
            menu.AddItem(new GUIContent(sub + "Add Tag..."), false, () => AddTagLayer("Tags"));
        }

        public static void BatchTag(GameObject go, string tag)
        {
            BatchAction.Process(go, () => TagAction(tag));
            Utility.MarkSceneDirty(go.scene);
        }

        private static void TagAction(string tag)
        {
            var go = BatchAction.Current;
            go.tag = tag;
        }

        private static void TagPopup(Rect rect, GameObject go)
        {
            GenericMenu menu = new GenericMenu();
            TagPopup(menu, go, "");
            menu.DropDown(rect);
        }

        private static void SetTag(GameObject go, string tag)
        {
            go.tag = tag;
            Utility.MarkSceneDirty(go.scene);
        }

        public static void LayerPopup(GenericMenu menu, GameObject go, string sub)
        {
            foreach (var l in InternalEditorUtility.layers)
            {
                var layer = l;
                menu.AddItem(new GUIContent(sub + l), LayerMask.LayerToName(go.layer) == l, () => BatchLayer(go, layer));
            }
            menu.AddSeparator(sub);
            menu.AddItem(new GUIContent(sub + "Add Layer..."), false, () => AddTagLayer("Layers"));
        }

        public static void BatchLayer(GameObject go, string layer)
        {
            BatchAction.Process(go, () => LayerAction(layer));
            Utility.MarkSceneDirty(go.scene);
        }

        private static void LayerAction(string layer)
        {
            var go = BatchAction.Current;
            go.layer = LayerMask.NameToLayer(layer);
        }

        private static void LayerPopup(Rect rect, GameObject go)
        {
            GenericMenu menu = new GenericMenu();
            LayerPopup(menu, go, "");
            menu.DropDown(rect);
        }

        private static void SetLayer(GameObject go, string layer)
        {
            go.layer = LayerMask.NameToLayer(layer);
            Utility.MarkSceneDirty(go.scene);
        }

        private static void AddTagLayer(string value)
        {
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.TagManagerInspector");
            var method = type.GetMethod("ShowWithInitialExpansion", BindingFlags.Static | BindingFlags.NonPublic);
            var enumtype = type.GetNestedType("InitialExpansionState", BindingFlags.NonPublic);
            if (method == null || enumtype == null) return;
            var v = Enum.Parse(enumtype, value);
            method.Invoke(null, new object[] { v });
        }
    }
}