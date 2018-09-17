using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HierarchyPlus
{
    public enum EntryMode
    {
        MissingReference = 1,
        Error = 256,
        Warning = 512,
        Log = 1024,
        LogEntryMode = Log | Warning | Error
    }

    [InitializeOnLoad]
    public static class LogHelper
    {
        private static readonly Type kLogEntriesType;
        private static readonly MethodInfo kGetEntryMethod;
        private static readonly MethodInfo kStartMethod;
        private static readonly MethodInfo kEndMethod;

        private static readonly Type kLogEntryType;
        private static readonly ConstructorInfo kLogEntryConstructor;
        private static readonly FieldInfo KInstanceIDField;
        private static readonly FieldInfo kModeField;
        private static readonly FieldInfo kConditionField;
        private static readonly FieldInfo kFileField;

        private static bool s_MethodReady = false;
        private static bool s_LogMessageReceived = true;

        private static Dictionary<GameObject, List<LogEntry>> s_LogEntryTable = new Dictionary<GameObject, List<LogEntry>>();
        private static Dictionary<GameObject, List<LogEntry>> s_MissingTable = new Dictionary<GameObject, List<LogEntry>>();
        private static Dictionary<GameObject, HashSet<Component>> s_MissingProperty = new Dictionary<GameObject, HashSet<Component>>();        

        private interface ILogEntry
        {
            GameObject GameObject { get; set; }
            Component Component { get; set; }
            int InstanceID { get; set; }
            EntryMode Mode { get; set; }
            string Condition { get; set; }
            string File { get; set; }
        }

        public class LogEntry : ILogEntry
        {
            public GameObject GameObject { get { return _GameObject; } }
            public Component Component { get { return _Component; } }
            public int InstanceID { get { return _InstanceID; } }
            public EntryMode Mode { get { return _Mode; } }
            public string Condition { get { return _Condition; } }
            public string File { get { return _File; } }

            GameObject ILogEntry.GameObject { get { return _GameObject; } set { _GameObject = value; } }
            Component ILogEntry.Component { get { return _Component; } set { _Component = value; } }
            int ILogEntry.InstanceID { get { return _InstanceID; } set { _InstanceID = value; } }
            EntryMode ILogEntry.Mode { get { return _Mode; } set { _Mode = value; } }
            string ILogEntry.Condition { get { return _Condition; } set { _Condition = value; } }
            string ILogEntry.File { get { return _File; } set { _File = value; } }

            private GameObject _GameObject;
            private Component _Component;
            private int _InstanceID;
            private EntryMode _Mode;
            private string _Condition;
            private string _File;
        }

        static LogHelper()
        {
            var binding = BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance;
            kLogEntriesType = typeof(EditorWindow).Assembly.GetType("UnityEditorInternal.LogEntries");
            if (kLogEntriesType == null) kLogEntriesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries");
            if (kLogEntriesType != null)
            {
                kGetEntryMethod = kLogEntriesType.GetMethod("GetEntryInternal", binding);
                kStartMethod = kLogEntriesType.GetMethod("StartGettingEntries", binding);
                kEndMethod = kLogEntriesType.GetMethod("EndGettingEntries", binding);
            }

            kLogEntryType = typeof(EditorWindow).Assembly.GetType("UnityEditorInternal.LogEntry");
            if (kLogEntryType == null) kLogEntryType = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntry");
            if (kLogEntryType != null)
            {
                kLogEntryConstructor = kLogEntryType.GetConstructor(new Type[0]);
                KInstanceIDField = kLogEntryType.GetField("instanceID");
                kModeField = kLogEntryType.GetField("mode");
                kConditionField = kLogEntryType.GetField("condition");
                kFileField = kLogEntryType.GetField("file");
            }

            s_MethodReady = true;
            if (kGetEntryMethod == null) s_MethodReady = false;
            if (kStartMethod == null) s_MethodReady = false;
            if (kEndMethod == null) s_MethodReady = false;
            if (kLogEntryConstructor == null) s_MethodReady = false;
            if (KInstanceIDField == null) s_MethodReady = false;
            if (kModeField == null) s_MethodReady = false;
            if (kConditionField == null) s_MethodReady = false;
            if (kFileField == null) s_MethodReady = false;

            Application.logMessageReceivedThreaded += LogMessageReceivedThreaded;
            EditorApplication.update += ReloadLogEntry;
#if UNITY_2018_1_OR_NEWER
            EditorApplication.hierarchyChanged += HierarchyWindowChanged;
            EditorApplication.projectChanged += ProjectWindowChanged;
#else
            EditorApplication.hierarchyWindowChanged += HierarchyWindowChanged;
            EditorApplication.projectWindowChanged += ProjectWindowChanged;
#endif
        }

        private static void LogMessageReceivedThreaded(string condition, string stackTrace, LogType type)
        {
            s_LogMessageReceived = true;
        }

        private static void HierarchyWindowChanged()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            s_MissingTable.Clear();
            s_MissingProperty.Clear();
        }

        private static void ProjectWindowChanged()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            s_MissingTable.Clear();
            s_MissingProperty.Clear();
        }

        private static void ReloadLogEntry()
        {
            if (!Prefs.HierarchyItemDrawer) return;
            if (!Prefs.GetFunctionActive(FunctionType.Log)) return;
            if (!s_MethodReady) return;
            if (!s_LogMessageReceived) return;
            s_LogMessageReceived = false;

            s_LogEntryTable.Clear();

            var logEntry = kLogEntryConstructor.Invoke(null);
            var count = (int)kStartMethod.Invoke(null, null);

            for (int i = 0; i < count; i++)
            {
                kGetEntryMethod.Invoke(null, new object[] { i, logEntry });

                ILogEntry entry = new LogEntry();
                entry.InstanceID = (int)KInstanceIDField.GetValue(logEntry);
                entry.Mode = (EntryMode)kModeField.GetValue(logEntry) & EntryMode.LogEntryMode;
                entry.Condition = (string)kConditionField.GetValue(logEntry);
                entry.File = (string)kFileField.GetValue(logEntry);

                if ((entry.Mode & EntryMode.LogEntryMode) == 0)
                    continue;

                var obj = EditorUtility.InstanceIDToObject(entry.InstanceID);
                if (obj != null)
                {
                    if (obj is GameObject)
                    {
                        entry.Component = null;
                        entry.GameObject = obj as GameObject;
                    }
                    else if (obj is Component)
                    {
                        entry.Component = obj as Component;
                        entry.GameObject = entry.Component.gameObject;
                    }
                    else
                        continue;

                    if (s_LogEntryTable.ContainsKey(entry.GameObject))
                        s_LogEntryTable[entry.GameObject].Add((LogEntry)entry);
                    else
                        s_LogEntryTable.Add(entry.GameObject, new List<LogEntry> { (LogEntry)entry });
                }
            }
            kEndMethod.Invoke(null, null);
        }

        private static void SearchMissingReferenceInChildren(GameObject go)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            foreach (var child in go.GetChildrenList())
                SearchMissingReference(child);
        }

        private static void SearchMissingReference(GameObject go)
        {
            if (s_MissingProperty.ContainsKey(go))
                foreach (var comp in s_MissingProperty[go].Where(i => i != null))
                    if (!CheckReferenceInComponent(comp, (sp) => { }))
                        s_MissingTable.Remove(go);
            if (s_MissingTable.ContainsKey(go)) return;
            s_MissingProperty.Remove(go);

            var list = new List<LogEntry>();
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null)
                {
                    ILogEntry entry = new LogEntry();
                    entry.InstanceID = go.GetInstanceID();
                    entry.Mode = EntryMode.MissingReference;
                    entry.Condition = string.Format("Missing Component in {0}.", go.name);
                    entry.File = string.Empty;
                    list.Add((LogEntry)entry);
                    continue;
                }

                if (c is Transform) continue;

                CheckReferenceInComponent(c, (sp) =>
                {
                    ILogEntry entry = new LogEntry();
                    entry.InstanceID = go.GetInstanceID();
                    entry.Mode = EntryMode.MissingReference;
                    entry.Condition = string.Format("Missing Reference {1} in {0}.", ObjectNames.NicifyVariableName(c.GetType().Name), sp.displayName);
                    entry.File = AssetDatabase.GetAssetPath(c);
                    list.Add((LogEntry)entry);
                    if (!s_MissingProperty.ContainsKey(go))
                        s_MissingProperty[go] = new HashSet<Component>();
                    s_MissingProperty[go].Add(c);
                });
            }
            s_MissingTable[go] = list;
        }

        private static bool CheckReferenceInComponent(Component comp, Action<SerializedProperty> action)
        {
            bool missing = false;
            SerializedObject so = new SerializedObject(comp);
            var sp = so.GetIterator();
            while (sp.NextVisible(true))
            {
                if (sp.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (sp.objectReferenceValue == null && sp.objectReferenceInstanceIDValue != 0)
                {
                    missing = true;
                    action(sp);
                }
            }
            return missing;
        }

        public static List<LogEntry> GetLogEntry(GameObject go)
        {
            var log = s_LogEntryTable.ContainsKey(go) ? s_LogEntryTable[go] : new List<LogEntry>();
            SearchMissingReferenceInChildren(go);
            if (s_MissingTable.ContainsKey(go))
                log.AddRange(s_MissingTable[go]);
            return log;
        }
    }
}