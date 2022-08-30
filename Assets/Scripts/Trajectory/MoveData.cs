using UnityEngine;


//模拟运动数据
public class MoveData 
{
    public static float resistance;//阻力
    public static float nowDir;//当前角度
    public static float aimDir;//目标角度

    public static float angleVelocity;//转向速度
    public static Vector3 acceleration;//加速度
    public static Vector3 velocity;//速度

    public static float maxSpeed;//最大速度限制
    MoveData()
    {
        acceleration = new Vector3(0, 0, 0);
        velocity = new Vector3(0, 0, 0);
        resistance = 10f;
    }

}
