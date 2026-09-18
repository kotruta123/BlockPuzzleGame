using UnityEngine;

[CreateAssetMenu(
    fileName = "GameSettings",
    menuName = "Block Puzzle/Game Settings")]
public class GameSettings : ScriptableObject
{
    [Min(1)] public int width = 8;
    [Min(1)] public int height = 8;
    [Min(1)] public int pointsPerCell = 10;

    [Range(0f, 1f)] public float musicVolume = 0.35f;
    [Range(0f, 1f)] public float pausedMusicVolume = 0.08f;
}