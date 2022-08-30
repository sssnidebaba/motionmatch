//#if UNITY_EDITOR
//using UnityEngine;
//using UnityEditor;
//using UnityEditor.SceneManagement;
//using UnityEditor.Animations;
//using System.Collections.Generic;
//using System.Text.RegularExpressions;

//[CustomEditor(typeof(AnimationClip))]
//public class AnimationClipInspector : Editor
//{
//    static PreProcessor pPro;
//    static bool isPreview = false;

//    //preview场景中动画回放参数 以及gizmos组件的获取
//    static float curTime = 0f;
//    static float playSpeed = Config.playSpeed;              // = 0.1f;
//    static Animator animator;
//    static PreviewGiz previewGiz;                           //Gizmos
//    static bool initialized = false;

//    //本地(Inspector)存储的临时Tag 避免误操作污染动画数据
//    static List<List<string>> localTag = new();
//    static List<bool> tagFoldout = new();

//    //动画相关参数
//    static int index = -1;
//    static AnimationClip animClip;
//    static AnimatorStateMachine stateMachine;
//    static AnimatorState preview;
//    static bool applyIK = false;

//    //场景参数
//    static string usrScenePath;
//    static Vector3 usrSceneCamPos;
//    static Quaternion usrSceneCamRot;
//    static string previewScenePath = Config.previewScenePath;

//    override public void OnInspectorGUI()
//    {
//        if (isPreview) {
//            PreviewGUI();
//        }
//        else {
//            DefaultGUI();
//        }
//    }

//    private void DefaultGUI()
//    {
//        if (GUILayout.Button("TogglePreview"))
//        {
//            #region 进入Preview
//            ///获取animClip
//            // 假如使用ObjectField获取 animClip = (AnimationClip)EditorGUILayout.ObjectField("目标动画(*.anim):", animClip, typeof(AnimationClip), true);
//            #region 设置localTag并初始化
//            if (animClip != (AnimationClip)target || !initialized)  //更换选择的animClip或初始化
//            {
//                //更新animClip -> 搜索preProcessor看是否已经打tag 是则更新到本地(Inspector)
//                animClip = (AnimationClip)target;

//                index = -1;
//                for (int i = 0; i < pPro.myTags.Count; i++)
//                {
//                    if (pPro.myTags[i].animationName == animClip.name)
//                    {
//                        index = i;
//                        break;
//                    }
//                }
//                localTag.Clear();
//                tagFoldout.Clear();
//                //(int)Tag.End == pPro.myTags[index].Count
//                for (int i = 0; i < (int)TagType.End; i++)
//                {
//                    localTag.Add(new List<string>());
//                    tagFoldout.Add(true);
//                }
//                if (index != -1)
//                {
//                    //在index处有tag -> 获取tag到本地
//                    int tagIndex = (int)(pPro.myTags[index].intervals[0].x);
//                    int tagCount = (int)(pPro.myTags[index].intervals[0].y);
//                    for (int i = 1; i < pPro.myTags[index].intervals.Count; i++)
//                    {
//                        if (tagCount > 0)
//                        {
//                            localTag[tagIndex].Add(pPro.myTags[index].intervals[i].x.ToString());
//                            localTag[tagIndex].Add(pPro.myTags[index].intervals[i].y.ToString());
//                            tagCount--;
//                        }
//                        else
//                        {
//                            tagIndex = (int)(pPro.myTags[index].intervals[i].x);
//                            tagCount = (int)(pPro.myTags[index].intervals[i].y);
//                        }
//                    }
//                }
//                initialized = true;
//            }
//            #endregion

//            #region 保存原本场景信息
//            // 此处虽然会弹窗询问是否保存 但并未判断用户的输入 而是直接进入下一步 可更新为判断版本
//            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
//            usrScenePath = EditorSceneManager.GetActiveScene().path;
//            Transform usrSceneCamTransform = SceneView.lastActiveSceneView.camera.transform;
//            usrSceneCamPos = usrSceneCamTransform.position;
//            usrSceneCamRot = usrSceneCamTransform.rotation;
//            #endregion

//            #region 打开preview场景
//            EditorSceneManager.OpenScene(previewScenePath);
//            // Scene previewScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
//            // previewScene.name = "Preview";

//            ///设置preview场景的初始视角
//            GameObject cameraPoint = new GameObject();
//            cameraPoint.transform.SetPositionAndRotation(new Vector3(-4f, 3f, 4f), Quaternion.Euler(23f, 125f, 0f));
//            SceneView.lastActiveSceneView.AlignViewToObject(cameraPoint.transform);
//            GameObject.DestroyImmediate(cameraPoint);
//            #endregion

