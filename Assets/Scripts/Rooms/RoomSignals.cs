using System;
using Edgar.Unity; // for RoomInstanceGrid2D
using UnityEngine;

namespace Edgar.Unity.Examples.CurrentRoomDetection
{
    /// <summary>Global events for room entry/exit.</summary>
    public static class RoomSignals
    {
        public static event Action<RoomInstanceGrid2D, GameObject> RoomEntered;
        public static event Action<RoomInstanceGrid2D, GameObject> RoomExited;

        public static void RaiseEntered(RoomInstanceGrid2D room, GameObject player) => RoomEntered?.Invoke(room, player);
        public static void RaiseExited(RoomInstanceGrid2D room, GameObject player) => RoomExited?.Invoke(room, player);
    }
}