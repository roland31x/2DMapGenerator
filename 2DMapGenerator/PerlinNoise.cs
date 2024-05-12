using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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

            while (n > 0)
            {
                n--;
                int k = rng.Next(n + 1);
                int toadd = ints[k];
                generated.Add(toadd);
                ints.Remove(toadd);
            }

            permutation = generated.ToArray();
        }

        private double Fade(double t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        private double Lerp(double t, double a, double b)
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
            int X = (int)Math.Floor(x) % 255;
            int Y = (int)Math.Floor(y) % 255;

            double xf = x - Math.Floor(x);
            double yf = y - Math.Floor(y);

            double u = Fade(xf);
            double v = Fade(yf);


            Vector2 topRight = new Vector2(xf - 1.0, yf - 1.0);
            Vector2 topLeft = new Vector2(xf, yf - 1.0);
            Vector2 bottomRight = new Vector2(xf - 1.0, yf);
            Vector2 bottomLeft = new Vector2(xf, yf);

            int tr = permutation[X + 1] + Y + 1;
            Vector2 constantVectorTopRight = GetConstantVector(permutation[tr % 255]);

            int tl = permutation[X] + Y + 1;
            Vector2 constantVectorTopLeft = GetConstantVector(permutation[tl % 255]);

            int br = permutation[X + 1] + Y;
            Vector2 constantVectorBottomRight = GetConstantVector(permutation[br % 255]);

            int bl = permutation[X] + Y;
            Vector2 constantVectorBottomLeft = GetConstantVector(permutation[bl % 255]);

            double dotTopRight = topRight.Dot(constantVectorTopRight);
            double dotTopLeft = topLeft.Dot(constantVectorTopLeft);
            double dotBottomRight = bottomRight.Dot(constantVectorBottomRight);
            double dotBottomLeft = bottomLeft.Dot(constantVectorBottomLeft);

            double result = Lerp(u,
                Lerp(v, dotBottomLeft, dotTopLeft),
                Lerp(v, dotBottomRight, dotTopRight)
            );



            return (float)result;
            
        }

        public float FractalBrownianMotion(float x, float y, int numOctaves)
        {
            float result = 0.0f;
            float amplitude = 1.0f;
            float frequency = 0.005f;

            for (int octave = 0; octave < numOctaves; octave++)
            {
                float n = amplitude * ComputePerlinNoise(x * frequency, y * frequency);
                result += n;

                amplitude *= 0.5f;
                frequency *= 2.0f;
            }

            return result;
        }

        public Task<Map> ComputePerlinNoiseMapAsync(int width, int height, int smoothness)
        {
            Map map = new Map(width, height);

            float minsofar = float.MaxValue;
            float maxsofar = float.MinValue;

            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    map[x, y] = FractalBrownianMotion(x, y, smoothness);
                    if (map[x, y] < minsofar)
                        minsofar = map[x, y];
                    if (map[x, y] > maxsofar)
                        maxsofar = map[x, y];
                }
            });

            //float range = maxsofar - minsofar;

            //Parallel.For(0, height, y =>
            //{
            //    for (int x = 0; x < width; x++)
            //    {
            //        map[x, y] = (map[x, y] - minsofar) / range;
            //    }
            //});

            return Task.FromResult(map);
        }

        public static PerlinNoiseGenerator Create(int seed)
        {
            return new PerlinNoiseGenerator(seed);
        }
    }
}
