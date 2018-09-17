using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HierarchyPlus
{
    public enum BatchMode { Ask, Same, Toggle }
    public enum BatchTarget { None, Children, Selected }

    public static class BatchAction
    {
        public static BatchMode Mode { get; private set; }
        public static GameObject Current { get; private set; }

        private static BatchTarget Target;
        private static GameObject Original;

        public static void Prepare()
        {
            CheckTarget();
        }

        public static void Process(GameObject go, Action action)
        {
            Process(go, action, BatchMode.Same);
        }

        public static void ProcessToggle(GameObject go, Action action)
        {
            if (Target == BatchTarget.None)
            {
                Process(go, action, BatchMode.Same);
                return;
            }

            var mode = (BatchMode)Prefs.hBatchMode;
            if (mode != BatchMode.Ask)
                Process(go, action, mode);
            else
            {
                //var menu = new GenericMenu();
                //menu.AddItem(new GUIContent("Same as Target"), false, () => Process(go, action, BatchMode.Same));
                //menu.AddItem(new GUIContent("Toggle Itself"), false, () => Process(go, action, BatchMode.Toggle));
                //menu.DropDown(rect);
                var same = EditorUtility.DisplayDialog("Select Batch Mode", "Which action do you want?", "Same as Target", "Toggle Self");
                Process(go, action, same ? BatchMode.Same : BatchMode.Toggle);
            }
        }

        private static void Process(GameObject go, Action action, BatchMode mode)
        {
            Original = go;
            Mode = mode;
            switch (Target)
            {
                case BatchTarget.None:
                    ProcessOriginal(action);
                    break;
                case BatchTarget.Children:
                    ProcessChildren(action);
                    break;
                case BatchTarget.Selected:
                    ProcessSelected(action);
                    break;
            }
        }

        private static void CheckTarget()
        {
            Target = BatchTarget.None;
            if (Utility.CheckModifierKey((ModifierKey)Prefs.hBatchModeChildren)) Target = BatchTarget.Children;
            if (Utility.CheckModifierKey((ModifierKey)Prefs.hBatchModeSelected)) Target = BatchTarget.Selected;
        }

        private static void ProcessOriginal(Action action)
        {
            Current = Original;
            action.Invoke();
        }

        private static void ProcessChildren(Action action)
        {
            ProcessOriginal(action);
            foreach (var go in Original.GetChildrenList(false))
            {
                Current = go;
                action.Invoke();
            }
        }

        private static void ProcessSelected(Action action)
        {
            ProcessOriginal(action);
            foreach (var go in Selection.gameObjects.Where(go => go.scene.isLoaded && go != Original))
            {
                Current = go;
                action.Invoke();
            }
        }
    }
}