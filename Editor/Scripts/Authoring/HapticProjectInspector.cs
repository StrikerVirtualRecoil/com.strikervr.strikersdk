using Malee.List;
using StrikerLink.Shared.Haptics.Engines.V1.Types;
using StrikerLink.Unity.Authoring;
using StrikerLink.Unity.Editor.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StrikerLink.Unity.Editor.Authoring
{
    // Provides a custom editor window in the Unity Editor for the HapticProject type.
    [CustomEditor(typeof(HapticProject))]
    public class HapticProjectInspector : UnityEditor.Editor
    {
        // Keeps track of whether each effect is expanded or collapsed in the UI.
        List<bool> effectsFoldoutToggles = new List<bool>();

        // A list of reorderable lists, allowing the effects' tracks to be reordered in the UI.
        List<List<ReorderableList>> reorderableEffectTrackLists = new List<List<ReorderableList>>();

        float curveHeight = 50; // The height of the curve for visualization.

        // Flags whether the object picker dialog has been opened.
        bool hasOpenedObjectPicker = false;

        // Indicates if the haptic data needs to be resent to devices.
        bool needsResend = true;

        // Enum to represent device selections with unique bitwise values.
        internal enum DeviceSelectionEnum
        {
            DeviceIndex0 = 0x1,
            DeviceIndex1 = 0x2,
            DeviceIndex3 = 0x4,
            DeviceIndex4 = 0x8,
            DeviceIndex5 = 0x16,
            DeviceIndex6 = 0x32,
            DeviceIndex7 = 0x64,
            DeviceIndex8 = 0x128,
            DeviceIndex9 = 0x256,
            DeviceIndex10 = 0x512,
            DeviceIndex11 = 0x1024,
            DeviceIndex12 = 0x2048,
            DeviceIndex13 = 0x4096,
            DeviceIndex14 = 0x8192,
            DeviceIndex15 = 0x16384,
            DeviceIndex16 = 0x32768
        }

        // Called when the custom editor window is displayed in the Unity Editor.
        public override void OnInspectorGUI()
        {
            HandleEvents(); // Check for and respond to various editor events.
            serializedObject.Update(); // Update the serialized object.

            EditorGUIUtility.labelWidth = 100f;
            HapticProject proj = (HapticProject)target;

            // Ensure all is well.
            if (proj.effects == null)
                proj.effects = new List<HapticProject.UnityHapticEffect>();

            GUILayout.Space(20);

            EditorGUI.BeginChangeCheck();

            proj.identifier = EditorGUILayout.DelayedTextField("Library Identifier", proj.identifier);

            GUILayout.Space(20);

            DrawEffectsBox(proj);

            GUILayout.Space(20);

            DrawButtons(proj);

            //sampleFoldoutGroup = EditorGUILayout.BeginFoldoutHeaderGroup(sampleFoldoutGroup, "Hello World");

            //EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUIUtility.labelWidth = 0f; // Reset

            if (EditorGUI.EndChangeCheck())
            {
                // If any changes were made in the inspector, apply those changes to the actual object.
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(proj); // Mark the project as modified.
                serializedObject.Update();
                needsResend = true; // Indicate data needs to be resent to devices.
            }
        }

        // Handles events related to the Unity Editor's object picker dialog.
        void HandleEvents()
        {
            if (Event.current.commandName == "ObjectSelectorClosed" && hasOpenedObjectPicker)
            {
                hasOpenedObjectPicker = false;
                int outputId = EditorGUIUtility.GetObjectPickerControlID();

                int trackIndex = (outputId & 0xffff) - 5000;
                int effectIndex = ((outputId >> 16) & 0xffff) - 5000;
                
                if (trackIndex >= 0 && effectIndex >= 0 && EditorGUIUtility.GetObjectPickerObject() is UnityHapticSample)
                {
                    UnityHapticSample selectedSample = (UnityHapticSample)EditorGUIUtility.GetObjectPickerObject();

                    //Debug.Log("Selected Object " + selectedSample.name + " for effect " + effectIndex + " and track " + trackIndex);

                    HapticProject proj = (HapticProject)target;

                    if(proj != null && proj.effects.Count > effectIndex && proj.effects[effectIndex].tracks.Count > trackIndex)
                    {
                        proj.effects[effectIndex].tracks[trackIndex].sequence.Add(new HapticProject.UnityHapticSequence()
                        {
                            sampleObject = selectedSample,
                        });
                    }
                }
            }
        }

        #region Effects UI
        // Draws the UI section for editing effects in the HapticProject.
        void DrawEffectsBox(HapticProject proj)
        {
            //EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal(StrikerEditorUtility.CreateStyleWithPadding(5));

            GUILayout.Label("Effects", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            bool doCreateEffectsEntry = false;

            if (GUILayout.Toolbar(-1, new string[] { "+" }) == 0)
            {
                doCreateEffectsEntry = true; // We create it after handling the reorderable lists, due to serialisation weirdness
            }

            EditorGUILayout.EndHorizontal();

            while (effectsFoldoutToggles.Count < proj.effects.Count)
            {
                effectsFoldoutToggles.Add(false);// Ensure we have enough toggles for each effect.
            }

            while (reorderableEffectTrackLists.Count < proj.effects.Count)
            {
                reorderableEffectTrackLists.Add(new List<ReorderableList>());// Ensure we have enough reorderable lists for each effect.
            }

            if (doCreateEffectsEntry)
            {
                proj.effects.Add(new HapticProject.UnityHapticEffect() { id = "new_effect_" + proj.effects.Count.ToString(), tracks = new List<HapticProject.UnityHapticTrack>() });
                effectsFoldoutToggles.Add(true);
            }

            EditorGUILayout.BeginVertical(StrikerEditorUtility.CreateStyleWithPadding(15, 0));

            EditorGUI.indentLevel++;

            int deleteIndex = -1;

            // Loops through and displays each effect in the HapticProject.
            for (int i = 0; i < proj.effects.Count; i++)
            {
                EditorGUILayout.BeginVertical(new GUIStyle(EditorStyles.helpBox).WithMargin(0, 0, 0, 25));

                EditorGUILayout.BeginHorizontal(new GUIStyle());

                effectsFoldoutToggles[i] = EditorGUILayout.BeginFoldoutHeaderGroup(effectsFoldoutToggles[i], proj.effects[i].id, StrikerEditorUtility.CreateTransparentFoldoutHeader().WithMargin(15, 5, 0, 0));

                int toolbarPress = GUILayout.Toolbar(-1, new GUIContent[] { EditorGUIUtility.IconContent("TreeEditor.Trash"), EditorGUIUtility.IconContent("PlayButton") }, GUILayout.ExpandWidth(false));

                if (toolbarPress == 0)
                {
                    if(EditorUtility.DisplayDialog("Are you sure?", "Delete Effect '" + proj.effects[i].id + "'?", "Yes", "Never Mind"))
                        deleteIndex = i;
                } else if(toolbarPress == 1)
                {
                    TestHaptic(proj.effects[i].id);
                }

                EditorGUILayout.EndHorizontal();

                if (effectsFoldoutToggles[i])
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.BeginVertical(new GUIStyle().WithMargin(0, 25, 10, 10));

                    proj.effects[i].id = EditorGUILayout.TextField("Effect ID", proj.effects[i].id);

                    EditorGUILayout.Space(10);

                    if (proj.effects[i].tracks == null)
                        proj.effects[i].tracks = new List<HapticProject.UnityHapticTrack>();

                    int trackDeleteIndex = -1;

                    // Loops through and displays each track for the current effect.
                    for (int j = 0; j < proj.effects[i].tracks.Count; j++)
                    {
                        EditorGUILayout.BeginVertical(new GUIStyle(EditorStyles.helpBox).WithMargin(15, 0, 0, 20).WithPadding(0, 35, 10, 10));

                        EditorGUILayout.BeginHorizontal(new GUIStyle().WithMargin(10, 10, 0, 0));
                        GUILayout.Label("Track " + j, new GUIStyle(EditorStyles.boldLabel).WithMargin(10, 0, 0, 0));

                        int trackToolbarPress = GUILayout.Toolbar(-1, new GUIContent[] { EditorGUIUtility.IconContent("TreeEditor.Trash") }, GUILayout.ExpandWidth(false));

                        if(trackToolbarPress == 0)
                        {
                            if (EditorUtility.DisplayDialog("Are you sure?", "Delete Track " + j + " on effect '" + proj.effects[i].id + "'?", "Yes", "Never Mind"))
                                trackDeleteIndex = j;
                        }

                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(5);

                        int deviceIdIndex = EditorGUILayout.Popup("Device", HapticEditingConstants.ValueMapping.GetIndexForId(proj.effects[i].tracks[j].deviceId, HapticEditingConstants.DeviceIdMap), HapticEditingConstants.DeviceIdMap.Select(x => x.Text).ToArray());

                        string id = HapticEditingConstants.ValueMapping.GetIdForIndex(deviceIdIndex, HapticEditingConstants.DeviceIdMap);

                        if(id != null)
                            proj.effects[i].tracks[j].deviceId = id;

                        EditorGUILayout.Space(10);

                        if (reorderableEffectTrackLists[i].Count <= j)
                        {
                            SerializedProperty effects = serializedObject.FindProperty("effects");
                            if (effects == null)
                                continue;

                            SerializedProperty entry = effects.GetArrayElementAtIndex(i);
                            if (entry == null)
                                continue;

                            SerializedProperty trackList = entry.FindPropertyRelative("tracks");
                            if (trackList == null)
                                continue;

                            SerializedProperty track = trackList.GetArrayElementAtIndex(j);
                            if (track == null)
                                continue;

                            SerializedProperty seqList = track.FindPropertyRelative("sequence");
                            if (seqList == null)
                                continue;

                            ReorderableList list = new ReorderableList(seqList, true, true, true, ReorderableList.ElementDisplayType.Expandable, null, "Sequence", null);

                            int localEffectIndex = i;
                            int localTrackIndex = j;

                            list.getElementNameCallback += (element) => { return effectSequenceListElementNameCallback(element, localEffectIndex, localTrackIndex); };
                            list.onAddDropdownCallback += (rect, l) => { effectSequenceListDropdownCallback(rect, l, localEffectIndex, localTrackIndex); };
                            list.onRemoveCallback += (l) => { effectSequenceListRemoveCallback(l, localEffectIndex, localTrackIndex); };
                            list.drawElementCallback += (rect, element, label, selected, focused) => { effectSequenceDrawElementCallback(rect, element, label, selected, focused, localEffectIndex, localTrackIndex); };
                            list.getElementHeightCallback += effectSequenceElementHeightCallback;
                            list.label = new GUIContent("Sequence");

                            reorderableEffectTrackLists[i].Add(list);
                        }

                        // Display the reorderable list for the sequence of haptic samples in the current track.
                        reorderableEffectTrackLists[i][j].DoLayoutList();

                        EditorGUILayout.EndVertical();
                    }

                    if(trackDeleteIndex >= 0)
                    {
                        proj.effects[i].tracks.RemoveAt(trackDeleteIndex);
                        reorderableEffectTrackLists[i].RemoveAt(trackDeleteIndex);
                        reorderableEffectTrackLists.Clear();
                    }

                    if(GUILayout.Button("New Track", new GUIStyle(GUI.skin.button).WithMargin(10, 0, 0, 0)))
                    {
                        ShowAddTrackMenu(i);
                    }

                    EditorGUILayout.EndVertical();

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUILayout.EndVertical();
            }

            if(deleteIndex >= 0)
            {
                effectsFoldoutToggles.RemoveAt(deleteIndex);
                reorderableEffectTrackLists.Clear();
                proj.effects.RemoveAt(deleteIndex);
                serializedObject.Update();
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();
        }

        private void effectSequenceListDropdownCallback(Rect buttonRect, ReorderableList list, int effectIndex, int trackIndex)
        {   
            EditorGUIUtility.ShowObjectPicker<UnityHapticSample>(null, false, null, ((effectIndex + 5000) << 16) | (trackIndex + 5000));
            hasOpenedObjectPicker = true;  
        }

        void ShowAddTrackMenu(int effectIndex)
        {
            //GenericMenu addMenu = new GenericMenu();

            HapticProject proj = (HapticProject)target;

            /*for(int i = 0; i < HapticEditingConstants.DeviceIdMap.Count; i++)
            {
                int optIndex = i;
                addMenu.AddItem(new GUIContent(HapticEditingConstants.DeviceIdMap[optIndex].Text), false, () =>
                {
                    proj.effects[effectIndex].tracks.Add(new HapticProject.UnityHapticTrack()
                    {
                        deviceId = HapticEditingConstants.DeviceIdMap[optIndex].Text,
                        sequence = new List<HapticProject.UnityHapticSequence>()
                    });
                });
            }*/

            // Add a new track without setting the deviceId
        
                proj.effects[effectIndex].tracks.Add(new HapticProject.UnityHapticTrack()
                {
                    deviceId = "", // Initially, the deviceId is not set
                    sequence = new List<HapticProject.UnityHapticSequence>()
                });
           

            //addMenu.ShowAsContext();
        }

        private void effectSequenceDrawElementCallback(Rect rect, SerializedProperty element, GUIContent label, bool selected, bool focused, int effectIndex, int trackIndex)
        {
            HapticProject proj = (HapticProject)target;
            string deviceId = proj.effects[effectIndex].tracks[trackIndex].deviceId;
            

            // Get the deviceId property from the track
            //SerializedProperty deviceIdProp = HapticEditingConstants.DeviceIdMap;  //element.FindPropertyRelative("deviceId");


            SerializedProperty overlayProp = element.FindPropertyRelative("overlay");
            SerializedProperty overdriveProp = element.FindPropertyRelative("overdrive");
            SerializedProperty waveformProp = element.FindPropertyRelative("waveform");

            //SerializedProperty propSampleId = element.FindPropertyRelative("sampleId");
            SerializedProperty propSampleObject = element.FindPropertyRelative("sampleObject");

            SerializedProperty propModifyIntensity = element.FindPropertyRelative("modifyIntensity");
            SerializedProperty propIntensityCurve = element.FindPropertyRelative("intensityCurve");

            SerializedProperty propModifyFrequency = element.FindPropertyRelative("modifyFrequency");
            SerializedProperty propFrequencyCurve = element.FindPropertyRelative("frequencyCurve");

            SerializedProperty propModifyDuration = element.FindPropertyRelative("modifyDuration");
            SerializedProperty propDurationTarget = element.FindPropertyRelative("durationTarget");

            if (propSampleObject == null)
                return;

            float y = rect.y;

            

            propSampleObject.objectReferenceValue = EditorGUI.ObjectField(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "Sample", propSampleObject.objectReferenceValue, typeof(UnityHapticSample), false);
            y += EditorGUIUtility.singleLineHeight + 15f;

            bool guiWasEnabled = GUI.enabled;

            if (propSampleObject.objectReferenceValue == null)
                GUI.enabled = false;

            bool oldPropModifyIntensity = propModifyIntensity.boolValue;
            propModifyIntensity.boolValue = EditorGUI.ToggleLeft(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "Modify Intensity", propModifyIntensity.boolValue);
            y += EditorGUIUtility.singleLineHeight + 5f;

            if (propModifyIntensity.boolValue)
            {
                propIntensityCurve.animationCurveValue = EditorGUI.CurveField(new Rect(rect.x, y, rect.width, curveHeight), "Intensity Curve", propIntensityCurve.animationCurveValue);

                y += curveHeight + 15f;
            }


            propModifyFrequency.boolValue = EditorGUI.ToggleLeft(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "Modify Frequency", propModifyFrequency.boolValue);
            y += EditorGUIUtility.singleLineHeight + 5f;

            if (propModifyFrequency.boolValue)
            {
                propFrequencyCurve.animationCurveValue = EditorGUI.CurveField(new Rect(rect.x, y, rect.width, curveHeight), "Frequency Curve", propFrequencyCurve.animationCurveValue);
                y += curveHeight + 15f;
            }


            propModifyDuration.boolValue = EditorGUI.ToggleLeft(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "Modify Duration", propModifyDuration.boolValue);
            y += EditorGUIUtility.singleLineHeight + 5f;

            if (propModifyDuration.boolValue)
            {
                float oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 150f;
                propDurationTarget.floatValue = EditorGUI.FloatField(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "Duration Target (ms)", propDurationTarget.floatValue);
                y += EditorGUIUtility.singleLineHeight;

                EditorGUIUtility.labelWidth = oldWidth;
            }

            // Conditional UI based on device type
            if (deviceId.Equals("hammerTop"))
            {
                overlayProp.boolValue = EditorGUI.ToggleLeft(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "Overlay", overlayProp.boolValue);
                y += EditorGUIUtility.singleLineHeight + 5f;
            }
            else
            {
                overlayProp.boolValue = EditorGUI.ToggleLeft(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "Overlay", overlayProp.boolValue);
                y += EditorGUIUtility.singleLineHeight + 5f;

                overdriveProp.boolValue = EditorGUI.ToggleLeft(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "Overdrive", overdriveProp.boolValue);
                y += EditorGUIUtility.singleLineHeight + 5f;

                waveformProp.enumValueIndex = EditorGUI.Popup(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "Waveform", waveformProp.enumValueIndex, waveformProp.enumDisplayNames);
                y += EditorGUIUtility.singleLineHeight + 5f;
            }

            GUI.enabled = guiWasEnabled;
        }

        private float effectSequenceElementHeightCallback(SerializedProperty element)
        {
            SerializedProperty propModifyIntensity = element.FindPropertyRelative("modifyIntensity");
            SerializedProperty propModifyFrequency = element.FindPropertyRelative("modifyFrequency");
            SerializedProperty propModifyDuration = element.FindPropertyRelative("modifyDuration");

            if (propModifyIntensity == null)
                return 0f;

            float height = 0f;

            // Sample ID
            height += EditorGUIUtility.singleLineHeight + 15f;

            // Modify Intensity, Frequency, Duration, Overlay, Overdrive, and Waveform toggles
            height += (EditorGUIUtility.singleLineHeight + 15f) * 6;

            // Intensity Curve
            if (propModifyIntensity.boolValue)
                height += curveHeight;

            // Frequency Curve
            if (propModifyFrequency.boolValue)
                height += curveHeight;

            // Duration Field
            if (propModifyDuration.boolValue)
                height += EditorGUIUtility.singleLineHeight;

            height += 10f;

            return height;
        }

        private void effectSequenceListRemoveCallback(ReorderableList list, int effectIndex, int trackIndex)
        {
            HapticProject proj = (HapticProject)target;

            foreach (int index in list.Selected)
            {
                proj.effects[effectIndex].tracks[trackIndex].sequence.RemoveAt(index);
            }
        }

        private string effectSequenceListElementNameCallback(SerializedProperty element, int effectIndex, int trackIndex)
        {
            return element.FindPropertyRelative("sampleId")?.stringValue ?? "[ERROR]";
        }

        #endregion

        #region Buttons
        void DrawButtons(HapticProject proj)
        {
            EditorGUILayout.BeginVertical();

            float previousWidth = EditorGUIUtility.labelWidth;

            EditorGUIUtility.labelWidth = 150f;

            DeviceSelectionEnum deviceSelection = (DeviceSelectionEnum)EditorGUILayout.EnumFlagsField("Preview Devices: ", StrikerEditorUtility.GetPreviewDevices());

            EditorGUIUtility.labelWidth = previousWidth;

            StrikerEditorUtility.StorePreviewDevices(deviceSelection);

            if(GUILayout.Button("Send Library to Runtime"))
            {
                SendAsset(true);
            }

            if(GUILayout.Button("Export Library JSON"))
            {
                ExportAsset();
            }

            EditorGUILayout.EndVertical();
        }
        #endregion

        void ExportAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("Saving Haptic Library as asset...", target.name, "hapt", "Where would you like to save your compiled haptic library asset?");

            if (string.IsNullOrEmpty(path))
                return;

            HapticProject proj = (HapticProject)target;

            System.IO.File.WriteAllText(path, proj.ToJson());

            AssetDatabase.Refresh();
        }

        void TestHaptic(string haptic)
        {
            if (Application.isPlaying)
            {
                Shared.Client.StrikerClient client = GetClient();

                if (client == null || !client.IsConnected)
                {
                    Debug.LogError("[STRIKER] No client currently initialised, use in a scene with a StrikerController present!");
                }
                else
                {
                    if (needsResend)
                    {
                        SendAsset(false);
                        System.Threading.Thread.Sleep(500);
                    }

                    HapticProject proj = (HapticProject)target;

                    bool fired = false;

                    DeviceSelectionEnum previewDevices = StrikerEditorUtility.GetPreviewDevices();

                    int i = 0;

                    foreach (System.Enum value in System.Enum.GetValues(typeof(DeviceSelectionEnum)))
                    {
                        if (previewDevices.HasFlag(value) && client.GetDevice(i) != null && client.GetDevice(i).IsReady)
                        {
                            fired = true;
                            client.FireHaptic(0, GetController().libraryPrefix + proj.identifier, haptic);
                            Debug.Log("[STRIKER] Previewing " + haptic + " on " + ((DeviceSelectionEnum)value).ToString());
                        }

                        i++;
                    }

                    if (!fired)
                        Debug.Log("[STRIKER] No devices selections to preview this haptic on, or the selected devices are not connected");
                }
            }
            else
            {
                Debug.LogError("[STRIKER] Haptic Preview is only available in play mode, in a scene with an active StrikerController script");
            }
        }

        void SendAsset(bool showConfirmation = false)
        {
            if(Application.isPlaying)
            {
                Shared.Client.StrikerClient client = GetClient();

                if(client == null || !client.IsConnected)
                {
                    Debug.LogError("[STRIKER] No client currently initialised, use in a scene with a StrikerController present!");
                } else
                {
                    HapticProject proj = (HapticProject)target;
                    client.UpdateAppLibrary(GetController().libraryPrefix + proj.identifier, proj.ToJson());

                    if(showConfirmation)
                        EditorUtility.DisplayDialog("Success!", "The library has been sent to the runtime", "Ok");

                    needsResend = false;
                }
            } else
            {
                Debug.LogError("[STRIKER] Haptic Testing is only available in play mode, in a scene with an active StrikerController script");
            }
        }

        Runtime.Core.StrikerController GetController()
        {
            return Runtime.Core.StrikerController.Controller;
        }

        Shared.Client.StrikerClient GetClient()
        {
            if (Application.isPlaying)
            {
                Runtime.Core.StrikerController controller = GetController();

                if (controller != null)
                    return controller.GetClient();
                else
                    return null;
            }
            else
                return null;
        }
    }
}
