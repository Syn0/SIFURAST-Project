using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HierarchyPlus
{
    public static class DataObject
    {
        public const string kDataObjectName = "HierarchyPlus_Data_Object";

        private static Dictionary<Scene, HierarchyData> s_SceneHierarchyData = new Dictionary<Scene, HierarchyData>();
        private static Dictionary<Scene, GameObject> s_SceneDataObject = new Dictionary<Scene, GameObject>();

        private static HierarchyData GetHierarchyData(Scene scene)
        {
            if (s_SceneHierarchyData.ContainsKey(scene) && s_SceneHierarchyData[scene] != null)
                return s_SceneHierarchyData[scene];

            HierarchyData data = null;
            var go = scene.GetRootGameObjects().FirstOrDefault(i => i.name.Equals(kDataObjectName));
            if (go == null) Utility.MarkSceneDirty(scene);
            go = go ?? new GameObject(kDataObjectName);
            go.hideFlags |= HideFlags.DontSaveInBuild;
            if (Prefs.doHideInHierarchy) go.hideFlags |= HideFlags.HideInHierarchy;

            data = go.GetComponent<HierarchyData>();
            if (data == null) Utility.MarkSceneDirty(scene);
            data = data ?? go.AddComponent<HierarchyData>();

            if (go.scene != scene)
                SceneManager.MoveGameObjectToScene(go, scene);
            s_SceneHierarchyData[scene] = data;
            s_SceneDataObject[scene] = go;

            return data;
        }

        public static bool CheckDataObject()
        {
            if (!Prefs.doHierarchyDataObject)
            {
                ChangeDataObject(remove: true);
                return false;
            }
            ChangeDataObject(hide: Prefs.doHideInHierarchy);
            return true;
        }

        private static void ChangeDataObject(bool hide = false, bool remove = false)
        {
            var dirty = false;
            var update = false;
            foreach (var scene in Utility.GetAllLoadedScene())
            {
                if (!s_SceneDataObject.ContainsKey(scene) || remove)
                    s_SceneDataObject[scene] = scene.GetRootGameObjects().FirstOrDefault(i => i.name.Equals(kDataObjectName));
                var go = s_SceneDataObject[scene];
                if (go == null) continue;
                var data = go.GetComponent<HierarchyData>();
                if (data == null) continue;

                dirty = false;
                if (hide && (go.hideFlags & HideFlags.HideInHierarchy) == 0) dirty = true;
                if (!hide && (go.hideFlags & HideFlags.HideInHierarchy) != 0) dirty = true;
                if (remove) dirty = true;

                if (hide) go.hideFlags |= HideFlags.HideInHierarchy;
                if (!hide) go.hideFlags &= ~HideFlags.HideInHierarchy;
                if (remove) Object.DestroyImmediate(go);

                if (dirty) Utility.MarkSceneDirty(scene);
                if (dirty) update = true;
            }
            if (update) EditorApplication.DirtyHierarchyWindowSorting();
        }

        public static IEnumerable<HierarchyItem> GetHierarchyAllItems(params Scene[] scenes)
        {
            var items = Enumerable.Empty<HierarchyItem>();
            if (!CheckDataObject())
                return items;

            var list = scenes.AsEnumerable();
            if (!list.Any())
                list = Utility.GetAllLoadedScene();
            foreach (var scene in list)
                items = items.Concat(GetHierarchyData(scene).items);
            return items.Where(i => i.gameObject != null);
        }

        public static HierarchyItem GetHierarchyItem(GameObject go)
        {
            if (!CheckDataObject())
                return new HierarchyItem(go, null);
            var data = GetHierarchyData(go.scene);
            return data.GetHierarchyItem(go);
        }

        public static List<RecentGameObjectItem> GetRecentGameObjects(Scene scene)
        {
            if (!CheckDataObject())
                return new List<RecentGameObjectItem>();
            var data = GetHierarchyData(scene);
            return data.recentGameObjects;
        }

        public static SelectionList GetSelectionList()
        {
            var list = new SelectionList();
            if (!CheckDataObject())
                return list;
            foreach (var scene in Utility.GetAllLoadedScene())
            {
                var sl = GetHierarchyData(scene).selectionList;
                for (int i = 0; i < 10; i++)
                    list[i].AddRange(sl[i]);
            }
            return list;
        }

        public static void SetSelectionList(int index, List<GameObject> list)
        {
            if (!CheckDataObject())
                return;
            foreach (var scene in Utility.GetAllLoadedScene())
            {
                var sl = GetHierarchyData(scene).selectionList;
                sl[index] = list.Where(go => go.scene == scene).ToList();
            }
        }
    }
}