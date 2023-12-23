//ParameterRenameTool.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.IO;
using System.Linq;
using VRC.SDK3.Avatars.Components;

namespace ContactGloveOSC.Editor
{
    public class ParameterRenameTool : EditorWindow
    {
        private ParameterRenameToolSettings settings;

        private AutomaticSetupSettings autosetsettings;
        private AutomaticSetup automaticSetup;

        private string[] parameterNames = { "GestureLeft", "GestureRight", "GestureLeftWeight", "GestureRightWeight" };
        private string[] GestureParameterNames = { "_GestureLeft", "_GestureRight", "_GestureLeftWeight", "_GestureRightWeight" };
        private string[] FXParameterNames = { "cgGestureLeft", "cgGestureRight", "cgGestureLeftWeight", "cgGestureRightWeight" };


        private string statusMessage = "";
        private Vector2 scrollPosition = Vector2.zero;

        private string[] languageOptions = { "English", "日本語" };

        private AnimatorController selectedController_Gesture,selectedController_FX;
        private int _layerSelect_gesture = 2;
        private int _layerSelect_fx = 4;

        private int notParamFound = 0;

        [MenuItem("ContactGloveOSC/Parameter Rename Tool")]
        private static void ShowWindow()
        {
            ParameterRenameTool window = GetWindow(typeof(ParameterRenameTool), false, "Parameter Rename Tool") as ParameterRenameTool;
            window.maxSize = new Vector2(500f, 250f); 
            window.minSize = new Vector2(500f, 250f);
            window.LoadSettings();
        }

        private void OnGUI()
        {
            GUILayout.Label(GetLocalizedString("Select Language:"), EditorStyles.boldLabel);
            settings.selectedLanguage = EditorGUILayout.Popup(settings.selectedLanguage, languageOptions);

            GUILayout.Space(10);

            settings._avDescriptor = (VRCAvatarDescriptor)EditorGUILayout.ObjectField
            (
                new GUIContent
                (
                    "Avatar",
                    "Select an avatar with VRCAvatarDescriptor."
                ),
                settings._avDescriptor,
                typeof(VRCAvatarDescriptor),
                true
            );

            if (settings._avDescriptor != null)
            {

                selectedController_Gesture = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(settings._avDescriptor.baseAnimationLayers[_layerSelect_gesture].animatorController));
                selectedController_FX = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(settings._avDescriptor.baseAnimationLayers[_layerSelect_fx].animatorController));

                GUILayout.Space(10);

                if (GUILayout.Button(GetLocalizedString("Rename Parameters")))
                {
                    if (CheckParametersExist())
                    {
                        RenameParameters();
                        SyncStatus(GetLocalizedString("Parameters ( for Gesture & FX ) renamed successfully!\n"));
                    }
                    else
                    {
                        SyncStatus(GetLocalizedString("Rename aborted.\n"));
                    }
                }

                GUILayout.Space(10);

                if (GUILayout.Button(GetLocalizedString("Revert Changes")))
                {
                    if (RevertChanges())
                    {
                        SyncStatus(GetLocalizedString("Changes reverted successfully! ( for Gesture & FX )\n"));
                    }
                    else
                    {
                        SyncStatus(GetLocalizedString("No changes to revert.\n"));
                    }
                }

                GUILayout.Space(10);

