using UnityEditor;
using UnityEngine;

namespace HierarchyPlus
{
    public static class Styles
    {
        public static bool forceProSkin = false;

        public static readonly Color normalColor;
        public static readonly Color altColor;
        public static readonly Color lineColor;
        public static readonly Color[] cycleColor;

        public static readonly GUIStyle miniLabel;
        public static readonly GUIStyle iconOnly;
        public static readonly GUIStyle emptyStyle;

        public static readonly Texture2D fadeMask;

        public static Color overlayColor { get; private set; }
        public static Color selectedOverlayColor { get; private set; }
        public static Color iconColor { get; private set; }
        public static Color iconDisabledColor { get; private set; }

        private static readonly Color overlayColorLight;
        private static readonly Color selectedOverlayColorLight;
        private static readonly Color overlayColorDark;
        private static readonly Color selectedOverlayColorDark;
        private static readonly Color iconColorLight;
        private static readonly Color iconDisabledColorLight;
        private static readonly Color iconColorDark;
        private static readonly Color iconDisabledColorDark;

        static Styles()
        {
            if (EditorGUIUtility.isProSkin || forceProSkin)
            {
                normalColor = new Color32(56, 56, 56, 255);
                altColor = new Color32(51, 51, 51, 255);
                lineColor = new Color32(30, 30, 30, 255);
            }
            else
            {
                normalColor = new Color32(194, 194, 194, 255);
                altColor = new Color32(206, 206, 206, 255);
                lineColor = new Color32(150, 150, 150, 255);
            }

            overlayColorDark = new Color32(0, 0, 0, 25);
            selectedOverlayColorDark = new Color32(62, 95, 150, 100);
            iconColorDark = new Color32(235, 235, 107, 255);
            iconDisabledColorDark = new Color32(125, 125, 125, 100);

            overlayColorLight = new Color32(255, 255, 255, 50);
            selectedOverlayColorLight = new Color32(62, 125, 231, 100);
            iconColorLight = new Color32(56, 56, 56, 255);
            iconDisabledColorLight = new Color32(125, 125, 125, 100);

            ResetColor();

            cycleColor = new Color[8];
            //cycleColor[0] = new Color32(144, 144, 144, 255);
            cycleColor[0] = new Color32(80, 80, 80, 255);
            //cycleColor[1] = new Color32(75, 129, 213, 255);
            cycleColor[1] = new Color32(49, 80, 243, 255);
            cycleColor[6] = new Color32(73, 189, 166, 255);
            cycleColor[3] = new Color32(50, 188, 49, 255);
            cycleColor[4] = new Color32(235, 208, 45, 255);
            cycleColor[5] = new Color32(235, 145, 45, 255);
            cycleColor[2] = new Color32(206, 49, 49, 255);
            cycleColor[7] = new Color32(190, 80, 174, 255);

            miniLabel = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle("ShurikenLabel");
            miniLabel.alignment = TextAnchor.MiddleCenter;
            miniLabel.clipping = TextClipping.Overflow;
            miniLabel.normal.textColor = Color.white;
            miniLabel.hover.textColor = Color.white;
            miniLabel.focused.textColor = Color.white;
            miniLabel.active.textColor = Color.white;

            iconOnly = new GUIStyle();
            iconOnly.alignment = TextAnchor.MiddleCenter;
            iconOnly.imagePosition = ImagePosition.ImageOnly;

            emptyStyle = new GUIStyle();
            emptyStyle.imagePosition = ImagePosition.ImageOnly;

            fadeMask = CreateFadeTexture();
        }

        public static void ResetColor()
        {
            if (EditorGUIUtility.isProSkin || forceProSkin)
            {
                overlayColor = overlayColorDark;
                selectedOverlayColor = selectedOverlayColorDark;
                iconColor = iconColorDark;
                iconDisabledColor = iconDisabledColorDark;
            }
            else
            {
                overlayColor = overlayColorLight;
                selectedOverlayColor = selectedOverlayColorLight;
                iconColor = iconColorLight;
                iconDisabledColor = iconDisabledColorLight;
            }
        }

