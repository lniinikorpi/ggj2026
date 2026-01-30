using UnityEngine;

public class GrassInteractor : MonoBehaviour
{
    public Material grassMaterial;
    private static readonly int PlayerPositionID = Shader.PropertyToID("_PlayerPosition");

    void Update()
    {
        Vector3 pos = transform.position;
        grassMaterial.SetVector(PlayerPositionID, new Vector3(pos.x, pos.y, pos.z));
    }
}
