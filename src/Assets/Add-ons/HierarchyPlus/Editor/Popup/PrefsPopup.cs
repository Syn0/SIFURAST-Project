using UnityEditor;
using UnityEngine;

namespace HierarchyPlus
{
    public class PrefsPopup : PopupWindowContent
    {
        public override Vector2 GetWindowSize()
        {
            return new Vector2(400, 600);
        }

        public override void OnGUI(Rect rect)
        {
            EditorGUIUtility.labelWidth = 200;
            Prefs.OnPreferencesGUI();
        }
    }
}