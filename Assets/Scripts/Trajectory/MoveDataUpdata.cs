using UnityEngine;

public class MoveDataUpdata : MonoBehaviour
{
    
    private float horizontal;//输入
    private float vertical;//输入
    private Vector3 dirAccnow;//当前加速度方向
    private float tempRo;//转向角度
    



    

    [Header("Settings")]
    public float walkMaxSpeed;//最大行走速度
    public float walkAcc;//步行加速度
    public float walkAngleVelocity;//步行转向速度
    public float walkResistance;//步行阻力

    public float runMaxSpeed;//最大跑步速度
    public float runAcc;//跑步加速度
    public float runAngleVelocity;//跑步转向速度
    public float runResistance;//跑步阻力
    [Range(0,0.2f)]
    public float run2walkSpeed;//跑到走转换速度
    [Range(0, 0.2f)]
    public float walk2runSpeed;//走到跑转换速度

    public Transform mCamera;//摄像机

    [Header("Monitor")]
    public Vector3 acceleration;//加速度
    public Vector3 velocity;//速度
    public float nowDir;//当前朝向
    public float aimDir;//目标朝向
    public float maxSpeed;//最大速度限制
    public float accelerationForce;//加速度大小
    public float resistance;//阻力
    public float angleVelocity;//转向速度
    public bool isWalking, isRunning;//运动状态





    // Update is called once per frame
    private void Start()
    {
        accelerationForce += resistance;

        MoveData.resistance =resistance ;
        MoveData.angleVelocity = angleVelocity;
        MoveData.maxSpeed = maxSpeed;
    }
    void Update()
    {
        CharaterInput();
        VecChange();
        RoChange();

        //inspector窗口监视以下值
        resistance = MoveData.resistance;
        acceleration = MoveData.acceleration;
        velocity = MoveData.velocity;
        nowDir = MoveData.nowDir;
        aimDir = MoveData.aimDir;
        angleVelocity = MoveData.angleVelocity;
    }

    

    private void CharaterInput()//获取输入改变加速度
    {
        vertical = Input.GetAxis("Horizontal");
        horizontal = Input.GetAxis("Vertical");


        float deltaAngle = mCamera.eulerAngles.y;
        dirAccnow =Quaternion.Euler(0,deltaAngle,0 )* new Vector3(vertical, 0,  horizontal) * accelerationForce;
        //将输入转变为以镜头正方向为基准的wasd加速度控制

        if (Input.GetKey(KeyCode.LeftShift))
        {
            isWalking = true;
            isRunning = false;
        }
        else
        {
            isWalking = false;
            isRunning = true;
        }

        if (isWalking)
        {
            MoveData.angleVelocity = walkAngleVelocity;
            MoveData.resistance = walkResistance;
            MoveData.maxSpeed = walkMaxSpeed;
            accelerationForce = walkAcc;

            maxSpeed = Mathf.Lerp(maxSpeed, walkMaxSpeed, run2walkSpeed);//运动状态变化时最大速度的插值
            MoveData.maxSpeed = maxSpeed;
        }
        else if(isRunning)
        {
            MoveData.angleVelocity = runAngleVelocity;
            MoveData.resistance = runResistance;
            MoveData.maxSpeed = runMaxSpeed;
            accelerationForce = runAcc;

            maxSpeed = Mathf.Lerp(maxSpeed, runMaxSpeed, walk2runSpeed);//运动状态变化时最大速度的插值
            MoveData.maxSpeed = maxSpeed;
        }


        if (dirAccnow.magnitude >= 0.001f)
        {
            MoveData.aimDir = Tool.Vector3ToAngle(dirAccnow);
            MoveData.acceleration = dirAccnow;
        }
        else
        {
            MoveData .acceleration = new Vector3(0,0,0);

            MoveData.nowDir = transform.eulerAngles.y;
            MoveData.aimDir = transform.eulerAngles.y;
            //没有输入时角度恢复为人物朝向
        }


    }
    private void VecChange()//速度根据阻力和加速度更新
    {
        if (MoveData.velocity.magnitude > maxSpeed)
        {
            MoveData.velocity *= maxSpeed / MoveData.velocity.magnitude;
        }
            if (MoveData.velocity.magnitude > 0.001f)
        {
            if(dirAccnow.magnitude < 0.1f|| (Vector3.Project(dirAccnow,MoveData.velocity).magnitude!=dirAccnow.magnitude))
            { 

                MoveData.velocity -= MoveData.velocity.normalized * MoveData.resistance * Time.deltaTime;
            }
        }
        else
        {
            MoveData.velocity = new Vector3(0, 0, 0);
        }
        if (MoveData.velocity.magnitude <maxSpeed)
        {
            MoveData.velocity += (MoveData.acceleration * Time.deltaTime);
        }
        

    }
    private void RoChange()//角度更新
    {
         tempRo = MoveData.aimDir - MoveData.nowDir;


        if ((tempRo <= 180 && tempRo >= 0) || (tempRo < -180))
        {
            if (tempRo < 0)
            {
                tempRo += 360;
            }
            if (MoveData.angleVelocity * Time.deltaTime < tempRo)
            {
                MoveData.nowDir = MoveData.nowDir + MoveData.angleVelocity * Time.deltaTime;
            }
        }
        else
        {
            if (tempRo > 0)
            {
                tempRo -= 360;
            }
            if (MoveData.angleVelocity * Time.deltaTime < -1*tempRo)
            {
                MoveData.nowDir = MoveData.nowDir- MoveData.angleVelocity * Time.deltaTime;
            }
        }
       

        MoveData.nowDir += 360;
        MoveData.nowDir %= 360;
    }
}
