using System;
using System.Collections.Generic;
using UnityEngine;

public static class DirectionHelper
{
    private static readonly List<Vector3> Directions = new()
    {
        Vector3.zero,
        Vector3.up,
        (Vector3.up + Vector3.right).normalized,
        Vector3.right,
        (Vector3.right + Vector3.down).normalized,
        Vector3.down,
        (Vector3.down + Vector3.left).normalized,
        Vector3.left,
        (Vector3.left + Vector3.up).normalized,
    };

    public static Vector3 IntToDirection(int direction)
    {
        return Directions[direction];
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