using HierarchyPlus;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HierarchyData))]
public class HDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("This GameObject is auto created and managed by Hierarchy Plus. " +
                                "It stores extra data of GameObjects in the current scene. " +
                                "This object will not be included in the final build."
                                , MessageType.Info, true);
        GUILayout.Space(10);
        if (GUILayout.Button((Prefs.doHideInHierarchy ? "Show" : "Hide") + " this GameObject", GUILayout.ExpandWidth(true), GUILayout.Height(20)))
        {
            Prefs.doHideInHierarchy = !Prefs.doHideInHierarchy;
            Prefs.SaveAllPrefs(prefix: "do");
            DataObject.CheckDataObject();
        }
        EditorGUILayout.HelpBox("This GameObject will not appear in the hierarchy after hide. " +
                                "If you want to show it again, you need to change in settings."
                                , MessageType.Info, true);
        GUILayout.Space(10);
        if (GUILayout.Button("Remove and Disable this GameObject", GUILayout.ExpandWidth(true), GUILayout.Height(20)))
        {
            Prefs.doHierarchyDataObject = false;
            Prefs.SaveAllPrefs(prefix: "do");
            Selection.activeGameObject = null;
            DataObject.CheckDataObject();
        }
        EditorGUILayout.HelpBox("You can remove and disable this GameObject, Hierarchy Plus will not re-create it again. " +
                                "However, all functions dependent on this GameObject will not work."
                                , MessageType.Info, true);
        GUILayout.Space(10);
        if (Prefs.doShowContent)
        {
            if (GUILayout.Button("Hide Content", GUILayout.ExpandWidth(true), GUILayout.Height(20)))
            {
                Prefs.doShowContent = false;
                Prefs.SaveAllPrefs(prefix: "do");
            }
            base.OnInspectorGUI();
        }
        else
        {
            if (GUILayout.Button("Show Content", GUILayout.ExpandWidth(true), GUILayout.Height(20)))
            {
                Prefs.doShowContent = true;
                Prefs.SaveAllPrefs(prefix: "do");
            }
        }
    }
}
