using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Tool 
{
    public static float Vector3ToAngle(Vector3 resourceVec)//����ת��Ϊ�Ƕ�,z��������Ϊ��׼
    {
        float angle;
        if (Vector3.Cross(resourceVec, Vector3.forward).y <= 0)
        {
            angle = Vector3.Angle(resourceVec, Vector3.forward);
        }
        else
        {
            angle = 360f - Vector3.Angle(resourceVec, Vector3.forward);
        }

        if ((360f - angle) < 1) angle = 0;
        return angle;
    }

    public static Vector3 AngleToVector3(float resourceAngle)//�Ƕ�ת��Ϊ����,z��������Ϊ��׼
    {
        Vector3 dir=new Vector3();
        dir = Quaternion.Euler(0, resourceAngle, 0)*Vector3.forward  ;
        return dir;
    }

    public static Vector3 AngleToVector2(float resourceAngle)//�Ƕ�ת��Ϊ����,z��������Ϊ��׼
    {
        Vector3 dir = new Vector3();
        dir = Quaternion.Euler(0, resourceAngle, 0) * Vector3.forward;
        return dir;
    }
}
