using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TrackSnapping : MonoBehaviour
{
    [Header("Generation Settings")]
    public int seed = 12345;
    public int totalPieces = 20;

    [Header("Constraints")]
    [Tooltip("The track will try to stay within these Y-heights relative to the start.")]
    public float minHeight = -5f;
    public float maxHeight = 5f;

    [Tooltip("If true, removes any 'Roll' or banking from the track to prevent twisting.")]
    public bool forceUpright = true;

    [Header("Piece Pool")]
    public List<GameObject> trackPrefabs;

    // Internal Analysis Data
    private struct PieceData
    {
        public GameObject prefab;
        public float heightChange; // How much this piece moves you up/down
        public float endPitch;     // Does the exit point up (ramp) or flat?
    }

    private List<PieceData> analyzedPieces = new List<PieceData>();
    private List<GameObject> spawnedPieces = new List<GameObject>();

    [ContextMenu("Generate Track")]
    public void Generate()
    {
        ClearTrack();

        // 1. Analyze the pieces so we know what they do
        AnalyzePrefabs();

        Random.InitState(seed);

        // Track the current state of the "tip" of the track
        Transform previousSocketOut = null;
        float currentHeight = 0f;

        for (int i = 0; i < totalPieces; i++)
        {
            // 2. Filter the list of pieces based on our Constraints
            GameObject chosenPrefab = PickValidPiece(currentHeight);

            if (chosenPrefab == null)
            {
                Debug.LogError("No valid pieces found! Check your Min/Max height constraints.");
                break;
            }

            // 3. Instantiate
            GameObject newPiece = Instantiate(chosenPrefab, transform);
            TrackPiece pieceScript = newPiece.GetComponent<TrackPiece>(); // Assuming you have this helper script

            // If you don't have the TrackPiece script, we find children manually:
            Transform socketIn = pieceScript ? pieceScript.socketIn : newPiece.transform.Find("Socket_In");
            Transform socketOut = pieceScript ? pieceScript.socketOut : newPiece.transform.Find("Socket_Out");

            if (socketIn == null || socketOut == null)
            {
                Debug.LogError($"Piece {newPiece.name} missing sockets!");
                break;
            }

            // 4. Snap
            if (previousSocketOut != null)
            {
                SnapWithConstraints(newPiece.transform, socketIn, previousSocketOut);
            }
            else
            {
                // First piece stays at (0,0,0)
                newPiece.transform.localPosition = Vector3.zero;
            }

            // Update State
            previousSocketOut = socketOut;
            currentHeight = socketOut.position.y - transform.position.y;
            spawnedPieces.Add(newPiece);
        }
    }

    private void AnalyzePrefabs()
    {
        analyzedPieces.Clear();
        foreach (var prefab in trackPrefabs)
        {
            // We instantiate a ghost copy to measure it accurately
            // (You could also read the transform data directly if you trust the prefab values)
            GameObject ghost = Instantiate(prefab, Vector3.zero + Vector3.down * 1000, Quaternion.identity);

            Transform sIn = ghost.transform.Find("Socket_In");
            Transform sOut = ghost.transform.Find("Socket_Out");

            if (sIn && sOut)
            {
                // Calculate local height difference
                float deltaY = sOut.position.y - sIn.position.y;
                // Calculate output pitch (Is it pointing up?)
                float pitch = sOut.rotation.eulerAngles.x;

                analyzedPieces.Add(new PieceData
                {
                    prefab = prefab,
                    heightChange = deltaY,
                    endPitch = pitch
                });
            }
            DestroyImmediate(ghost);
        }
    }

    private GameObject PickValidPiece(float currentHeight)
    {
        // Find all pieces that WON'T break our height limits
        var validOptions = analyzedPieces.Where(p =>
        {
            float predictedHeight = currentHeight + p.heightChange;
            return predictedHeight >= minHeight && predictedHeight <= maxHeight;
        }).ToList();

        // Fallback: If no piece fits (e.g. we are at max height and only have Up-Ramps),
        // try to find the "least bad" option (closest to 0 height change).
        if (validOptions.Count == 0)
        {
            validOptions = analyzedPieces.OrderBy(p => Mathf.Abs(p.heightChange)).Take(1).ToList();
        }

        if (validOptions.Count == 0) return null;

        // Pick random from the valid list
        return validOptions[Random.Range(0, validOptions.Count)].prefab;
    }

    private void SnapWithConstraints(Transform pieceRoot, Transform pieceSocketIn, Transform targetSocketOut)
    {
        // 1. Calculate Rotation
        // We want -pieceSocketIn.forward to match targetSocketOut.forward

        Quaternion targetRot = targetSocketOut.rotation;

        if (forceUpright)
        {
            // REMOVE ROLL:
            // This prevents the "corkscrew" effect where small errors make the track sideways.
            // We take the target forward vector, but we force the "Up" vector to be World.Up
            Vector3 forward = targetSocketOut.forward;

            // If the track is vertical (straight up/down), LookRotation fails with Vector3.up.
            // We check for that edge case.
            if (Mathf.Abs(Vector3.Dot(forward, Vector3.up)) < 0.99f)
            {
                targetRot = Quaternion.LookRotation(forward, Vector3.up);
            }
        }

        // Apply the rotation difference (Standard Socket Math)
        Quaternion socketInLocalRot = pieceSocketIn.localRotation;
        pieceRoot.rotation = targetRot * Quaternion.Euler(0, 180, 0) * Quaternion.Inverse(socketInLocalRot);

        // 2. Calculate Position
        // Move root so socketIn lands exactly on targetSocketOut
        Vector3 currentSocketInPos = pieceSocketIn.position;
        Vector3 offset = targetSocketOut.position - currentSocketInPos;
        pieceRoot.position += offset;
    }

    private void ClearTrack()
    {
        foreach (var p in spawnedPieces)
        {
            if (p) DestroyImmediate(p);
        }
        spawnedPieces.Clear();
    }
}