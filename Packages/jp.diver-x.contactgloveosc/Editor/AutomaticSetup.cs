//AutomaticSetup.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using VRC.SDK3.Avatars.Components;

namespace ContactGloveOSC.Editor
{
    public class AutomaticSetup : EditorWindow
    {
        private AutomaticSetupSettings settings;

        private string[] languageOptions = { "English", "日本語" };
        public string statusMessage = "";

        private Vector2 scrollPosition = Vector2.zero;
        private Vector2 miniscrollPosition = Vector2.zero;

        private ParameterRenameTool parameterRenameTool;
        private HandSignCopyTool handSignCopyTool;

        private bool setParametersAndCopyAnimationsClicked = false;
        private bool revertChangesAndInitializeClicked = false;

        [MenuItem("ContactGloveOSC/Automatic Setup")]
        private static void ShowWindow()
        {
            AutomaticSetup window = GetWindow<AutomaticSetup>("Automatic Setup");
            window.minSize = new Vector2(500f, 630f);
            window.maxSize = new Vector2(500f, 630f);
            window.LoadSettings();
        }

        private void OnGUI()
        {
            using (var verticalScope = new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.Space();

                GUILayout.Label(GetLocalizedString("Select Language:"), EditorStyles.boldLabel);
                settings.selectedLanguage = EditorGUILayout.Popup(settings.selectedLanguage, languageOptions);

                EditorGUILayout.Space();

                settings._avDescriptor = (VRCAvatarDescriptor)EditorGUILayout.ObjectField(
                    new GUIContent("Avatar", "Select an avatar with VRCAvatarDescriptor."),
                    settings._avDescriptor,
                    typeof(VRCAvatarDescriptor),
                    true
                );

                EditorGUILayout.Space();

                EditorGUILayout.LabelField(GetLocalizedString("ContactGloveOSC Settings"), EditorStyles.boldLabel);

                EditorGUILayout.Space();

                EditorGUI.BeginChangeCheck();
                settings.toggleFullVer = EditorGUILayout.Toggle("Full Ver: (177bit)", settings.toggleFullVer);

                settings.toggleLiteVer = (settings.toggleFullVer) ? false : true;

                settings.toggleLiteVer = EditorGUILayout.Toggle("Lite Ver: (107bit)", settings.toggleLiteVer);

                settings.toggleFullVer = (settings.toggleLiteVer) ? false : true;

                EditorGUILayout.Space();

                settings.toggleHandSignSetup = EditorGUILayout.ToggleLeft(GetLocalizedString("[Experimental] : HandSign Setup"), settings.toggleHandSignSetup);

                EditorGUILayout.Space();

                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));

                EditorGUILayout.Space();

                if (settings._avDescriptor != null)
                {

                    if (GUILayout.Button(GetLocalizedString("Auto Setup")))
                    {
                        statusMessage = "";
                        SetParametersInTools();
                        RemovePrefab();
                        SetupPrefab();

                        setParametersAndCopyAnimationsClicked = true; 
                    }

                    GUILayout.Space(10);

                    miniscrollPosition = EditorGUILayout.BeginScrollView(miniscrollPosition, GUILayout.Height(40f));
                    EditorGUILayout.TextArea(settings.autosetMessage, GUILayout.ExpandHeight(true));
                    EditorGUILayout.EndScrollView();

                    GUILayout.Space(10);

                    if (GUILayout.Button(GetLocalizedString("Revert Changes")))
                    {
                        statusMessage = "";                        
                        RevertChangesAndInitialize();
                        RemovePrefab();

                        revertChangesAndInitializeClicked = true;
                    }

                }
                else
                {
                    GUILayout.Space(102);
                }
                

                GUILayout.Space(10);

