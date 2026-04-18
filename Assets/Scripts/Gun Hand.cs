using UnityEngine;

public class GunHand : MonoBehaviour
{
    public Transform leftHand;
    public Transform rightHand;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
       if (Input.GetKeyDown(KeyCode.A))
        {
            RightHand();
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            LeftHand();
        } 
    }

    void RightHand()
    {
        transform.SetParent(rightHand);
        transform.localPosition = Vector3.zero;
        transform.localScale = new Vector3(1f, 1f, 1f);
    }
    void LeftHand()
    {
        transform.SetParent(leftHand);
        transform.localPosition = Vector3.zero;
        transform.localScale = new Vector3(1f, 1f, 1f);
    }
}