//            #region 绑定相关组件(Component)
//            animator = GameObject.Find("Kyle").GetComponent<Animator>();
//            stateMachine = ((AnimatorController)animator.runtimeAnimatorController).layers[0].stateMachine;
//            previewGiz = GameObject.Find("Kyle").GetComponent<PreviewGiz>();
//            #endregion

//            curTime = 0f;
//            #endregion

//            isPreview = !isPreview;
//        }
//        applyIK = GUILayout.Toggle(applyIK, "Apply Foot IK In Preview");

//        //检测PreProcessor文件是否更换?初始化设false:;
//        EditorGUI.BeginChangeCheck();
//        pPro = (PreProcessor)EditorGUILayout.ObjectField(pPro, typeof(PreProcessor), true);
//        if (EditorGUI.EndChangeCheck())
//        {
//            initialized = false;
//        }

//        //本意是绘制animationClip原本可以播放动画的inspector 但绘制不出来 可研究如何调出
//        DrawDefaultInspector();
//    }

//    private void PreviewGUI()
//    {
//        if (GUILayout.Button("TogglePreview"))
//        {
//            #region Quit Preview Scene
//            //删除本次编辑对场景的影响(地上的线 动画播放器)
//            for (int i = stateMachine.states.Length; i > 0; i--)
//            {
//                stateMachine.RemoveState(stateMachine.states[i - 1].state);
//            }
//            previewGiz.RefreshPos();

//            //重新打开之前的场景 并设置为切换前的视角
//            EditorSceneManager.OpenScene(usrScenePath);
//            GameObject cameraPoint = new GameObject();
//            cameraPoint.transform.SetPositionAndRotation(usrSceneCamPos, usrSceneCamRot);
//            SceneView.lastActiveSceneView.AlignViewToObject(cameraPoint.transform);
//            GameObject.DestroyImmediate(cameraPoint);
//            #endregion

//            isPreview = !isPreview;
//        }

//        GUILayout.Label("  FrameRate: " + animClip.frameRate);
//        GUILayout.Label("  TotalFrame:" + (int)(animClip.length / (1 / animClip.frameRate)));

//        #region 场景中查看动画
//        if (GUILayout.Button("Set Kyle to (0,0,0)"))
//        {
//            animator.playbackTime = 0f;
//            animator.Update(0);
//            GameObject.Find("Kyle").transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
//        }

//        if (GUILayout.Button("Bake AnimClip for Preview"))
//        {
//            preview = stateMachine.AddState(animClip.name);
//            preview.motion = animClip;
//            preview.iKOnFeet = applyIK;
//            stateMachine.defaultState = preview;
//            animator.Rebind();

//            previewGiz.RefreshPos();    //删除gizmos画的线
//            int totalFrame = (int)(animClip.length / (1 / animClip.frameRate));
//            animator.StopPlayback();

//            animator.recorderStartTime = 0;
//            animator.StartRecording(totalFrame);
//            for (int i = 0; i < totalFrame + 1; i++)
//            {
//                animator.Update(1f / animClip.frameRate);
//                previewGiz.line.Add(GameObject.Find("Kyle").transform.position);
//            }
//            animator.StopRecording();
//            animator.StartPlayback();

//            animator.playbackTime = 0f;
//            animator.Update(0);
//            curTime = 0f;
//        }
//        EditorGUI.BeginChangeCheck();
//        curTime = EditorGUILayout.Slider("Time:", curTime, 0f, animClip.length);
//        if (EditorGUI.EndChangeCheck())
//        {
//            manualUpdate();
//            previewGiz.point = GameObject.Find("Kyle").transform.position;
//        }

//        GUILayout.BeginHorizontal();
//        if (GUILayout.Button("Play", GUILayout.Width(100)))
//        {
//            EditorApplication.update += myPlay;
//        }
//        playSpeed = EditorGUILayout.Slider("interval:", playSpeed, 0f, 1f);
//        if (GUILayout.Button("Pause", GUILayout.Width(100)))
//        {
//            EditorApplication.update -= myPlay;
//        }
//        GUILayout.EndHorizontal();

//        #endregion

