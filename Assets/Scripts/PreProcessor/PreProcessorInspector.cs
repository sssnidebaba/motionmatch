#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

[CustomEditor(typeof(PreProcessor))]
public class PreProcessorInspector : Editor
{
    private PreProcessor preProcessor;

    private void OnEnable()
    {
        preProcessor = (PreProcessor)target;
    }

    public override void OnInspectorGUI()
    {

        DrawDefaultInspector();
        GUILayout.Space(20f);
        if (GUILayout.Button("PreProcess"))
        {
            preProcessor.RemoveDuplicateElements();
            if (!preProcessor.HasIdleClips())
            {
                preProcessor.Initialize();
                preProcessor.ProcessAnimationClips();
                preProcessor.DestoryModel();
                EditorUtility.SetDirty(preProcessor);
                AssetDatabase.SaveAssets();
            }
            else
            {
                preProcessor.Initialize();
                preProcessor.ProcessAnimationClips();
                preProcessor.DestoryModel();
                preProcessor.ProcessIdleAnimation();
                preProcessor.DestoryModel();
                EditorUtility.SetDirty(preProcessor);
                AssetDatabase.SaveAssets();
            }
            preProcessor.GenerateAnimationData(AssetDatabase.GenerateUniqueAssetPath(Path.Combine(AssetDatabase.GetAssetPath(target), "../") + "/AnimationData.asset"));
            serializedObject.ApplyModifiedProperties();
        }
        if (GUILayout.Button("Clear Tags"))
        {
            preProcessor.ClearTags();
        }
        // if(GUILayout.Button("TestKDTree"))
        // {
        //     preProcessor.GenerateKdTree();
        // }
        //if (GUILayout.Button("PreProcess(Idle Set Mode)"))
        //{
        //    preProcessor.ProcessIdleAnimation();
        //    preProcessor.DestoryModel();
        //    EditorUtility.SetDirty(preProcessor);
        //    AssetDatabase.SaveAssets();
        //}

        //if (GUILayout.Button("GenerateKDTree"))
        //{
        //    preProcessor.GenerateKdTree();
        //    preProcessor.GenerateKDTreeAssetFile(AssetDatabase.GenerateUniqueAssetPath(Path.Combine(AssetDatabase.GetAssetPath(target),"../")+"/KDTree.asset"));
        //}
    }
}
#endif