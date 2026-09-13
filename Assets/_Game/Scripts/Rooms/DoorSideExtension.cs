using System;

namespace DungeonCrawler.Rooms
{
    public static class DoorSideExtensions
    {
        public static DoorSide Opposite(this DoorSide side)
        {
            return side switch
            {
                DoorSide.North => DoorSide.South,
                DoorSide.South => DoorSide.North,
                DoorSide.East => DoorSide.West,
                DoorSide.West => DoorSide.East,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}