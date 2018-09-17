using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace HierarchyPlus
{
    [AttributeUsage(AttributeTargets.Field)]
    public class PrefsKeyAttribute : Attribute
    {
        public bool Enabled;
        public int Integer;
        public string String;
        public string Color;

        public string Group;
        public bool Indent = false;
        public Type EnumType = null;
        public bool Slider = false;
        public int Min = 0;
        public int Max = 1;
        public int TextLine = 1;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class PrefsDescriptionAttribute : Attribute
    {
        public string Text;
        public string Tooltip;
        public string Helpbox;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class FunctionTypeAttribute : Attribute
    {
        public string Label;
        public string Image;
        public bool Enable = true;
        public bool Default = false;
        public Type Function;
        public FunctionTypeAttribute(string label) { Label = label; }
    }

    public enum HighlightPosition { None, All, Start, Foldout, Label }

    public enum TagLayerOption { Tag, Layer, TagOrLayer, LayerOrTag, Both }

    public enum TagLayerLabelOption { Orginal, Initial, Capital, Abbreviation, HeadTail, MultiHeadTail }

    public enum FunctionType
    {
        [FunctionType("All-in-One Button", Function = typeof(AllInOneButton))]
        AllInOne,
        [FunctionType("Activate", Enable = false, Default = true, Function = typeof(GeneralButton))]
        Active,
        [FunctionType("Selectable", Image = "select2", Default = true, Function = typeof(GeneralButton))]
        Selectable,
        [FunctionType("Non-Editable", Enable = false, Image = "lock", Function = typeof(GeneralButton))]
        Lock,
        [FunctionType("Favorite", Image = "star", Function = typeof(GeneralButton))]
        Favorite,
        [FunctionType("Selection Base", Enable = false, Image = "hand", Function = typeof(GeneralButton))]
        SelectionBase,
        [FunctionType("Hierarchy Base", Enable = false, Image = "tree", Function = typeof(GeneralButton))]
        HierarchyBase,
        [FunctionType("Persistent Data", Enable = false, Image = "save", Function = typeof(GeneralButton))]
        PersistentData,
        [FunctionType("GameObject Note", Enable = false, Function = typeof(GameObjectNote))]
        Note,
        [FunctionType("Static", Enable = false, Image = "cube", Function = typeof(StaticButton))]
        Static,
        [FunctionType("Prefab", Function = typeof(PrefabButton))]
        Prefab,
        [FunctionType("Tag and Layer", Function = typeof(TagLayerLabel))]
        TagLayer,
        [FunctionType("Tag and Layer (2)", Enable = false, Function = typeof(TagLayerLabel2))]
        TagLayer2,
        [FunctionType("Sorting Layer", Function = typeof(SortingLayerLabel))]
        SortingLayer,
        [FunctionType("GameObject Icon", Function = typeof(GameObjectIcon))]
        Icon,
        [FunctionType("Non-Toggleable Component", Enable = false, Function = typeof(ComponentButton))]
        NonToggleable,
        [FunctionType("Toggleable Component", Function = typeof(ComponentButton))]
        Toggleable,
        [FunctionType("UI Component", Function = typeof(ComponentButton))]
        UI,
        [FunctionType("Script Component", Function = typeof(ComponentButton))]
        Script,
        [FunctionType("Image Effect Component", Function = typeof(ComponentButton))]
        ImageEffect,
        [FunctionType("Log", Function = typeof(LogIcon))]
        Log,
        [FunctionType("Separator", Function = typeof(Separator))]
        Separator,
    }

    public class FunctionTypeItem
    {
        public bool active { get { return EditorApplication.isPlayingOrWillChangePlaymode ? PlayMode : Enabled; } }
        public FunctionType Type;
        public bool Enabled;
        public bool PlayMode;
        public bool Foldout;
        public SeparatorConfig SeparatorConfig = new SeparatorConfig();
    }

    public class SeparatorConfig
    {
        public bool Show = true;
        public bool Movable = true;
        public int Offset = 16;
    }

    public class PrefsItem
    {
        public FieldInfo Field;
        public PrefsKeyAttribute Key;
        public PrefsDescriptionAttribute Desc;
    }

    [InitializeOnLoad]
    public static class Prefs
    {
        public static bool SceneViewHighlighter
        {
            get { return _SceneViewHighlighter; }
            set { _SceneViewHighlighter = value; EditorPrefs.SetBool("HierarchyPlus.sceneViewHighlighter", value); SaveAllPrefs(); }
        }

        public static bool HierarchyItemDrawer
        {
            get { return _HierarchyItemDrawer; }
            set { _HierarchyItemDrawer = value; EditorPrefs.SetBool("HierarchyPlus.hierarchyItemDrawer", value); SaveAllPrefs(); }
        }

        public static bool HierarchyTopbar
        {
            get { return _HierarchyTopbar; }
            set { _HierarchyTopbar = value; EditorPrefs.SetBool("HierarchyPlus.hierarchyTopbar", value); SaveAllPrefs(); }
        }

        public static bool FunctionButton
        {
            get { return _FunctionButtonEnable; }
            set { _FunctionButtonEnable = value; EditorPrefs.SetBool("HierarchyPlus.FunctionButtonEnable", value); SaveAllPrefs(); }
        }

        [PrefsKey(Enabled = true)]
        private static bool _SceneViewHighlighter;
        [PrefsKey(Enabled = true)]
        private static bool _HierarchyItemDrawer;
        [PrefsKey(Enabled = true)]
        private static bool _HierarchyTopbar;
        [PrefsKey(Enabled = true)]
        private static bool _FunctionButtonEnable;
        [PrefsKey(Enabled = true)]
        public static bool doHierarchyDataObject;
        #region Data Object
        [PrefsKey(Group = "HierarchyDataObject", Enabled = false)]
        [PrefsDescription(Text = "Hide in Hierarchy", Tooltip = "")]
        public static bool doHideInHierarchy;
        [PrefsKey(Group = "HierarchyDataObject", Enabled = false)]
        [PrefsDescription(Text = "Show Content in Inspector", Tooltip = "")]
        public static bool doShowContent;
        [PrefsKey(Group = "HierarchyDataObject", Enabled = false)]
        [PrefsDescription(Text = "Hide Warning", Tooltip = "")]
        public static bool doHideWarning;
        #endregion
        #region SceneView Highlighter
        [PrefsKey(Group = "SceneViewHighlighter", Enabled = true)]
        [PrefsDescription(Text = "Hover in Hierarchy", Tooltip = "")]
        public static bool svOutlineHierarchy;
        [PrefsKey(Group = "SceneViewHighlighter", Integer = (int)ModifierKey.None, Indent = true, EnumType = typeof(ModifierKey))]
        [PrefsDescription(Text = "Modifier", Tooltip = "")]
        public static int svOutlineHierarchyModifier;
        [PrefsKey(Group = "SceneViewHighlighter", Enabled = true)]
        [PrefsDescription(Text = "Hover in SceneView", Tooltip = "")]
        public static bool svOutlineSceneView;
        [PrefsKey(Group = "SceneViewHighlighter", Integer = (int)ModifierKey.Ctrl, Indent = true, EnumType = typeof(ModifierKey))]
        [PrefsDescription(Text = "Modifier", Tooltip = "")]
        public static int svOutlineSceneViewModifier;
        [PrefsKey(Group = "SceneViewHighlighter", Enabled = true, Indent = true)]
        [PrefsDescription(Text = "Show Name (Non-Renderer)", Tooltip = "")]
        public static bool svOutlineSceneViewName;
        [PrefsKey(Group = "SceneViewHighlighter", Enabled = true, Indent = true)]
        [PrefsDescription(Text = "Line to Pivot (Non-Renderer)", Tooltip = "")]
        public static bool svOutlineSceneViewLine;
        [PrefsKey(Group = "SceneViewHighlighter", Enabled = true)]
        [PrefsDescription(Text = "Outline Renderer", Tooltip = "Outline the renderer in Scene View when mouse hover the item.")]
        public static bool svOutline;
        [PrefsKey(Group = "SceneViewHighlighter", Color = "0,0,255,50", Indent = true)]
        [PrefsDescription(Text = "Color", Tooltip = "Outline Color")]
        public static Color svOutlineColor;
        [PrefsKey(Group = "SceneViewHighlighter", Integer = 20, Indent = true, Min = 0)]
        [PrefsDescription(Text = "Children Level", Tooltip = "Also outline children of the game object.")]
        public static int svOutlineLevel;
        [PrefsKey(Group = "SceneViewHighlighter", Integer = 500, Indent = true, Min = 1)]
        [PrefsDescription(Text = "Max Renderer", Tooltip = "Max renderer to outline.")]
        public static int svOutlineMax;
        [PrefsKey(Group = "SceneViewHighlighter", Enabled = true)]
        [PrefsDescription(Text = "Outline Selected Sprites", Tooltip = "")]
        public static bool svOutlineSelectedSprites;
        [PrefsKey(Group = "SceneViewHighlighter", Color = "255,102,0,12", Indent = true)]
        [PrefsDescription(Text = "Color", Tooltip = "Selected Sprites Outline Color")]
        public static Color svOutlineSelectedSpritesColor;
        [PrefsKey(Group = "SceneViewHighlighter", Enabled = false)]
        [PrefsDescription(Text = "Outline Buffer Half Size", Tooltip = "Increase speed but decrease quality")]
        public static bool svOutlineHalfSizeBuffer;
        [PrefsKey(Group = "SceneViewHighlighter", Enabled = true)]
        [PrefsDescription(Text = "Indicator", Tooltip = "Draw indicator in Scene View when mouse hover the item.")]
        public static bool svIndicator;
        [PrefsKey(Group = "SceneViewHighlighter", Color = "0,0,255,255", Indent = true)]
        [PrefsDescription(Text = "Color", Tooltip = "Indicator Color")]
        public static Color svIndicatorColor;
        [PrefsKey(Group = "SceneViewHighlighter", Enabled = true)]
        [PrefsDescription(Text = "RectTransform Overlay", Tooltip = "Draw Overlay in Scene View when mouse hover the RectTransform.")]
        public static bool svRectTransform;
        [PrefsKey(Group = "SceneViewHighlighter", Color = "0,0,255,50", Indent = true)]
        [PrefsDescription(Text = "Color", Tooltip = "RectTransform Overlay Color")]
        public static Color svRectTransformOverlayColor;
        [PrefsKey(Group = "SceneViewHighlighter", Enabled = false)]
        [PrefsDescription(Text = "Hide Warning", Tooltip = "")]
        public static bool svHideWarning;
        #endregion
        #region Topbar
        [PrefsKey(Group = "HierarchyTopbar", Color = "0,255,255,200")]
        [PrefsDescription(Text = "Color", Tooltip = "Topbar Color")]
        public static Color svTopbarColor;
        [PrefsKey(Group = "HierarchyTopbar", Integer = 36)]
        [PrefsDescription(Text = "Right Margin", Tooltip = "")]
        public static int tbRightMargin;
        [PrefsKey(Group = "HierarchyTopbar", Enabled = true)]
        [PrefsDescription(Text = "Set Dirty on Recent Changed", Tooltip = "Set the scene dirty if recent game object list changed.")]
        public static bool tbRecentDirty;
        #endregion
        #region Hierarchy Item Drawer
        [PrefsKey(Group = "HierarchyItemDrawer", Enabled = true)]
        [PrefsDescription(Text = "Row Shading", Tooltip = "Different color for odd and even row.")]
        public static bool hRowShading;
        [PrefsKey(Group = "HierarchyItemDrawer", Enabled = true)]
        [PrefsDescription(Text = "Separator", Tooltip = "Draw separator between each row.")]
        public static bool hSeparator;
        [PrefsKey(Group = "HierarchyItemDrawer", Enabled = true)]
        [PrefsDescription(Text = "Draw Tree", Tooltip = "Draw line between parent and children.")]
        public static bool hDrawTree;
        [PrefsKey(Group = "HierarchyItemDrawer", Enabled = true)]
        [PrefsDescription(Text = "Draw Mask", Tooltip = "Draw mask on function button area.")]
        public static bool fbDrawMask;
        #endregion
        #region Batch Mode
        [PrefsKey(Group = "HierarchyItemDrawer", Integer = (int)BatchMode.Same, EnumType = typeof(BatchMode))]
        [PrefsDescription(Text = "Batch Mode", Tooltip = "Action mode for batch action.")]
        public static int hBatchMode;
        [PrefsKey(Group = "HierarchyItemDrawer", Integer = (int)ModifierKey.Ctrl, Indent = true, EnumType = typeof(ModifierKey))]
        [PrefsDescription(Text = "All Children Modifier", Tooltip = "Batch action for all children")]
        public static int hBatchModeChildren;
        [PrefsKey(Group = "HierarchyItemDrawer", Integer = (int)ModifierKey.Shift, Indent = true, EnumType = typeof(ModifierKey))]
        [PrefsDescription(Text = "All Selected Modifier", Tooltip = "Batch action for all selected")]
        public static int hBatchModeSelected;
        #endregion
        #region Customize
        [PrefsKey(Group = "HierarchyItemDrawer", Enabled = true)]
        [PrefsDescription(Text = "Customize", Tooltip = "Customize game object item in hierarchy.")]
        public static bool hCustomize;
        [PrefsKey(Group = "HierarchyItemDrawer", Integer = (int)KeyCode.C, EnumType = typeof(KeyCode), Indent = true)]
        [PrefsDescription(Text = "Hotkey", Tooltip = "Hotkey for quick Customize.")]
        public static int hCustomizeKey;
        #endregion
        #region Activate (Left)
        [PrefsKey(Group = "HierarchyItemDrawer", Enabled = true)]
        [PrefsDescription(Text = "Activate (Left)", Tooltip = "Toggle button to activate game object on the left.")]
        public static bool hLeftActive;
        [PrefsKey(Group = "HierarchyItemDrawer", Enabled = true, Indent = true)]
        [PrefsDescription(Text = "Draw Icon", Tooltip = "Draw icon as activate toggle button.")]
        public static bool hLeftActiveIcon;
        #endregion
        #region Children Count
        [PrefsKey(Group = "HierarchyItemDrawer", Enabled = true)]
        [PrefsDescription(Text = "Children Count", Tooltip = "Show children count of the game object.")]
        public static bool hChildrenCount;
        [PrefsKey(Group = "HierarchyItemDrawer", Enabled = false, Indent = true)]
        [PrefsDescription(Text = "Total", Tooltip = "Show total children count.")]
        public static bool hChildrenCountTotal;
        [PrefsKey(Group = "HierarchyItemDrawer", Integer = (int)ModifierKey.Ctrl, Indent = true, EnumType = typeof(ModifierKey))]
        [PrefsDescription(Text = "Modifier Key", Tooltip = "Switch between count and total count")]
        public static int hChildrenCountModifier;
        #endregion
        #region Level Highlighter
        [PrefsKey(Group = "HierarchyItemDrawer", Integer = (int)HighlightPosition.Foldout, EnumType = typeof(HighlightPosition))]
        [PrefsDescription(Text = "Level Highlighter", Tooltip = "Draw Highlight that show the level of hierarchy.")]
        public static int hLevelHighlighter;
        [PrefsKey(Group = "HierarchyItemDrawer", Integer = 12, Indent = true, Slider = true, Min = 1, Max = 50)]
        [PrefsDescription(Text = "Length", Tooltip = "The length of highlight.")]
        public static int hLevelHighlighterLength;
        [PrefsKey(Group = "HierarchyItemDrawer", Enabled = false, Indent = true)]
        [PrefsDescription(Text = "Draw on First Level", Tooltip = "")]
        public static bool hLevelHighlighterDrawFirstLevel;
        [PrefsKey(Group = "HierarchyItemDrawer", Integer = (int)HighlightPosition.Foldout)]
        public static int hLevelHighlighterLastMode;
        #endregion
        #region Back to Parent
        [PrefsKey(Group = "HierarchyItemDrawer", Enabled = true)]
        [PrefsDescription(Text = "Back to Parent", Tooltip = "Show button to collapse the parent of the game object.")]
        public static bool hBackToParent;
        [PrefsKey(Group = "HierarchyItemDrawer", Integer = (int)ModifierKey.Ctrl, Indent = true, EnumType = typeof(ModifierKey))]
        [PrefsDescription(Text = "Modifier Key", Tooltip = "Show if modifier key pressed.")]
        public static int hBackToParentModifier;
        [PrefsKey(Group = "HierarchyItemDrawer", Enabled = true, Indent = true)]
        [PrefsDescription(Text = "Modifier Key to Hide", Tooltip = "Hide if modifier key pressed.")]
        public static bool hBackToParentModifierHide;
        [PrefsKey(Group = "HierarchyItemDrawer", Enabled = true, Indent = true)]
        [PrefsDescription(Text = "Draw Dot", Tooltip = "Draw dot to fit tree line.")]
        public static bool hBackToParentDrawDot;
        #endregion
        #region Shadow Label
        [PrefsKey(Group = "HierarchyItemDrawer", Enabled = true)]
        [PrefsDescription(Text = "Shadow Label", Tooltip = "Show shadow label of the game object.")]
        public static bool hShadowLabel;
        [PrefsKey(Group = "HierarchyItemDrawer", Integer = (int)ModifierKey.Ctrl, Indent = true, EnumType = typeof(ModifierKey))]
        [PrefsDescription(Text = "Modifier Key", Tooltip = "Show if modifier key pressed.")]
        public static int hShadowLabelModifier;
        [PrefsKey(Group = "HierarchyItemDrawer", Enabled = false, Indent = true)]
        [PrefsDescription(Text = "Modifier Key to Hide", Tooltip = "Hide if modifier key pressed.")]
        public static bool hShadowLabelModifierHide;
        [PrefsKey(Group = "HierarchyItemDrawer", Integer = 4, Indent = true, Slider = true, Min = 1, Max = 20)]
        [PrefsDescription(Text = "Level of Hierarchy", Tooltip = "The level of hierarchy to show shadow label.")]
        public static int hShadowLabelLevel;
        [PrefsKey(Group = "HierarchyItemDrawer", Integer = 4, Indent = true, Slider = true, Min = 1, Max = 20)]
        [PrefsDescription(Text = "Indent Offset", Tooltip = "")]
        public static int hShadowLabelIndent;
        [PrefsKey(Group = "HierarchyItemDrawer", Integer = 8, Indent = true, Slider = true, Min = 0, Max = 50)]
        [PrefsDescription(Text = "Highlight Length", Tooltip = "The length of highlight.")]
        public static int hShadowLabelHighlighterLength;
        #endregion
        #region Component
        [PrefsKey(Group = "HierarchyItemDrawer", String = "Transform,RectTransform,GUILayer,FlareLayer,AudioListener", TextLine = 3)]
        [PrefsDescription(Text = "Exclude Component", Tooltip = "Do not show component on list.")]
        public static string hExcludeComponent;
        #endregion
        #region General Function
        [PrefsKey(Group = "FunctionButton.Active", Enabled = false)]
        [PrefsDescription(Text = "Hide if Default", Tooltip = "Hide if game object is active.")]
        public static bool fbActiveHideDefault;
        [PrefsKey(Group = "FunctionButton.Selectable", Enabled = false)]
        [PrefsDescription(Text = "Hide if Default", Tooltip = "Hide if game object is selectable.")]
        public static bool fbSelectableHideDefault;
        [PrefsKey(Group = "FunctionButton.Lock", Enabled = false)]
        [PrefsDescription(Text = "Hide if Default", Tooltip = "Hide if game object is not locked.")]
        public static bool fbLockHideDefault;
        [PrefsKey(Group = "FunctionButton.Favorite", Enabled = false)]
        [PrefsDescription(Text = "Hide if Default", Tooltip = "Hide if game object is not favorite.")]
        public static bool fbFavoriteHideDefault;
        [PrefsKey(Group = "FunctionButton.SelectionBase", Enabled = false)]
        [PrefsDescription(Text = "Hide if Default", Tooltip = "Hide if game object is not selection base.")]
        public static bool fbSelectionBaseHideDefault;
        [PrefsKey(Group = "FunctionButton.HierarchyBase", Enabled = false)]
        [PrefsDescription(Text = "Hide if Default", Tooltip = "Hide if game object is not hierarchy base.")]
        public static bool fbHierarchyBaseHideDefault;
        [PrefsKey(Group = "FunctionButton.PersistentData", Enabled = false)]
        [PrefsDescription(Text = "Hide if Default", Tooltip = "Hide if game object is not persistent data.")]
        public static bool fbPersistentDataHideDefault;
        #endregion
        #region Note
        [PrefsKey(Group = "FunctionButton.Note", Enabled = false)]
        [PrefsDescription(Text = "Hide if Empty", Tooltip = "Hide if game object note is empty.")]
        public static bool fbNoteHideDefault;
        [PrefsKey(Group = "FunctionButton.Note", Enabled = false)]
        [PrefsDescription(Text = "Show Title", Tooltip = "")]
        public static bool fbNoteShowText;
        [PrefsKey(Group = "FunctionButton.Note", Enabled = true)]
        [PrefsDescription(Text = "Auto Width", Tooltip = "")]
        public static bool fbNoteAutoWidth;
        [PrefsKey(Group = "FunctionButton.Note", Integer = 80)]
        [PrefsDescription(Text = "Text Max Width", Tooltip = "")]
        public static int fbNoteTextWidth;
        [PrefsKey(Group = "FunctionButton.Note", Enabled = true)]
        [PrefsDescription(Text = "Expand Width", Tooltip = "Expand Width to next separator")]
        public static bool fbNoteExpandWidth;
        #endregion
        #region Static
        [PrefsKey(Group = "FunctionButton.Static", Enabled = false)]
        [PrefsDescription(Text = "Hide if Default", Tooltip = "Hide if game object is not static.")]
        public static bool fbStaticHideDefault;
        #endregion
        #region Prefab
        [PrefsKey(Group = "FunctionButton.Prefab", Enabled = true)]
        [PrefsDescription(Text = "Hide if Default", Tooltip = "Hide if game object is not prefab.")]
        public static bool fbPrefabHideDefault;
        [PrefsKey(Group = "FunctionButton.Prefab", Enabled = true)]
        [PrefsDescription(Text = "Hide GameObject icon", Tooltip = "Hide game object icon.")]
        public static bool fbPrefabHideGameObject;
        [PrefsKey(Group = "FunctionButton.Prefab", Enabled = true, TextLine = 2)]
        [PrefsDescription(Text = "Group Outline", Tooltip = "Group all game objects from same prefab.", Helpbox = "Group outline requires disable (Hide if Default) and aligned by separator.")]
        public static bool fbPrefabGroupOutline;
        [PrefsKey(Group = "FunctionButton.Prefab", Enabled = false)]
        [PrefsDescription(Text = "Outline Single GameObject", Tooltip = "")]
        public static bool fbPrefabGroupOutlineSingle;
        [PrefsKey(Group = "FunctionButton.Prefab", String = "m_Name,m_IsActive,m_Enabled,m_Icon,m_Layer,m_TagString,m_RootOrder", TextLine = 3)]
        [PrefsDescription(Text = "Prefab Ignore Changed", Tooltip = "Ignore the change of property in Prefab.")]
        public static string fbPrefabIgnoreChanged;
        #endregion
        #region Tag Layer
        [PrefsKey(Group = "FunctionButton.TagLayer", Enabled = true)]
        [PrefsDescription(Text = "Hide if Default", Tooltip = "Hide if Tag/Layer is Untagged/Default.")]
        public static bool fbTagLayerHideDefault;
        [PrefsKey(Group = "FunctionButton.TagLayer", Enabled = false)]
        [PrefsDescription(Text = "Small Font", Tooltip = "Use small font.")]
        public static bool fbTagLayerSmallFont;
        [PrefsKey(Group = "FunctionButton.TagLayer", Integer = (int)TagLayerOption.TagOrLayer, EnumType = typeof(TagLayerOption))]
        [PrefsDescription(Text = "Show Tag or Layer", Tooltip = "")]
        public static int fbTagLayerOption;
        [PrefsKey(Group = "FunctionButton.TagLayer", Integer = (int)ModifierKey.Ctrl, EnumType = typeof(ModifierKey))]
        [PrefsDescription(Text = "Modifier Key to Invert", Tooltip = "Invert the visible of tag and layer.")]
        public static int fbTagLayerInvert;
        [PrefsKey(Group = "FunctionButton.TagLayer", Integer = (int)TagLayerLabelOption.Capital, EnumType = typeof(TagLayerLabelOption))]
        [PrefsDescription(Text = "Label of Tag and Layer", Tooltip = "")]
        public static int fbTagLayerLabelOption;
        [PrefsKey(Group = "FunctionButton.TagLayer", Integer = 4, Slider = true, Min = 2, Max = 8)]
        [PrefsDescription(Text = "Abbreviation Chop", Tooltip = "")]
        public static int fbTagLayerChop;
        [PrefsKey(Group = "FunctionButton.TagLayer", Integer = 3, Slider = true, Min = 1, Max = 8)]
        [PrefsDescription(Text = "Abbreviation Chop to", Tooltip = "")]
        public static int fbTagLayerChopTo;
        [PrefsKey(Group = "FunctionButton.TagLayer", Enabled = false)]
        [PrefsDescription(Text = "No Color Tint", Tooltip = "")]
        public static bool fbTagLayerNoColorTint;
        #endregion
        #region Tag Layer 2
        [PrefsKey(Group = "FunctionButton.TagLayer2", Enabled = true)]
        [PrefsDescription(Text = "Hide if Default", Tooltip = "Hide if Tag/Layer is Untagged/Default.")]
        public static bool fbTagLayer2HideDefault;
        [PrefsKey(Group = "FunctionButton.TagLayer2", Enabled = false)]
        [PrefsDescription(Text = "Small Font", Tooltip = "Use small font.")]
        public static bool fbTagLayer2SmallFont;
        [PrefsKey(Group = "FunctionButton.TagLayer2", Integer = (int)TagLayerOption.TagOrLayer, EnumType = typeof(TagLayerOption))]
        [PrefsDescription(Text = "Show Tag or Layer", Tooltip = "")]
        public static int fbTagLayer2Option;
        [PrefsKey(Group = "FunctionButton.TagLayer2", Integer = (int)ModifierKey.Ctrl, EnumType = typeof(ModifierKey))]
        [PrefsDescription(Text = "Modifier Key to Invert", Tooltip = "Invert the visible of tag and layer.")]
        public static int fbTagLayer2Invert;
        [PrefsKey(Group = "FunctionButton.TagLayer2", Integer = (int)TagLayerLabelOption.Capital, EnumType = typeof(TagLayerLabelOption))]
        [PrefsDescription(Text = "Label of Tag and Layer", Tooltip = "")]
        public static int fbTagLayer2LabelOption;
        [PrefsKey(Group = "FunctionButton.TagLayer2", Integer = 4, Slider = true, Min = 2, Max = 8)]
        [PrefsDescription(Text = "Abbreviation Chop", Tooltip = "")]
        public static int fbTagLayer2Chop;
        [PrefsKey(Group = "FunctionButton.TagLayer2", Integer = 3, Slider = true, Min = 1, Max = 8)]
        [PrefsDescription(Text = "Abbreviation Chop to", Tooltip = "")]
        public static int fbTagLayer2ChopTo;
        [PrefsKey(Group = "FunctionButton.TagLayer2", Enabled = false)]
        [PrefsDescription(Text = "No Color Tint", Tooltip = "")]
        public static bool fbTagLayer2NoColorTint;
        #endregion
        #region Sorting Layer
        [PrefsKey(Group = "FunctionButton.SortingLayer", Enabled = false)]
        [PrefsDescription(Text = "Show Only on Sprite", Tooltip = "Show only on sprite renderer.")]
        public static bool fbSortingLayerSpriteOnly;
        [PrefsKey(Group = "FunctionButton.SortingLayer", Enabled = false)]
        [PrefsDescription(Text = "Small Font", Tooltip = "Use small font.")]
        public static bool fbSortingLayerSmallFont;
        [PrefsKey(Group = "FunctionButton.SortingLayer", Integer = (int)TagLayerLabelOption.Capital, EnumType = typeof(TagLayerLabelOption))]
        [PrefsDescription(Text = "Label of Tag and Layer", Tooltip = "")]
        public static int fbSortingLayerLabelOption;
        [PrefsKey(Group = "FunctionButton.SortingLayer", Integer = 4, Slider = true, Min = 2, Max = 8)]
        [PrefsDescription(Text = "Abbreviation Chop", Tooltip = "")]
        public static int fbSortingLayerChop;
        [PrefsKey(Group = "FunctionButton.SortingLayer", Integer = 3, Slider = true, Min = 1, Max = 8)]
        [PrefsDescription(Text = "Abbreviation Chop to", Tooltip = "")]
        public static int fbSortingLayerChopTo;
        [PrefsKey(Group = "FunctionButton.SortingLayer", Enabled = false)]
        [PrefsDescription(Text = "No Color Tint", Tooltip = "")]
        public static bool fbSortingLayerNoColorTint;
        #endregion
        #region NonToggleable
        [PrefsKey(Group = "FunctionButton.NonToggleable", Integer = 80, Slider = true, Min = 0, Max = 100)]
        [PrefsDescription(Text = "Darken Icon", Tooltip = "Darken Non-Toggleable Component Icon.")]
        public static int fbNonToggleableDarken;
        #endregion
        #region Log
        [PrefsKey(Group = "FunctionButton.Log", Enabled = true)]
        [PrefsDescription(Text = "Disable in Playmode", Tooltip = "")]
        public static bool fbLogDisablePlaymode;
        #endregion
        #region Color
        [PrefsKey(Group = "Color", Color = "255,255,255,255")]
        public static Color colCustomize1;
        [PrefsKey(Group = "Color", Color = "255,0,0,255")]
        public static Color colCustomize2;
        [PrefsKey(Group = "Color", Color = "255,255,0,255")]
        public static Color colCustomize3;
        [PrefsKey(Group = "Color", Color = "0,255,0,255")]
        public static Color colCustomize4;
        [PrefsKey(Group = "Color", Color = "128,128,128,255")]
        public static Color colCustomize5;
        [PrefsKey(Group = "Color", Color = "0,0,0,255")]
        public static Color colCustomize6;
        [PrefsKey(Group = "Color", Color = "0,255,255,255")]
        public static Color colCustomize7;
        [PrefsKey(Group = "Color", Color = "0,0,255,255")]
        public static Color colCustomize8;
        [PrefsKey(Group = "Color", Color = "255,0,255,255")]
        public static Color colCustomize9;
        [PrefsKey(Group = "Color", Color = "64,64,64,255")]
        public static Color colCustomize10;
        #endregion

        public static readonly GUIContent sceneViewHighlighterContent = new GUIContent("Scene View Highlighter", "Enable/disable scene view highlighter.");
        public static readonly GUIContent hierarchyItemDrawerContent = new GUIContent("Hierarchy Item Drawer", "Enable/disable hierarchy item drawer.");
        public static readonly GUIContent hierarchyTopbarContent = new GUIContent("Hierarchy Topbar", "Enable/disable hierarchy topbar.");
        public static readonly GUIContent functionButtonContent = new GUIContent("Function Button", "Enable/disable function button.");
        public static readonly GUIContent hierarchyDataObjectContent = new GUIContent("Hierarchy Data Object", "Enable/disable hierarchy data object.");

        private static readonly GUIContent SeparatorShowContent = new GUIContent("Show", "");
        private static readonly GUIContent SeparatorMovableContent = new GUIContent("Movable", "");
        private static readonly GUIContent SeparatorOffsetContent = new GUIContent("Offset", "");

        private static Vector2 s_Scroll;
        private static Rect s_GUIRect;

        private static List<PrefsItem> s_FieldList;
        private static ReorderableList s_ReorderableList;
        private static List<FunctionTypeItem> s_FunctionList;
        private static FunctionTypeItem s_ItemToRemove;

        static Prefs()
        {
            var fields = typeof(Prefs).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).AsEnumerable();
            fields = fields.Where(i => Attribute.GetCustomAttribute(i, typeof(PrefsKeyAttribute)) != null);
            s_FieldList = fields.Select(field =>
            {
                var item = new PrefsItem();
                item.Field = field;
                item.Key = Attribute.GetCustomAttribute(field, typeof(PrefsKeyAttribute)) as PrefsKeyAttribute;
                item.Desc = Attribute.GetCustomAttribute(field, typeof(PrefsDescriptionAttribute)) as PrefsDescriptionAttribute;
                return item;
            }).ToList();
            Update();
        }

        private static void Update()
        {
            LoadAllPrefs();

            var functionlist = Enumerable.Range(0, Enum.GetNames(typeof(FunctionType)).Length).Cast<FunctionType>().Where(i => i != FunctionType.Separator);

            var s = EditorPrefs.GetString("HierarchyPlus.FunctionButton");
            s_FunctionList = s.Split(',').Select(i => i.Split('.')).Where(i => Enum.IsDefined(typeof(FunctionType), i[0])).Select(i =>
                {
                    var item = new FunctionTypeItem();
                    item.Type = (FunctionType)Enum.Parse(typeof(FunctionType), i[0]);
                    bool.TryParse(i[1], out item.Enabled);
                    if (i.Length == 3)
                        bool.TryParse(i[2], out item.PlayMode);
                    else
                        item.PlayMode = item.Enabled;
                    return item;
                }).ToList();
            var remain = functionlist.Except(s_FunctionList.Select(i => i.Type).Where(i => i != FunctionType.Separator));
            s_FunctionList = s_FunctionList.Concat(remain.Select(i => new FunctionTypeItem() { Type = i, Enabled = i.GetEnable(), PlayMode = i.GetEnable() })).ToList();
            LoadSeparatorPrefs();

            s_ReorderableList = new ReorderableList(s_FunctionList, typeof(FunctionTypeItem), true, true, false, false);
            //_reorderableList.elementHeight = EditorGUIUtility.singleLineHeight + 4;
            s_ReorderableList.elementHeightCallback += (index) => GetFunctionGUIHeight(index);
            s_ReorderableList.drawElementCallback += DrawElementCallback;
            s_ReorderableList.drawHeaderCallback += DrawHeaderCallback;
            s_ReorderableList.onReorderCallback += (list) => GUI.changed = true;
        }

        private static void DrawHeaderCallback(Rect itemRect)
        {
            var rect = new Rect(itemRect);
            GUI.Label(rect, "Function Button", EditorStyles.boldLabel);
            rect.xMin = itemRect.xMax - 100;
            if (GUI.Button(rect, "Add Separator", EditorStyles.miniButton))
                s_FunctionList.Insert(0, new FunctionTypeItem() { Type = FunctionType.Separator, Enabled = true, PlayMode = true });
            rect.xMin = itemRect.xMax - 160;
            using (new GUIColorTint(Styles.EnhanceColor(Styles.GetCycleColor(6, 0.8f))))
                GUI.Label(rect, "PlayMode", EditorStyles.miniLabel);
        }

        private static void DrawElementCallback(Rect itemRect, int index, bool isActive, bool isFocused)
        {
            var rect = new Rect(itemRect);
            rect.y += 2;
            rect.width = 16;
            rect.height = EditorGUIUtility.singleLineHeight;
            var item = s_FunctionList[index];
            if (GetPrefsCount(item.Type) > 0)
            {
                if (item.Foldout)
                {
                    var r = new Rect(itemRect);
                    r.x -= 1;
                    r.width += 2;
                    r.height = GetFunctionGUIHeight(index) + 1;
                    GUI.Label(r, GUIContent.none, "box");
                }
                item.Foldout = EditorGUI.Foldout(rect, item.Foldout, GUIContent.none);
            }

            var label = item.Type.GetLabel();
            rect.x += 16;
            rect.width = GUI.skin.toggle.CalcSize(Utility.TempContent(label)).x + 0;
            item.Enabled = GUI.Toggle(rect, item.Enabled, label);

            if(!item.Foldout)
            {
                var r1 = new Rect(itemRect);
                r1.y += 2;
                r1.xMin = r1.xMax - 140;
                r1.width = 14;
                r1.height = EditorGUIUtility.singleLineHeight;
                var ro = new RectOffset(2, 2, 3, 1);
                EditorGUI.DrawRect(ro.Add(r1), Styles.EnhanceColor(Styles.GetCycleColor(6, 0.2f)));
                item.PlayMode = GUI.Toggle(r1, item.PlayMode, GUIContent.none);
            }

            if (item.Type == FunctionType.Separator)
            {
                var r = new Rect(itemRect);
                r.xMin = r.xMax - 100;
                r.height = EditorGUIUtility.singleLineHeight;
                if (GUI.Button(r, "Remove", EditorStyles.miniButton))
                    s_ItemToRemove = item;
            }
            if (item.Foldout)
            {
                var start = new Vector2(rect.x, rect.yMax);
                var end = new Vector2(rect.xMax, rect.yMax);
                Handles.color = Color.black;
                Handles.DrawLine(start, end);

                rect.y += EditorGUIUtility.singleLineHeight + 2;
                rect.xMax = itemRect.xMax;
                if (item.Type == FunctionType.Separator)
                    DrawSeparatorGUI(rect, item);
                else
                    DrawElementGUI(rect, item.Type);
            }
        }

        private static float GetFunctionGUIHeight(int index)
        {
            var item = s_FunctionList[index];
            if (!item.Foldout) return EditorGUIUtility.singleLineHeight + 4;

            int line = GetPrefsCount(item.Type) + 1;
            return line * EditorGUIUtility.singleLineHeight + 4;
        }

        private static void DrawElementGUI(Rect itemRect, FunctionType type)
        {
            EditorGUIUtility.labelWidth = 160;
            s_GUIRect = new Rect(itemRect);
            DrawPrefsGUI("FunctionButton." + type.ToString(), GetNextRect);
        }

        public static PrefsItem GetPrefsItem(string name)
        {
            return s_FieldList.Where(i => i.Field.Name.Equals(name)).FirstOrDefault();
        }

        public static ReadOnlyCollection<FunctionTypeItem> GetRawFunctionList()
        {
            return s_FunctionList.AsReadOnly();
        }

        public static ReadOnlyCollection<FunctionTypeItem> GetFunctionList()
        {
            return s_FunctionList.Where(i => i.active).ToList().AsReadOnly();
        }

        public static ReadOnlyCollection<SeparatorConfig> GetSeparatorList()
        {
            return s_FunctionList.Where(i => i.active && i.Type == FunctionType.Separator).Select(i => i.SeparatorConfig).ToList().AsReadOnly();
        }

        public static bool GetFunctionActive(FunctionType type)
        {
            return s_FunctionList.SkipWhile(i => i.Type != type).FirstOrDefault().active;
        }

        [PreferenceItem("Hierarchy Plus")]
        public static void OnPreferencesGUI()
        {
            EditorGUI.BeginChangeCheck();
            s_Scroll = EditorGUILayout.BeginScrollView(s_Scroll, false, false);

            if (!doHideWarning)
                EditorGUILayout.HelpBox("Hierarchy Data Object stores extra data of GameObjects. " +
                            "If you disable this, all functions dependent on this will not work."
                            , MessageType.Info, true);

            EditorGUI.BeginChangeCheck();
            using (new ToggleableGroup(hierarchyDataObjectContent, ref doHierarchyDataObject))
                if (doHierarchyDataObject)
                    DrawPrefsGUI("HierarchyDataObject", GetLayoutRect, true);
            if (EditorGUI.EndChangeCheck())
                DataObject.CheckDataObject();

            if (!svHideWarning)
                EditorGUILayout.HelpBox("Outline huge amount of renderer may result lag in editor. " +
                        "Please select a proper value."
                        , MessageType.Info, true);

            using (new ToggleableGroup(sceneViewHighlighterContent, ref _SceneViewHighlighter))
                if (_SceneViewHighlighter)
                    DrawPrefsGUI("SceneViewHighlighter", GetLayoutRect, true);

            using (new ToggleableGroup(hierarchyTopbarContent, ref _HierarchyTopbar))
                if (_HierarchyTopbar)
                    DrawPrefsGUI("HierarchyTopbar", GetLayoutRect, true);

            using (new ToggleableGroup(hierarchyItemDrawerContent, ref _HierarchyItemDrawer))
                if (_HierarchyItemDrawer)
                    DrawPrefsGUI("HierarchyItemDrawer", GetLayoutRect, true);

            if (EditorGUI.EndChangeCheck())
            {
                SaveAllPrefs();
                //EditorApplication.RepaintHierarchyWindow();
                InternalEditorUtility.RepaintAllViews();
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
            s_ReorderableList.DoLayoutList();
            EditorGUILayout.EndVertical();

            if (s_ItemToRemove != null)
            {
                s_FunctionList.Remove(s_ItemToRemove);
                s_ItemToRemove = null;
            }

            if (EditorGUI.EndChangeCheck())
            {
                SaveFunctionButtonPrefs();
                SaveAllPrefs(prefix: "fb");
                SaveSeparatorPrefs();
                EditorApplication.RepaintHierarchyWindow();
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Use Defaults", GUILayout.ExpandWidth(false)))
            {
                SaveAllPrefs(true);
                EditorPrefs.DeleteKey("HierarchyPlus.FunctionButton");
                Update();
                DataObject.CheckDataObject();
            }
        }

        private static Rect GetLayoutRect(float height)
        {
            var rect = new Rect(EditorGUILayout.GetControlRect(true, height));
            return rect;
        }

        private static Rect GetNextRect(float height)
        {
            var rect = new Rect(s_GUIRect);
            rect.height = height;
            s_GUIRect.y += height;
            return rect;
        }

        private static int GetPrefsCount(FunctionType type)
        {
            if (type == FunctionType.Separator)
                return 3;

            int count = 0;
            var group = "FunctionButton." + type.ToString();
            foreach (var item in s_FieldList)
            {
                if (item.Key == null || item.Desc == null) continue;
                if (!item.Key.Group.Equals(group)) continue;
                count++;
                if (item.Key.TextLine > 1)
                    count += item.Key.TextLine;
            }
            return count;
        }

        private static void DrawPrefsGUI(string group, Func<float, Rect> getRect, bool shading = false)
        {
            bool even = false;
            var indent = EditorGUI.indentLevel;
            var ro = new RectOffset(1, 1, 1, 1);
            foreach (var item in s_FieldList)
            {
                if (item.Key == null || item.Desc == null) continue;
                if (!item.Key.Group.Equals(group)) continue;

                if (item.Key.Indent) EditorGUI.indentLevel++;
                var rect = getRect(EditorGUIUtility.singleLineHeight);
                var content = Utility.TempContent(item.Desc.Text, tooltip: item.Desc.Tooltip);

                if (EditorGUI.indentLevel == indent) even = !even;
                if (even && shading)
                    if (Event.current.type == EventType.Repaint)
                    {
                        EditorGUI.DrawRect(ro.Add(rect), Styles.overlayColor);
                        EditorGUI.DrawRect(ro.Add(rect), Styles.overlayColor);
                    }

                if (item.Field.FieldType == typeof(bool))
                {
                    var v = EditorGUI.Toggle(rect, content, (bool)item.Field.GetValue(null));
                    item.Field.SetValue(null, v);
                }
                if (item.Field.FieldType == typeof(int))
                {
                    if (item.Key.Slider == true)
                    {
                        int max = item.Key.Max;
                        if (item.Field.Name.Equals("fbTagLayerChopTo")) max = fbTagLayerChop;
                        if (item.Field.Name.Equals("fbTagLayer2ChopTo")) max = fbTagLayer2Chop;
                        if (item.Field.Name.Equals("fbSortingLayerChopTo")) max = fbSortingLayerChop;
                        var v = EditorGUI.IntSlider(rect, content, (int)item.Field.GetValue(null), item.Key.Min, max);
                        item.Field.SetValue(null, v);
                    }
                    else if (item.Key.EnumType == null)
                    {
                        var v = EditorGUI.IntField(rect, content, (int)item.Field.GetValue(null));
                        if (v < item.Key.Min) v = item.Key.Min;
                        item.Field.SetValue(null, v);
                    }
                    else
                    {
                        int value = (int)item.Field.GetValue(null);
                        if (item.Field.Name.Equals("svOutlineSceneViewModifier"))
                            if ((ModifierKey)value == ModifierKey.None)
                                value = (int)ModifierKey.Ctrl;
                        var v = Enum.ToObject(item.Key.EnumType, value);
                        v = EditorGUI.EnumPopup(rect, content, (Enum)v);
                        if (item.Field.Name.Equals("svOutlineSceneViewModifier"))
                            if ((ModifierKey)v == ModifierKey.None)
                                v = value;
                        item.Field.SetValue(null, (int)v);
                    }
                }
                if (item.Field.FieldType == typeof(string))
                {
                    if (item.Key.TextLine <= 1)
                    {
                        var v = EditorGUI.TextField(rect, content, (string)item.Field.GetValue(null));
                        item.Field.SetValue(null, v);
                    }
                    else
                    {
                        var style = new GUIStyle(GUI.skin.textArea);
                        style.wordWrap = true;
                        EditorGUI.PrefixLabel(rect, content);
                        rect = getRect(item.Key.TextLine * EditorGUIUtility.singleLineHeight);
                        var v = EditorGUI.TextArea(rect, (string)item.Field.GetValue(null), style);
                        item.Field.SetValue(null, v);
                    }
                }
                if (item.Field.FieldType == typeof(Color))
                {
                    var v = EditorGUI.ColorField(rect, content, (Color)item.Field.GetValue(null));
                    item.Field.SetValue(null, v);
                }

                if (!string.IsNullOrEmpty(item.Desc.Helpbox))
                {
                    rect = getRect(item.Key.TextLine * EditorGUIUtility.singleLineHeight);
                    EditorGUI.HelpBox(rect, item.Desc.Helpbox, MessageType.Info);
                }

                if (item.Key.Indent) EditorGUI.indentLevel--;
            }
        }

        private static void DrawSeparatorGUI(Rect itemRect, FunctionTypeItem item)
        {
            EditorGUIUtility.labelWidth = 160;
            s_GUIRect = new Rect(itemRect);
            var sc = item.SeparatorConfig;
            var rect = GetNextRect(EditorGUIUtility.singleLineHeight);
            sc.Show = EditorGUI.Toggle(rect, SeparatorShowContent, sc.Show);
            rect = GetNextRect(EditorGUIUtility.singleLineHeight);
            sc.Movable = EditorGUI.Toggle(rect, SeparatorMovableContent, sc.Movable);
            rect = GetNextRect(EditorGUIUtility.singleLineHeight);
            sc.Offset = EditorGUI.IntField(rect, SeparatorOffsetContent, sc.Offset);
            if (sc.Offset < 0) sc.Offset = 0;
        }

        private static void LoadSeparatorPrefs()
        {
            int index = 0;
            var list = s_FunctionList.Where(i => i.Type == FunctionType.Separator).ToList();
            foreach (var item in list)
            {
                var sc = item.SeparatorConfig;
                string key = "HierarchyPlus.Separator" + index;
                sc.Show = EditorPrefs.GetBool(key + ".Show", sc.Show);
                sc.Movable = EditorPrefs.GetBool(key + ".Movable", sc.Movable);
                sc.Offset = EditorPrefs.GetInt(key + ".Offset", sc.Offset);
                index++;
            }
        }

        public static void SaveSeparatorPrefs()
        {
            int index = 0;
            var list = s_FunctionList.Where(i => i.Type == FunctionType.Separator).ToList();
            foreach (var item in list)
            {
                var sc = item.SeparatorConfig;
                string key = "HierarchyPlus.Separator" + index;
                EditorPrefs.SetBool(key + ".Show", sc.Show);
                EditorPrefs.SetBool(key + ".Movable", sc.Movable);
                EditorPrefs.SetInt(key + ".Offset", sc.Offset);
                index++;
            }
        }

        public static void SaveFunctionButtonPrefs()
        {
            var list = s_FunctionList.Select(i => i.Type.ToString() + "." + i.Enabled.ToString() + "." + i.PlayMode.ToString()).ToArray();
            EditorPrefs.SetString("HierarchyPlus.FunctionButton", string.Join(",", list));
        }

        private static void LoadAllPrefs()
        {
            foreach (var item in s_FieldList)
            {
                if (item.Key == null) continue;

                string key = "HierarchyPlus." + item.Field.Name.TrimStart('_');
                if (item.Field.FieldType == typeof(bool))
                {
                    var v = EditorPrefs.GetBool(key, item.Key.Enabled);
                    item.Field.SetValue(null, v);
                }
                if (item.Field.FieldType == typeof(int))
                {
                    var v = EditorPrefs.GetInt(key, item.Key.Integer);
                    item.Field.SetValue(null, v);
                }
                if (item.Field.FieldType == typeof(string))
                {
                    var v = EditorPrefs.GetString(key, item.Key.String);
                    item.Field.SetValue(null, v);
                }
                if (item.Field.FieldType == typeof(Color))
                {
                    var v = EditorPrefs.GetString(key, item.Key.Color);
                    item.Field.SetValue(null, v.ToColor(item.Key.Color.ToColor()));
                }
            }
        }

        public static void SaveAllPrefs(bool reset = false, string prefix = null)
        {
            foreach (var item in s_FieldList)
            {
                if (item.Key == null) continue;

                if (!string.IsNullOrEmpty(prefix) && !item.Field.Name.TrimStart('_').StartsWith(prefix))
                    continue;

                string key = "HierarchyPlus." + item.Field.Name.TrimStart('_');
                if (item.Field.FieldType == typeof(bool))
                {
                    bool v = reset ? item.Key.Enabled : (bool)item.Field.GetValue(null);
                    EditorPrefs.SetBool(key, v);
                }
                if (item.Field.FieldType == typeof(int))
                {
                    int v = reset ? item.Key.Integer : (int)item.Field.GetValue(null);
                    EditorPrefs.SetInt(key, v);
                }
                if (item.Field.FieldType == typeof(string))
                {
                    string v = reset ? item.Key.String : (string)item.Field.GetValue(null);
                    EditorPrefs.SetString(key, v);
                }
                if (item.Field.FieldType == typeof(Color))
                {
                    Color v = reset ? item.Key.Color.ToColor() : (Color)item.Field.GetValue(null);
                    EditorPrefs.SetString(key, v.ToPrefString());
                }
            }
        }
    }
}