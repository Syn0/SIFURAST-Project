using System;
using UnityEditor;
using UnityEngine;

namespace HierarchyPlus
{
    public class StaticButton : FunctionButton
    {
        public override float Width { get { return _Width; } }
        public override float Offset { get { return _Offset; } }

        private float _Offset;
        private float _Width;

        public override float Layout(GameObject go, float offset, float expand = 0)
        {
            _Offset = offset;
            _Width = kButtonWidth;
            if (Prefs.fbStaticHideDefault && GameObjectUtility.GetStaticEditorFlags(go) == 0)
                _Width = 0;
            return _Width;
        }

        public override float Draw(GameObject go, Rect itemRect)
        {
            if (_Width == 0) return 0;
            var rect = new Rect(itemRect);
            rect.x = rect.xMax - _Offset - _Width;
            rect.width = _Width;

            BatchAction.Prepare();
            var flags = GameObjectUtility.GetStaticEditorFlags(go);
            var left = flags != 0 ? Styles.iconColor : Styles.iconDisabledColor;
            var right = flags == (StaticEditorFlags)(-1) ? Styles.iconColor : Styles.iconDisabledColor;
            GUIHelper.DrawHalfFadeIcon(rect, FunctionType.Static.GetIcon(), left, right);
            var gc = new GUIContent(string.Empty, FunctionType.Static.GetLabel());
            if (GUI.Button(rect, gc, Styles.emptyStyle))
                StaticPopup(rect, go);
            return _Width;
        }

        public static void StaticPopup(GenericMenu menu, GameObject go, string sub)
        {
            var flags = GameObjectUtility.GetStaticEditorFlags(go);
            menu.AddItem(new GUIContent(sub + "Nothing"), flags == 0, () => BatchStatic(go, 0));
            menu.AddItem(new GUIContent(sub + "Everything"), flags == (StaticEditorFlags)(-1), () => BatchStatic(go, (StaticEditorFlags)(-1)));
            foreach (StaticEditorFlags f in Enum.GetValues(typeof(StaticEditorFlags)))
            {
                var flag = f;
                menu.AddItem(new GUIContent(sub + ObjectNames.NicifyVariableName(f.ToString())), (flags & f) != 0, () => BatchStatic(go, flag));
            }
        }

        public static void BatchStatic(GameObject go, StaticEditorFlags flag)
        {
            if (flag == 0 || flag == (StaticEditorFlags)(-1))
                BatchAction.Process(go, () => StaticAction(flag, true));
            else
            {
                var f = GameObjectUtility.GetStaticEditorFlags(go);
                BatchAction.ProcessToggle(go, () => StaticAction(flag, (f & flag) == 0));
            }
        }

        private static void StaticAction(StaticEditorFlags flag, bool value)
        {
            var go = BatchAction.Current;
            if (flag == 0 || flag == (StaticEditorFlags)(-1))
                GameObjectUtility.SetStaticEditorFlags(go, flag);
            else
            {
                var f = GameObjectUtility.GetStaticEditorFlags(go);
                if (BatchAction.Mode == BatchMode.Toggle)
                    f = f ^ flag;
                else
                    f = value ? f | flag : f & ~flag;
                if (f == (StaticEditorFlags)((1 << Enum.GetValues(typeof(StaticEditorFlags)).Length) - 1))
                    f = (StaticEditorFlags)(-1);
                GameObjectUtility.SetStaticEditorFlags(go, f);
            }
            Utility.MarkSceneDirty(go.scene);
        }

        private static void StaticPopup(Rect rect, GameObject go)
        {
            GenericMenu menu = new GenericMenu();
            StaticPopup(menu, go, "");
            menu.DropDown(rect);
        }

        private static void ToggleStatic(GameObject go, StaticEditorFlags flag)
        {
            var f = GameObjectUtility.GetStaticEditorFlags(go);
            if (flag == 0 || flag == (StaticEditorFlags)(-1))
            {
                if (flag == f) return;
                f = flag;
            }
            else
            {
                f = f ^ flag;
                if (f == (StaticEditorFlags)((1 << Enum.GetValues(typeof(StaticEditorFlags)).Length) - 1))
                    f = (StaticEditorFlags)(-1);
            }
            GameObjectUtility.SetStaticEditorFlags(go, f);
            Utility.MarkSceneDirty(go.scene);
        }
    }
}