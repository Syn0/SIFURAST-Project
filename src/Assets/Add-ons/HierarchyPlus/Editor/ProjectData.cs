using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HierarchyPlus
{
    [Serializable]
    public class RecentScene
    {
        public const int kListMax = 10;

        public string this[int index] { get { return GetSceneName(index); } }
        public int count { get { return m_List.Count; } }

        [SerializeField]
        private List<string> m_List = new List<string>();

        private string GetSceneName(int index)
        {
            var guid = m_List[index];
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return null;
            var name = Path.GetFileNameWithoutExtension(path);
            return name;
        }

        public void Add(Scene scene)
        {
            if (string.IsNullOrEmpty(scene.path)) return;
            var guid = AssetDatabase.AssetPathToGUID(scene.path);
            if (m_List.Contains(guid))
                m_List.Remove(guid);
            else if (m_List.Count >= kListMax)
                m_List.RemoveAt(0);
            m_List.Add(guid);
        }

        public void LoadScene(int index)
        {
            var guid = m_List[index];
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!PathIsValid(path)) return;
            var ok = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            if (ok) EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }

        public void RemoveInvalid()
        {
            m_List = m_List.Where(id => PathIsValid(AssetDatabase.GUIDToAssetPath(id))).ToList();
        }

        private bool PathIsValid(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return File.Exists(path);
        }
    }

    public class ProjectData : ScriptableObject
    {
        public static ProjectData instance { get { return GetInstance(); } }
        private static ProjectData s_Instance = null;

        public RecentScene recentScene { get { return m_RecentScene; } }

        public List<Texture2D> recentIcon = new List<Texture2D>();

        [SerializeField]
        private RecentScene m_RecentScene = new RecentScene();

        private static ProjectData GetInstance()
        {
            if (s_Instance != null) return s_Instance;

            var path = AssetDatabase.GetAssetPath(Shader.Find("Hidden/SceneViewOutline"));
            path = path.Replace("SceneViewOutline.shader", "Editor/ProjectData.asset");

            s_Instance = AssetDatabase.LoadAssetAtPath<ProjectData>(path);
            if (s_Instance == null)
            {
                s_Instance = CreateInstance<ProjectData>();
                AssetDatabase.CreateAsset(s_Instance, path);
                AssetDatabase.SaveAssets();
            }
            return s_Instance;
        }

        public static void Update()
        {
            EditorUtility.SetDirty(instance);
        }
    }
}