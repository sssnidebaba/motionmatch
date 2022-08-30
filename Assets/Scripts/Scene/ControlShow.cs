using UnityEngine;
using UnityEngine.UI;

public class ControlShow : MonoBehaviour
{
    public Text WKey, SKey, AKey, DKey;
    private Color KeyDownColor = Color.red;
    private Color NormalColor = Color.black;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            WKey.color = KeyDownColor;
        }
        if (Input.GetKeyUp(KeyCode.W))
        {
            WKey.color = NormalColor;
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            AKey.color = KeyDownColor;
        }
        if (Input.GetKeyUp(KeyCode.A))
        {
            AKey.color = NormalColor;
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            SKey.color = KeyDownColor;
        }
        if (Input.GetKeyUp(KeyCode.S))
        {
            SKey.color = NormalColor;
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            DKey.color = KeyDownColor;
        }
        if (Input.GetKeyUp(KeyCode.D))
        {
            DKey.color = NormalColor;
        }
    }
}
