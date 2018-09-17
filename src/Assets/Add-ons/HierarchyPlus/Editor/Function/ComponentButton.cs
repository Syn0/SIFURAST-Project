using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HierarchyPlus
{
    public interface ISetComponent
    {
        void SetComponent(Component[] comps);
    }

    public class ComponentButton : FunctionButton, IFunctionType, ISetComponent
    {
        public FunctionType FunctionType { get; set; }
        public override float Width { get { return _Width; } }
        public override float Offset { get { return _Offset; } }

        private float _Offset;
        private float _Width;
        private Component[] Components;
        private Component[] _Filtered;

        public void SetComponent(Component[] comps)
        {
            Components = comps;
        }

        public override float Layout(GameObject go, float offset, float expand = 0)
        {
            _Offset = offset;

            var e = Components.AsEnumerable().Where(i => i != null);
            switch (FunctionType)
            {
                case FunctionType.NonToggleable:
                    e = e.Where(i => EditorUtility.GetObjectEnabled(i) == -1);
                    break;
                case FunctionType.Toggleable:
                    e = e.Where(i => EditorUtility.GetObjectEnabled(i) != -1);
                    e = e.Where(i => !(i is MonoBehaviour));
                    break;
                case FunctionType.UI:
                    e = e.Where(i => EditorUtility.GetObjectEnabled(i) != -1);
                    e = e.Where(i => i is UIBehaviour && !Utility.IsImageEffect(i));
                    break;
                case FunctionType.Script:
                    e = e.Where(i => EditorUtility.GetObjectEnabled(i) != -1);
                    e = e.Where(i => i is MonoBehaviour && !(i is UIBehaviour) && !Utility.IsImageEffect(i));
                    break;
                case FunctionType.ImageEffect:
                    e = e.Where(i => EditorUtility.GetObjectEnabled(i) != -1).Where(i => Utility.IsImageEffect(i));
                    break;
            }
            var filter = Prefs.hExcludeComponent.Split(',');
            _Filtered = e.Where(i => !filter.Contains(i.GetType().Name)).ToArray();

            _Width = _Filtered.Count() * kButtonWidth;
            return _Width;
        }

        public override float Draw(GameObject go, Rect itemRect)
        {
            var rect = new Rect(itemRect);
            rect.x = rect.xMax - _Offset - _Width;
            rect.width = kButtonWidth;

            BatchAction.Prepare();
            foreach (var comp in _Filtered)
            {
                var en = EditorUtility.GetObjectEnabled(comp) != 0;
                var gc = Utility.TempContent();
                if (FunctionType != FunctionType.ImageEffect)
                    gc.image = IconCache.GetIcon(comp);
                else
                    gc.image = IconCache.GetIcon(typeof(Texture2D));
                gc.tooltip = ObjectNames.NicifyVariableName(comp.GetType().Name);

                if (GUIHelper.IsMouseUp(rect, 1))
                {
                    if (comp is Transform)
                        TransformPopup(rect, comp.gameObject);
                    else
                        ComponentPopup(rect, comp);
                }

                var darken = Prefs.fbNonToggleableDarken / 100f;
                EditorGUI.BeginChangeCheck();
                if (FunctionType == FunctionType.NonToggleable)
                    using (new GUIColorTint(new Color(darken, darken, darken)))
                        GUI.Button(rect, gc, Styles.iconOnly);
                else
                    using (new GUIColorTint(en ? 1f : 0.4f))
                        en = GUI.Toggle(rect, en, gc, Styles.iconOnly);
                if (EditorGUI.EndChangeCheck() && FunctionType != FunctionType.NonToggleable)
                {
                    BatchToggle(comp);
                    //EditorUtility.SetObjectEnabled(comp, en);
                    InternalEditorUtility.RepaintAllViews();
                }
                rect.x += kButtonWidth;
            }
            return _Width;
        }

        public static void Remove(Component comp)
        {
            if (comp is Transform) return;
            UnityEngine.Object.DestroyImmediate(comp);
        }

        public static void RemoveAll(GameObject go)
        {
            var list = go.GetComponents<Component>();
            foreach (var comp in list)
                Remove(comp);
        }

        public static void BatchToggle(Component comp)
        {
            var en = EditorUtility.GetObjectEnabled(comp) == 0;
            BatchAction.ProcessToggle(comp.gameObject, () => ToggleAction(comp, en));
        }

        private static void ToggleAction(Component comp, bool value)
        {
            var go = BatchAction.Current;
            var c = go == comp.gameObject ? comp : go.GetComponent(comp.GetType());
            if (c == null) return;
            var en = EditorUtility.GetObjectEnabled(c);
            if (en == -1) return;
            if (BatchAction.Mode == BatchMode.Toggle)
                value = en == 0;
            EditorUtility.SetObjectEnabled(c, value);
        }

        public static void BatchRemove(Component comp)
        {
            BatchAction.Process(comp.gameObject, () => RemoveAction(comp));
            UnityEngine.Object.DestroyImmediate(comp);
        }

        private static void RemoveAction(Component comp)
        {
            if (comp is Transform) return;
            var go = BatchAction.Current;
            if (go == comp.gameObject) return;
            var c = go.GetComponent(comp.GetType());
            if (c == null) return;
            UnityEngine.Object.DestroyImmediate(c);
        }

        public static void BatchPasteComponentValue(Component comp)
        {
            BatchAction.Process(comp.gameObject, () => PasteComponentValueAction(comp));
        }

        private static void PasteComponentValueAction(Component comp)
        {
            if (comp is Transform) return;
            var go = BatchAction.Current;
            if (go == comp.gameObject) return;
            var c = go.GetComponent(comp.GetType());
            if (c == null) return;
            Clipboard.PasteComponentValue(c);
        }

        private static void ComponentPopup(Rect rect, Component comp)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Copy"), false, () => Clipboard.CopyComponent(comp));
            menu.AddItem(new GUIContent("Add to Clipboard"), false, () => Clipboard.CopyComponent(comp, false));
            menu.AddItem(new GUIContent("Paste Value"), false, () => BatchPasteComponentValue(comp));
            menu.AddItem(new GUIContent("Remove"), false, () => BatchRemove(comp));
            menu.DropDown(rect);
        }

        public static void TransformPopup(GenericMenu menu, GameObject go, string sub)
        {
            if (go.transform is RectTransform)
            {
                menu.AddItem(new GUIContent(sub + "Reset"), false, () => BatchResetRectTransform(go));
                menu.AddItem(new GUIContent(sub + "Copy"), false, () => Clipboard.CopyRectTransform(go.transform as RectTransform));
                menu.AddItem(new GUIContent(sub + "Paste"), false, () => BatchPasteRectTransform(go));
            }
            else if (go.transform is Transform)
            {
                menu.AddItem(new GUIContent(sub + "Reset"), false, () => BatchResetTransform(go));
                menu.AddItem(new GUIContent(sub + "Copy"), false, () => Clipboard.CopyTransform(go.transform));
                menu.AddItem(new GUIContent(sub + "Paste Local"), false, () => BatchPasteTransform(go, false));
                menu.AddItem(new GUIContent(sub + "Paste World"), false, () => BatchPasteTransform(go, true));
            }
        }

        private static void BatchResetRectTransform(GameObject go)
        {
            BatchAction.Process(go, () => ResetRectTransformAction());
        }

        private static void ResetRectTransformAction()
        {
            var go = BatchAction.Current;
            var rect = go.transform as RectTransform;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            rect.anchoredPosition = Vector2.zero;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(100, 100);
        }

        private static void BatchPasteRectTransform(GameObject go)
        {
            BatchAction.Process(go, () => PasteRectTransformAction());
        }

        private static void PasteRectTransformAction()
        {
            var go = BatchAction.Current;
            Clipboard.PasteRectTransform(go.transform as RectTransform);
        }

        private static void BatchResetTransform(GameObject go)
        {
            BatchAction.Process(go, () => ResetTransformAction());
        }

        private static void ResetTransformAction()
        {
            var go = BatchAction.Current;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
        }

        private static void BatchPasteTransform(GameObject go, bool world)
        {
            BatchAction.Process(go, () => PasteTransformAction(world));
        }

        private static void PasteTransformAction(bool world)
        {
            var go = BatchAction.Current;
            if (world)
                Clipboard.PasteTransformWorld(go.transform);
            else
                Clipboard.PasteTransformLocal(go.transform);
        }

        private static void TransformPopup(Rect rect, GameObject go)
        {
            GenericMenu menu = new GenericMenu();
            TransformPopup(menu, go, "");
            menu.DropDown(rect);
        }
    }
}