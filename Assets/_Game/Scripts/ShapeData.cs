using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewShape",
    menuName = "Block Puzzle/Shape")]
public class ShapeData : ScriptableObject
{
    [SerializeField, HideInInspector]
    private bool[] cells = new bool[25];

    public List<Vector2Int> GetCells()
    {
        var result = new List<Vector2Int>();

        if (cells == null || cells.Length != 25)
            return result;

        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                if (cells[y * 5 + x])
                    result.Add(new Vector2Int(x, y));
            }
        }

        return Normalize(result);
    }

    public static List<Vector2Int> Normalize(
        List<Vector2Int> source)
    {
        var result = new List<Vector2Int>();

        if (source.Count == 0)
            return result;

        int minX = int.MaxValue;
        int minY = int.MaxValue;

        foreach (Vector2Int cell in source)
        {
            minX = Mathf.Min(minX, cell.x);
            minY = Mathf.Min(minY, cell.y);
        }

        foreach (Vector2Int cell in source)
        {
            result.Add(new Vector2Int(
                cell.x - minX,
                cell.y - minY));
        }

        return result;
    }

    public static Vector2Int GetSize(List<Vector2Int> source)
    {
        int width = 0;
        int height = 0;

        foreach (Vector2Int cell in source)
        {
            width = Mathf.Max(width, cell.x + 1);
            height = Mathf.Max(height, cell.y + 1);
        }

        return new Vector2Int(width, height);
    }

    public static List<Vector2Int> RotateClockwise(
        List<Vector2Int> source)
    {
        var result = new List<Vector2Int>();
        Vector2Int size = GetSize(source);

        foreach (Vector2Int cell in source)
        {
            result.Add(new Vector2Int(
                size.y - 1 - cell.y,
                cell.x));
        }

        return Normalize(result);
    }
}