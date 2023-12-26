//HandSignCopyTool.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using System;

namespace ContactGloveOSC.Editor
{
    public class HandSignCopyTool : EditorWindow
    {
        private AutomaticSetupSettings autosetsettings;
        private AutomaticSetup automaticSetup;

        private string statusMessage = "";

        private HandSignCopyToolSettings settings;
        private Vector2 scrollPosition = Vector2.zero;
        
        private AnimatorController handsign_animatorController; // AnimatorController
        private int _layerSelect = 2; //GestureController

        private string[] languageOptions = { "English", "日本語" };

        [MenuItem("ContactGloveOSC/HandSign Copy Tool")]
        private static void ShowWindow()
        {
            HandSignCopyTool window = GetWindow(typeof(HandSignCopyTool), false, "HandSign Copy Tool") as HandSignCopyTool;
            window.minSize = new Vector2(800f, 480f+87f); // Increased initial width
            //window.maxSize = new Vector2(800f, 480f); // Increased initial width
            window.LoadSettings();
        }

        private void OnGUI()
        {
            var mainlogo_texture = AssetDatabase.LoadAssetAtPath<Texture>("Packages/jp.diver-x.contactgloveosc/Editor/Logo/Diver-X_Logo.png");
            LogoDisplay(mainlogo_texture);

            DrawLanguageSelection();

            EditorGUILayout.Space();

            //VRCAvatarDescriptor
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

            EditorGUILayout.BeginHorizontal();

            DrawLeftSide();
            DrawRightSide();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));

            EditorGUILayout.Space();


