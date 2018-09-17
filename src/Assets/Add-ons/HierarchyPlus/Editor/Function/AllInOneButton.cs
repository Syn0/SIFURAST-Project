using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HierarchyPlus
{
    public class AllInOneButton : FunctionButton
    {
        public override float Width { get { return kButtonWidth; } }
        public override float Offset { get { return _Offset; } }

        private float _Offset;

        private static GUIStyle _IconStyle = null;
        private static string _Label = FunctionType.AllInOne.GetLabel();

        private void InitStyle()
        {
            if (_IconStyle != null) return;
            _IconStyle = new GUIStyle("PaneOptions");
            _IconStyle.fixedHeight = 16;
            _IconStyle.border = new RectOffset(0, 0, 0, 11);
        }

        public override float Layout(GameObject go, float offset, float expand = 0)
        {
            _Offset = offset;
            return kButtonWidth;
        }

        public override float Draw(GameObject go, Rect itemRect)
        {
            var rect = new Rect(itemRect);
            rect.x = rect.xMax - _Offset - kButtonWidth;
            rect.width = kButtonWidth;

            InitStyle();
            if (GUI.Button(rect, Utility.TempContent(string.Empty, tooltip: _Label), _IconStyle))
                ShowAioMenu(go, rect);
            return kButtonWidth;
        }

        public static void ShowAioMenu(GameObject go, Rect rect)
        {
            var popupRect = new Rect(rect);
            popupRect.position = GUIUtility.GUIToScreenPoint(popupRect.position);

            BatchAction.Prepare();
            var menu = new GenericMenu();
            var list = Enumerable.Range(0, Enum.GetNames(typeof(FunctionType)).Length).Cast<FunctionType>();
            list = list.Where(i => i.GetAttribute().Function.Equals(typeof(GeneralButton)));
            foreach (var ft in list)
            {
                var functiontype = ft;
                var value = GeneralButton.GetFunctionState(ft, go);
                menu.AddItem(new GUIContent(ft.GetLabel()), value, () => GeneralButton.BatchToggle(functiontype, go));
            }
            menu.AddSeparator("");
            StaticButton.StaticPopup(menu, go, "Static/");
            TagLayerLabel.TagPopup(menu, go, "Tag/");
            TagLayerLabel.LayerPopup(menu, go, "Layer/");
            SortingLayerLabel.LayerPopup(menu, go, "Sorting Layer/");

            PopupFavoriteList(menu, "Favorite/");

            menu.AddSeparator("");
            ComponentButton.TransformPopup(menu, go, "Transform/");
            ComponentMenu(menu, go);
            CameraMenu(menu, go);

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Customize"), false, () => ShowCustomize(popupRect, go));
            menu.AddItem(new GUIContent("Edit Note"), false, () => ShowEditNote(popupRect, go));
            menu.AddItem(new GUIContent("Gizmos Icon"), false, () => ShowIconSelector(popupRect, go));
            if (PrefabUtility.GetPrefabType(go) == PrefabType.None)
                menu.AddItem(new GUIContent("Create Prefab"), false, () => PrefabButton.CreatePrefab(go));
            menu.DropDown(rect);
        }

        public static void ShowIconSelector(Rect rect, GameObject go)
        {
            HierarchyDrawer.handleCommandEvent = () =>
            {
                if (Event.current.commandName.Equals("IconSelector"))
                {
                    Event.current.Use();
                    GameObjectIcon.ShowIconSelector(GUIUtility.ScreenToGUIRect(rect), go);
                }
            };
            HierarchyDrawer.hierarchyWindow.SendEvent(EditorGUIUtility.CommandEvent("IconSelector"));
        }

        public static void ShowCustomize(Rect rect, GameObject go)
        {
            HierarchyDrawer.handleCommandEvent = () =>
            {
                if (Event.current.commandName.Equals("Customize"))
                {
                    Event.current.Use();
                    var popup = new CustomPopup(go);
                    PopupWindow.Show(GUIUtility.ScreenToGUIRect(rect), popup);
                }
            };
            HierarchyDrawer.hierarchyWindow.SendEvent(EditorGUIUtility.CommandEvent("Customize"));
        }

        private static void PopupFavoriteList(GenericMenu menu, string sub)
        {
            var list = DataObject.GetHierarchyAllItems().Where(i => i.favorite).Select(i => i.gameObject);
            if (!list.Any() && string.IsNullOrEmpty(sub))
                menu.AddDisabledItem(new GUIContent("Empty"));
            foreach (var go in list)
            {
                var gameobject = go;
                menu.AddItem(new GUIContent(sub + go.name), false, () => Selection.activeGameObject = gameobject);
            }
        }

        public static void PopupFavoriteList(Rect rect)
        {
            GenericMenu menu = new GenericMenu();
            PopupFavoriteList(menu, "");
            menu.DropDown(rect);
        }

        public static void ShowEditNote(Rect rect, GameObject go)
        {
            HierarchyDrawer.handleCommandEvent = () =>
            {
                if (Event.current.commandName.Equals("EditNote"))
                {
                    Event.current.Use();
                    GameObjectNote.EditNote(GUIUtility.ScreenToGUIRect(rect), go);
                }
            };
            HierarchyDrawer.hierarchyWindow.SendEvent(EditorGUIUtility.CommandEvent("EditNote"));
        }

        private static void PopupNoteList(GenericMenu menu, string sub)
        {
            var list = DataObject.GetHierarchyAllItems().Where(i => !i.noteData.IsDefault()).Select(i => i.gameObject);
            if (!list.Any() && string.IsNullOrEmpty(sub))
                menu.AddDisabledItem(new GUIContent("Empty"));
            foreach (var go in list)
            {
                var gameobject = go;
                menu.AddItem(new GUIContent(sub + go.name), false, () => Selection.activeGameObject = gameobject);
            }
        }

        public static void PopupNoteList(Rect rect)
        {
            GenericMenu menu = new GenericMenu();
            PopupNoteList(menu, "");
            menu.DropDown(rect);
        }

        private static void ComponentMenu(GenericMenu menu, GameObject go)
        {
            menu.AddItem(new GUIContent("Component/Copy All"), false, () => Clipboard.CopyAllComponent(go));
            menu.AddItem(new GUIContent("Component/Add All to Clipboard"), false, () => Clipboard.CopyAllComponent(go, false));
            menu.AddItem(new GUIContent("Component/Paste All"), false, () => BatchPasteAllComponent(go));
            menu.AddItem(new GUIContent("Component/Paste and Overwrite All"), false, () => BatchOverwriteAllComponent(go));
            menu.AddItem(new GUIContent("Component/Remove All"), false, () => BatcRemoveAllComponent(go));
        }

        private static void BatchPasteAllComponent(GameObject go)
        {
            BatchAction.Process(go, () => PasteAllComponentAction());
        }

        private static void PasteAllComponentAction()
        {
            var go = BatchAction.Current;
            Clipboard.PasteAllComponent(go);
        }

        private static void BatchOverwriteAllComponent(GameObject go)
        {
            BatchAction.Process(go, () => OverwriteAllComponentAction());
        }

        private static void OverwriteAllComponentAction()
        {
            var go = BatchAction.Current;
            Clipboard.OverwriteAllComponent(go);
        }

        private static void BatcRemoveAllComponent(GameObject go)
        {
            BatchAction.Process(go, () => RemoveAllComponentAction());
        }

        private static void RemoveAllComponentAction()
        {
            var go = BatchAction.Current;
            ComponentButton.RemoveAll(go);
        }

        private static void CameraMenu(GenericMenu menu, GameObject go)
        {
            var cam = go.GetComponent<Camera>();
            var sv = SceneView.lastActiveSceneView;
            if (cam != null && sv != null && sv.camera != null)
            {
                menu.AddItem(new GUIContent("Camera/Sync To SceneView"), false, () =>
                {
                    sv.LookAt(cam.transform.position + cam.transform.forward, cam.transform.rotation, 1, false, false);
                });
                menu.AddItem(new GUIContent("Camera/Sync From SceneView"), false, () =>
                {
                    cam.transform.position = sv.camera.transform.position;
                    cam.transform.rotation = sv.camera.transform.rotation;
                });
            }
        }
    }
}
