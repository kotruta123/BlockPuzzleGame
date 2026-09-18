using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ScoreStore
{
    [Serializable]
    private class SaveData
    {
        public List<int> scores = new List<int>();
    }

    public static string FilePath =>
        Path.Combine(
            Application.persistentDataPath,
            "highscores.json");

    public static List<int> Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new List<int>();

            string json = File.ReadAllText(FilePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            if (data == null || data.scores == null)
                return new List<int>();

            data.scores.RemoveAll(score => score < 0);
            SortAndTrim(data.scores);

            return data.scores;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "Could not load high scores: " +
                exception.Message);

            return new List<int>();
        }
    }

    public static List<int> AddScore(int score)
    {
        List<int> scores = Load();
        scores.Add(Mathf.Max(0, score));
        SortAndTrim(scores);

        try
        {
            Directory.CreateDirectory(
                Application.persistentDataPath);

            var data = new SaveData { scores = scores };

            File.WriteAllText(
                FilePath,
                JsonUtility.ToJson(data, true));
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "Could not save high scores: " +
                exception.Message);
        }

        return scores;
    }

    private static void SortAndTrim(List<int> scores)
    {
        scores.Sort((a, b) => b.CompareTo(a));

        if (scores.Count > 3)
            scores.RemoveRange(3, scores.Count - 3);
    }
}