                EditorGUILayout.LabelField(GetLocalizedString("Status:"), EditorStyles.boldLabel);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300f));
                EditorGUILayout.TextArea(statusMessage, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space(10);

            if (setParametersAndCopyAnimationsClicked || revertChangesAndInitializeClicked)
            {
                CloseEditorWindows();

                setParametersAndCopyAnimationsClicked = false;
                revertChangesAndInitializeClicked = false;
            }
        }

        private void OnEnable()
        {

            ShowWindow();
            settings.onwindow = true;
        }

        private void OnDisable()
        {

            SaveSettings();
            settings.onwindow = false;
        }

        private string GetLocalizedString(string key)
        {
            switch (settings.selectedLanguage)
            {
                case 0: // English
                    return key;
                case 1: // 日本語
                    if (key == "Select Language:")
                        return "言語:";
                    else if (key == "ContactGloveOSC Settings")
                        return "ContactGloveOSC 設定";
                    else if (key == "Status:")
                        return "ステータス:";
                    else if (key == "Auto Setup")
                        return "自動セットアップ";
                    else if (key == "Revert Changes")
                        return "変更を元に戻す";
                    else if (key == "ParameterRenameTool not found in the scene.\n")
                        return "ParameterRenameTool が見つかりません。";
                    else if (key == "HandSignCopyTool not found in the scene.\n")
                        return "HandSignCopyTool が見つかりません。";
                    else if (key == "Setup Finished.")
                        return "セットアップ完了。";
                    else if (key == "HandSign Animations.\n")
                        return "全ハンドサインAnimation。";
                    else if (key == "Revert Changes Finished.\n")
                        return "全ての変更が元に戻りました。";
                    else if (key == "Failed to instantiate prefab: ")
                        return "設定用Prefabのインスタンス化に失敗しました。";
                    else if (key == "Prefab not found: ")
                        return "設定用Prefabが見つかりません。";
                    else if (key == "Now setup: ")
                        return "現在の設定: ";
                    else if (key == "HandSign(OSC): ")
                        return "ハンドサイン(OSC): ";
                    else if (key == "Disabled")
                        return "無効化";
                    else if (key == "Enabled")
                        return "有効化";
                    else if (key == "[Experimental] : HandSign Setup")
                        return "[実験的] : ハンドサインを設定する";
                    else
                        return key;
                default:
                    return key;
            }
        }

        private void SetParametersInTools()
        {
            VRCAvatarDescriptor avatarDescriptor = settings._avDescriptor;

            if (parameterRenameTool == null)
            {
                parameterRenameTool = EditorWindow.GetWindow<ParameterRenameTool>("Parameter Rename Tool") as ParameterRenameTool;
            }

            if (handSignCopyTool == null)
            {
                handSignCopyTool = EditorWindow.GetWindow<HandSignCopyTool>("Hand Sign Copy Tool") as HandSignCopyTool;
            }

            if (parameterRenameTool != null && handSignCopyTool != null)
            {
                parameterRenameTool.AutoSetLanguage(settings.selectedLanguage);
                parameterRenameTool.SetAvatarDescriptor(avatarDescriptor);
               
                parameterRenameTool.AutoSetRenameParameters();

                if ( settings.toggleHandSignSetup )
                { 
                    handSignCopyTool.AutoSetLanguage(settings.selectedLanguage);
                    handSignCopyTool.SetAvatarDescriptor(avatarDescriptor);
                    handSignCopyTool.AutoSetFromGestureController();
                    handSignCopyTool.AutoSetCopyAnimations();
                }
                
            }
            else
            {
                statusMessage += "[ AutomaticSetup ]: " + GetLocalizedString("ParameterRenameTool not found in the scene.\n");
                statusMessage += "[ AutomaticSetup ]: " + GetLocalizedString("HandSignCopyTool not found in the scene.\n"); 
            }
        }

        private void RevertChangesAndInitialize()
        {
            VRCAvatarDescriptor avatarDescriptor = settings._avDescriptor;

            if (parameterRenameTool == null)
            {
                parameterRenameTool = EditorWindow.GetWindow<ParameterRenameTool>("Parameter Rename Tool") as ParameterRenameTool;
            }

            if (handSignCopyTool == null)
            {
                handSignCopyTool = EditorWindow.GetWindow<HandSignCopyTool>("Hand Sign Copy Tool") as HandSignCopyTool;
            }

            if (parameterRenameTool != null && handSignCopyTool != null)
            {
                parameterRenameTool.AutoSetLanguage(settings.selectedLanguage);
                parameterRenameTool.SetAvatarDescriptor(avatarDescriptor);

                parameterRenameTool.AutoSetRevertChanges();

                handSignCopyTool.AutoSetLanguage(settings.selectedLanguage);
                handSignCopyTool.SetAvatarDescriptor(avatarDescriptor);
                handSignCopyTool.AutoSetInitializeAnimations();
            }

            statusMessage += "===\n[ AutomaticSetup ]: " + GetLocalizedString("Revert Changes Finished.\n");

            settings.autosetMessage = GetLocalizedString("Now setup: ") + "Not Set ContactGloveOSC\n";
            settings.autosetMessage += GetLocalizedString("HandSign(OSC): ") + GetLocalizedString("Disabled");
        }

        private void SetupPrefab()
        {
            if (settings.toggleFullVer)
            {
                if (settings.toggleHandSignSetup)
                {
                    SetupPrefabAtPath("Packages/jp.diver-x.contactgloveosc/Runtime/Prefabs/HandSign[Experimental]/[ContactGloveOSC]AutoSet_HandSign.prefab");
                    statusMessage += "===\n[ AutomaticSetup ]: " + GetLocalizedString("Setup Finished.") + " ContactGloveOSC Full ver\n";
                    statusMessage += "[ AutomaticSetup ]: " + GetLocalizedString("Setup Finished.") + " "+ GetLocalizedString("HandSign Animations.\n");

                    settings.autosetMessage = GetLocalizedString("Now setup: ") + "ContactGloveOSC Full ver\n";
                    settings.autosetMessage += GetLocalizedString("HandSign(OSC): ") + GetLocalizedString("Enabled");
                }
                else
                {
                    SetupPrefabAtPath("Packages/jp.diver-x.contactgloveosc/Runtime/Prefabs/[ContactGloveOSC]AutoSet.prefab");
                    statusMessage += "===\n[ AutomaticSetup ]: " + GetLocalizedString("Setup Finished.") + " ContactGloveOSC Full ver\n";

                    settings.autosetMessage = GetLocalizedString("Now setup: ") + "ContactGloveOSC Full ver\n";
                    settings.autosetMessage += GetLocalizedString("HandSign(OSC): ") + GetLocalizedString("Disabled");
                }

                
            }

            if (settings.toggleLiteVer)
            {
                if (settings.toggleHandSignSetup)
                {
                    SetupPrefabAtPath("Packages/jp.diver-x.contactgloveosc/Runtime/Prefabs/HandSign[Experimental]/[ContactGloveOSC]Lite_AutoSet_HandSign.prefab");
                    statusMessage += "===\n[ AutomaticSetup ]: " + GetLocalizedString("Setup Finished.") + " ContactGloveOSC Lite ver\n";
                    statusMessage += "[ AutomaticSetup ]: " + GetLocalizedString("Setup Finished.") + " " + GetLocalizedString("HandSign Animations.\n");

                    settings.autosetMessage = GetLocalizedString("Now setup: ") + "ContactGloveOSC Lite ver\n";
                    settings.autosetMessage += GetLocalizedString("HandSign(OSC): ") + GetLocalizedString("Enabled");
                }
                else
                {
                    SetupPrefabAtPath("Packages/jp.diver-x.contactgloveosc/Runtime/Prefabs/[ContactGloveOSC]Lite_AutoSet.prefab");
                    statusMessage += "===\n[ AutomaticSetup ]: " + GetLocalizedString("Setup Finished.") + " ContactGloveOSC Lite ver.\n";

                    settings.autosetMessage = GetLocalizedString("Now setup: ") + "ContactGloveOSC Lite ver\n";
                    settings.autosetMessage += GetLocalizedString("HandSign(OSC): ") + GetLocalizedString("Disabled");
                }

                
            }

        }

        private void SetupPrefabAtPath(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab != null)
            {
                GameObject instantiatedPrefab = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

                if (instantiatedPrefab != null)
                {
                    Undo.RegisterCreatedObjectUndo(instantiatedPrefab, "Instantiate Prefab");
                    instantiatedPrefab.transform.parent = settings._avDescriptor.transform;
                    instantiatedPrefab.transform.localPosition = Vector3.zero;
                    instantiatedPrefab.transform.localRotation = Quaternion.identity;
                    instantiatedPrefab.transform.localScale = Vector3.one;
                }
                else
                {
                    statusMessage += "\n[ AutomaticSetup ]: " + GetLocalizedString("Failed to instantiate prefab: ")+ prefabPath;
                }
            }
            else
            {
                statusMessage += "\n[ AutomaticSetup ]: " + GetLocalizedString("Prefab not found: ")+ prefabPath;
            }
        }

        private void RemovePrefab()
        {
            RemovePrefabByName("[ContactGloveOSC]AutoSet");
            RemovePrefabByName("[ContactGloveOSC]Lite_AutoSet");
            RemovePrefabByName("[ContactGloveOSC]AutoSet_HandSign");
            RemovePrefabByName("[ContactGloveOSC]Lite_AutoSet_HandSign");
        }

        private void RemovePrefabByName(string prefabName)
        {
            Transform prefabTransform = settings._avDescriptor.transform.Find(prefabName);

            if (prefabTransform != null)
            {
                Undo.DestroyObjectImmediate(prefabTransform.gameObject);
            }
        }

        

        private void CloseEditorWindows()
        {
            CloseWindow(parameterRenameTool);
            CloseWindow(handSignCopyTool);
        }

        private void CloseWindow(EditorWindow window)
        {
            if (window != null)
            {
                window.Close();
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
            settings = AssetDatabase.LoadAssetAtPath<AutomaticSetupSettings>("Packages/jp.diver-x.contactgloveosc/Editor/Data/AutomaticSetupSettings.asset");
            if (settings == null)
            {
                settings = CreateInstance<AutomaticSetupSettings>();
                AssetDatabase.CreateAsset(settings, "Packages/jp.diver-x.contactgloveosc/Editor/Data/AutomaticSetupSettings.asset");
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
            settings = AssetDatabase.LoadAssetAtPath<AutomaticSetupSettings>("Packages/jp.diver-x.contactgloveosc/Editor/Data/AutomaticSetupSettings.asset");
            if (settings == null)
            {
                settings = CreateInstance<AutomaticSetupSettings>();
                AssetDatabase.CreateAsset(settings, "Packages/jp.diver-x.contactgloveosc/Editor/Data/AutomaticSetupSettings.asset");
                SaveSettings(); // initial save
            }
        }
        public void WindowRepaint()
        {
            Repaint();
        }
    }
}
#endif
