using System.Collections.Generic;
using UnityEngine;

public class TrajectoryGenerate : MonoBehaviour
{


    private float radius = 0.1f;
    public float posScale;//缩放位置数据以对应动画数据
    public bool showTraAnim;//显示预测轨迹
    public bool showTraPre;//显示动画轨迹
    public PoseData nowAnimData;//正在播放动画的动画数据
    public TrajectoryData trajectoryData = new TrajectoryData();

    private float[] predictMoment, pastMoment;//预测未来，记录过去的时刻
    private int recordPastindex;//过去点记录的锚点
    private List<Vector3> pastpos;//临时记录过去位置
    private List<float> pastRo;//临时记录朝向

    private float[] TrajPoints;//从动画处理器获取轨迹处理的参数
    private float tempRo;//轨迹点的角度转向

    private bool gamestart;//处理OnDrawGizmos在游戏未开始时运行的bug


    private void Awake()
    {
        Time.timeScale = 1.0f;
        pastpos = new List<Vector3>();
        pastRo = new List<float>();

        gamestart = true;
    }

    // Start is called before the first frame update
    public void Initialized(float[] trajPoints)
    {
        TrajPoints = trajPoints;
        momentInit();

        for (int i = 0; i < predictMoment.Length; i++)
        {
            trajectoryData.futurepos.Add(new Vector3(0, 0, 0));
            trajectoryData.futuredir.Add(0);
        }
        for (int i = 0; i < pastMoment.Length; i++)
        {
            trajectoryData.pastpos.Add(new Vector3(0, 0, 0));
            trajectoryData.pastDir.Add(0);
        }
    }//从动画处理器获取轨迹处理的参数
    private void momentInit()
    {
        int pastlen = 0, futureLen = 0;
        foreach (float i in TrajPoints)
        {
            if (i < 0) pastlen++;
            if (i > 0) futureLen++;
        }
        pastMoment = new float[pastlen];
        predictMoment = new float[futureLen];

        int pi = 0, fi = 0;
        foreach (float i in TrajPoints)
        {
            if (i < 0)
            {
                pastMoment[pi] = -1 * i;
                pi++;
            }
            if (i > 0)
            {
                predictMoment[fi] = i;
                fi++;
            }
        }

    }//从动画处理器获取轨迹处理的参数

    // Update is called once per frame
    void Update()
    {
        PredictPosRo();
    }

    private void FixedUpdate()
    {
        RecordPos();
        PastTrajectoryUpdata();
    }
    private void RecordPos()//记录十秒内的轨迹点
    {
        if (Time.realtimeSinceStartup < 30f)
        {
            pastpos.Add(transform.position);
            pastRo.Add(transform.eulerAngles.y);
            recordPastindex++;
        }
        else
        {
            recordPastindex %= pastpos.Count;
            pastpos[recordPastindex] = transform.position;
            pastRo[recordPastindex] = transform.eulerAngles.y;
            recordPastindex++;
        }
    }

    private void PastTrajectoryUpdata()
    {
        var tempRecrecordPastindex = recordPastindex;
        for (int i = 0; i < pastMoment.Length; i++)
        {
            tempRecrecordPastindex = recordPastindex;
            if (Time.realtimeSinceStartup > pastMoment[i])
            {
                tempRecrecordPastindex -= (int)(pastMoment[i] / Time.fixedDeltaTime);
                if (tempRecrecordPastindex <= 0)
                {
                    tempRecrecordPastindex += (pastpos.Count - 1);
                }
                if (tempRecrecordPastindex < 0)
                {
                    tempRecrecordPastindex = 0;
                }
                if (tempRecrecordPastindex >= pastpos.Count)
                {
                    tempRecrecordPastindex = pastpos.Count - 1;
                }

                trajectoryData.pastpos[i] = transform.InverseTransformPoint(pastpos[tempRecrecordPastindex]);
                trajectoryData.pastDir[i] = pastRo[tempRecrecordPastindex];
            }

        }

    }//过去的轨迹点更新

