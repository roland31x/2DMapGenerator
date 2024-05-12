using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;

namespace _2DMapGenerator
{
    public class GenerationEngine
    {
        private static GenerationEngine _singleton;
        public static GenerationEngine Singleton
        {
            get
            {
                if (_singleton == null)
                {
                    _singleton = new GenerationEngine();
                }
                return _singleton;
            }
        }

    #region UI Stuff

        private bool _working = false;

        public bool Working
        {
            get
            {
                return _working;
            }
            private set
            {
                _working = value;
                if(_working)
                {
                    GenerationStarted?.Invoke(this, new EventArgs());
                }
                else
                {
                    GenerationFinished?.Invoke(this, new EventArgs());
                }
            }
        }

        private bool _forceStop = false;

        private double _status = 0;
        private int _percent = 0;
        private void SetStatus(double newstatus)
        {
            _status = newstatus;
            if (_status - _percent > 1)
            {
                _percent = (int)_status;
                InfoEvent?.Invoke(this, new InfoEventArgs("Generation status: " + _percent + "%"));
            }
           
        }

        private Map _map;
        public Map GeneratedMap
        {
            get
            {
                return _map;
            }
            set
            {
                _map = value;
            }
        }

        public event InfoEventHandler InfoEvent;
        public delegate void InfoEventHandler(object sender, InfoEventArgs e);

        public event GenerationStartEventHandler GenerationStarted;
        public delegate void GenerationStartEventHandler(object sender, EventArgs e);

        public event GenerationFinishedEventHandler GenerationFinished;
        public delegate void GenerationFinishedEventHandler(object sender, EventArgs e);

        #endregion

    #region Params

        private int _seed;
        public int Seed
        {
            get
            {
                return _seed;
            }
            set
            {
                if (!_working)
                {
                    _seed = value;
                    InfoEvent?.Invoke(this, new InfoEventArgs("Seed changed to " + _seed));
                }
                else
                {
                    InfoEvent?.Invoke(this, new InfoEventArgs("Cannot change seed while working..."));
                }
            }
        }

        private int _height;
        public int Height
        {
            get
            {
                return _height;
            }
            set
            {
                if (!_working)
                {
                    _height = value;
                    if(_height < 10)
                    {
                        InfoEvent?.Invoke(this, new InfoEventArgs("Height cannot be less than 10!"));
                    }
                    InfoEvent?.Invoke(this, new InfoEventArgs("Height changed to " + _height));
                }
                else
                {
                    InfoEvent?.Invoke(this, new InfoEventArgs("Cannot change height while working..."));
                }
            }
        }
        private int _width;
        public int Width
        {
            get
            {
                return _width;
            }
            set
            {
                if (!_working)
                {
                    _width = value;
                    if (_width < 10)
                    {
                        InfoEvent?.Invoke(this, new InfoEventArgs("Width cannot be less than 10!"));
                    }
                    InfoEvent?.Invoke(this, new InfoEventArgs("Width changed to " + _width));
                }
                else
                {
                    InfoEvent?.Invoke(this, new InfoEventArgs("Cannot change width while working..."));
                }
            }
        }

#endregion

        private GenerationEngine()
        {
            Height = 600;
            Width = 600;
        }

        public async void StartGenerate()
        {
            if (!_working)
            {
                Working = true;

                Map generated = await Task.Run(() => GenerateMap());
                GeneratedMap = generated;

                Working = false;
            }
            else
            {
                InfoEvent?.Invoke(this, new InfoEventArgs("Engine is already generating! Cancel the generation first!"));
            }
        }

        public void ForceStop()
        {
            if (_working)
            {
                _forceStop = true;
            }
            else
            {
                InfoEvent?.Invoke(this, new InfoEventArgs("Engine is not working!"));
            }
        }

        public class PerlinNoise
        {
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

            private static Vector2 RandomGradient(int ix, int iy, int seed)
            {
                // Use a hash function to generate a pseudo-random angle based on the coordinates and seed
                int hash = (1619 * ix + 31337 * iy + 1013 * seed) & 0x7fffffff; // Example hash function
                float randomAngle = (float)((float)hash / 0x7fffffff * 2 * Math.PI); // Convert hash to angle in radians

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

            public static float[,] ComputePerlinNoiseMap(int width, int height, float scale, int seed)
            {
                float[,] noiseMap = new float[height, width];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float xCoord = (float)x / width * scale;
                        float yCoord = (float)y / height * scale;
                        noiseMap[y, x] = ComputePerlinNoise(xCoord, yCoord, seed);
                    }
                }

                return noiseMap;
            }

            public static float ComputePerlinNoise(float x, float y, int seed)
            {
                // Determine grid cell coordinates
                int x0 = (int)Math.Floor(x);
                int x1 = x0 + 1;
                int y0 = (int)Math.Floor(y);
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
                return value; // Will return in range -1 to 1. To make it in range 0 to 1, multiply by 0.5 and add 0.5
            }
        }
        private async Task<Map> GenerateMap()
        {
            int width = 100;
            int height = 100;
            float scale = 2.0f;
            int seed = 12345;

            Map noiseMap = new Map(PerlinNoise.ComputePerlinNoiseMap(width, height, scale, seed));
            return noiseMap;
        }

    }
    public class Map
    {
        public float[,] map;
        public Map(float[,] map)
        { 
            this.map = map; 
        }
    }

    public class InfoEventArgs : EventArgs
    {
        public string Info { get; }
        public InfoEventArgs(string info)
        {
            Info = info;
        }
    }
}