//        #region 设置tag
//        for (int i = 0; i < (int)TagType.End; i++)
//        {
//            GUILayout.BeginHorizontal();
//            tagFoldout[i] = EditorGUILayout.Foldout(tagFoldout[i], ((TagType)i).ToString());
//            if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
//            {
//                localTag[i].Add("");
//                localTag[i].Add("");
//            }
//            if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
//            {
//                if (localTag[i].Count > 0)
//                {
//                    localTag[i].RemoveAt(localTag[i].Count - 1);
//                    localTag[i].RemoveAt(localTag[i].Count - 1);
//                }
//            }
//            if (GUILayout.Button("0", GUILayout.ExpandWidth(false)))
//            {
//                localTag[i].Clear();
//            }
//            GUILayout.EndHorizontal();

//            if (tagFoldout[i])
//            {
//                for (int j = 0; j < localTag[i].Count; j += 2)
//                {
//                    GUILayout.BeginHorizontal(GUILayout.MaxHeight(10));
//                    GUILayout.Label("", GUILayout.Width(50));   //用作缩进

//                    EditorGUI.BeginChangeCheck();
//                    localTag[i][j] = GUILayout.TextField(localTag[i][j], GUILayout.MaxWidth(150));
//                    //格式控制 确保只会获取float类型的string
//                    localTag[i][j] = Regex.Replace(localTag[i][j], @"[^0-9.]", "");
//                    if (Regex.IsMatch(localTag[i][j], @"[0-9]+\.[0-9]*\."))
//                    {
//                        localTag[i][j] = localTag[i][j].Substring(0, localTag[i][j].Length - 1);
//                    }
//                    if (EditorGUI.EndChangeCheck())
//                    {
//                        // Debug.Log("changed");
//                        curTime = float.Parse(localTag[i][j]);
//                        manualUpdate();
//                        previewGiz.point = GameObject.Find("Kyle").transform.position;
//                    }

//                    GUILayout.Label("-", GUILayout.ExpandWidth(false));

//                    EditorGUI.BeginChangeCheck();
//                    localTag[i][j + 1] = GUILayout.TextField(localTag[i][j + 1], GUILayout.MaxWidth(150));
//                    localTag[i][j + 1] = Regex.Replace(localTag[i][j + 1], @"[^0-9.]", "");
//                    if (Regex.IsMatch(localTag[i][j + 1], @"[0-9]+\.[0-9]*\."))
//                    {
//                        localTag[i][j + 1] = localTag[i][j + 1].Substring(0, localTag[i][j + 1].Length - 1);
//                    }
//                    if (EditorGUI.EndChangeCheck())
//                    {
//                        curTime = float.Parse(localTag[i][j + 1]);
//                        manualUpdate();
//                        previewGiz.point = GameObject.Find("Kyle").transform.position;
//                    }
//                    GUILayout.EndHorizontal();
//                }
//            }
//        }
//        if (GUILayout.Button("Save", GUILayout.ExpandWidth(false)))
//        {
//            if (index != -1)
//            {
//                // 动画位于index -> update
//                pPro.myTags[index].intervals.Clear();
//            }
//            else
//            {
//                // 并未存储tag -> upload
//                pPro.myTags.Add(new Tags(animClip.name));
//                index = pPro.myTags.Count - 1;
//            }
//            for (int i = 0; i < (int)TagType.End; i++)
//            {
//                int count = 0;
//                if (localTag[i].Count > 0)
//                {
//                    for (int j = 0; j < localTag[i].Count; j += 2)
//                    {
//                        pPro.myTags[index].intervals.Add(new Vector2(float.Parse(localTag[i][j]), float.Parse(localTag[i][j + 1])));
//                        count++;
//                    }
//                }
//                pPro.myTags[index].intervals.Add(new Vector2(i, count));
//            }
//            pPro.myTags[index].intervals.Reverse();
//            EditorUtility.SetDirty(pPro);
//            AssetDatabase.SaveAssets();
//        }
//        #endregion
//    }

//    private void manualUpdate()
//    {
//        animator.playbackTime = curTime;
//        animator.Update(0);
//    }

//    private void myPlay()
//    {
//        curTime += (1f / animClip.frameRate) * playSpeed;
//        if (curTime > animClip.length) curTime -= animClip.length;
//        animator.playbackTime = curTime;
//        animator.Update(0);
//        previewGiz.point = GameObject.Find("Kyle").transform.position;
//    }

//    private void patternCheck(ref string toCheck, ref Regex rgx)
//    {
//        if (!rgx.IsMatch(toCheck))
//        {
//            toCheck = toCheck.Substring(0, toCheck.Length - 1);
//        }
//    }
//}
//#endif