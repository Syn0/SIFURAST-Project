using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace HierarchyPlus
{
    [InitializeOnLoad]
    public static class Highlighter
    {
        private class Outline
        {
            public SceneViewOutline Hierarchy = null;
            public SceneViewOutline SceneView = null;
            public SceneViewOutline Sprite = null;

            public Outline(GameObject go)
            {
                Hierarchy = go.AddComponent<SceneViewOutline>();
                SceneView = go.AddComponent<SceneViewOutline>();
                Sprite = go.AddComponent<SceneViewOutline>();
            }

            public void ClearRenderers()
            {
                Hierarchy.Renderers = null;
                SceneView.Renderers = null;
                Sprite.Renderers = null;
            }
        }

        private static readonly Type kHierarchyWindowType;

        private static GameObject s_HoveredGameObject = null;
        private static bool s_DrawIndicator = false;
        private static bool s_Enabled = false;

        private static GameObject s_HoveredGameObjectSV = null;
        private static GameObject s_PickedGameObject = null;
        private static bool s_LastModifierKey = false;
        private static Vector2 s_LastMousePosition;
        private static Func<Vector2, IEnumerable<GameObject>> GetAllOverlappingFunc;

        private static Dictionary<SceneView, Outline> s_SceneViewOutlineTable = new Dictionary<SceneView, Outline>();

        static Highlighter()
        {
            kHierarchyWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
            EditorApplication.update += EditorUpdate;
            EditorApplication.modifierKeysChanged += ModifierKeysChanged;
            SceneView.onSceneGUIDelegate += OnSceneGUIDelegate;
            Selection.selectionChanged += SelectionChanged;
            s_Enabled = Prefs.SceneViewHighlighter;

            var bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var type = typeof(Editor).Assembly.GetType("UnityEditor.SceneViewPicking");
            var method = type.GetMethod("GetAllOverlapping", bf);
            GetAllOverlappingFunc = (Func<Vector2, IEnumerable<GameObject>>)Delegate.CreateDelegate(typeof(Func<Vector2, IEnumerable<GameObject>>), null, method, false);
        }

        private static void ModifierKeysChanged()
        {
            if (!Prefs.SceneViewHighlighter) return;
            if (!Prefs.svOutlineSceneView) return;
            foreach (SceneView sv in SceneView.sceneViews)
                sv.SendEvent(EditorGUIUtility.CommandEvent("ModifierKeyChanged"));
        }

        private static void EditorUpdate()
        {
            if (s_Enabled != Prefs.SceneViewHighlighter)
            {
                s_Enabled = Prefs.SceneViewHighlighter;
                if (!s_Enabled)
                    foreach (var outline in s_SceneViewOutlineTable.Values)
                        outline.ClearRenderers();
                else
                    HighlightSelectedSprite();

                if (SceneView.lastActiveSceneView)
                    SceneView.lastActiveSceneView.Repaint();
            }
            if (!Prefs.SceneViewHighlighter) return;

            var currentWindow = EditorWindow.mouseOverWindow;
            if (currentWindow && currentWindow.GetType() == kHierarchyWindowType)
            {
                if (!currentWindow.wantsMouseMove)
                    currentWindow.wantsMouseMove = true;
            }
            else
                RemoveHighlightObject();

        }

        private static void SelectionChanged()
        {
            if (!Prefs.SceneViewHighlighter) return;
            HighlightObject();
            HighlightSelectedSprite();
        }

        private static void HierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            if (!Prefs.SceneViewHighlighter) return;
            if (!Prefs.svOutlineHierarchy) return;
            if (!Utility.CheckModifierKey((ModifierKey)Prefs.svOutlineHierarchyModifier, true))
            {
                RemoveHighlightObject();
                return;
            }

            var evt = Event.current;
            switch (evt.type)
            {
                case EventType.MouseDrag:
                case EventType.DragUpdated:
                case EventType.DragPerform:
                case EventType.DragExited:
                case EventType.MouseMove:
                    var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
                    if (go == null)
                        break;
                    var rect = new Rect(selectionRect);
                    rect.xMin = 0;
                    if (rect.Contains(evt.mousePosition) && s_HoveredGameObject != go)
                    {
                        s_HoveredGameObject = go;
                        HighlightObject();
                    }
                    if (!rect.Contains(evt.mousePosition) && s_HoveredGameObject == go)
                    {
                        RemoveHighlightObject();
                    }
                    break;
            }
        }

        private static void OnSceneGUIDelegate(SceneView sceneView)
        {
            if (!Prefs.SceneViewHighlighter) return;

            var evt = Event.current;
            if (evt.type == EventType.Repaint)
            {
                var go = s_HoveredGameObject;
                if (go == null) go = s_HoveredGameObjectSV;
                if (go == null || !s_DrawIndicator) return;

                Color hc = Handles.color;
                if (go.transform is RectTransform && Prefs.svRectTransform)
                {
                    var rect = go.transform as RectTransform;
                    var v = new Vector3[4];
                    rect.GetWorldCorners(v);
                    var outline = Prefs.svRectTransformOverlayColor;
                    outline.a = 1;
                    Handles.DrawSolidRectangleWithOutline(v, Prefs.svRectTransformOverlayColor, outline);
                }
                else if (Prefs.svIndicator)
                {
                    var s = HandleUtility.GetHandleSize(go.transform.position) / 2.5f;
                    Quaternion q = Quaternion.Euler(45, -45, 0);
                    Handles.color = Prefs.svIndicatorColor;
                    Handles.ConeHandleCap(0, go.transform.position + new Vector3(s / 2, s / 2, -s / 2), q, s, EventType.Repaint);
                }
                if (go == s_HoveredGameObjectSV)
                    if (GUIUtility.ScreenToGUIRect(sceneView.position).Contains(evt.mousePosition))
                    {
                        var style = (GUIStyle)"sv_label_1";
                        var rect = new Rect();
                        rect.size = style.CalcSize(Utility.TempContent(go.name));
                        rect.center = evt.mousePosition;
                        if (Prefs.svOutlineSceneViewName)
                            rect.y += 30;

                        var pos = HandleUtility.GUIPointToWorldRay(rect.center).GetPoint(1);
                        if (Prefs.svOutlineSceneViewLine)
                            Handles.DrawDottedLine(go.transform.position, pos, 5);

                        Handles.BeginGUI();
                        if (Prefs.svOutlineSceneViewName)
                            GUI.Label(rect, go.name, style);
                        Handles.EndGUI();
                    }
                Handles.color = hc;
            }

            if (evt.type == EventType.ExecuteCommand && evt.commandName == "ModifierKeyChanged")
            {
                evt.Use();
                if (s_LastModifierKey != Utility.CheckModifierKey((ModifierKey)Prefs.svOutlineSceneViewModifier))
                {
                    s_LastModifierKey = Utility.CheckModifierKey((ModifierKey)Prefs.svOutlineSceneViewModifier);
                    if (s_LastModifierKey)
                        CheckSceneViewHover();
                    else
                        RemoveHighlightObjectSV();
                }
            }

            if (evt.type == EventType.MouseMove)
            {
                s_LastMousePosition = evt.mousePosition;
                if (s_LastModifierKey)
                    CheckSceneViewHover();
            }

            if (s_LastModifierKey && s_HoveredGameObjectSV != null)
            {
                if (evt.type == EventType.ScrollWheel)
                {
                    evt.Use();
                    int delta = 0;
                    if (evt.delta.y > 1) delta = 1;
                    if (evt.delta.y < 1) delta = -1;
                    if (delta != 0) SwitchSceneViewHover(delta);
                }

                int cid = GUIUtility.GetControlID("HoveredGameObjectSV".GetHashCode(), FocusType.Passive);
                switch (Event.current.GetTypeForControl(cid))
                {
                    case EventType.MouseDown:
                        GUIUtility.hotControl = cid;
                        Event.current.Use();
                        break;
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == cid)
                        {
                            GUIUtility.hotControl = 0;
                            Event.current.Use();
                            Selection.activeGameObject = s_HoveredGameObjectSV;
                        }
                        break;
                }
            }
        }

        private static void CheckSceneViewHover()
        {
            if (!Prefs.SceneViewHighlighter) return;
            if (!Prefs.svOutlineSceneView) return;

            var go = HandleUtility.PickGameObject(s_LastMousePosition, true);
            while (go != null && !DataObject.GetHierarchyItem(go).selectable)
                go = GetNextHover(go, 1);
            s_PickedGameObject = go;
            if (go == null || go != s_HoveredGameObjectSV)
            {
                s_HoveredGameObjectSV = go;
                HighlightObjectSV();
            }
        }

        private static GameObject GetNextHover(GameObject current, int delta)
        {
            var all = GetAllOverlappingFunc(s_LastMousePosition);
            all = all.Where(go => DataObject.GetHierarchyItem(go).selectable);
            if (all.Any() && s_PickedGameObject != null)
                all = all.Union(new[] { s_PickedGameObject });
            if (delta < 0)
                all = all.Reverse();
            GameObject result = null;
            bool get = false;
            foreach (var go in all)
            {
                if (result == null)
                    result = go;
                if (go == current)
                {
                    get = true;
                    continue;
                }
                if (get)
                {
                    result = go;
                    break;
                }
            }
            if (result == current)
                result = null;
            return result;
        }

        private static void SwitchSceneViewHover(int delta)
        {
            if (!Prefs.SceneViewHighlighter) return;
            if (!Prefs.svOutlineSceneView) return;

            if (s_HoveredGameObjectSV == null) return;
            var go = GetNextHover(s_HoveredGameObjectSV, delta);
            while (go != null && !DataObject.GetHierarchyItem(go).selectable)
                go = GetNextHover(go, 1);
            if (go != null)
                s_HoveredGameObjectSV = go;
            HighlightObjectSV();
        }

        private static Outline SetupSceneViewOutline(SceneView sv)
        {
            if (!s_SceneViewOutlineTable.ContainsKey(sv))
                s_SceneViewOutlineTable[sv] = new Outline(sv.camera.gameObject);
            return s_SceneViewOutlineTable[sv];
        }

        private static void HighlightObject()
        {
            foreach (SceneView sv in SceneView.sceneViews)
                HighlightObjectInternal(sv);
        }

        private static void HighlightObjectInternal(SceneView sv)
        {
            var outline = SetupSceneViewOutline(sv);
            if (outline.Hierarchy == null) return;
            var camera = outline.Hierarchy.GetComponent<Camera>();
            if (camera == null) return;

            outline.Hierarchy.Renderers = null;
            outline.Hierarchy.halfBufferSize = Prefs.svOutlineHalfSizeBuffer;
            var go = s_HoveredGameObject;
            if (!Prefs.svOutline || (go != null && go.transform is RectTransform))
                s_DrawIndicator = true;
            else
            {
                outline.Hierarchy.OutlineColor = Prefs.svOutlineColor;
                s_DrawIndicator = false;
                if (go != null)
                {
                    var selected = Selection.gameObjects.SelectMany(i => i.GetChildrenList());
                    var objs = (Prefs.svOutlineLevel > 0 ? go.GetChildrenList() : new[] { go }).Except(selected);
                    objs = objs.Where(i => GetChildLevel(go, i) <= Prefs.svOutlineLevel);
                    var renderer = objs.Select(i => i.GetComponent<Renderer>()).Where(i => CheckRenderer(i));
                    renderer = renderer.Where(i => GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera), i.bounds));
                    renderer = renderer.OrderBy(i => GetChildLevel(go, i.gameObject)).Take(Prefs.svOutlineMax);
                    if (renderer.Any())
                        outline.Hierarchy.SetupCommandBuffer(renderer.ToArray());
                    else
                        s_DrawIndicator = true;
                    if (!CheckRenderer(go.GetComponent<Renderer>()))
                        s_DrawIndicator = true;
                }
            }
            sv.Repaint();
        }

        private static void HighlightObjectSV()
        {
            foreach (SceneView sv in SceneView.sceneViews)
                HighlightObjectSVInternal(sv);
        }

        private static void HighlightObjectSVInternal(SceneView sv)
        {
            var outline = SetupSceneViewOutline(sv);
            if (outline.SceneView == null) return;
            var camera = outline.Hierarchy.GetComponent<Camera>();
            if (camera == null) return;

            outline.SceneView.Renderers = null;
            outline.SceneView.halfBufferSize = Prefs.svOutlineHalfSizeBuffer;
            var go = s_HoveredGameObjectSV;
            if (!Prefs.svOutline || (go != null && go.transform is RectTransform))
                s_DrawIndicator = true;
            else
            {
                outline.SceneView.OutlineColor = Prefs.svOutlineColor;
                s_DrawIndicator = false;
                if (go != null)
                {
                    //var selected = Selection.gameObjects.SelectMany(i => i.GetChildrenList());
                    var selected = new GameObject[] { };
                    var objs = (Prefs.svOutlineLevel > 0 ? go.GetChildrenList() : new[] { go }).Except(selected);
                    objs = objs.Where(i => GetChildLevel(go, i) <= Prefs.svOutlineLevel);
                    var renderer = objs.Select(i => i.GetComponent<Renderer>()).Where(i => CheckRenderer(i));
                    renderer = renderer.Where(i => GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera), i.bounds));
                    renderer = renderer.OrderBy(i => GetChildLevel(go, i.gameObject)).Take(Prefs.svOutlineMax);
                    if (renderer.Any())
                        outline.SceneView.SetupCommandBuffer(renderer.ToArray());
                    else
                        s_DrawIndicator = true;
                    if (!CheckRenderer(go.GetComponent<Renderer>()))
                        s_DrawIndicator = true;
                }
            }
            sv.Repaint();
        }

        private static void HighlightSelectedSprite()
        {
            foreach (SceneView sv in SceneView.sceneViews)
                HighlightSelectedSpriteInternal(sv);
        }

        private static void HighlightSelectedSpriteInternal(SceneView sv)
        {
            var outline = SetupSceneViewOutline(sv);
            if (outline.Sprite == null) return;

            outline.Sprite.Renderers = null;
            outline.Sprite.halfBufferSize = Prefs.svOutlineHalfSizeBuffer;
            if (Prefs.svOutlineSelectedSprites)
            {
                outline.Sprite.OutlineColor = Prefs.svOutlineSelectedSpritesColor;
                var selected = Selection.gameObjects.SelectMany(i => i.GetComponentsInChildren<SpriteRenderer>());
                var renderer = selected.Distinct().Where(i => i.sprite != null);
                if (renderer.Any())
                    outline.Sprite.SetupCommandBuffer(renderer.ToArray());
            }
            sv.Repaint();
        }

        private static bool CheckRenderer(Renderer r)
        {
            if (r == null) return false;
            if (r is MeshRenderer)
            {
                var f = r.GetComponent<MeshFilter>();
                if (f == null || f.sharedMesh == null)
                    return false;
            }
            if (r is SkinnedMeshRenderer && (r as SkinnedMeshRenderer).sharedMesh == null)
                return false;
            if (r is SpriteRenderer && (r as SpriteRenderer).sprite == null)
                return false;
            return true;
        }

        private static int GetChildLevel(GameObject parent, GameObject child)
        {
            int lvl = 0;
            var tf = child.transform;
            while (tf != null)
            {
                if (tf == parent.transform) return lvl;
                lvl++;
                tf = tf.parent;
            }
            return -1;
        }

        private static void RemoveHighlightObject()
        {
            if (s_HoveredGameObject == null)
                return;
            s_HoveredGameObject = null;
            HighlightObject();
        }

        private static void RemoveHighlightObjectSV()
        {
            s_PickedGameObject = null;
            if (s_HoveredGameObjectSV == null)
                return;
            s_HoveredGameObjectSV = null;
            HighlightObjectSV();
        }
    }
}