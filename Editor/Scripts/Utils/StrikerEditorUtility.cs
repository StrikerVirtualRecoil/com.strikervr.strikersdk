using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StrikerLink.Unity.Editor.Utils
{
    public static class StrikerEditorUtility
    {
        public static GUIStyle CreateStyleWithPadding(int left, int right, int top, int bottom)
        {
            GUIStyle paddedStyle = new GUIStyle();

            paddedStyle.margin = new RectOffset(left, right, top, bottom);

            return paddedStyle;
        }

        public static GUIStyle CreateStyleWithPadding(int all)
        {
            return CreateStyleWithPadding(all, all, all, all);
        }

        public static GUIStyle CreateStyleWithPadding(int h, int v)
        {
            return CreateStyleWithPadding(h, h, v, v);
        }

        public static GUIStyle CreateTransparentFoldoutHeader()
        {
            GUIStyle style = new GUIStyle(EditorStyles.foldoutHeader);

            style.normal.background = null;
            style.active.background = null;
            style.focused.background = null;
            style.hover.background = null;

            return style;
        }

        public static GUIStyle WithMargin(this GUIStyle style, int left, int right, int top, int bottom)
        {
            style.margin = new RectOffset(left, right, top, bottom);

            return style;
        }

        public static GUIStyle WithMargin(this GUIStyle style, int all)
        {
            style.margin = new RectOffset(all, all, all, all);

            return style;
        }

        public static GUIStyle WithMargin(this GUIStyle style, int h, int v)
        {
            style.margin = new RectOffset(h, h, v, v);

            return style;
        }

        public static GUIStyle WithPadding(this GUIStyle style, int left, int right, int top, int bottom)
        {
            style.padding = new RectOffset(left, right, top, bottom);

            return style;
        }

        public static GUIStyle WithPadding(this GUIStyle style, int all)
        {
            style.padding = new RectOffset(all, all, all, all);

            return style;
        }

        public static GUIStyle WithPadding(this GUIStyle style, int h, int v)
        {
            style.padding = new RectOffset(h, h, v, v);

            return style;
        }

        internal static void StorePreviewDevices(Authoring.HapticProjectInspector.DeviceSelectionEnum val) {
            EditorPrefs.SetInt("STRIKER_PREVIEW_DEVICES", (int)val);
        }

        internal static Authoring.HapticProjectInspector.DeviceSelectionEnum GetPreviewDevices()
        {
            return (Authoring.HapticProjectInspector.DeviceSelectionEnum)EditorPrefs.GetInt("STRIKER_PREVIEW_DEVICES", 0x1);
        }
    }
}