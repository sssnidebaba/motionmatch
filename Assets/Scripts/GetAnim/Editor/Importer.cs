using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

public class Importer : AssetPostprocessor 
{
    //目前默认设置 导入的时候设置骨骼类型为人类 并以自己的模型创建avatar
    //可以改进
    //  设置开关-在需要的时候才使用这种导入方式
    //  更多导入设置-unity手册搜索ModelImporter查看其他参数
    //  导入的时候复用其他avatar 以节省内存
    void OnPreprocessModel()
    {
        ModelImporter model = (ModelImporter)assetImporter;
        if (model != null)
        {
            model.animationType = ModelImporterAnimationType.Human;
            model.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        }
    }
}