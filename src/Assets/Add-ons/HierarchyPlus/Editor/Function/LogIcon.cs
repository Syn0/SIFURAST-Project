using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HierarchyPlus
{
    public class LogIcon : FunctionButton
    {
        public override float Width { get { return _Width; } }
        public override float Offset { get { return _Offset; } }

        private float _Offset;
        private float _Width;

        private int _Info, _Warn, _Error, _Miss;
        private int _ChildInfo, _ChildWarn, _ChildError, _ChildMiss;
        private Dictionary<EntryMode, HashSet<GameObject>> _ChildLog = new Dictionary<EntryMode, HashSet<GameObject>>();
        private EntryMode _Icon;
        private string _LastLog;

        public override float Layout(GameObject go, float offset, float expand = 0)
        {
            _Offset = offset;
            if(Prefs.fbLogDisablePlaymode && EditorApplication.isPlayingOrWillChangePlaymode)
            {
                _Width = 0;
                return _Width;
            }

            _Info = _Warn = _Error = _Miss = 0;
            _ChildInfo = _ChildWarn = _ChildError = _ChildMiss = 0;
            _ChildLog.Clear();
            _ChildLog.Add(EntryMode.Error, new HashSet<GameObject>());
            _ChildLog.Add(EntryMode.Warning, new HashSet<GameObject>());
            _ChildLog.Add(EntryMode.Log, new HashSet<GameObject>());
            _ChildLog.Add(EntryMode.MissingReference, new HashSet<GameObject>());

            foreach (var child in go.GetChildrenList())
            {
                var log = LogHelper.GetLogEntry(child);
                if (log.Count == 0) continue;

                foreach (var g in log.GroupBy(i => i.Mode))
                {
                    if (child == go)
                    {
                        //_LastLog = g.First().Condition;
                        var logs = g.Select(i => new string(i.Condition.TakeWhile(c => c != '\n').ToArray()));
                        var count = logs.Count();
                        if (count > 20) logs = logs.Take(20);
                        _LastLog = string.Join("\n", logs.ToArray());
                        if (count > 20) _LastLog += "\n" + (count - 20) + " more logs.";
                    }
                    switch (g.Key)
                    {
                        case EntryMode.Error:
                            _Error += child == go ? g.Count() : 0;
                            _ChildError += child != go ? g.Count() : 0;
                            if (child != go) _ChildLog[EntryMode.Error].Add(child);
                            break;
                        case EntryMode.Warning:
                            _Warn += child == go ? g.Count() : 0;
                            _ChildWarn += child != go ? g.Count() : 0;
                            if (child != go) _ChildLog[EntryMode.Warning].Add(child);
                            break;
                        case EntryMode.Log:
                            _Info += child == go ? g.Count() : 0;
                            _ChildInfo += child != go ? g.Count() : 0;
                            if (child != go) _ChildLog[EntryMode.Log].Add(child);
                            break;
                        case EntryMode.MissingReference:
                            _Miss += child == go ? g.Count() : 0;
                            _ChildMiss += child != go ? g.Count() : 0;
                            if (child != go) _ChildLog[EntryMode.MissingReference].Add(child);
                            break;
                    }
                }
            }
            var childicon = _ChildError > 0 ? EntryMode.Error : _ChildMiss > 0 ? EntryMode.MissingReference : _ChildWarn > 0 ? EntryMode.Warning : EntryMode.Log;
            _Icon = _Error > 0 ? EntryMode.Error : _Miss > 0 ? EntryMode.MissingReference : _Warn > 0 ? EntryMode.Warning : _Info > 0 ? EntryMode.Log : childicon;

            _Width = kButtonWidth;
            if (_Info + _Warn + _Error + _Miss + _ChildInfo + _ChildWarn + _ChildError + _ChildMiss == 0)
                _Width = 0;
            return _Width;
        }

        public override float Draw(GameObject go, Rect itemRect)
        {
            if (_Width == 0) return 0;
            var rect = new Rect(itemRect);
            rect.x = rect.xMax - _Offset - _Width;
            rect.width = _Width;

            if (_Info + _Warn + _Error + _Miss == 0)
                _LastLog = string.Empty;

            var gc = Utility.TempContent();
            switch (_Icon)
            {
                case EntryMode.Error:
                    gc.image = EditorGUIUtility.FindTexture("console.erroricon.sml");
                    break;
                case EntryMode.Warning:
                    gc.image = EditorGUIUtility.FindTexture("console.warnicon.sml");
                    break;
                case EntryMode.Log:
                    gc.image = EditorGUIUtility.FindTexture("console.infoicon.sml");
                    break;
                case EntryMode.MissingReference:
                    gc.image = EditorGUIUtility.FindTexture("console.erroricon.sml");
                    break;
            }
            gc.text = string.Empty;
            var s = _Error == 0 ? "" : string.Format("{0} Error\n", _Error);
            s += _Miss == 0 ? "" : string.Format("{0} Missing Reference\n", _Miss);
            s += _Warn == 0 ? "" : string.Format("{0} Warning\n", _Warn);
            s += _Info == 0 ? "" : string.Format("{0} Log\n", _Info);

            s += _ChildError == 0 ? "" : string.Format("{0} Error in {1} Children\n", _ChildError, _ChildLog[EntryMode.Error].Count());
            s += _ChildMiss == 0 ? "" : string.Format("{0} Missing Reference in {1} Children\n", _ChildMiss, _ChildLog[EntryMode.MissingReference].Count());
            s += _ChildWarn == 0 ? "" : string.Format("{0} Warning in {1} Children\n", _ChildWarn, _ChildLog[EntryMode.Warning].Count());
            s += _ChildInfo == 0 ? "" : string.Format("{0} Log in {1} Children\n", _ChildInfo, _ChildLog[EntryMode.Log].Count());
            s += _LastLog;
            gc.tooltip = s.TrimStart().TrimEnd();

            using (new GUIColorTint(_Info + _Warn + _Error + _Miss > 0 ? 1f : 0.4f))
                if (GUI.Button(rect, gc, Styles.iconOnly))
                {
                    var e = _ChildLog.SelectMany(i => i.Value).Distinct();
                    if (e.Any())
                    {
                        var menu = new GenericMenu();
                        foreach (var g in e)
                        {
                            var gameobject = g;
                            menu.AddItem(new GUIContent(g.name), false, () => Selection.activeGameObject = gameobject);
                        }
                        menu.DropDown(rect);
                    }
                }
            return _Width;
        }
    }
}