            if (GUILayout.Button(GetLocalizedString("Copy Gesture Animations")))
            {
                CopyAnimations();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button(GetLocalizedString("Initialize ContactGlove Gesture Animations")))
            {
                InitializeAnimations();
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(GetLocalizedString("Status:"), EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(map(position.size.y,480f+87f,480f+87f+10f,80f,90f)));
            var style = new GUIStyle( EditorStyles.textArea ){wordWrap = true};
            EditorGUILayout.TextArea(statusMessage, style, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(GetLocalizedString("ContactGlove Gesture Animations Directory"), EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Left  : Packages/ContactGloveOSC/Runtime/Gesture/HandSign[Experimental]/Left", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Right: Packages/ContactGloveOSC/Runtime/Gesture/HandSign[Experimental]/Right", EditorStyles.boldLabel);
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
                
            //画像表示
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

        private void SelectLeftEqualsRightSource()
        {
            for (int i = 0; i < 7; i++)
            {
                // Check if LeftSource field is not empty
                if (settings.leftSourceAnimations[i] != null)
                {
                    // Set RightSource field with the animation from LeftSource
                    settings.rightSourceAnimations[i] = settings.leftSourceAnimations[i];
                    settings.rightAnimationPaths[i] = settings.leftAnimationPaths[i];
                }
            }
        }

        private void DrawLanguageSelection()
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(GetLocalizedString("Select Language:"), EditorStyles.boldLabel);
            settings.selectedLanguage = EditorGUILayout.Popup(settings.selectedLanguage, languageOptions);

            EditorGUILayout.EndHorizontal();
        }


        private string GetLocalizedString(string key)
        {
            switch (settings.selectedLanguage)
            {
                case 0: // English
                    return key;
                case 1: // Japanese
                    if (key == "Select Left Source Gesture Animation:")
                        return "左手用ハンドサインを選択:";
                    else if (key == "Select Right Source Gesture Animation:")
                        return "右手用ハンドサインを選択:";
                    else if (key == "Copy Gesture Animations")
                        return "ハンドサインをコピー";
                    else if (key == "Initialize ContactGlove Gesture Animations")
                        return "ContactGlove用ハンドサインの初期化";
                    else if (key == "Status:")
                        return "ステータス:";
                    else if (key == "Select Language:")
                        return "言語:";
                    else if (key == "ContactGlove Gesture Animations Directory")
                        return "ContactGlove用ハンドサインAnimationの場所";
                    else if (key == "")
                        return "";
                    else if (key == "Set Left for Right Gesture Animation")
                        return "左手用ハンドサインを右手用にもセットする";
                    else if (key == "Auto-set from Gesture Controller")
                        return "Gesture Controller から自動セットする";
                    else if (key == "Initialized animations.\n")
                        return "Glove用Animationを全て初期化しました。\n";
                    else if (key == "Gesture Controller not selected in VRCAvatarDescriptor.\n")
                        return "Gesture Controller が VRCAvatarDescriptor で選択されていません。\n";
                    else if (key == "The structure of the GestureController may be different than expected. Refer to the Document and manually set the Animation to the field.\n")
                        return "GestureControllerの構造が想定と異なる可能性があります。Documentを参照し、手動でフィールドにAnimationをセットしてください。\n";
                    else if (key == "Initialized All animations field.\n")
                        return "Animationフィールドを全て初期化しました。\n";
                    else if (key == "Only Right Hand Animation Key was registered, Mirror for Left Hand Sign Some Animation was enabled.\n")
                        return "右手のキーのみが登録されていたため,いくつかの左手ハンドサインAnimationのMirrorを有効化しました。\n";
                    else if (key == "Only Left Hand Animation Key was registered, Mirror for Right Hand Sign Some Animation was enabled.\n")
                        return "左手のキーのみが登録されていたため,いくつかの右手ハンドサインAnimationのMirrorを有効化しました。\n";
                    else
                        return key;
                default:
                    return key;
            }
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
                        automaticSetup.statusMessage += "[ HandSignCopyTool ]: " + message;
                    }
                }
            }
            
            statusMessage += message;
        }

        private void DrawLeftSide()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2));

            EditorGUILayout.Space();
            if(settings._avDescriptor != null)
            {
                if (GUILayout.Button(GetLocalizedString("Auto-set from Gesture Controller")))
                {
                    //Controller read
                    handsign_animatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(settings._avDescriptor.baseAnimationLayers[_layerSelect].animatorController));
                    SetAnimationsFromAnimatorController();
                }
            }
            else
            {
                EditorGUILayout.LabelField(GetLocalizedString(""));
            }

            EditorGUILayout.LabelField(GetLocalizedString("Select Left Source Gesture Animation:"), EditorStyles.boldLabel);

            for (int i = 0; i < 7; i++)
            {
                AnimationClip previousClip = settings.leftSourceAnimations[i];
                settings.leftSourceAnimations[i] = EditorGUILayout.ObjectField(GetDefaultAnimationName(i, "L_"), settings.leftSourceAnimations[i], typeof(AnimationClip), false) as AnimationClip;

                if (settings.leftSourceAnimations[i] != previousClip && settings.leftSourceAnimations[i] != null)
                {
                    settings.leftAnimationPaths[i] = AssetDatabase.GetAssetPath(settings.leftSourceAnimations[i]);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRightSide()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2));

            EditorGUILayout.Space();

            if (GUILayout.Button(GetLocalizedString("Set Left for Right Gesture Animation")))
            {
                SelectLeftEqualsRightSource();
            }

            EditorGUILayout.LabelField(GetLocalizedString("Select Right Source Gesture Animation:"), EditorStyles.boldLabel);

            for (int i = 0; i < 7; i++)
            {
                AnimationClip previousClip = settings.rightSourceAnimations[i];
                settings.rightSourceAnimations[i] = EditorGUILayout.ObjectField(GetDefaultAnimationName(i, "R_"), settings.rightSourceAnimations[i], typeof(AnimationClip), false) as AnimationClip;

                if (settings.rightSourceAnimations[i] != previousClip && settings.rightSourceAnimations[i] != null)
                {
                    settings.rightAnimationPaths[i] = AssetDatabase.GetAssetPath(settings.rightSourceAnimations[i]);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void CopyAnimations()
        {
            statusMessage = ""; // Reset status message
            bool right_handsign = false; //false = left,true = right 
            int[] CheckMirrorEnableCount = { 0, 0 }; //[0]left [1]right

            for (int i = 0; i < 7; i++)
            {

                // left field filled
                if (settings.leftSourceAnimations[i] != null)
                {
                    right_handsign = false;
                    string leftDestinationName = $"{GetDefaultAnimationName(i, "L_")}";
                    string leftDestinationPath = $"Packages/jp.diver-x.contactgloveosc/Runtime/Gesture/HandSign[Experimental]/Left/[ContactGloveOSC]{leftDestinationName}.anim";
                    CopyAnimation(settings.leftSourceAnimations[i], leftDestinationPath, leftDestinationName, right_handsign, CheckMirrorEnableCount);
                }
                else
                {
                    // if left field is empty,skip copy
                    SyncStatus(
                        (settings.selectedLanguage == 0)
                        ? $"Left field {GetDefaultAnimationName(i, "L_")} is empty. Skipping...\n"
                        : $"無選択：{GetDefaultAnimationName(i, "L_")} スキップします。\n"
                        );
                }

                // right field filled
                if (settings.rightSourceAnimations[i] != null)
                {
                    right_handsign = true;
                    string rightDestinationName = $"{GetDefaultAnimationName(i, "R_")}";
                    string rightDestinationPath = $"Packages/jp.diver-x.contactgloveosc/Runtime/Gesture/HandSign[Experimental]/Right/[ContactGloveOSC]{rightDestinationName}.anim";
                    CopyAnimation(settings.rightSourceAnimations[i], rightDestinationPath, rightDestinationName, right_handsign, CheckMirrorEnableCount);
                }
                else
                {
                    // if right field is empty,skip copy
                    SyncStatus(
                        (settings.selectedLanguage == 0)
                        ? $"Right field {GetDefaultAnimationName(i, "R_")} is empty. Skipping...\n"
                        : $"無選択：{GetDefaultAnimationName(i, "R_")} スキップします。\n"
                        );
                }
            }

            if (CheckMirrorEnableCount[0] > 0)
            {
                SyncStatus(GetLocalizedString("Only Right Hand Animation Key was registered, Mirror for Left Hand Sign Some Animation was enabled.\n"));
            }
            if (CheckMirrorEnableCount[1] > 0)
            {
                SyncStatus(GetLocalizedString("Only Left Hand Animation Key was registered, Mirror for Right Hand Sign Some Animation was enabled.\n"));
            }
        }

        private void CopyAnimation(AnimationClip source, string destinationPath, string destinationName, bool destinationHand, int[] count)
        {
            AnimationClip destinationAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>(destinationPath);
            
            if (destinationAnimation != null)
            {
                // Do not change the name of the destination animetion
                string originalName = destinationAnimation.name;
                //destinationAnimation.name = destinationName;

                EditorUtility.CopySerialized(source, destinationAnimation);

                // Write back the Animation name after copying
                destinationAnimation.name = originalName;

                //Check exist Animator Key for finger in source. 
                if (!CheckHandKeyExists(source, destinationHand))
                {
                    //if left finger animator key only exist in right source,right destinationAnimation changes mirror enabled. 
                    AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(destinationAnimation);
                    clipSettings.mirror = true;
                    AnimationUtility.SetAnimationClipSettings(destinationAnimation, clipSettings);

                    if (!destinationHand)
                    {
                        count[0]++;
                    }
                    else
                    {
                        count[1]++;
                    }
                    
                }

                SyncStatus(
                    (settings.selectedLanguage == 0)
                    ? $"Copied {source.name} to {destinationName}\n"
                    : $"コピー完了： {source.name} から {destinationName}\n"
                    );
            }
            else
            {
                // Skip even if copy animation does not exist
                SyncStatus(
                    (settings.selectedLanguage == 0)
                    ? $"Destination animation not found: {destinationPath}. Skipping...\n"
                    : $"Glove用Animationが見つかりません。該当Animation： {destinationPath}. 処理をスキップ。\n"
                    );
            }
        }

        bool CheckHandKeyExists(AnimationClip animationClip, bool hand)
        {
            bool foundRightHand = false;
            bool foundLeftHand = false;

            foreach (var curveBinding in AnimationUtility.GetCurveBindings(animationClip))
            {
                if (curveBinding.propertyName.Contains("RightHand"))
                {
                    foundRightHand = true;
                }
                
                if (curveBinding.propertyName.Contains("LeftHand"))
                {
                    foundLeftHand = true;
                }
                // no else condition here
            }

            if (foundRightHand && foundLeftHand)
            {
                //both hand key exist
                return true;
            }
            else if (foundRightHand && !foundLeftHand)
            {   
                //only right hand key in left hand animation
                return (!hand) ? false : true ;
            }
            else if (!foundRightHand && foundLeftHand)  
            {   
                //only left hand key in right hand animation
                return (hand) ? false : true;
            }
            else
            {
                //Exception
                SyncStatus(
                    (settings.selectedLanguage == 0)
                    ? $"Required Animator key is missing from hand sign animation: {animationClip.name} for your avatar. \n"
                    : $"Avatarの ハンドサインAnimation: {animationClip.name} に 必要な　Animator key がありません。\n"
                );

                return true;
            }
        }

        private string GetDefaultAnimationName(int index, string sidePrefix)
        {
            switch (index)
            {
                //case 0: return $"{sidePrefix}Idle"; //no use idle
                case 0: return $"{sidePrefix}Fist";
                case 1: return $"{sidePrefix}Open";
                case 2: return $"{sidePrefix}Point";
                case 3: return $"{sidePrefix}Peace";
                case 4: return $"{sidePrefix}RockNRoll";
                case 5: return $"{sidePrefix}Gun";
                case 6: return $"{sidePrefix}ThumbsUP";
                default: return "";
            }
        }

        private void InitializeAnimations()
        {
            statusMessage = ""; 

            for (int i = 0; i < 7; i++)
            {
                // init left field 
                string leftDestinationName = $"{GetDefaultAnimationName(i, "L_")}";
                string leftDestinationPath = $"Packages/jp.diver-x.contactgloveosc/Runtime/Gesture/HandSign[Experimental]/Left/[ContactGloveOSC]{leftDestinationName}.anim";
                InitializeAnimation(leftDestinationPath, leftDestinationName);

                // init right field
                string rightDestinationName = $"{GetDefaultAnimationName(i, "R_")}";
                string rightDestinationPath = $"Packages/jp.diver-x.contactgloveosc/Runtime/Gesture/HandSign[Experimental]/Right/[ContactGloveOSC]{rightDestinationName}.anim";
                InitializeAnimation(rightDestinationPath, rightDestinationName);
            }

            SyncStatus(GetLocalizedString("Initialized animations.\n"));

            for (int i = 0; i < 7; i++)
            {
                settings.leftSourceAnimations[i] = null;
                settings.rightSourceAnimations[i] = null;
            }
            SyncStatus(GetLocalizedString("Initialized All animations field.\n"));
        }

        private void InitializeAnimation(string destinationPath, string destinationName)
        {
            AnimationClip destinationAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>(destinationPath);
            if (destinationAnimation != null)
            {
                // Loop Time/ Loop Pose to false、Cycle Offset = 0.(initialize)
                AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(destinationAnimation);
                clipSettings.loopTime = false;
                clipSettings.loopBlend = false;
                clipSettings.cycleOffset = 0;
                clipSettings.mirror = false;
                AnimationUtility.SetAnimationClipSettings(destinationAnimation, clipSettings);

                // initialize origin Animation
                destinationAnimation.ClearCurves();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            else
            {
                SyncStatus(
                    (settings.selectedLanguage == 0)
                    ? $"Failed to load destination animation {destinationName}. Please make sure it exists.\n"
                    : $"Glove用Animationのロードに失敗しました。該当Animation： {destinationName}を確認して下さい。\n"
                    );
            }
            
        }

        private void SetAnimationsFromAnimatorController()
        {
            // expected Layer names
            string[] expectedLayers = { "Left Hand", "Right Hand" };
            statusMessage = ""; 

            if (handsign_animatorController == null)
            {
                SyncStatus(GetLocalizedString("Gesture Controller not selected in VRCAvatarDescriptor.\n"));
                return;
            }

            // Iterate through layers in the AnimatorController
            foreach (AnimatorControllerLayer layer in handsign_animatorController.layers)
            {
                if (layer.name == "Left Hand" || layer.name == "Right Hand")
                {
                    SetAnimationsForLayer(layer);
                }
            }

            //If expected Layer is missing, display in statusMessage
            foreach (string expectedLayer in expectedLayers)
            {
                if (Array.FindIndex(handsign_animatorController.layers, L => L.name == expectedLayer) == -1)
                {
                    SyncStatus(
                        (settings.selectedLanguage == 0)
                        ? $"Expected layer not found: {expectedLayer}\n"
                        : $"Layerが見つかりません：{expectedLayer}\n"
                        );
                    
                }
            }
        }

        private void SetAnimationsForLayer(AnimatorControllerLayer layer)
        {
            // expected Animation names
            string[] expectedStates = { "Fist", "Open", "Point", "Peace", "RockNRoll", "Gun", "Thumbs up" };
            // flag all states are missing
            bool allStatesMissing = true;

            // Iterate through the states in the layer
            foreach (ChildAnimatorState state in layer.stateMachine.states)
            {
                int index = GetIndexFromStateName(state.state.name);
                if(index != -1)
                {
                    // Set animation for Left Hand or Right Hand based on the layer
                    if (layer.name == "Left Hand")
                    {
                        settings.leftSourceAnimations[index] = state.state.motion as AnimationClip;
                        settings.leftAnimationPaths[index] = AssetDatabase.GetAssetPath(settings.leftSourceAnimations[index]);
                    }
                    else if (layer.name == "Right Hand")
                    {
                        settings.rightSourceAnimations[index] = state.state.motion as AnimationClip;
                        settings.rightAnimationPaths[index] = AssetDatabase.GetAssetPath(settings.rightSourceAnimations[index]);
                    }
                    allStatesMissing = false;
                }
                
            }

            // If all states are missing, display Layer name in statusMessage
            if (allStatesMissing)
            {
                SyncStatus(
                    (settings.selectedLanguage == 0)
                    ? $"Layer: {layer.name}:All states are missing. \n"
                    : $"Layer: {layer.name}:全stateが見つかりません. \n"
                    );
                SyncStatus(GetLocalizedString("The structure of the GestureController may be different than expected. Refer to the Document and manually set the Animation to the field.\n"));
            }
            else
            {
                // If expected state is missing, display name in statusMessage
                foreach (string expectedState in expectedStates)
                {
                    if (Array.FindIndex(layer.stateMachine.states, s => s.state.name == expectedState) == -1)
                    {
                        SyncStatus(
                            (settings.selectedLanguage == 0)
                            ? $"Expected state not found: {expectedState} ( '{layer.name}' Layer )\n"
                            : $"stateが見つかりません: {expectedState} ( '{layer.name}' レイヤー )\n"
                            );
                    }
                }
            }

            if (!(allStatesMissing))
            {
                SyncStatus(
                    (settings.selectedLanguage == 0)
                    ? $"Auto Set animations from {layer.name} layer.\n"
                    : $"{layer.name} レイヤーから Animation を 自動セットしました。\n"
                    );
            }
            
        }

        private int GetIndexFromStateName(string stateName)
        {
            switch (stateName)
            {
                case "Fist": return 0;
                case "Open": return 1;
                case "Point": return 2;
                case "Peace": return 3;
                case "RockNRoll": return 4;
                case "Gun": return 5;
                case "Thumbs up": return 6;
                default: return -1; // Handle unexpected state names
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
            settings = AssetDatabase.LoadAssetAtPath<HandSignCopyToolSettings>("Packages/jp.diver-x.contactgloveosc/Editor/Data/HandSignCopyToolSettings.asset");
            if (settings == null)
            {
                settings = CreateInstance<HandSignCopyToolSettings>();
                AssetDatabase.CreateAsset(settings, "Packages/jp.diver-x.contactgloveosc/Editor/Data/HandSignCopyToolSettings.asset");
            }
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(); // reflesh
        }

        private void LoadSettings()
        {
            // check Data folder 
            if (!UnityEditor.AssetDatabase.IsValidFolder("Packages/jp.diver-x.contactgloveosc/Editor/Data"))
            {
                // mkdir
                AssetDatabase.CreateFolder("Packages/jp.diver-x.contactgloveosc/Editor", "Data");
            }
            settings = AssetDatabase.LoadAssetAtPath<HandSignCopyToolSettings>("Packages/jp.diver-x.contactgloveosc/Editor/Data/HandSignCopyToolSettings.asset");
            if (settings == null)
            {
                settings = CreateInstance<HandSignCopyToolSettings>();
                AssetDatabase.CreateAsset(settings, "Packages/jp.diver-x.contactgloveosc/Editor/Data/HandSignCopyToolSettings.asset");
                SaveSettings(); // init save
            }
        }

        //autosetup access process

        public void SetAvatarDescriptor(VRCAvatarDescriptor avatarDescriptor)
        {
            settings._avDescriptor = avatarDescriptor;
        }

        public void AutoSetFromGestureController()
        {
            if(settings._avDescriptor != null)
            {
                //Controller read
                handsign_animatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(settings._avDescriptor.baseAnimationLayers[_layerSelect].animatorController));
                SetAnimationsFromAnimatorController();
            }
        }

        public void AutoSetLanguage(int language)
        {
            settings.selectedLanguage = language;
            Repaint();
        }

        public void AutoSetInitializeAnimations()
        {
            InitializeAnimations();
        }

        public void AutoSetCopyAnimations()
        {
            CopyAnimations();
        }
    }
}
#endif
