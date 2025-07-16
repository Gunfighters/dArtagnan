public static class VecConverter
{
    public static UnityEngine.Vector2 ToUnityVec(System.Numerics.Vector2 vec)
    {
        return new UnityEngine.Vector2(vec.X, vec.Y);
    }

    public static System.Numerics.Vector2 ToSystemVec(UnityEngine.Vector2 vec)
    {
        return new System.Numerics.Vector2(vec.x, vec.y);
    }
}