using System;

namespace _2DMapGenerator
{
    public class PerlinNoise
    {
        public static int[,] Permutation = new int[16, 16]
        {
            { 151, 160, 137,  91,  90,  15, 131,  13, 201,  95,  96,  53, 194, 233,   7, 225 },
            { 140,  36, 103,  30,  69, 142,   8,  99,  37, 240,  21,  10,  23, 190,   6, 148},
            {247, 120, 234,  75,   0,  26, 197,  62,  94, 252, 219, 203, 117,  35,  11,  32},
            { 57, 177,  33,  88, 237, 149,  56,  87, 174,  20, 125, 136, 171, 168,  68, 175},
            { 74, 165,  71, 134, 139,  48,  27, 166,  77, 146, 158, 231,  83, 111, 229, 122},
            { 60, 211, 133, 230, 220, 105,  92,  41,  55,  46, 245,  40, 244, 102, 143,  54},
            { 65,  25,  63, 161,   1, 216,  80,  73, 209,  76, 132, 187, 208,  89,  18, 169},
            {200, 196, 135, 130, 116, 188, 159,  86, 164, 100, 109, 198, 173, 186,   3,  64},
            { 52, 217, 226, 250, 124, 123,   5, 202,  38, 147, 118, 126, 255,  82,  85, 212},
            {207, 206,  59, 227,  47,  16,  58,  17, 182, 189,  28,  42, 223, 183, 170, 213},
            {119, 248, 152,   2,  44, 154, 163,  70, 221, 153, 101, 155, 167,  43, 172,   9},
            {129,  22,  39, 253,  19,  98, 108, 110,  79, 113, 224, 232, 178, 185, 112, 104},
            {218, 246,  97, 228, 251,  34, 242, 193, 238, 210, 144,  12, 191, 179, 162, 241},
            { 81,  51, 145, 235, 249,  14, 239, 107,  49, 192, 214,  31, 181, 199, 106, 157},
            {184,  84, 204, 176, 115, 121,  50,  45, 127,   4, 150, 254, 138, 236, 205,  93},
            {222, 114,  67,  29,  24,  72, 243, 141, 128, 195,  78,  66, 215,  61, 156, 180}
        };
        public struct Vector2
        {
            public float x, y;
        }
        private static float Smoothstep(float t)
        {
            // Smoothstep interpolation function
            return t * t * (3 - 2 * t);
        }

        private static float Interpolate(float a0, float a1, float w)
        {
            // Smoothstep interpolation between a0 and a1
            return a0 + Smoothstep(w) * (a1 - a0);
        }

        private static Vector2 RandomGradient(uint ix, uint iy, int seed)
        {
            // Use a hash function to generate a pseudo-random angle based on the coordinates and seed
            //int hash = (1619 * ix + 31337 * iy + 1013 * seed) & 0x7fffffff; // Example hash function
            //float randomAngle = (float)((float)hash / 0x7fffffff * 2 * Math.PI); // Convert hash to angle in radians

            // Convert angle to unit vector
            Vector2 gradient;
            gradient.x = (float)Math.Cos(randomAngle);
            gradient.y = (float)Math.Sin(randomAngle);

            return gradient;
        }

        private static float DotGridGradient(int ix, int iy, float x, float y, int seed)
        {
            // Get gradient from integer coordinates
            Vector2 gradient = RandomGradient(ix, iy, seed);

            // Compute the distance vector
            float dx = x - ix;
            float dy = y - iy;

            // Compute the dot-product
            return (dx * gradient.x + dy * gradient.y);
        }

        public static float[,] ComputePerlinNoiseMap(int width, int height)
        {
            float[,] noiseMap = new float[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    noiseMap[y, x] = ComputePerlinNoise(x, y);
                }
            }

            return noiseMap;
        }

        public static float ComputePerlinNoise(float x, float y)
        {
            int scaledX = (int)x & 255;
            int scaledY = (int)y & 255;

            // Determine grid cell coordinates
            int xf = (int)Math.Floor(x);
            int x1 = x0 + 1;
            int yf = (int)Math.Floor(y);
            int y1 = y0 + 1;

            // Determine interpolation weights
            // Could also use higher order polynomial/s-curve here
            float sx = x - x0;
            float sy = y - y0;

            // Interpolate between grid point gradients
            float n0, n1, ix0, ix1, value;

            n0 = DotGridGradient(x0, y0, x, y, seed);
            n1 = DotGridGradient(x1, y0, x, y, seed);
            ix0 = Interpolate(n0, n1, sx);

            n0 = DotGridGradient(x0, y1, x, y, seed);
            n1 = DotGridGradient(x1, y1, x, y, seed);
            ix1 = Interpolate(n0, n1, sx);

            value = Interpolate(ix0, ix1, sy);
            return value * 0.5f + 0.5f; // Will return in range -1 to 1. To make it in range 0 to 1, multiply by 0.5 and add 0.5
        }
    }
}
