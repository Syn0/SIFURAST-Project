using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using UnityEditor;
using UnityEngine;

namespace HierarchyPlus
{
    public static class TreeViewInject
    {
        public static EditorWindow hierarchyWindow { get { return s_HierarchyWindow; } }

        private static bool s_Init = false;
        private static EditorWindow s_HierarchyWindow;

        private static Func<object> SceneHierarchyWindow_LastInteractedHierarchyWindow;
        private static Action<int, bool> SceneHierarchyWindow_SetExpandedRecursive;
        private static Func<bool> TreeView_HasFocus;
        private static Func<int, int> TreeView_GetLastChildRowUnder;
        private static Func<object, bool> TreeView_IsItemDragSelectedOrSelected;
        private static Func<int> TreeViewData_RowCount;
        private static Func<int, int> TreeViewData_GetRow;
        private static Func<int, object> TreeViewData_GetItem;
        private static Func<int, bool> TreeViewData_IsExpanded;
        private static Func<object, bool> TreeViewData_IsExpandable;
        private static Func<int, bool> TreeViewGUI_IsRenaming;
        private static Func<bool> SearchableEditorWindow_HasSearchFilter;

        private delegate void GetFirstAndLastRowVisible_Delegate(out int firstRowVisible, out int lastRowVisible);
        private static GetFirstAndLastRowVisible_Delegate TreeViewGUI_GetFirstAndLastRowVisible;
        private static Func<object, object> GameObjectTreeViewItem_objectPPTR;

        private static object TreeViewInjectGUI;
        private static object GameObjectTreeViewGUI;
        private static Action GameObjectTreeViewGUI_BeginRowGUI;
        private static Action GameObjectTreeViewGUI_EndRowGUI;

