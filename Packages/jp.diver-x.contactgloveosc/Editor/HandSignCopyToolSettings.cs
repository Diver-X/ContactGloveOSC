// HandSignCopyToolSettings.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;

namespace ContactGloveOSC.Editor
{
    [CreateAssetMenu(fileName = "HandSignCopyToolSettings", menuName = "ScriptableObjects/HandSignCopyToolSettings", order = 1)]
    public class HandSignCopyToolSettings : ScriptableObject
    {
        private static HandSignCopyToolSettings instance;

        public static HandSignCopyToolSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = UnityEditor.AssetDatabase.LoadAssetAtPath<HandSignCopyToolSettings>("Packages/jp.diver-x.contactgloveosc/Editor/Data/HandSignCopyToolSettings.asset");

                    if (instance == null)
                    {
                        instance = CreateInstance<HandSignCopyToolSettings>();
                        UnityEditor.AssetDatabase.CreateAsset(instance, "Packages/jp.diver-x.contactgloveosc/Editor/Data/HandSignCopyToolSettings.asset");
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

        public AnimationClip[] leftSourceAnimations = new AnimationClip[7];
        public AnimationClip[] rightSourceAnimations = new AnimationClip[7];
        public string[] leftAnimationPaths = new string[7];
        public string[] rightAnimationPaths = new string[7];
        public VRCAvatarDescriptor _avDescriptor;

        public int selectedLanguage;
    }
}
#endif