    private void PredictPosRo()//预测未来轨迹
    {

        for (int i = 0; i < predictMoment.Length; i++)
        {
            #region 轨迹点位置预测

            if (MoveData.acceleration.magnitude > 0)
            {
                Vector3 verticalDir = Vector3.Project(MoveData.acceleration, Quaternion.Euler(0, 90, 0) * MoveData.velocity);
                Vector3 horDir = Vector3.Project(MoveData.acceleration, MoveData.velocity);

                if (Vector3.Dot(horDir, MoveData.velocity) > 0)
                {
                    if ((MoveData.velocity + horDir * predictMoment[i]).magnitude <= MoveData.maxSpeed)
                    {

                        trajectoryData.futurepos[i] = MoveData.velocity * predictMoment[i] + horDir * 0.5f * Mathf.Pow(predictMoment[i], 2);
                    }
                    else
                    {

                        var accT = (MoveData.maxSpeed - MoveData.velocity.magnitude) / horDir.magnitude;
                        trajectoryData.futurepos[i] = 0.5f * (MoveData.velocity + MoveData.velocity.normalized * MoveData.maxSpeed) * accT + (predictMoment[i] - accT) * MoveData.velocity.normalized * MoveData.maxSpeed;
                    }

                }
                else
                {
                    if ((MoveData.velocity + horDir * predictMoment[i]).magnitude <= MoveData.maxSpeed)
                    {

                        trajectoryData.futurepos[i] = MoveData.velocity * predictMoment[i] + horDir * 0.5f * Mathf.Pow(predictMoment[i], 2);
                    }
                    else
                    {

                        var accT = (MoveData.maxSpeed + MoveData.velocity.magnitude) / horDir.magnitude;

                        trajectoryData.futurepos[i] = 0.5f * (MoveData.velocity - MoveData.velocity.normalized * MoveData.maxSpeed) * accT + (predictMoment[i] - accT) * -1 * MoveData.velocity.normalized * MoveData.maxSpeed;

                    }
                }

                trajectoryData.futurepos[i] += verticalDir * 0.5f * Mathf.Pow(predictMoment[i], 2);
            }
            else
            {

                if ((MoveData.velocity.magnitude - MoveData.resistance * predictMoment[i]) >= 0)
                {
                    trajectoryData.futurepos[i] = MoveData.velocity * predictMoment[i] + -1 * MoveData.velocity.normalized * MoveData.resistance * 0.5f * Mathf.Pow(predictMoment[i], 2);
                }
                else
                {
                    trajectoryData.futurepos[i] = MoveData.velocity.normalized * MoveData.resistance * 0.5f * Mathf.Pow(MoveData.velocity.magnitude / MoveData.resistance, 2);
                }
            }
            #endregion

            #region 轨迹点朝向预测


            tempRo = MoveData.aimDir - MoveData.nowDir;



            if ((tempRo <= 180 && tempRo >= 0) || (tempRo < -180))
            {
                if (tempRo < 0)
                {
                    tempRo += 360;
                }
                if (MoveData.angleVelocity * predictMoment[i] < tempRo)
                {
                    trajectoryData.futuredir[i] = MoveData.nowDir + MoveData.angleVelocity * predictMoment[i];
                }
                else
                {
                    trajectoryData.futuredir[i] = MoveData.nowDir + tempRo;
                }
                trajectoryData.futuredir[i] %= 360;
            }
            else
            {
                if (tempRo > 0)
                {
                    tempRo -= 360;
                }
                if (MoveData.angleVelocity * predictMoment[i] < -1 * tempRo)
                {
                    trajectoryData.futuredir[i] = MoveData.nowDir - MoveData.angleVelocity * predictMoment[i];
                }
                else
                {
                    trajectoryData.futuredir[i] = MoveData.nowDir + tempRo;
                }
                trajectoryData.futuredir[i] += 360;
                trajectoryData.futuredir[i] %= 360;
            }




            #endregion


            if (MoveData.acceleration.magnitude < 0.1)
            {
                trajectoryData.futuredir[i] = transform.eulerAngles.y;
            }//没有输入时未来轨迹朝向回正
        }

    }


    private void OnDrawGizmos()
    {

        if (gamestart)
        {
            if (showTraPre)
            {
                // 画未来轨迹
                Gizmos.color = Color.green;
                for (int i = 0; i < predictMoment.Length; i++)
                {
                    //Gizmos.DrawSphere(transform.position + trajectoryData.futurepos[i], 0.2f);
                    UnityEditor.Handles.color = Color.green;
                    UnityEditor.Handles.DrawSolidDisc(transform.position + trajectoryData.futurepos[i], Vector3.up, radius);
                    Gizmos.DrawLine(transform.position + trajectoryData.futurepos[i], transform.position + trajectoryData.futurepos[i] + Tool.AngleToVector3(trajectoryData.futuredir[i]));

                }



                //画过去轨迹
                Gizmos.color = Color.green;
                var tempRecrecordPastindex = recordPastindex;
                for (int i = 0; i < pastMoment.Length; i++)
                {

                    if (Time.realtimeSinceStartup > pastMoment[i])
                    {
                        tempRecrecordPastindex = recordPastindex;
                        tempRecrecordPastindex -= (int)(pastMoment[i] / Time.fixedDeltaTime);
                        if (tempRecrecordPastindex <= 0)
                        {
                            tempRecrecordPastindex += pastpos.Count;
                        }
                        if (tempRecrecordPastindex < 0)
                        {
                            tempRecrecordPastindex = 0;
                        }
                        if (tempRecrecordPastindex >= pastpos.Count)
                        {
                            tempRecrecordPastindex = pastpos.Count - 1;
                        }
                        UnityEditor.Handles.color = Color.green;
                        UnityEditor.Handles.DrawSolidDisc(pastpos[tempRecrecordPastindex], Vector3.up, radius);
                        //Gizmos.DrawSphere(pastpos[tempRecrecordPastindex], 0.2f);
                        Gizmos.DrawLine(pastpos[tempRecrecordPastindex], pastpos[tempRecrecordPastindex] + Tool.AngleToVector3(pastRo[tempRecrecordPastindex]));

                    }
                }
            }
            //画现在
            Gizmos.color = Color.white;
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, radius);
            //Gizmos.DrawSphere(transform.position, 0.2f);
            Gizmos.DrawLine(transform.position, transform.position + Tool.AngleToVector3(transform.eulerAngles.y));


            if (showTraAnim)
            {
               
                //画动画轨迹
                Gizmos.color = Color.red;
                foreach (TrajectoryPoint i in nowAnimData.Trajectory)
                {
                    float rotationAngle = Vector3.Angle(nowAnimData.LocalVelocity, new Vector3(0f, 0f, 1));
                    UnityEditor.Handles.color = Color.red;
                    UnityEditor.Handles.DrawSolidDisc(transform.TransformPoint(i.Position), Vector3.up, radius);
                    //Gizmos.DrawSphere(transform.TransformPoint(i.Position), 0.2f);
                    Gizmos.DrawLine(transform.TransformPoint(i.Position), transform.TransformPoint(i.Position) + Tool.AngleToVector3(i.FacingAngle + transform.eulerAngles.y));
                }
            }
        }
    }//画轨迹点

    public TrajectoryData Gettraject()//轨迹输出接口
    {
        return trajectoryData;
    }
}
