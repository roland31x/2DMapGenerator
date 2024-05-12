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

        private async Task<Map> GenerateMap()
        {
            Map generated = new Map(Width, Height);
            Random random = new Random(Seed);
            
            for(int y = 0; y < Height; y++)
            {
                for(int x = 0; x < Width; x++)
                {
                    if (_forceStop)
                    {
                        return null;
                    }

                    generated[x, y] = random.NextDouble();
                    SetStatus(((double)(x * y) / (double)(Width * Height)) * 100.0);
                }
            }

            return generated;
        }

    }
    public class Map
    {
        private double[,] map;

        public int Width { get { return map.GetLength(0); } }
        public int Height { get { return map.GetLength(1); } }
        public double this[int x, int y]
        {
            get
            {
                return map[x, y];
            }
            set
            {
                map[x, y] = value;
            }
        }
        public Map(int w, int h)
        {
            map = new double[w, h];
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