        public static void Init()
        {
            if (s_Init) return;
            s_Init = true;

            var windows = Resources.FindObjectsOfTypeAll(typeof(Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow"));
            if (windows.Length <= 0) return;
            s_HierarchyWindow = windows[0] as EditorWindow;

            InitDelegate();
            InjectGUI();
        }

        private static void InitDelegate()
        {
            var bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            var type = typeof(Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
            var field = type.GetField("s_LastInteractedHierarchy", bf);
            SceneHierarchyWindow_LastInteractedHierarchyWindow = Getter(field);
            var method = type.GetMethod("SetExpandedRecursive", bf);
            SceneHierarchyWindow_SetExpandedRecursive = (Action<int, bool>)Delegate.CreateDelegate(typeof(Action<int, bool>), s_HierarchyWindow, method, false);

            var treeview = type.GetField("m_TreeView", bf).GetValue(s_HierarchyWindow);
            method = treeview.GetType().GetMethod("HasFocus", bf);
            TreeView_HasFocus = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), treeview, method, false);
            method = treeview.GetType().GetMethod("GetLastChildRowUnder", bf);
            TreeView_GetLastChildRowUnder = (Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), treeview, method, false);

            method = treeview.GetType().GetMethod("IsItemDragSelectedOrSelected", bf);
            var obj = Expression.Parameter(typeof(object), "obj");
            var item = Expression.Convert(obj, method.GetParameters().First().ParameterType);
            var call = Expression.Call(Expression.Constant(treeview), method, item);
            var lambda = Expression.Lambda<Func<object, bool>>(call, obj);
            TreeView_IsItemDragSelectedOrSelected = lambda.Compile();

            var data = treeview.GetType().GetProperty("data", bf).GetValue(treeview, null);
            method = data.GetType().GetProperty("rowCount", bf).GetGetMethod();
            TreeViewData_RowCount = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), data, method, false);
            method = data.GetType().GetMethod("GetRow", bf);
            TreeViewData_GetRow = (Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), data, method, false);
            method = data.GetType().GetMethod("GetItem", bf);
            TreeViewData_GetItem = (Func<int, object>)Delegate.CreateDelegate(typeof(Func<int, object>), data, method, false);
            method = data.GetType().GetMethod("IsExpanded", new Type[] { typeof(int) });
            TreeViewData_IsExpanded = (Func<int, bool>)Delegate.CreateDelegate(typeof(Func<int, bool>), data, method, false);
            method = data.GetType().GetMethod("IsExpandable", bf);
            var call2 = Expression.Call(Expression.Constant(data), method, item);
            var lambda2 = Expression.Lambda<Func<object, bool>>(call2, obj);
            TreeViewData_IsExpandable = lambda2.Compile();

            var gui = treeview.GetType().GetProperty("gui", bf).GetValue(treeview, null);
            method = gui.GetType().GetMethod("IsRenaming", bf);
            TreeViewGUI_IsRenaming = (Func<int, bool>)Delegate.CreateDelegate(typeof(Func<int, bool>), gui, method, false);
            method = gui.GetType().GetMethod("GetFirstAndLastRowVisible", bf);
            TreeViewGUI_GetFirstAndLastRowVisible = (GetFirstAndLastRowVisible_Delegate)Delegate.CreateDelegate(typeof(GetFirstAndLastRowVisible_Delegate), gui, method, false);

            type = typeof(Editor).Assembly.GetType("UnityEditor.SearchableEditorWindow");
            method = type.GetProperty("hasSearchFilter", bf).GetGetMethod(true);
            SearchableEditorWindow_HasSearchFilter = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), s_HierarchyWindow, method, false);

            type = typeof(Editor).Assembly.GetType("UnityEditor.GameObjectTreeViewItem");
            method = type.GetProperty("objectPPTR", bf).GetGetMethod(true);
            var item3 = Expression.Convert(obj, type);
            var call3 = Expression.Call(item3, method);
            var lambda3 = Expression.Lambda<Func<object, object>>(call3, obj);
            GameObjectTreeViewItem_objectPPTR = lambda3.Compile();
        }

        private static void InjectGUI()
        {
            var assembly = typeof(Editor).Assembly;
            var type1 = assembly.GetType("UnityEditor.SceneHierarchyWindow");

            var bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var treeview = type1.GetField("m_TreeView", bf).GetValue(s_HierarchyWindow);
            GameObjectTreeViewGUI = treeview.GetType().GetProperty("gui", bf).GetValue(treeview, null);

            //var type2 = assembly.GetType("UnityEditor.GameObjectTreeViewGUI");
            var type2 = GameObjectTreeViewGUI.GetType();
            var m1 = type2.GetMethod("BeginRowGUI");
            var m2 = type2.GetMethod("EndRowGUI");

            var ab = AppDomain.CurrentDomain.DefineDynamicAssembly(assembly.GetName(), AssemblyBuilderAccess.Run);
            var mb = ab.DefineDynamicModule(assembly.GetName().Name);
            var tb = mb.DefineType("TreeViewInjectGUI", TypeAttributes.Public, type2);

            var cb = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(object) });
            var il = cb.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, typeof(TreeViewInject).GetMethod("TreeViewInject_ctor", bf));
            il.Emit(OpCodes.Ret);

            var ma = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final;
            var mb1 = tb.DefineMethod("BeginRowGUI", ma, CallingConventions.HasThis, null, Type.EmptyTypes);
            var mb2 = tb.DefineMethod("EndRowGUI", ma, CallingConventions.HasThis, null, Type.EmptyTypes);

            var il1 = mb1.GetILGenerator();
            il1.Emit(OpCodes.Call, typeof(TreeViewInject).GetMethod("BeginRowGUI", bf));
            il1.Emit(OpCodes.Ret);
            var il2 = mb2.GetILGenerator();
            il2.Emit(OpCodes.Call, typeof(TreeViewInject).GetMethod("EndRowGUI", bf));
            il2.Emit(OpCodes.Ret);

            tb.DefineMethodOverride(mb1, m1);
            tb.DefineMethodOverride(mb2, m2);
            var type3 = tb.CreateType();
            TreeViewInjectGUI = Activator.CreateInstance(type3, new object[] { treeview });
            treeview.GetType().GetProperty("gui", bf).SetValue(treeview, TreeViewInjectGUI, null);

            var dm1 = new DynamicMethod("BaseBeginRowGUI", null, new Type[] { type3 }, type3);
            il1 = dm1.GetILGenerator();
            il1.Emit(OpCodes.Ldarg_0);
            il1.Emit(OpCodes.Call, m1);
            il1.Emit(OpCodes.Ret);
            GameObjectTreeViewGUI_BeginRowGUI = (Action)dm1.CreateDelegate(typeof(Action), TreeViewInjectGUI);

            var dm2 = new DynamicMethod("BaseEndRowGUI", null, new Type[] { type3 }, type3);
            il2 = dm2.GetILGenerator();
            il2.Emit(OpCodes.Ldarg_0);
            il2.Emit(OpCodes.Call, m2);
            il2.Emit(OpCodes.Ret);
            GameObjectTreeViewGUI_EndRowGUI = (Action)dm2.CreateDelegate(typeof(Action), TreeViewInjectGUI);
        }

        public static Func<object> Getter(FieldInfo field)
        {
            var name = field.ReflectedType.FullName + ".get_" + field.Name;
            var method = new DynamicMethod(name, typeof(object), new Type[] { }, field.Module, true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldsfld, field);
            il.Emit(OpCodes.Castclass, field.FieldType);
            il.Emit(OpCodes.Ret);
            return (Func<object>)method.CreateDelegate(typeof(Func<object>));
        }

        public static void TreeViewInject_ctor(object self, object treeview)
        {
            var type = typeof(Editor).Assembly.GetType("UnityEditor.GameObjectTreeViewGUI");
            var constructor = type.GetConstructor(new[] { treeview.GetType(), typeof(bool) });
            constructor.Invoke(self, new object[] { treeview, false });
        }

        public static EditorWindow LastInteractedHierarchyWindow()
        {
            return SceneHierarchyWindow_LastInteractedHierarchyWindow() as EditorWindow;
        }

        public static void BeginRowGUI()
        {
            Topbar.HandleEvent(s_HierarchyWindow.position);
            GameObjectTreeViewGUI_BeginRowGUI.Invoke();
        }

        public static void EndRowGUI()
        {
            GameObjectTreeViewGUI_EndRowGUI.Invoke();
            Topbar.Draw(s_HierarchyWindow.position);
        }

        public static int GetRowCount()
        {
            return TreeViewData_RowCount();
        }

        public static int GetRow(GameObject go)
        {
            return TreeViewData_GetRow(go.GetInstanceID());
        }

        public static GameObject GetGameObject(int row)
        {
            if (row < 0 || row >= GetRowCount()) return null;
            var item = TreeViewData_GetItem(row);
            return GameObjectTreeViewItem_objectPPTR(item) as GameObject;
        }

        public static int GetLastRowVisible()
        {
            int firstRowVisible, lastRowVisible;
            TreeViewGUI_GetFirstAndLastRowVisible(out firstRowVisible, out lastRowVisible);
            return lastRowVisible;
        }

        public static int GetLastChildRowUnder(int row)
        {
            return TreeView_GetLastChildRowUnder(row);
        }

        public static bool IsLastChild(GameObject go)
        {
            var row1 = TreeViewData_GetRow(go.transform.parent.gameObject.GetInstanceID());
            var row2 = TreeViewData_GetRow(go.GetInstanceID());
            if (row1 < 0 || row1 >= GetRowCount()) return false;
            if (row2 < 0 || row2 >= GetRowCount()) return false;
            var row3 = TreeView_GetLastChildRowUnder(row1);
            var row4 = TreeView_GetLastChildRowUnder(row2);
            return row3 == row4;
        }

        public static int GetDistanceToPrefabParent(GameObject go, GameObject parent)
        {
            var row1 = TreeViewData_GetRow(parent.GetInstanceID());
            var row2 = TreeViewData_GetRow(go.GetInstanceID());
            var row3 = TreeView_GetLastChildRowUnder(row1);
            if (row1 < 0 || row1 >= GetRowCount()) return -1;
            if (row2 < 0 || row2 >= GetRowCount()) return -1;
            if (row3 < 0 || row3 >= GetRowCount()) return -1;
            if (row2 != row3) return -1;
            return row2 - row1;
        }

        public static bool IsItemSelected(GameObject go)
        {
            var row = TreeViewData_GetRow(go.GetInstanceID());
            if (row < 0 || row >= GetRowCount()) return false;
            var item = TreeViewData_GetItem(row);
            return TreeView_IsItemDragSelectedOrSelected(item);
        }

        public static bool HasSearchFilter()
        {
            return SearchableEditorWindow_HasSearchFilter();
        }

        public static bool HasFocus()
        {
            return TreeView_HasFocus();
        }

        public static bool IsRenaming(GameObject go)
        {
            return TreeViewGUI_IsRenaming(go.GetInstanceID());
        }

        public static bool IsExpandable(GameObject go)
        {
            var row = TreeViewData_GetRow(go.GetInstanceID());
            if (row < 0 || row >= GetRowCount()) return false;
            var item = TreeViewData_GetItem(row);
            return TreeViewData_IsExpandable(item);
        }

        public static bool IsExpanded(GameObject go)
        {
            return TreeViewData_IsExpanded(go.GetInstanceID());
        }

        public static void SetExpandedRecursive(GameObject go, bool expand)
        {
            SceneHierarchyWindow_SetExpandedRecursive(go.GetInstanceID(), expand);
        }
    }
}