                EditorGUILayout.LabelField(GetLocalizedString("Status:"), EditorStyles.boldLabel);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(80f));
                EditorGUILayout.TextArea(statusMessage, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(10);
            }
        }

        private void OnEnable()
        {
            ShowWindow();
        }

        private void OnDisable()
        {
            SaveSettings();
        }

        private void SyncStatus(string message)
        {
            // check Data folder 
            if (UnityEditor.AssetDatabase.IsValidFolder("Packages/jp.diver-x.contactgloveosc/Editor/Data"))
            {
                autosetsettings = AssetDatabase.LoadAssetAtPath<AutomaticSetupSettings>("Packages/jp.diver-x.contactgloveosc/Editor/Data/AutomaticSetupSettings.asset");
                if (autosetsettings != null)
                {
                    if (autosetsettings.onwindow)
                    {
                        automaticSetup = EditorWindow.GetWindow<AutomaticSetup>("Automatic Setup") as AutomaticSetup;
                        automaticSetup.statusMessage += "[ ParameterRenameTool ]: " + message;
                    }
                }
            }

            statusMessage += message;
        }

        private bool CheckParametersExist()
        {
            statusMessage = "";
            notParamFound = 0;

            if (selectedController_Gesture == null)
            {
                SyncStatus(GetLocalizedString("No Gesture AnimatorController selected!\n"));
            }

            if (selectedController_FX == null)
            {
                SyncStatus(GetLocalizedString("No FX AnimatorController selected!\n"));
            }

            if ((selectedController_Gesture == null) || (selectedController_FX == null))
            {
                return false;
            }

            foreach (string paramName in parameterNames)
            {
                
                if ( !selectedController_Gesture.parameters.Any(p => p.name == paramName) && !selectedController_Gesture.parameters.Any(p => p.name == "_" +paramName) )
                {
                    SyncStatus(string.Format(GetLocalizedString("Gesture Parameter '{0}' not found!\n"), paramName));
                    notParamFound += 1;
                }

                if ( !selectedController_FX.parameters.Any(p => p.name == paramName) && !selectedController_FX.parameters.Any(p => p.name == "cg" +paramName) )
                {
                    SyncStatus(string.Format(GetLocalizedString("FX Parameter '{0}' not found!\n"), paramName));
                    notParamFound += 1;
                }
            }

            if( notParamFound > 0 )
            {
                return false;
            }

            return true;
        }

        private void RenameParameters()
        {
            SaveSettings();

            statusMessage = "";

            if (selectedController_Gesture == null)
            {
                SyncStatus(GetLocalizedString("No Gesture AnimatorController selected!\n"));
            }

            if (selectedController_FX== null)
            {
                SyncStatus(GetLocalizedString("No FX AnimatorController selected!\n"));
            }
            if ((selectedController_Gesture == null) || (selectedController_FX == null))
            {
                return;
            }

            string controllerPath_Gesture = AssetDatabase.GetAssetPath(selectedController_Gesture);
            string controllerPath_FX = AssetDatabase.GetAssetPath(selectedController_FX);

            string controllerText_Gesture = File.ReadAllText(controllerPath_Gesture);
            string controllerText_FX = File.ReadAllText(controllerPath_FX);

            string[] selectedPatternNewNames_Gesture = GestureParameterNames;
            string[] selectedPatternNewNames_FX = FXParameterNames;

            for (int i = 0; i < parameterNames.Length; i++)
            {
                string oldName = parameterNames[i];
                string newName_gesture = selectedPatternNewNames_Gesture[i];
                string newName_fx = selectedPatternNewNames_FX[i];

                // Use a regular expression to match only whole words and replace them
                string pattern = "\\b" + oldName + "\\b";
                controllerText_Gesture = System.Text.RegularExpressions.Regex.Replace(controllerText_Gesture, pattern, newName_gesture);
                controllerText_FX = System.Text.RegularExpressions.Regex.Replace(controllerText_FX, pattern, newName_fx);
            }

            File.WriteAllText(controllerPath_Gesture, controllerText_Gesture);
            File.WriteAllText(controllerPath_FX, controllerText_FX);
            AssetDatabase.Refresh();
        }

        private bool RevertChanges()
        {
            statusMessage = "";

            if (selectedController_Gesture == null)
            {
                SyncStatus(GetLocalizedString("No Gesture AnimatorController selected!\n"));
            }

            if (selectedController_FX == null)
            {
                SyncStatus(GetLocalizedString("No FX AnimatorController selected!\n"));
            }

            if ((selectedController_Gesture == null) || (selectedController_FX == null))
            {
                return false;
            }

            string controllerPath_Gesture = AssetDatabase.GetAssetPath(selectedController_Gesture);
            string controllerPath_FX = AssetDatabase.GetAssetPath(selectedController_FX);

            string controllerText_Gesture = File.ReadAllText(controllerPath_Gesture);
            string controllerText_FX = File.ReadAllText(controllerPath_FX);

            string[] selectedPatternNewNames_Gesture = GestureParameterNames;
            string[] selectedPatternNewNames_FX = FXParameterNames;

            bool changesReverted = false;

            for (int i = 0; i < parameterNames.Length; i++)
            {
                string oldName_gesture = selectedPatternNewNames_Gesture[i];
                string oldName_fx = selectedPatternNewNames_FX[i];
                
                string newName_gesture = parameterNames[i];
                string newName_fx = parameterNames[i];

                // Use a regular expression to match only whole words and replace them
                string pattern_gesture = "\\b" + oldName_gesture + "\\b";
                string pattern_fx = "\\b" + oldName_fx + "\\b";

                controllerText_Gesture = System.Text.RegularExpressions.Regex.Replace(controllerText_Gesture, pattern_gesture, newName_gesture);
                controllerText_FX = System.Text.RegularExpressions.Regex.Replace(controllerText_FX, pattern_fx, newName_fx);

                // If any change is made, set changesReverted to true
                if ( (controllerText_Gesture != File.ReadAllText(controllerPath_Gesture) ) && (controllerText_FX != File.ReadAllText(controllerPath_FX)))
                {
                    changesReverted = true;
                }
            }

            if (changesReverted)
            {
                File.WriteAllText(controllerPath_Gesture, controllerText_Gesture);
                File.WriteAllText(controllerPath_FX, controllerText_FX);
                AssetDatabase.Refresh();
            }

            return changesReverted;
        }

        private string GetLocalizedString(string key)
        {
            switch (settings.selectedLanguage)
            {
                case 0: // English
                    return key;
                case 1: // 日本語
                    if (key == "Parameters ( for Gesture & FX ) renamed successfully!\n")
                        return "Gesture用 & FX用のパラメータへ正常にリネームされました。\n";
                    else if (key == "Changes reverted successfully! ( for Gesture & FX )\n")
                        return "変更が正常に戻されました。( Gesture用 & FX用パラメータ )\n";
                    else if (key == "Rename aborted.\n")
                        return "リネームをスキップ。\n";
                    else if (key == "No Gesture AnimatorController selected!\n")
                        return "Gesture Controller が選択されていません！\n";
                    else if (key == "No FX AnimatorController selected!\n")
                        return "FX Controller が選択されていません！\n";
                    else if (key == "Gesture Parameter '{0}' not found!\n")
                        return "Gesture パラメータ '{0}' が見つかりませんでした。\n";
                    else if (key == "FX Parameter '{0}' not found!\n")
                        return "FX パラメータ '{0}' が見つかりませんでした。\n";
                    else if (key == "No changes to revert.\n")
                        return "パラメータは既に元に戻っています。\n";
                    else if (key == "Changes reverted successfully!\n")
                        return "変更が正常に戻されました。\n";
                    else if (key == "Select Language:")
                        return "言語:";
                    else if (key == "Status:")
                        return "ステータス:";
                    else if (key == "Rename Parameters")
                        return "パラメータをリネーム";
                    else if (key == "Revert Changes")
                        return "変更を元に戻す";
                    else
                        return key;
                default:
                    return key;
            }
        }

        private void SaveSettings()
        {
            // check Data folder 
            if (!UnityEditor.AssetDatabase.IsValidFolder("Packages/jp.diver-x.contactgloveosc/Editor/Data"))
            {
            // mkdir
            AssetDatabase.CreateFolder("Packages/jp.diver-x.contactgloveosc/Editor", "Data");
            }
            settings = AssetDatabase.LoadAssetAtPath<ParameterRenameToolSettings>("Packages/jp.diver-x.contactgloveosc/Editor/Data/ParameterRenameToolSettings.asset");
            if (settings == null)
            {
                settings = CreateInstance<ParameterRenameToolSettings>();
                AssetDatabase.CreateAsset(settings, "Packages/jp.diver-x.contactgloveosc/Editor/Data/ParameterRenameToolSettings.asset");
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(); // refresh
        }

        private void LoadSettings()
        {
            // check Data folder 
            if (!UnityEditor.AssetDatabase.IsValidFolder("Packages/jp.diver-x.contactgloveosc/Editor/Data"))
            {
            // mkdir
            AssetDatabase.CreateFolder("Packages/jp.diver-x.contactgloveosc/Editor", "Data");
            }
            settings = AssetDatabase.LoadAssetAtPath<ParameterRenameToolSettings>("Packages/jp.diver-x.contactgloveosc/Editor/Data/ParameterRenameToolSettings.asset");
            if (settings == null)
            {
                settings = CreateInstance<ParameterRenameToolSettings>();
                AssetDatabase.CreateAsset(settings, "Packages/jp.diver-x.contactgloveosc/Editor/Data/ParameterRenameToolSettings.asset");
                SaveSettings(); // initial save
            }
        }

        //autosetup access process

        public void AutoSetRevertChanges()
        {
            if (RevertChanges())
            {
                SyncStatus(GetLocalizedString("Changes reverted successfully! ( for Gesture & FX )\n"));
            }
            else
            {
                SyncStatus(GetLocalizedString("No changes to revert.\n"));
            }
        }

        public void AutoSetRenameParameters()
        {
            if (CheckParametersExist())
            {
                RenameParameters();
                SyncStatus(GetLocalizedString("Parameters ( for Gesture & FX ) renamed successfully!\n"));
            }
            else
            {
                SyncStatus(GetLocalizedString("Rename aborted.\n"));
            }
        }

        public void SetAvatarDescriptor(VRCAvatarDescriptor avatarDescriptor)
        {
            settings._avDescriptor = avatarDescriptor;
            selectedController_Gesture = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(settings._avDescriptor.baseAnimationLayers[_layerSelect_gesture].animatorController));
            selectedController_FX = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(settings._avDescriptor.baseAnimationLayers[_layerSelect_fx].animatorController));
        }

        public void AutoSetLanguage(int language)
        {
            settings.selectedLanguage = language;
            Repaint();
        }
    }
}
#endif
