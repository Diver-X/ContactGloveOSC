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
            window.maxSize = new Vector2(600f, 470f); 
            window.minSize = new Vector2(300f, 225f+87f);
            window.LoadSettings();
        }

        private void OnGUI()
        {
            var mainlogo_texture = AssetDatabase.LoadAssetAtPath<Texture>("Packages/jp.diver-x.contactgloveosc/Editor/Logo/Diver-X_Logo.png");
            LogoDisplay(mainlogo_texture);

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
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(map(position.size.y,225f+87f,225f+87f+10f,80f,90f)));
                var style = new GUIStyle( EditorStyles.textArea ){wordWrap = true};
                EditorGUILayout.TextArea(statusMessage, style, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();

                GUILayout.Space(10);
            }
        }

        private RuntimeAnimatorController UpdateAnimatorController(AnimatorController controller)
        {
            string controllerPath = AssetDatabase.GetAssetPath(controller);
            string controllerName = System.IO.Path.GetFileNameWithoutExtension(controllerPath);

            // without "[ContactGloveOSC]" at the prefix.
            if (!controllerName.StartsWith("[ContactGloveOSC]"))
            {
                return UpdateAndSetController(controllerPath, controllerName);
            }
            else
            {
                // if prefixed with "[ContactGloveOSC]".
                string renamedControllerPath = "Assets/[ContactGloveOSC] RenamedController/[ContactGloveOSC] " + controllerName + ".controller";

                // Check if a controller with the same name exists in the destination.
                if (CheckControllerExistence(renamedControllerPath))
                {
                    // Overwrites a controller with the same name if one exists.
                    AssetDatabase.CopyAsset(controllerPath, renamedControllerPath);
                    AssetDatabase.Refresh();
                }

                return AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            }
        }

        private RuntimeAnimatorController UpdateAndSetController(string controllerPath, string controllerName)
        {
            // Path to duplicate and save to.
            string newPath = "Assets/[ContactGloveOSC] RenamedController/[ContactGloveOSC] " + controllerName + ".controller";

            // mkdir
            string directory = System.IO.Path.GetDirectoryName(newPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // Copy and save controller
            AssetDatabase.CopyAsset(controllerPath, newPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(); // refresh

            SyncStatus(GetLocalizedString("Copied Controller: ")+$"{controllerName}\n"+$" >> 'Assets/[ContactGloveOSC] RenamedController/[ContactGloveOSC] {controllerName} \n");

            return AssetDatabase.LoadAssetAtPath<AnimatorController>(newPath);     
        }

        private bool CheckControllerExistence(string controllerPath)
        {
            return AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null;
        }

        private float map(float x, float in_min, float in_max, float out_min, float out_max) {
        return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }

        private void LogoDisplay(Texture logo_texture){
            Texture2D texture2d = logo_texture as Texture2D;

            float originwid = (float)texture2d.width;
            float originvert = (float)texture2d.height;

            var Space = map(position.size.x,300f,500f,10f,110f);

            float wid = position.size.x - (Space*2);

            var viewPosition = new Vector2(Space, 0);
            var viewSize = new Vector2(wid, originvert*wid/originwid);
            var texturePosition = new Vector2(0f, 0);   
            var textureAspectRate = new Vector2(1f, 1f);

            EditorGUILayout.Space(wid*originvert/originwid);
                
            //show figure
            GUI.DrawTextureWithTexCoords(new Rect(viewPosition, viewSize), logo_texture, new Rect(texturePosition, textureAspectRate));
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

            //set controller VRC Avatar Descriptor.
            settings._avDescriptor.baseAnimationLayers[_layerSelect_gesture].animatorController = UpdateAnimatorController(selectedController_Gesture);
            settings._avDescriptor.baseAnimationLayers[_layerSelect_fx].animatorController = UpdateAnimatorController(selectedController_FX);
            //Update focus controller for rename. 
            selectedController_Gesture = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(settings._avDescriptor.baseAnimationLayers[_layerSelect_gesture].animatorController));
            selectedController_FX = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(settings._avDescriptor.baseAnimationLayers[_layerSelect_fx].animatorController));

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

            //set controller VRC Avatar Descriptor.
            settings._avDescriptor.baseAnimationLayers[_layerSelect_gesture].animatorController = UpdateAnimatorController(selectedController_Gesture);
            settings._avDescriptor.baseAnimationLayers[_layerSelect_fx].animatorController = UpdateAnimatorController(selectedController_FX);
            //Update focus controller for rename. 
            selectedController_Gesture = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(settings._avDescriptor.baseAnimationLayers[_layerSelect_gesture].animatorController));
            selectedController_FX = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(settings._avDescriptor.baseAnimationLayers[_layerSelect_fx].animatorController));

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
                    else if (key == "Copied Controller: ")
                        return "コントローラを複製しました: ";
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
