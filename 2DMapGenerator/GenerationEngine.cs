using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
                    if(value < 10)
                    {
                        InfoEvent?.Invoke(this, new InfoEventArgs("Height cannot be less than 10!"));
                        return;
                    }

                    _height = value;
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
                    if (value < 10)
                    {
                        InfoEvent?.Invoke(this, new InfoEventArgs("Width cannot be less than 10!"));
                        return;
                    }
                    _width = value;
                    InfoEvent?.Invoke(this, new InfoEventArgs("Width changed to " + _width));
                }
                else
                {
                    InfoEvent?.Invoke(this, new InfoEventArgs("Cannot change width while working..."));
                }
            }
        }

        private int _rough;
        public int Roughness
        {
            get
            {
                return _rough;
            }
            set
            {
                if (!_working)
                {

                    if(value < 1)
                    {
                        InfoEvent?.Invoke(this, new InfoEventArgs("Roughness cannot be less than 1!"));
                        return;
                    }
                    if(value > 10)
                    {
                        InfoEvent?.Invoke(this, new InfoEventArgs("Roughness cannot be more than 10!"));
                        return;
                    }
                    _rough = value;
                    InfoEvent?.Invoke(this, new InfoEventArgs("Roughness changed to " + _rough));
                }
                else
                {
                    InfoEvent?.Invoke(this, new InfoEventArgs("Cannot change smoothness while working..."));
                }
            }
        }

        #endregion

        private GenerationEngine()
        {
            Height = 600;
            Width = 600;
            Seed = 0;
            Roughness = 6;
        }

        public async void StartGenerate()
        {
            if (!_working)
            {
                Working = true;
                InfoEvent?.Invoke(this, new InfoEventArgs("Generation started!"));
                Map generated = await Task.Run(() => GenerateMap());
                GeneratedMap = generated;
                InfoEvent?.Invoke(this, new InfoEventArgs("Generation finished!"));
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

        
        private async Task<Map> GenerateMap()
        {
            int width = Width;
            int height = Height;
            int roughness = Roughness;

            //if(Seed == 0)
            //    Seed = new Random().Next();

            PerlinNoiseGenerator pg = PerlinNoiseGenerator.Create(Seed);
            Map perlinMap = await pg.ComputePerlinNoiseMapAsync(width, height, roughness);

            return perlinMap;
        }

    }

    public class Map
    {

        float[,] map;
        public int Height
        {
            get
            {
                return map.GetLength(0);
            }
        }
        public int Width
        {
            get
            {
                return map.GetLength(1);
            }
        }
        public float this[int x, int y]
        {
            get
            {
                return map[y, x];
            }
            set
            {
                map[y, x] = value;
            }
        }

        public Map(int width, int height)
        { 
            this.map = new float[height, width];
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
