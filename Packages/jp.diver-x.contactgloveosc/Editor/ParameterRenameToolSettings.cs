
//ParameterRenameToolSettings.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace ContactGloveOSC.Editor
{
    [CreateAssetMenu(fileName = "ParameterRenameToolSettings", menuName = "ScriptableObjects/ParameterRenameToolSettings", order = 1)]
    public class ParameterRenameToolSettings : ScriptableObject
    {

        private static ParameterRenameToolSettings instance;

        public static ParameterRenameToolSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = UnityEditor.AssetDatabase.LoadAssetAtPath<ParameterRenameToolSettings>("Packages/jp.diver-x.contactgloveosc/Editor/Data/ParameterRenameToolSettings.asset");

                    if (instance == null)
                    {
                        instance = CreateInstance<ParameterRenameToolSettings>();
                        UnityEditor.AssetDatabase.CreateAsset(instance, "Packages/jp.diver-x.contactgloveosc/Editor/Data/ParameterRenameToolSettings.asset");
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
    }
}
#endif