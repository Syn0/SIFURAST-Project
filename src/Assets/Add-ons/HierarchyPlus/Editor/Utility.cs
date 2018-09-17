using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HierarchyPlus
{
    public static class Utility
    {
        private static GUIContent s_GUIContent = new GUIContent();

        public static GUIContent TempContent(string text = null, string image = null, Texture tex = null, string tooltip = null)
        {
            s_GUIContent.text = string.IsNullOrEmpty(text) ? string.Empty : text;
            s_GUIContent.image = tex != null ? tex : string.IsNullOrEmpty(image) ? null : EditorGUIUtility.IconContent(image).image;
            s_GUIContent.tooltip = string.IsNullOrEmpty(tooltip) ? string.Empty : tooltip;
            return s_GUIContent;
        }

        public static GUIContent CreateContent(string text = null, string image = null, Texture tex = null, string tooltip = null)
        {
            var content = new GUIContent();
            content.text = string.IsNullOrEmpty(text) ? string.Empty : text;
            content.image = tex != null ? tex : string.IsNullOrEmpty(image) ? null : EditorGUIUtility.IconContent(image).image;
            content.tooltip = string.IsNullOrEmpty(tooltip) ? string.Empty : tooltip;
            return content;
        }

        public static void SetComponent(this FunctionButton fb, Component[] comps)
        {
            var b = fb as ISetComponent;
            if (b != null) b.SetComponent(comps);
        }

        public static void SetAligned(this FunctionButton fb, bool aligned)
        {
            var b = fb as ISetAligned;
            if (b != null) b.SetAligned(aligned);
        }

        public static string ConvertTagLayerLabel(this string s, TagLayerLabelOption option, int chop, int to)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            switch (option)
            {
                case TagLayerLabelOption.Initial:
                    s = s.TrimStart().Substring(0, 1);
                    break;
                case TagLayerLabelOption.Capital:
                    var c = new string(s.Where(i => char.IsUpper(i)).ToArray());
                    s = string.IsNullOrEmpty(c) ? s.TrimStart().Substring(0, 1) : c;
                    break;
                case TagLayerLabelOption.Abbreviation:
                    s = Regex.Replace(s, @"((?<=[a-z])[A-Z]|(?<!^)[A-Z](?=[a-z]))", " $1");
                    var e = s.Split(' ').Select(i => i.Length <= chop ? i : i.Substring(0, to));
                    s = string.Join(" ", e.ToArray()).Trim();
                    break;
                case TagLayerLabelOption.HeadTail:
                    var ht = s.TrimStart().Substring(0, 1);
                    if (s.Length > 1) ht += s.TrimEnd().Substring(s.TrimEnd().Length - 1, 1);
                    s = ht;
                    break;
                case TagLayerLabelOption.MultiHeadTail:
                    s = Regex.Replace(s, @"((?<=[a-z])[A-Z]|(?<!^)[A-Z](?=[a-z]))", " $1");
                    var sb = new StringBuilder();
                    foreach (var m in s.Split(' '))
                    {
                        if (m.Length == 0) continue;
                        var mht = m.Substring(0, 1);
                        if (m.Length > 1) mht += m.Substring(m.Length - 1, 1);
                        sb.Append(mht);
                    }
                    s = sb.ToString();
                    break;
            }
            return s;
        }

        public static float CalcWidth(this string s, GUIStyle style)
        {
            return style.CalcSize(TempContent(s)).x;
        }

        public static string ToPrefString(this Color c)
        {
            string[] r = new string[4];
            for (int i = 0; i < 4; i++)
                r[i] = Mathf.FloorToInt(Mathf.Clamp01(c[i]) * 255).ToString();
            return string.Join(",", r);
        }

        public static Color ToColor(this string s, Color c = new Color())
        {
            var e = s.Split(',').Select(i => i.ToIntDef(-1));
            e = e.Where(i => i >= 0 && i <= 255);
            var l = e.ToArray();
            if(l.Length == 4)
            {
                for (int i = 0; i < 4; i++)
                    c[i] = l[i] / 255f;
            }
            return c;
        }

        public static int ToIntDef(this string s, int def = 0)
        {
            int r;
            return int.TryParse(s, out r) ? r : def;
        }

        public static FunctionTypeAttribute GetAttribute(this FunctionType value)
        {
            var type = value.GetType();
            var name = Enum.GetName(type, value);
            if (name == null) return null;
            var field = type.GetField(name);
            if (field == null) return null;
            var attr = Attribute.GetCustomAttribute(field, typeof(FunctionTypeAttribute)) as FunctionTypeAttribute;
            if (attr == null) return null;
            return attr;
        }

        public static string GetLabel(this FunctionType value)
        {
            return value.GetAttribute().Label;
        }

        public static bool GetEnable(this FunctionType value)
        {
            return value.GetAttribute().Enable;
        }

        public static bool GetDefault(this FunctionType value)
        {
            return value.GetAttribute().Default;
        }

        public static FunctionButton GetFunction(this FunctionType value)
        {
            var t = value.GetAttribute().Function;
            var fb = Activator.CreateInstance(t) as FunctionButton;
            if (fb is IFunctionType) (fb as IFunctionType).FunctionType = value;
            fb.Init();
            return fb;
        }

        public static Texture GetIcon(this FunctionType value)
        {
            var icon = value.GetAttribute().Image;
            return Resources.Load(icon) as Texture;
        }

        public static bool CheckModifierKey(ModifierKey mk, bool none = false)
        {
            switch (mk)
            {
                case ModifierKey.None:
                    return none;
                case ModifierKey.Ctrl:
                    return Event.current.control;
                case ModifierKey.Alt:
                    return Event.current.alt;
                case ModifierKey.Shift:
                    return Event.current.shift;
                case ModifierKey.Command:
                    return Event.current.command;
            }
            return false;
        }

        public static void MarkSceneDirty(Scene scene)
        {
            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
                EditorSceneManager.MarkSceneDirty(scene);
        }

        public static IEnumerable<Scene> GetAllLoadedScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.IsValid())
                    yield return scene;
            }
        }

        public static IEnumerable<GameObject> GetChildrenList(this GameObject go, bool self = true)
        {
            if (self) yield return go;
            var stack = new Stack<IEnumerator>();
            stack.Push(go.transform.GetEnumerator());
            while (stack.Count > 0)
            {
                var enumerator = stack.Peek();
                if (enumerator.MoveNext())
                {
                    var t = enumerator.Current as Transform;
                    yield return t.gameObject;
                    stack.Push(t.GetEnumerator());
                }
                else
                {
                    stack.Pop();
                }
            }
        }

        public static Component GetMainComponent(this GameObject go)
        {
            var e = go.GetComponents<Component>().OrderBy(i => GetComponentPriority(i));
            //e = e.OrderBy(i => 1 - EditorUtility.GetObjectEnabled(i));
            return e.FirstOrDefault();
        }

        public static bool IsImageEffect(Component c)
        {
            if (c is UIBehaviour) return false;
            if (!(c is MonoBehaviour)) return false;
            if (c.GetType().GetMethod("OnRenderImage", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) == null)
                return false;
            return c.gameObject.GetComponent<Camera>() != null;
        }

        public static bool IsScript(Component c)
        {
            if (c is UIBehaviour) return false;
            return c is MonoBehaviour;
        }

        public static bool IsCustomIconScript(Component c)
        {
            var icon = EditorGUIUtility.ObjectContent(c, c.GetType()).image;
            var cs = EditorGUIUtility.FindTexture("cs Script Icon") as Texture;
            var js = EditorGUIUtility.FindTexture("Js Script Icon") as Texture;
            return icon != cs && icon != js;
        }

        public static int GetComponentPriority(Component c)
        {
            int i = 0;
            if (c is Camera) return i; else i++;
            if (c is Terrain) return i; else i++;
            if (c is ParticleSystem) return i; else i++;
            if (c is Renderer && !(c is ParticleSystemRenderer)) return i; else i++;
            if (c is Light) return i; else i++;
            if (c is Collider) return i; else i++;
            if (c is Canvas) return i; else i++;
            if (c is UIBehaviour && c is Selectable) return i; else i++;
            if (c is UIBehaviour && c is Graphic) return i; else i++;
            if (c is UIBehaviour) return i; else i++;
            if (c is Behaviour) return i; else i++;
            if (c is MonoBehaviour && IsCustomIconScript(c)) return i; else i++;
            if (c is MonoBehaviour) return i; else i++;
            if (c is Transform) return i; else i++;
            return i;
        }
    }

    public static class IconCache
    {
        private static Dictionary<UnityEngine.Object, Texture> _ObjIconCache = new Dictionary<UnityEngine.Object, Texture>();
        private static Dictionary<Type, Texture> _TypeIconCache = new Dictionary<Type, Texture>();
        private static Dictionary<string, Texture> _PathIconCache = new Dictionary<string, Texture>();

        public static Texture GetIcon(UnityEngine.Object obj)
        {
            if (obj is Component && !Utility.IsScript(obj as Component))
                return GetIcon(obj.GetType());
            if (!_ObjIconCache.ContainsKey(obj))
                _ObjIconCache[obj] = EditorGUIUtility.ObjectContent(obj, obj.GetType()).image;
            return _ObjIconCache[obj];
        }

        public static Texture GetIcon(Type type)
        {
            if (!_TypeIconCache.ContainsKey(type))
                _TypeIconCache[type] = EditorGUIUtility.ObjectContent(null, type).image;
            return _TypeIconCache[type];
        }

        public static Texture GetIcon(string path)
        {
            if (!_PathIconCache.ContainsKey(path))
                _PathIconCache[path] = AssetDatabase.GetCachedIcon(path);
            return _PathIconCache[path];
        }
    }

    public class ProfileScope : GUI.Scope
    {
        public ProfileScope(string name)
        {
            Profiler.BeginSample(name);
        }

        protected override void CloseScope()
        {
            Profiler.EndSample();
        }
    }

}