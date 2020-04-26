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
    }
}