        public static void UseGameColor()
        {
            overlayColor = overlayColorDark;
            selectedOverlayColor = selectedOverlayColorDark;
            iconColor = iconColorDark;
            iconDisabledColor = iconDisabledColorDark;
        }

        private static Texture2D CreateTextureFromRaw(byte[] data, int width, Color c)
        {
            int h = data.Length * 8 / width;
            var t = new Texture2D(width, h);
            t.hideFlags = HideFlags.HideAndDontSave;
            var b = t.GetPixels();
            var alpha = new float[width * h];
            int x = 0;
            int y = h - 1;
            for (int i = 0; i < data.Length; i++)
            {
                int j = y * width + x;
                alpha[j++] = (data[i] & 0x80) == 0 ? 0 : 1;
                alpha[j++] = (data[i] & 0x40) == 0 ? 0 : 1;
                alpha[j++] = (data[i] & 0x20) == 0 ? 0 : 1;
                alpha[j++] = (data[i] & 0x10) == 0 ? 0 : 1;
                alpha[j++] = (data[i] & 0x08) == 0 ? 0 : 1;
                alpha[j++] = (data[i] & 0x04) == 0 ? 0 : 1;
                alpha[j++] = (data[i] & 0x02) == 0 ? 0 : 1;
                alpha[j++] = (data[i] & 0x01) == 0 ? 0 : 1;
                x = (x + 8) % width;
                if (x == 0) y--;
            }
            for (y = 0; y < h; y++)
                for (x = 0; x < width; x++)
                {
                    int x1 = x > 0 ? x - 1 : x;
                    int x2 = x < width - 1 ? x + 1 : x;
                    int y1 = y > 0 ? y - 1 : y;
                    int y2 = y < h - 1 ? y + 1 : y;
                    var c1 = alpha[y1 * width + x1] * 0.02f;
                    var c2 = alpha[y1 * width + x] * 0.1f;
                    var c3 = alpha[y1 * width + x2] * 0.02f;
                    var c4 = alpha[y * width + x1] * 0.1f;
                    var c5 = alpha[y * width + x] * 0.52f;
                    var c6 = alpha[y * width + x2] * 0.1f;
                    var c7 = alpha[y2 * width + x1] * 0.02f;
                    var c8 = alpha[y2 * width + x] * 0.1f;
                    var c9 = alpha[y2 * width + x2] * 0.02f;
                    var a = (c1 + c2 + c3 + c4 + c5 + c6 + c7 + c8 + c9) * 2;
                    a = Mathf.Min(a, 1);
                    b[y * width + x] = a > 0 ? c : Color.clear;
                    b[y * width + x].a = a;
                }
            t.SetPixels(b);
            t.Apply();
            return t;
        }

        public static Color GetCycleColor(int index, float fade)
        {
            var c = cycleColor[Mathf.RoundToInt(Mathf.Repeat(index, cycleColor.Length))];
            c.a *= fade;
            return c;
        }

        public static Color EnhanceColor(Color c, bool selected = false)
        {
            bool darken = !EditorGUIUtility.isProSkin && !forceProSkin;
            float h, s, v, a;
            a = c.a;
            Color.RGBToHSV(c, out h, out s, out v);
            if (darken)
            {
                //s *= 2f;
                v = v > 0.6f ? 0.6f : v;
                //v *= 0.7f;
            }
            else
            {
                //s = s > 0.6f ? 0.6f : s;
                s *= 0.7f;
                v = v < 0.9f ? 0.9f : v;
            }
            c = Color.HSVToRGB(h, s, v);
            c.a = a;
            return c;
        }

        private static Texture2D CreateFadeTexture()
        {
            var t = new Texture2D(256, 1);
            t.hideFlags = HideFlags.HideAndDontSave;
            for (int i = 0; i < 256; i++)
                t.SetPixel(i, 0, new Color32(255, 255, 255, (byte)i));
            t.Apply();
            return t;
        }
    }
}