using System.Collections.Generic;
using UnityEngine;


//켣
[System.Serializable]
public class TrajectoryData 
{
    public List<Vector3> futurepos;
    public List<float> futuredir;
    public List<Vector3> pastpos;
    public List<float> pastDir;

    public TrajectoryData()
    {
        futurepos = new List<Vector3>();
        futuredir = new List<float>();
        pastpos = new List<Vector3>();
        pastDir = new List<float>();
    }
}
