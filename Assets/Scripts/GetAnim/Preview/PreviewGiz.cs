using System.Collections.Generic;
using UnityEngine;

public class PreviewGiz : MonoBehaviour
{
    public List<Vector3> line;
    public Vector3 point;

    void Start(){
        line = new List<Vector3>();
    }

    public void RefreshPos(){
        line.Clear();
        line.Add(Vector3.zero);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        for(int i = 0; i < line.Count - 1; i++){
            Gizmos.DrawLine(line[i], line[i + 1]);
        }
        
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.DrawSolidDisc(point, Vector3.up, .03f);
    }
}
