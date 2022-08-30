using UnityEngine;
[System.Serializable]
public class TrajectoryPoint
{
    public Vector3 Position;
    public float FacingAngle;
    public TrajectoryPoint(Vector3 position, float facingAngle)
    {
        Position = position;
        FacingAngle = facingAngle;
    }
}