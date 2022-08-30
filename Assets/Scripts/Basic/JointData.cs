using UnityEngine;
[System.Serializable]
public class JointData
{
    public Vector3 Position;
    public Vector3 Velocity;
    public JointData(Vector3 position, Vector3 velocity)
    {
        Position = position;
        Velocity = velocity;
    }
}