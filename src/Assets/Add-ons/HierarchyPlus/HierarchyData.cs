using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

[Serializable]
public class CustomData
{
#if UNITY_EDITOR
    public bool enabled = false;
    public Texture2D icon = null;
    public Color iconTint = Color.white;
    public Color highlight = Color.white;
    public Color textColor = Color.black;
    public bool bold = false;

    public bool IsDefault()
    {
        return !enabled;
    }
#endif
}

[Serializable]
public class NoteData
{
#if UNITY_EDITOR
    public string title = string.Empty;
    public string content = string.Empty;
    public Color color = Color.white;
    public bool tooltip = false;
    public bool showInSceneView = true;
    public int stickyWidth = 300;
    public Color stickyColor = Color.white;
    public Color stickyTextColor = Color.black;
    public int stickyTextSize = 13;
    public Vector2 offset = new Vector2(0, 10);
    public bool maximized = true;

    public bool IsDefault()
    {
        return string.IsNullOrEmpty(title) && string.IsNullOrEmpty(content);
    }
#endif
}

[Serializable]
public class HierarchyItem
{
#if UNITY_EDITOR
    public GameObject gameObject { get { return m_GameObject; } private set { m_GameObject = value; } }
    public bool selectable { get { return m_Selectable; } set { m_Selectable = value; UpdateDate(); } }
    public bool favorite { get { return m_Favorite; } set { m_Favorite = value; UpdateDate(); } }
    public bool selectionBase { get { return m_SelectionBase; } set { m_SelectionBase = value; UpdateDate(); } }
    public bool hierarchyBase { get { return m_HierarchyBase; } set { m_HierarchyBase = value; UpdateDate(); } }
    public bool persistentData { get { return m_PersistentData; } set { m_PersistentData = value; UpdateDate(); } }
    public CustomData customData { get { return m_CustomData; } set { m_CustomData = value; UpdateDate(); } }
    public NoteData noteData { get { return m_NoteData; } set { m_NoteData = value; UpdateDate(); } }

    [SerializeField]
    private GameObject m_GameObject;
    [SerializeField]
    private HierarchyData m_HierarchyData;
    [SerializeField]
    private bool m_Selectable = true;
    [SerializeField]
    private bool m_Favorite = false;
    [SerializeField]
    private bool m_SelectionBase = false;
    [SerializeField]
    private bool m_HierarchyBase = false;
    [SerializeField]
    private bool m_PersistentData = false;
    [SerializeField]
    private CustomData m_CustomData = new CustomData();
    [SerializeField]
    private NoteData m_NoteData = new NoteData();

    public HierarchyItem(GameObject go, HierarchyData data)
    {
        m_GameObject = go;
        m_HierarchyData = data;
    }

    public bool IsDefault()
    {
        return m_Selectable && !m_Favorite && !m_SelectionBase && !m_HierarchyBase && !m_PersistentData && m_CustomData.IsDefault() && m_NoteData.IsDefault();
    }

    private void UpdateDate()
    {
        if (m_HierarchyData != null)
            m_HierarchyData.SetHierarchyItem(m_GameObject, this);
    }
#endif
}

[Serializable]
public class SelectionList
{
#if UNITY_EDITOR
    [Serializable]
    private class GameObjectList
    {
        public List<GameObject> gameObjects = new List<GameObject>();
    }

    public const int kListMax = 10;

    public List<GameObject> this[int index] { get { return m_List[index].gameObjects; } set { m_List[index].gameObjects = value; } }

    [SerializeField]
    private List<GameObjectList> m_List;

    public SelectionList()
    {
        m_List = Enumerable.Range(0, kListMax).Select(i => new GameObjectList()).ToList();
    }
#endif
}

[Serializable]
public class RecentGameObjectItem
{
#if UNITY_EDITOR
    public GameObject gameObject;
    public long timestamp;
#endif
}

[ExecuteInEditMode]
[DisallowMultipleComponent]
public class HierarchyData : MonoBehaviour
{
#if UNITY_EDITOR
    public ReadOnlyCollection<HierarchyItem> items { get { return m_Items.AsReadOnly(); } }
    public SelectionList selectionList { get { return m_SelectionList; } }
    public List<RecentGameObjectItem> recentGameObjects { get { return m_RecentGameObjects; } }

    [SerializeField]
    private List<HierarchyItem> m_Items = new List<HierarchyItem>();
    [SerializeField]
    private SelectionList m_SelectionList = new SelectionList();
    [SerializeField]
    private List<RecentGameObjectItem> m_RecentGameObjects = new List<RecentGameObjectItem>();

    private Dictionary<GameObject, HierarchyItem> m_ItemTable = new Dictionary<GameObject, HierarchyItem>();

    void OnEnable()
    {
        ReloadItemTable();
    }

    private void ReloadItemTable()
    {
        m_Items = m_Items.Where(i => i.gameObject != null && !i.IsDefault()).ToList();
        m_ItemTable = m_Items.ToDictionary(i => i.gameObject, i => i);
    }

    public HierarchyItem GetHierarchyItem(GameObject go)
    {
        if (!m_ItemTable.ContainsKey(go)) return new HierarchyItem(go, this);
        return m_ItemTable[go];
    }

    public void SetHierarchyItem(GameObject go, HierarchyItem item)
    {
        if (item.IsDefault())
        {
            if (m_ItemTable.ContainsKey(go))
                m_Items.Remove(m_ItemTable[go]);
            m_ItemTable.Remove(go);
        }
        else
        {
            if (!m_ItemTable.ContainsKey(go))
                m_Items.Add(item);
            m_ItemTable[go] = item;
        }
    }
#endif
}