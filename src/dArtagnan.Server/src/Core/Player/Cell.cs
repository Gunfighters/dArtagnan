using System.Numerics;

namespace dArtagnan.Server;

public readonly record struct Cell(int X, int Y)
{
    public static Cell operator +(Cell a, Cell b) => new(a.X + b.X, a.Y + b.Y);
    public static Cell operator -(Cell a, Cell b) => new(a.X - b.X, a.Y - b.Y);
    public Vector2 ToVec() => new(X, Y);
    public override string ToString() => $"Cell ({X}, {Y})";
}