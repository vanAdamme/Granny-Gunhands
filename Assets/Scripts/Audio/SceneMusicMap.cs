using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Scene Music Map")]
public class SceneMusicMap : ScriptableObject
{
    [Serializable]
    public struct Entry { public string sceneName; public MusicTrack track; }

    public List<Entry> entries = new();

    public MusicTrack GetFor(string sceneName)
    {
        for (int i = 0; i < entries.Count; i++)
            if (!string.IsNullOrEmpty(entries[i].sceneName) &&
                string.Equals(entries[i].sceneName, sceneName, StringComparison.Ordinal))
                return entries[i].track;
        return null;
    }
}