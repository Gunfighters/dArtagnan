namespace Utils
{
    public static class VectorExtension
    {
        public static UnityEngine.Vector2 ToUnityVec(this System.Numerics.Vector2 vec)
        {
            return new UnityEngine.Vector2(vec.X, vec.Y);
        }

        public static System.Numerics.Vector2 ToSystemVec(this UnityEngine.Vector2 vec)
        {
            return new System.Numerics.Vector2(vec.x, vec.y);
        }
        public static UnityEngine.Vector2 SnapToCardinalDirection(this UnityEngine.Vector2 dir)
        {
            if (dir == UnityEngine.Vector2.zero) return UnityEngine.Vector2.zero;

            var angle = UnityEngine.Mathf.Atan2(dir.y, dir.x) * UnityEngine.Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            return angle switch
            {
                >= 45f and < 135f => UnityEngine.Vector2.up,
                >= 135f and < 225f => UnityEngine.Vector2.left,
                >= 225f and < 315f => UnityEngine.Vector2.down,
                _ => UnityEngine.Vector2.right
            };
        }
    }
}