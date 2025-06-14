using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using dArtagnan.Shared;

public class DirectionHelperClient
{
    private static readonly List<Vector3> Directions = DirectionHelper.Directions.Select(d => new Vector3(d.X, d.Y, d.Z)).ToList();

    public static Vector3 IntToDirection(int i)
    {
        return Directions[i];
    }
    
    public static int DirectionToInt(Vector3 direction)
    {
        if (direction == Vector3.zero) return 0;
        var minAngle = float.MaxValue;
        var closestIndex = -1;

        for (var i = 1; i < Directions.Count; i++)
        {
            var angle = Vector3.Angle(direction, Directions[i]);
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