using UnityEngine;
using UnityEditor;
using System.IO;

public class GetAnim : EditorWindow
{
    //GetAnim类中所有路径string 均统一"\"为路径分隔符
    //GetFBX区域的参数
    string blenderPath = Config.blenderPath;        // = ""
    string bvhFolder = "";                              //此项参数变动性较大 故不设Config
    string fbxFolder = Config.fbxFolder;            // = ""
    int batchSize = Config.batchSize;               // = 10

    //GetAnim区域的参数
    string fbxFolderInAsset = "";                       //同上
    string animFolder = Config.animFolder;          // = "Assets\\Anim\\"
    
    [UnityEditor.MenuItem("getAnim/window")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(GetAnim));
    }
    
    private void OnEnable() 
    {
        // 给'选择'这项委托增加函数
        Selection.selectionChanged += OnSlctChanged;
    }

    void OnGUI ()
    {
        GUI.skin.button.wordWrap = true;

        #region GetFBX
        GUILayout.Label ("GetFBX", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        blenderPath = EditorGUILayout.TextField ("  BlenderPath", blenderPath);
        if(GUILayout.Button("BlenderPath", GUILayout.Width(0)))
        {
            //检查路径正确则打开 否则默认C盘
            if(blenderPath.EndsWith("\\blender.exe"))
            {
                blenderPath = EditorUtility.OpenFilePanel("Select Blender.exe", blenderPath.Replace("\\blender.exe", ""), "exe");
            }
            else
            {
                blenderPath = EditorUtility.OpenFilePanel("Select Blender.exe", "C:", "exe");
            }
            blenderPath = blenderPath.Replace("/", "\\");
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        bvhFolder = EditorGUILayout.TextField ("  bvhFolder", bvhFolder);
        if(GUILayout.Button("bvhFolder", GUILayout.Width(0)))
        {
            bvhFolder = EditorUtility.OpenFolderPanel("Select bvh Folder", bvhFolder, "");
            bvhFolder = bvhFolder.Replace("/","\\");
            if(bvhFolder != "" && !bvhFolder.EndsWith("\\"))
            {
                bvhFolder += "\\";
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        fbxFolder = EditorGUILayout.TextField ("  fbxFolder", fbxFolder);
        if(GUILayout.Button("fbxFolder", GUILayout.Width(0)))
        {
            fbxFolder = EditorUtility.OpenFolderPanel("Select fbx Folder", fbxFolder, "");
            fbxFolder = fbxFolder.Replace("/","\\");
            if(fbxFolder != "" && !fbxFolder.EndsWith("\\"))
            {
                fbxFolder += "\\";
            }
        }
        GUILayout.EndHorizontal();

        batchSize = EditorGUILayout.IntField("  batchSize", batchSize);
        if (GUILayout.Button("Convert"))
        {
            OnConvert();
        }
        #endregion

        GUILayout.Label ("GetAnim", EditorStyles.boldLabel);
        GUILayout.Label(" From:                                       " + fbxFolderInAsset);
        animFolder = EditorGUILayout.TextField("  To", animFolder);
        if (GUILayout.Button("Abstract"))
        {
            OnAbstract();
        }
    }

    //鼠标点击更新fbxFolderInAsset
    private void OnSlctChanged()
    {
        string[] SelectFile = Selection.assetGUIDs;
        if (SelectFile == null || SelectFile.Length <= 0)
        {
            fbxFolderInAsset = "";
            return;
        }
        fbxFolderInAsset = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
        fbxFolderInAsset = fbxFolderInAsset.Replace("/", "\\");
    }

    //调用Python脚本实现格式转换
    private void OnConvert()
    {
        System.Diagnostics.Process process = new System.Diagnostics.Process();
        //后台打开blender 并给py脚本传参
        process.StartInfo.FileName = blenderPath;
        process.StartInfo.Arguments = " --background -P " + Config.pyPath;
        process.StartInfo.Arguments += ' ' + bvhFolder + ' ' + fbxFolder + ' ' + batchSize;
        process.Start();
        process.WaitForExit();
        process.Close();
        //转换完成后自动打开输出文件夹
        System.Diagnostics.Process.Start("explorer.exe", fbxFolder);
    }

    private void OnAbstract(){
        //获取文件夹下所有直接子文件 根据后缀筛选获得fbx
        DirectoryInfo direction = new DirectoryInfo(fbxFolderInAsset);
        FileInfo[] files = direction.GetFiles("*.fbx", SearchOption.AllDirectories);

        for(int i = 0; i < files.Length; i++)
        {
            getAnimFromFBX(files[i].Name.Split(".")[0]);
        }
    }

    private void getAnimFromFBX(string fName)
    {
        if (!Directory.Exists(animFolder))
        {
            Directory.CreateDirectory(animFolder);
        }
        //读取fbx文件里面所有子资产
        Object[] fbx = AssetDatabase.LoadAllAssetsAtPath(fbxFolderInAsset + "\\" + fName + ".fbx");
        for (int i = 0; i < fbx.Length; i++)
        {
            if (fbx[i] is AnimationClip)
            {
                //筛除多余anim文件
                if (fbx[i].name.Contains("__preview__"))
                {
                    continue;
                }
                string aniName = fbx[i].name;
                AnimationClip clip = new AnimationClip();
                EditorUtility.CopySerialized(fbx[i], clip);
                AnimationClipSettings clipSetting = AnimationUtility.GetAnimationClipSettings(clip);
                AnimationUtility.SetAnimationClipSettings(clip, clipSetting);
                //假如fbx是从上面的方法转换而来 需要对anim文件名字更新+
                if(aniName.Contains(fName + "|"))
                {
                    aniName = aniName.Replace(fName + "|", "");
                }
                AssetDatabase.CreateAsset(clip, animFolder + aniName + ".anim");
                AssetDatabase.SaveAssets();
            }
        }
    }
}