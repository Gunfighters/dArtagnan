using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using dArtagnan.Shared;

public static class DirectionHelperClient
{
    private static readonly List<Vector2> Directions = DirectionHelper.Directions.Select(d => new Vector2(d.X, d.Y)).ToList();

    public static Vector2 IntToDirection(this int i)
    {
        return Directions[i];
    }
    
    public static int DirectionToInt(this Vector2 direction)
    {
        if (direction == Vector2.zero) return 0;
        var minAngle = float.MaxValue;
        var closestIndex = -1;

        for (var i = 1; i < Directions.Count; i++)
        {
            var angle = Vector2.Angle(direction, Directions[i]);
            if (angle < minAngle)
            {
                minAngle = angle;
                closestIndex = i;
            }
        }

        if (closestIndex == -1)
            throw new Exception($"DirectionToInt(): Can't convert into int: {direction}");
        return closestIndex;
    }
}