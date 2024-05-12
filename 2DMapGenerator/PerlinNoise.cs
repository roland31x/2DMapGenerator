using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace _2DMapGenerator
{
    public struct Vector2
    {
        public double x, y;

        public Vector2(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public double Dot(Vector2 other)
        {
            return x * other.x + y * other.y;
        }
    }
    public class PerlinNoiseGenerator
    {
        int[] permutation;

        private PerlinNoiseGenerator(int seed)
        {
            Random rng = new Random(seed);
            int n = 256;
            List<int> ints = new List<int>();
            for (int i = 0; i < n; i++)
                ints.Add(i);

            List<int> generated = new List<int>();

            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                int toadd = ints[k];
                generated.Add(toadd);
                ints.Remove(toadd);
            }

            permutation = generated.ToArray();
        }

        private static double Fade(double t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        private static double Lerp(double t, double a, double b)
        {
            return a + t * (b - a);
        }
        
        private Vector2 GetConstantVector(int v)
        {
            int h = v & 3;
            switch (h)
            {
                case 0:
                    return new Vector2(1.0, 1.0);
                case 1:
                    return new Vector2(-1.0, 1.0);
                case 2:
                    return new Vector2(-1.0, -1.0);
                default:
                    return new Vector2(1.0, -1.0);
            }
        }
        public float ComputePerlinNoise(float x, float y)
        {
            int X = (int)Math.Floor(x) & 255;
            int Y = (int)Math.Floor(y) & 255;

            double xf = x - Math.Floor(x);
            double yf = y - Math.Floor(y);

            double u = Fade(xf);
            double v = Fade(yf);


            Vector2 topRight = new Vector2(xf - 1.0, yf - 1.0);
            Vector2 topLeft = new Vector2(xf, yf - 1.0);
            Vector2 bottomRight = new Vector2(xf - 1.0, yf);
            Vector2 bottomLeft = new Vector2(xf, yf);

            Vector2 constantVectorTopRight = GetConstantVector(permutation[(permutation[X + 1] + Y + 1) % 255]);
            Vector2 constantVectorTopLeft = GetConstantVector(permutation[(permutation[X] + Y + 1) % 255]);
            Vector2 constantVectorBottomRight = GetConstantVector(permutation[(permutation[X + 1] + Y) % 255]);
            Vector2 constantVectorBottomLeft = GetConstantVector(permutation[(permutation[X] + Y) % 255]);

            double dotTopRight = topRight.Dot(constantVectorTopRight);
            double dotTopLeft = topLeft.Dot(constantVectorTopLeft);
            double dotBottomRight = bottomRight.Dot(constantVectorBottomRight);
            double dotBottomLeft = bottomLeft.Dot(constantVectorBottomLeft);

            double result = Lerp(u,
                Lerp(v, dotBottomLeft, dotTopLeft),
                Lerp(v, dotBottomRight, dotTopRight)
            );

            return (float)result * 0.5f + 0.5f;
            
        }

        public float[,] ComputePerlinNoiseMap(int width, int height, float scale)
        {
            float[,] noiseMap = new float[height, width];

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    noiseMap[y, x] = ComputePerlinNoise(x * scale, y * scale);

            return noiseMap;
        }

        public static PerlinNoiseGenerator Create(int seed)
        {
            return new PerlinNoiseGenerator(seed);
        }
    }
}
