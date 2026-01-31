using UnityEngine;
using System.Collections.Generic;

public enum Direction
{
    Left, Right, Up, Down
}

[CreateAssetMenu(fileName = "TrickData", menuName = "Scriptable Objects/TrickData")]
public class TrickData : ScriptableObject
{
    public string trickName;
    public List<Direction> directions;
    public float trickTime;
}
