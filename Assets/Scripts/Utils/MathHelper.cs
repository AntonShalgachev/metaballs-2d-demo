using UnityEngine;

namespace UnityPrototype
{
    public static class MathHelper
    {
        public static float IntegerPow(float value, int power)
        {
            var result = 1.0f;
            while (power > 0)
            {
                result *= value;
                --power;
            }

            return result;
        }

        public static float DistanceSqr(Vector2 a, Vector2 b)
        {
            var dx = b.x - a.x;
            var dy = b.y - a.y;
            return dx * dx + dy * dy;
        }
    }
}
