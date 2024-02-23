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
    // This custom editor is for the UnityHapticSample object, providing a user-friendly interface in the Unity Editor.
    [CustomEditor(typeof(UnityHapticSample))]
    public class HapticSampleInspector : UnityEditor.Editor
    {
        // Represents a list that allows reordering of its elements.
        private ReorderableList sequenceList;

        // Main method that defines the custom editor UI
        public override void OnInspectorGUI()
        {
            // Begin tracking changes on the serialized object.
            serializedObject.Update();

            // Setting the label width for the UI controls.
            EditorGUIUtility.labelWidth = 125f;

            // Cast the target object to UnityHapticSample for easier access.
            UnityHapticSample sample = (UnityHapticSample)target;

            // Initialize the primitiveSequence if it's null.
            if (sample.primitiveSequence == null)
                sample.primitiveSequence = new List<UnityPrimitiveData>();

            // Add some space for layout organization.
            GUILayout.Space(20);

            // Track changes in the following UI drawing block.
            EditorGUI.BeginChangeCheck();

            // Start a vertical group for the UI controls.
            EditorGUILayout.BeginVertical();
            // Draw the main section of the UI for haptic samples.
            DrawSamplesBox(sample);
            EditorGUILayout.EndVertical();

            // Reset the label width to default.
            EditorGUIUtility.labelWidth = 0f;

            // If there were any changes in the previous block, apply those to the serialized object.
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                // Mark the object as dirty to ensure it saves changes.
                EditorUtility.SetDirty(sample);
                serializedObject.Update();
            }
        }

        // Region for UI related to Haptic samples.
        #region Samples Palette UI

        // Draws the list of haptic samples.
        void DrawSamplesBox(UnityHapticSample sample)
        {
            // Find the serialized property for the primitive sequence.
            SerializedProperty seq = serializedObject.FindProperty("primitiveSequence");

            // If it's null, re-initialize it.
            if (seq == null)
            {
                sample.primitiveSequence = new List<UnityHapticSample.UnityPrimitiveData>();
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // Initialize the reorderable list if it's null.
            if (sequenceList == null)
            {
                sequenceList = new ReorderableList(seq, true, true, true, ReorderableList.ElementDisplayType.Expandable, null, "Sequence", null);
                // Assign callbacks to various list events.
                sequenceList.getElementNameCallback += paletteListElementNameCallback;
                sequenceList.onAddDropdownCallback += paletteListDropdownCallback;
                sequenceList.onRemoveCallback += paletteListRemoveCallback;
                sequenceList.drawElementCallback += paletteDrawElementCallback;
                sequenceList.getElementHeightCallback += paletteElementHeightCallback;
                sequenceList.label = new GUIContent("Sequence");
            }

            // Draw the list in the editor.
            sequenceList.DoLayoutList();
            // End Samples Box
        }

        private void paletteListDropdownCallback(Rect buttonRect, ReorderableList list)
        {
            // Create a new dropdown menu.
            GenericMenu addMenu = new GenericMenu();

            // Define dropdown items and their respective actions.
            addMenu.AddItem(new GUIContent(HapticEditingConstants.ValueMapping.GetTextForId("tick", HapticEditingConstants.CommandMap)), false, () =>
            {
                paletteListDropdownSelected("tick", 0.0f, 0, 10, 0, 0, 0, false, false);
            });

            addMenu.AddItem(new GUIContent(HapticEditingConstants.ValueMapping.GetTextForId("pulse", HapticEditingConstants.CommandMap)), false, () =>
            {
                paletteListDropdownSelected("pulse", 0.0f, 0, 10, 0, 0, 0, false, false);
            });

            addMenu.AddItem(new GUIContent(HapticEditingConstants.ValueMapping.GetTextForId("vibrate", HapticEditingConstants.CommandMap)), false, () =>
            {
                paletteListDropdownSelected("vibrate", 500.0f, 0, 50, 0, 0, 0, false, false);
            });

            addMenu.AddItem(new GUIContent(HapticEditingConstants.ValueMapping.GetTextForId("pause", HapticEditingConstants.CommandMap)), false, () =>
            {
                paletteListDropdownSelected("pause",  0.0f, 0, 10, 1, 0, 0, false, false);
            });

            // Display the menu.
            addMenu.ShowAsContext();
        }

        // Callback when an option is selected from the dropdown.
        void paletteListDropdownSelected(string command, float frequency, int frequency_time, int duration, float intensity, int intensity_time, int waveform, bool overlay, bool overDrive)
        {
            UnityHapticSample sample = (UnityHapticSample)target;

            if (sample.primitiveSequence == null)
                sample.primitiveSequence = new List<UnityPrimitiveData>();

            // Add a new haptic command with the provided parameters.
            sample.primitiveSequence.Add(new UnityPrimitiveData()
            {
                command = command,
                frequency = frequency,
                frequencyTime = frequency_time,
                duration = duration,
                intensity = intensity,
                intensityTime = intensity_time,
                //waveform = waveform,
                //overlay = overlay,
                //overDrive = overDrive,
            });
        }

        // Callback to render individual haptic sample entries in the list.
        private void paletteDrawElementCallback(Rect rect, SerializedProperty element, GUIContent label, bool selected, bool focused)
        {
            SerializedProperty propCommand = element.FindPropertyRelative("command");
            SerializedProperty propFrequency = element.FindPropertyRelative("frequency");
            SerializedProperty propFreqTime = element.FindPropertyRelative("frequencyTime");
            SerializedProperty propDuration = element.FindPropertyRelative("duration");
            SerializedProperty propIntensity = element.FindPropertyRelative("intensity");
            SerializedProperty propIntensityTime = element.FindPropertyRelative("intensityTime");
            //SerializedProperty propWaveform = element.FindPropertyRelative("waveform");
            //SerializedProperty propOverlay = element.FindPropertyRelative("overlay");
            //SerializedProperty propOverDrive = element.FindPropertyRelative("overDrive");


            float y = rect.y;

            int commandIndex = EditorGUI.Popup(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "Command", HapticEditingConstants.ValueMapping.GetIndexForId(propCommand.stringValue, HapticEditingConstants.CommandMap), HapticEditingConstants.CommandMap.Select(x => x.Text).ToArray());

            string id = HapticEditingConstants.ValueMapping.GetIdForIndex(commandIndex, HapticEditingConstants.CommandMap);

            if (id != null)
                propCommand.stringValue = id;

            y += EditorGUIUtility.singleLineHeight;

            

            if (propCommand.stringValue == "vibrate")
            {
                propFrequency.floatValue = EditorGUI.FloatField(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "Frequency", propFrequency.floatValue);
                y += EditorGUIUtility.singleLineHeight;

                propFreqTime.intValue = EditorGUI.IntField(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "Frq Timestamp (ms)", propFreqTime.intValue);
                y += EditorGUIUtility.singleLineHeight;
            }

            if (propCommand.stringValue != "pause")
            {
                propIntensity.floatValue = EditorGUI.FloatField(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "Intensity", propIntensity.floatValue);
                y += EditorGUIUtility.singleLineHeight;
            }

            if (propCommand.stringValue == "vibrate")
            {
                propIntensityTime.intValue = EditorGUI.IntField(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "Int Timestamp (ms)", propIntensityTime.intValue);
                y += EditorGUIUtility.singleLineHeight;

                /*propWaveform.intValue = EditorGUI.IntField(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "Waveform", propWaveform.intValue);
                y += EditorGUIUtility.singleLineHeight;

                propOverlay.boolValue = EditorGUI.Toggle(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "Overlay", propOverlay.boolValue);
                y += EditorGUIUtility.singleLineHeight;

                propOverDrive.boolValue = EditorGUI.Toggle(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "Over-Drive", propOverDrive.boolValue);
                y += EditorGUIUtility.singleLineHeight;*/
            }

            propDuration.intValue = EditorGUI.IntField(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "Duration (ms)", propDuration.intValue);
            y += EditorGUIUtility.singleLineHeight;

            //HapticProject.UnityPrimitiveData data = proj.palette[paletteIndex].primitiveSequence[element.pa
        }

        // Callback to determine the height of an element in the list.
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
                height += EditorGUIUtility.singleLineHeight + 40f;

            // Duration (ALL)
            height += EditorGUIUtility.singleLineHeight;

            height += 10f;

            return height;
        }

        // Callback to handle removal of selected entries from the list.
        private void paletteListRemoveCallback(ReorderableList list)
        {
            UnityHapticSample sample = (UnityHapticSample)target;

            foreach (int index in list.Selected)
            {
                sample.primitiveSequence.RemoveAt(index);
            }
        }

        // Callback to fetch the name of a list element.
        private string paletteListElementNameCallback(SerializedProperty element)
        {
            return element.FindPropertyRelative("command").stringValue;
        }
        #endregion

    }
}