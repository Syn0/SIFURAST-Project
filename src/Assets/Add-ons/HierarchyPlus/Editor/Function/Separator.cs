using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HierarchyPlus
{
    public interface ISeparator
    {
        void SetSeparator(int index, SeparatorConfig sc);
    }

    public class Separator : FunctionButton, ISeparator
    {
        public const float kWidth = 4;
        public override float Width { get { return _Data[_Index].Width; } }
        public override float Offset { get { return _Data[_Index].Offset; } }

        private class Data
        {
            public float Offset;
            public float Width;
            public SeparatorConfig Config;
            public bool MouseDown;
            public float DragX;
        }

        private int _Index;
        private Dictionary<int, Data> _Data = new Dictionary<int, Data>();

        public void SetSeparator(int index, SeparatorConfig sc)
        {
            _Index = index;
            if (!_Data.ContainsKey(_Index))
                _Data[_Index] = new Data();
            _Data[_Index].Config = sc;
        }

        public override float Layout(GameObject go, float offset, float expand = 0)
        {
            var data = _Data[_Index];
            data.Offset = offset;
            data.Width = data.Config.Show ? kWidth : 0;

            return data.Width;
        }

        public override float Draw(GameObject go, Rect itemRect)
        {
            var data = _Data[_Index];
            if (data.Width == 0) return 0;
            var rect = new Rect(itemRect);
            rect.x = rect.xMax - data.Offset - data.Width;
            rect.width = data.Width;

            Color col = Styles.lineColor;
            int diff = 0;

            var evt = Event.current;
            var mx = evt.mousePosition.x;
            if (data.Config.Movable && mx >= rect.xMin && mx <= rect.xMax)
                col = Styles.EnhanceColor(Color.red);
            if (evt.type == EventType.MouseDown && rect.Contains(evt.mousePosition))
            {
                data.MouseDown = true;
                data.DragX = mx;
                evt.Use();
            }
            if (evt.type == EventType.MouseUp)
                data.MouseDown = false;
            if (data.MouseDown)
            {
                var old = data.Config.Offset;
                data.Config.Offset += (int)(data.DragX - mx);
                if (data.Config.Offset < 0) data.Config.Offset = 0;
                diff = data.Config.Offset - old;
                data.DragX = mx;
                col = Styles.EnhanceColor(Color.red);
                Prefs.SaveSeparatorPrefs();
                EditorApplication.RepaintHierarchyWindow();
            }

            var start = new Vector2(rect.x + 2 - diff, rect.yMin);
            var end = new Vector2(rect.x + 2 - diff, rect.yMax);
            Handles.color = col;
            Handles.DrawLine(start, end);
            start.x++;
            end.x++;
            Handles.color = Styles.overlayColor;
            Handles.DrawLine(start, end);

            return data.Width;
        }
    }
}