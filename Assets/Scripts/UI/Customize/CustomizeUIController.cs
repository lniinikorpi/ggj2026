using UnityEngine;

public class CustomizeUIController : MonoBehaviour
{
    public SkinnedMeshRenderer meshRenderer;
    public Transform dogTransform;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        dogTransform.eulerAngles += new Vector3(0, 30, 0) * Time.deltaTime;
    }
}
