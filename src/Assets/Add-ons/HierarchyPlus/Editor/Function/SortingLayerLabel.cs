using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HierarchyPlus
{
    public class SortingLayerLabel : FunctionButton
    {
        public override float Width { get { return _Width; } }
        public override float Offset { get { return _Offset; } }

        private float _Offset;
        private float _Width;
        private Renderer _Renderer;
        private string _LayerLabel;
        private GUIStyle _Style;

        public override float Layout(GameObject go, float offset, float expand = 0)
        {
            _Offset = offset;
            _Style = new GUIStyle(Styles.miniLabel);
            _Style.font = EditorStyles.miniFont;
            _Style.fontStyle = FontStyle.Bold;

            _Width = 1;
            _Renderer = go.GetComponent<Renderer>();
            if (Prefs.fbSortingLayerSpriteOnly && !(_Renderer is SpriteRenderer)) _Width = 0;
            if (_Renderer == null) _Width = 0;
            if (_Width == 0) return _Width;

            _LayerLabel = _Renderer.sortingLayerName.ConvertTagLayerLabel((TagLayerLabelOption)Prefs.fbSortingLayerLabelOption, Prefs.fbSortingLayerChop, Prefs.fbSortingLayerChopTo);
            _LayerLabel += ":" + _Renderer.sortingOrder;
            if (!Prefs.fbSortingLayerSmallFont)
            {
                _Style.font = EditorStyles.boldFont;
                _Width = _LayerLabel.CalcWidth(_Style) + 1;
            }
            else
            {
                _Width = _LayerLabel.CalcWidth(_Style);
            }
            _Width += 2;
            return _Width;
        }

        public override float Draw(GameObject go, Rect itemRect)
        {
            if (_Width == 0) return 0;
            var rect = new Rect(itemRect);
            rect.y += 2;
            rect.height -= 4;

            var labelColor = ((GUIStyle)"PR Label").normal.textColor;

            BatchAction.Prepare();
            rect.x = rect.xMax - _Offset - _Width;
            rect.width = _Width - 1;
            _Offset += _Width;

            int wheel = GUIHelper.GetMouseWheel(rect);
            if (wheel != 0) BatchOrderInc(go, wheel);

            var idx = SortingLayer.GetLayerValueFromName(_Renderer.sortingLayerName);
            var layercolor = Styles.EnhanceColor(Styles.GetCycleColor(idx, 1.0f));
            layercolor = Prefs.fbSortingLayerNoColorTint ? labelColor : layercolor;
            using (new GUIColorTint(layercolor))
                if (GUI.Button(rect, new GUIContent(_LayerLabel, _Renderer.sortingLayerName), _Style))
                    LayerPopup(rect, go);
            return _Width;
        }

        public static void LayerPopup(GenericMenu menu, GameObject go, string sub)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            for (int i = -5; i < 6; i++)
            {
                int order = renderer.sortingOrder + i * 10;
                menu.AddItem(new GUIContent(sub + "Order/More/" + order), renderer.sortingOrder == order, () => BatchOrder(go, order));
            }
            menu.AddSeparator(sub + "Order/");
            for (int i = -5; i < 6; i++)
            {
                int order = renderer.sortingOrder + i;
                menu.AddItem(new GUIContent(sub + "Order/" + order), renderer.sortingOrder == order, () => BatchOrder(go, order));
            }
            menu.AddSeparator(sub);
            foreach (var l in SortingLayer.layers.Select(l => l.name))
            {
                var layer = l;
                menu.AddItem(new GUIContent(sub + l), renderer.sortingLayerName == layer, () => BatchLayer(go, layer));
            }
            menu.AddSeparator(sub);
            menu.AddItem(new GUIContent(sub + "Add Sorting Layer..."), false, () => AddSortingLayer("SortingLayers"));
        }

        public static void BatchOrderInc(GameObject go, int inc)
        {
            BatchAction.Process(go, () => OrderIncAction(inc));
            Utility.MarkSceneDirty(go.scene);
        }

        private static void OrderIncAction(int inc)
        {
            var go = BatchAction.Current;
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            renderer.sortingOrder += inc;
            if (SceneView.lastActiveSceneView)
                SceneView.lastActiveSceneView.Repaint();
        }

        public static void BatchOrder(GameObject go, int order)
        {
            BatchAction.Process(go, () => OrderAction(order));
            Utility.MarkSceneDirty(go.scene);
        }

        private static void OrderAction(int order)
        {
            var go = BatchAction.Current;
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            renderer.sortingOrder = order;
            if (SceneView.lastActiveSceneView)
                SceneView.lastActiveSceneView.Repaint();
        }

        public static void BatchLayer(GameObject go, string layer)
        {
            BatchAction.Process(go, () => LayerAction(layer));
            Utility.MarkSceneDirty(go.scene);
        }

        private static void LayerAction(string layer)
        {
            var go = BatchAction.Current;
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            renderer.sortingLayerName = layer;
            if (SceneView.lastActiveSceneView)
                SceneView.lastActiveSceneView.Repaint();
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

        private static void AddSortingLayer(string value)
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