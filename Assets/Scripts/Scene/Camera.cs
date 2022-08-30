using UnityEngine;

public class Camera : MonoBehaviour
{
    public UnityEngine.Camera myCamera;
    private Vector3 offset;
    // Start is called before the first frame update
    void Start()
    {
        offset = this.transform.position - new Vector3(3, 0, 0);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        myCamera.transform.position += this.transform.position - offset;
        offset = this.transform.position;

        if (Input.GetMouseButton(0))
        {
            float mouseX = Input.GetAxis("Mouse X") * 2;
            myCamera.transform.RotateAround(this.transform.position, Vector3.up, mouseX);
        }
    }
}
