//AutomaticSetupSettings.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;

namespace ContactGloveOSC.Editor
{
    [CreateAssetMenu(fileName = "AutomaticSetupSettings", menuName = "ScriptableObjects/AutomaticSetupSettings", order = 1)]
    public class AutomaticSetupSettings : ScriptableObject
    {
        private static AutomaticSetupSettings instance;

        public static AutomaticSetupSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = UnityEditor.AssetDatabase.LoadAssetAtPath<AutomaticSetupSettings>("Packages/jp.diver-x.contactgloveosc/Editor/Data/AutomaticSetupSettings.asset");

                    if (instance == null)
                    {
                        instance = CreateInstance<AutomaticSetupSettings>();
                        instance.hideFlags = HideFlags.DontSave;
                        UnityEditor.AssetDatabase.CreateAsset(instance, "Packages/jp.diver-x.contactgloveosc/Editor/Data/AutomaticSetupSettings.asset");
                        UnityEditor.AssetDatabase.SaveAssets();
                    }
                }

                return instance;
           }
        }

        private void OnEnable()
        {
            instance = this;
        }

        public VRCAvatarDescriptor _avDescriptor;

        public int selectedLanguage;

        public bool toggleFullVer = true;
        public bool toggleLiteVer;
        public bool toggleHandSignSetup = false;

        public bool onwindow = false;

        public string autosetMessage = ""+"\n"+"";
    }
}
#endif
