using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HierarchyPlus
{
    public class GameObjectIcon : FunctionButton
    {
        public override float Width { get { return _Width; } }
        public override float Offset { get { return _Offset; } }

        private float _Offset;
        private float _Width;
        private Texture _GameObjectIcon;

        public override float Layout(GameObject go, float offset, float expand = 0)
        {
            _Offset = offset;
            _Width = kButtonWidth;
            _GameObjectIcon = EditorGUIUtility.ObjectContent(go, typeof(GameObject)).image;
            if (_GameObjectIcon == null) _Width = 0;
            if (_GameObjectIcon == EditorGUIUtility.FindTexture("GameObject Icon") as Texture) _Width = 0;
            if (_GameObjectIcon == EditorGUIUtility.FindTexture("Prefab Icon") as Texture) _Width = 0;
            if (_GameObjectIcon == EditorGUIUtility.FindTexture("PrefabModel Icon") as Texture) _Width = 0;
            if (_GameObjectIcon == EditorGUIUtility.FindTexture("PrefabNormal Icon") as Texture) _Width = 0;
            if (_GameObjectIcon.width > _GameObjectIcon.height * 1.5)
                _Width *= 2;
            return _Width;
        }

        public override float Draw(GameObject go, Rect itemRect)
        {
            if (_Width == 0) return 0;
            var rect = new Rect(itemRect);
            rect.x = rect.xMax - _Offset - _Width;
            rect.width = _Width;

            var gc = Utility.TempContent();
            gc.image = _GameObjectIcon;
            gc.text = string.Empty;
            gc.tooltip = "Select Icon";

            if (GUI.Button(rect, gc, Styles.iconOnly))
            {
                ShowIconSelector(rect, go);
                Utility.MarkSceneDirty(go.scene);
            }
            return _Width;
        }

        public static void ShowIconSelector(Rect rect, GameObject go)
        {
            var type = typeof(Editor).Assembly.GetType("UnityEditor.IconSelector");
            var instance = ScriptableObject.CreateInstance(type);
            var parameters = new object[] { go, new Rect(rect), true };
            type.InvokeMember("Init", BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, instance, parameters);
        }
    }
}