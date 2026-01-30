using UnityEngine;

public class TrackPiece : MonoBehaviour
{
    public Transform socketIn;
    public Transform socketOut;

    // This helps you see the sockets in the Editor!
    private void OnDrawGizmos()
    {
        if (socketIn) DrawSocket(socketIn, Color.red);    // Red for In
        if (socketOut) DrawSocket(socketOut, Color.cyan); // Cyan for Out
    }

    private void DrawSocket(Transform t, Color c)
    {
        Gizmos.color = c;
        Gizmos.DrawSphere(t.position, 0.1f);
        Gizmos.color = Color.blue; // Forward direction
        Gizmos.DrawRay(t.position, t.forward * 0.5f);
    }
}