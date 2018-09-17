using UnityEngine;

namespace HierarchyPlus
{
    public interface IFunctionType
    {
        FunctionType FunctionType { get; set; }
    }

    public abstract class FunctionButton
    {
        protected const float kButtonWidth = 16;
        public abstract float Width { get; }
        public abstract float Offset { get; }

        public abstract float Layout(GameObject go, float offset, float expand = 0);
        public abstract float Draw(GameObject go, Rect rect);
        public virtual void Init() { }
    }

    public class GeneralButton : FunctionButton, IFunctionType
    {
        public FunctionType FunctionType { get; set; }
        public override float Width { get { return _Width; } }
        public override float Offset { get { return _Offset; } }

        private float _Offset;
        private float _Width;
        private Texture _icon;
        private string _tooltip;

        public override void Init()
        {
            _icon = FunctionType.GetIcon();
            _tooltip = FunctionType.GetLabel();
        }

        public override float Layout(GameObject go, float offset, float expand = 0)
        {
            _Offset = offset;
            _Width = kButtonWidth;
            if (GetHideDefault() && GetFunctionState(FunctionType, go) == FunctionType.GetDefault())
                _Width = 0;
            return _Width;
        }

        public override float Draw(GameObject go, Rect itemRect)
        {
            if (_Width == 0) return 0;
            var rect = new Rect(itemRect);
            rect.x = rect.xMax - _Offset - _Width;
            rect.width = _Width;

            var value = GetFunctionState(FunctionType, go);
            BatchAction.Prepare();
            switch (FunctionType)
            {
                case FunctionType.Active:
                    using (new GUIColorTint(go.activeInHierarchy ? 1f : 0.4f))
                        if (value != GUI.Toggle(rect, value, GUIContent.none))
                            BatchToggle(FunctionType, go);
                    break;
                default:
                    var gc = Utility.TempContent(string.Empty, tex: _icon, tooltip: _tooltip);
                    using (new GUIColorTint(value))
                        if (GUI.Button(rect, gc, Styles.iconOnly))
                            BatchToggle(FunctionType, go);
                    break;
            }
            return _Width;
        }

        private bool GetHideDefault()
        {
            switch (FunctionType)
            {
                case FunctionType.Active:
                    return Prefs.fbActiveHideDefault;
                case FunctionType.Selectable:
                    return Prefs.fbSelectableHideDefault;
                case FunctionType.Lock:
                    return Prefs.fbLockHideDefault;
                case FunctionType.Favorite:
                    return Prefs.fbFavoriteHideDefault;
                case FunctionType.SelectionBase:
                    return Prefs.fbSelectionBaseHideDefault;
                case FunctionType.HierarchyBase:
                    return Prefs.fbHierarchyBaseHideDefault;
                case FunctionType.PersistentData:
                    return Prefs.fbPersistentDataHideDefault;
            }
            return false;
        }

        public static void BatchToggle(FunctionType ft, GameObject go)
        {
            var value = GetFunctionState(ft, go);
            BatchAction.ProcessToggle(go, () => ToggleAction(ft, !value));
        }

        private static void ToggleAction(FunctionType ft, bool value)
        {
            var go = BatchAction.Current;
            if (BatchAction.Mode == BatchMode.Toggle)
                value = !GetFunctionState(ft, go);
            SetFunctionState(ft, go, value);
        }

        public static bool GetFunctionState(FunctionType ft, GameObject go)
        {
            bool value = false;
            var item = DataObject.GetHierarchyItem(go);
            switch (ft)
            {
                case FunctionType.Active:
                    value = go.activeSelf;
                    break;
                case FunctionType.Selectable:
                    value = item.selectable;
                    break;
                case FunctionType.Lock:
                    value = (go.hideFlags & HideFlags.NotEditable) != 0;
                    break;
                case FunctionType.Favorite:
                    value = item.favorite;
                    break;
                case FunctionType.SelectionBase:
                    value = item.selectionBase;
                    break;
                case FunctionType.HierarchyBase:
                    value = item.hierarchyBase;
                    break;
                case FunctionType.PersistentData:
                    value = item.persistentData;
                    break;
            }
            return value;
        }

        public static void SetFunctionState(FunctionType ft, GameObject go, bool value)
        {
            var item = DataObject.GetHierarchyItem(go);
            switch (ft)
            {
                case FunctionType.Active:
                    go.SetActive(value);
                    break;
                case FunctionType.Selectable:
                    item.selectable = value;
                    break;
                case FunctionType.Lock:
                    go.hideFlags &= ~HideFlags.NotEditable;
                    go.hideFlags |= value ? HideFlags.NotEditable : 0;
                    break;
                case FunctionType.Favorite:
                    item.favorite = value;
                    break;
                case FunctionType.SelectionBase:
                    item.selectionBase = value;
                    break;
                case FunctionType.HierarchyBase:
                    item.hierarchyBase = value;
                    break;
                case FunctionType.PersistentData:
                    item.persistentData = value;
                    break;
            }
            Utility.MarkSceneDirty(go.scene);
        }
    }
}