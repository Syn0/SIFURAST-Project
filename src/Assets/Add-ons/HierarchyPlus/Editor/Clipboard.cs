using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HierarchyPlus
{
    public static class Clipboard
    {
        private static Vector3 s_LocalPosition = Vector3.zero;
        private static Quaternion s_LocalRotation = Quaternion.identity;
        private static Vector3 s_LocalScale = Vector3.one;
        private static Vector3 s_Position = Vector3.zero;
        private static Quaternion s_Rotation = Quaternion.identity;
        private static Vector3 s_LossyScale = Vector3.one;

        private static Vector3 s_RectPosition = Vector3.zero;
        private static Quaternion s_RectRotation = Quaternion.identity;
        private static Vector3 s_RectScale = Vector3.one;
        private static Vector2 s_AnchoredPosition = Vector2.zero;
        private static Vector2 s_AnchorMin = new Vector2(0.5f, 0.5f);
        private static Vector2 s_AnchorMax = new Vector2(0.5f, 0.5f);
        private static Vector2 s_Pivot = new Vector2(0.5f, 0.5f);
        private static Vector2 s_SizeDelta = new Vector2(100, 100);

        private static List<Component> s_ComponentClipboard = new List<Component>();

        public static void CopyTransform(Transform src)
        {
            s_LocalPosition = src.localPosition;
            s_LocalRotation = src.localRotation;
            s_LocalScale = src.localScale;
            s_Position = src.position;
            s_Rotation = src.rotation;
            s_LossyScale = src.lossyScale;
        }

        public static void PasteTransformLocal(Transform src)
        {
            src.localPosition = s_LocalPosition;
            src.localRotation = s_LocalRotation;
            src.localScale = s_LocalScale;
        }

        public static void PasteTransformWorld(Transform dst)
        {
            dst.position = s_Position;
            dst.rotation = s_Rotation;
            if (dst.parent == null)
                dst.localScale = s_LossyScale;
            else
            {
                var p = dst.parent.lossyScale;
                dst.localScale = new Vector3(s_LossyScale.x / p.x, s_LossyScale.y / p.y, s_LossyScale.z / p.z);
            }
        }

        public static void CopyRectTransform(RectTransform src)
        {
            s_RectPosition = src.localPosition;
            s_RectRotation = src.localRotation;
            s_RectScale = src.localScale;
            s_AnchoredPosition = src.anchoredPosition;
            s_AnchorMin = src.anchorMin;
            s_AnchorMax = src.anchorMax;
            s_Pivot = src.pivot;
            s_SizeDelta = src.sizeDelta;
        }

        public static void PasteRectTransform(RectTransform dst)
        {
            dst.localPosition = s_RectPosition;
            dst.localRotation = s_RectRotation;
            dst.localScale = s_RectScale;
            dst.anchoredPosition = s_AnchoredPosition;
            dst.anchorMin = s_AnchorMin;
            dst.anchorMax = s_AnchorMax;
            dst.pivot = s_Pivot;
            dst.sizeDelta = s_SizeDelta;
        }

        public static void CopyComponent(Component comp, bool clear = true)
        {
            if (comp is Transform) return;
            if (clear) s_ComponentClipboard.Clear();
            s_ComponentClipboard.Add(comp);
        }

        public static void CopyAllComponent(GameObject go, bool clear = true)
        {
            if (clear) s_ComponentClipboard.Clear();
            var list = go.GetComponents<Component>().Where(c => !(c is Transform));
            s_ComponentClipboard.AddRange(list);
        }

        public static Component[] GetComponentsByType(Type type)
        {
            return s_ComponentClipboard.Where(c => c.GetType().Equals(type)).ToArray();
        }

        public static Component PasteComponent(GameObject go, Component comp)
        {
            if (comp is Transform) return null;
            var dest = go.AddComponent(comp.GetType());
            if (dest != null)
                EditorUtility.CopySerialized(comp, dest);
            return dest;
        }

        public static void PasteComponentValue(Component src, Component dest)
        {
            if (src == dest) return;
            EditorUtility.CopySerialized(src, dest);
        }

        public static void PasteComponentValue(Component dest)
        {
            var src = s_ComponentClipboard.FirstOrDefault(c => c.GetType().Equals(dest.GetType()));
            if (src != null) PasteComponentValue(src, dest);
        }

        public static void PasteAllComponent(GameObject go)
        {
            foreach (var comp in s_ComponentClipboard.Where(c => c != null))
                PasteComponent(go, comp);
        }

        public static void OverwriteAllComponent(GameObject go)
        {
            var done = new List<Component>();
            foreach (var comp in s_ComponentClipboard.Where(c => c != null))
            {
                var dest = go.GetComponents(comp.GetType()).Except(done).FirstOrDefault();
                if (comp != dest)
                {
                    if (dest == null)
                        dest = PasteComponent(go, comp);
                    else
                        PasteComponentValue(comp, dest);
                }
                if (dest != null) done.Add(dest);
            }
        }
    }
}