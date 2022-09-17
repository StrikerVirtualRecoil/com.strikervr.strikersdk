using Malee.List;
using StrikerLink.Unity.Authoring;
using StrikerLink.Unity.Editor.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static StrikerLink.Unity.Authoring.UnityHapticSample;

namespace StrikerLink.Unity.Editor.Authoring
{
    [CustomEditor(typeof(UnityHapticSample))]
    public class HapticSampleInspector : UnityEditor.Editor
    {
        bool forceApplyModifications = false;

        ReorderableList sequenceList;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUIUtility.labelWidth = 100f;
            UnityHapticSample sample = (UnityHapticSample)target;

            if (sample.primitiveSequence == null)
                sample.primitiveSequence = new List<UnityPrimitiveData>();

            GUILayout.Space(20);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginVertical();
            DrawSamplesBox(sample);
            EditorGUILayout.EndVertical();

            EditorGUIUtility.labelWidth = 0f; // Reset

            if(EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(sample);
                serializedObject.Update();
            }
        }

        #region Samples Palette UI
        void DrawSamplesBox(UnityHapticSample sample)
        {
            SerializedProperty seq = serializedObject.FindProperty("primitiveSequence");

            if (seq == null)
            {
                sample.primitiveSequence = new List<UnityHapticSample.UnityPrimitiveData>();
                serializedObject.ApplyModifiedProperties();
                return;
            }

            if (sequenceList == null)
            {
                sequenceList = new ReorderableList(seq, true, true, true, ReorderableList.ElementDisplayType.Expandable, null, "Sequence", null);

                sequenceList.getElementNameCallback += (element) => { return paletteListElementNameCallback(element); };
                sequenceList.onAddDropdownCallback += (rect, list) => { paletteListDropdownCallback(rect, list); };
                sequenceList.onRemoveCallback += (list) => { paletteListRemoveCallback(list); };
                sequenceList.drawElementCallback += (rect, element, label, selected, focused) => { paletteDrawElementCallback(rect, element, label, selected, focused); };
                sequenceList.getElementHeightCallback += paletteElementHeightCallback;
                sequenceList.label = new GUIContent("Sequence");
            }

            sequenceList.DoLayoutList();
            // End Samples Box
        }

        private void paletteListDropdownCallback(Rect buttonRect, ReorderableList list)
        {
            GenericMenu addMenu = new GenericMenu();

            addMenu.AddItem(new GUIContent(HapticEditingConstants.ValueMapping.GetTextForId("tick", HapticEditingConstants.CommandMap)), false, () =>
            {
                paletteListDropdownSelected("tick", 1f, -1, 50);
            });

            addMenu.AddItem(new GUIContent(HapticEditingConstants.ValueMapping.GetTextForId("pulse", HapticEditingConstants.CommandMap)), false, () =>
            {
                paletteListDropdownSelected("pulse", 1f, -1, 50);
            });

            addMenu.AddItem(new GUIContent(HapticEditingConstants.ValueMapping.GetTextForId("vibrate", HapticEditingConstants.CommandMap)), false, () =>
            {
                paletteListDropdownSelected("vibrate", 1f, 500, 50);
            });

            addMenu.AddItem(new GUIContent(HapticEditingConstants.ValueMapping.GetTextForId("pause", HapticEditingConstants.CommandMap)), false, () =>
            {
                paletteListDropdownSelected("pause", 0f, -1, 50);
            });

            addMenu.ShowAsContext();
        }

        void paletteListDropdownSelected(string command, float intensity, long frequency, int duration)
        {
            UnityHapticSample sample = (UnityHapticSample)target;

            if (sample.primitiveSequence == null)
                sample.primitiveSequence = new List<UnityPrimitiveData>();

            sample.primitiveSequence.Add(new UnityPrimitiveData()
            {
                command = command,
                intensity = intensity,
                frequency = frequency,
                duration = duration,
            });

            forceApplyModifications = true;
        }

        private void paletteDrawElementCallback(Rect rect, SerializedProperty element, GUIContent label, bool selected, bool focused)
        {
            SerializedProperty propCommand = element.FindPropertyRelative("command");
            SerializedProperty propIntensity = element.FindPropertyRelative("intensity");
            SerializedProperty propFrequency = element.FindPropertyRelative("frequency");
            SerializedProperty propDuration = element.FindPropertyRelative("duration");

            float y = rect.y;

            int commandIndex = EditorGUI.Popup(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "Command", HapticEditingConstants.ValueMapping.GetIndexForId(propCommand.stringValue, HapticEditingConstants.CommandMap), HapticEditingConstants.CommandMap.Select(x => x.Text).ToArray());

            string id = HapticEditingConstants.ValueMapping.GetIdForIndex(commandIndex, HapticEditingConstants.CommandMap);

            if (id != null)
                propCommand.stringValue = id;

            y += EditorGUIUtility.singleLineHeight;

            if (propCommand.stringValue != "pause")
            {
                propIntensity.floatValue = EditorGUI.FloatField(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "Intensity", propIntensity.floatValue);
                y += EditorGUIUtility.singleLineHeight;
            }

            if (propCommand.stringValue == "vibrate")
            {
                propFrequency.longValue = EditorGUI.LongField(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "Frequency", propFrequency.longValue);
                y += EditorGUIUtility.singleLineHeight;
            }

            propDuration.intValue = EditorGUI.IntField(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "Duration (ms)", propDuration.intValue);
            y += EditorGUIUtility.singleLineHeight;

            //HapticProject.UnityPrimitiveData data = proj.palette[paletteIndex].primitiveSequence[element.pa
        }

        private float paletteElementHeightCallback(SerializedProperty element)
        {
            SerializedProperty propCommand = element.FindPropertyRelative("command");

            float height = 0f;

            // Command (ALL)
            height += EditorGUIUtility.singleLineHeight;

            // Intensity (TICK, PULSE, VIBRATE)
            if (propCommand.stringValue != "pause")
                height += EditorGUIUtility.singleLineHeight;

            // Frequency (VIBRATE)
            if (propCommand.stringValue == "vibrate")
                height += EditorGUIUtility.singleLineHeight;

            // Duration (ALL)
            height += EditorGUIUtility.singleLineHeight;

            height += 10f;

            return height;
        }

        private void paletteListRemoveCallback(ReorderableList list)
        {
            UnityHapticSample sample = (UnityHapticSample)target;

            foreach (int index in list.Selected)
            {
                sample.primitiveSequence.RemoveAt(index);
            }

            forceApplyModifications = true;
        }

        private string paletteListElementNameCallback(SerializedProperty element)
        {
            return element.FindPropertyRelative("command").stringValue;
        }
        #endregion

    }
}