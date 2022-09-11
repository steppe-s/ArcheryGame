using UnityEngine;

namespace _Scripts.Utils
{
    public static class MathUtilities
    {
        public static Vector2 RotateVectorByDelta(Vector2 v, float delta) {
            return new Vector2(
                v.x * Mathf.Cos(delta) - v.y * Mathf.Sin(delta),
                v.x * Mathf.Sin(delta) + v.y * Mathf.Cos(delta)
            );
        }
    }
}
