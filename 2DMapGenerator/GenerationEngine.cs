using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;

namespace _2DMapGenerator
{
    public class GenerationEngine : INotifyPropertyChanged
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

        private string _status = "Idle...";

        public string Status
        {
            get
            {
                return _status;
            }
            private set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        private Map _map;
        private bool _validgeneration = false;
        public bool ValidGeneration
        {
            get
            {
                return _validgeneration;
            }
        }

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

        //public event InfoEventHandler InfoEvent;
        //public delegate void InfoEventHandler(object sender, InfoEventArgs e);

        public event GenerationStartEventHandler GenerationStarted;
        public delegate void GenerationStartEventHandler(object sender, EventArgs e);

        public event GenerationFinishedEventHandler GenerationFinished;
        public delegate void GenerationFinishedEventHandler(object sender, EventArgs e);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Params

        private int? _seed;
        public int? Seed
        {
            get
            {
                return _seed;
            }
            set
            {
                if (!_working)
                {
                    if(value == null)
                    {
                        _seed = null;
                        Status = "Seed reset!";
                        return;
                    }

                    if(value < 0)
                    {
                        Status = "Invalid Seed!";
                        return;
                    }
                    _seed = value;
                    Status = "Seed changed to " + _seed;
                }
                else
                {
                    Status = "Cannot change seed while working...";
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
                        Status = "Height cannot be less than 10!";
                        return;
                    }

                    _height = value;
                    Status = "Height changed to " + _height;
                }
                else
                {
                    Status = "Cannot change height while working...";
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
                        Status = "Width cannot be less than 10!";
                        return;
                    }
                    _width = value;
                    Status = "Width changed to " + _width;
                }
                else
                {
                    Status = "Cannot change width while working...";
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
                        Status = "Roughness cannot be less than 1!";
                        return;
                    }
                    if(value > 10)
                    {
                        Status = "Roughness cannot be more than 10!";
                        return;
                    }
                    _rough = value;
                    Status = "Roughness changed to " + _rough;
                }
                else
                {
                    Status = "Cannot change smoothness while working...";
                }
            }
        }

        #endregion

        private GenerationEngine()
        {
            Seed = null;
            Height = 600;
            Width = 600;
            Roughness = 6;
        }

        private CancellationTokenSource cts = new CancellationTokenSource();

        public async void StartGenerate()
        {
            if (!_working)
            {
                Working = true;
                cts = new CancellationTokenSource();
                CancellationToken token = cts.Token;            
                Status = "Generation started!";
                Map generated = await Task.Run(() => GenerateMap(token));
                GeneratedMap = generated;
                
                if (token.IsCancellationRequested)
                {
                    Status = "Generation cancelled!";
                    _validgeneration = false;
                    _map = null;
                    Working = false;
                    return;
                }

                _validgeneration = true;
                Status = "Generation finished!";
                Working = false;
            }
            else
            {
                Status = "Engine is already generating! Cancel the generation first!";
            }
        }

        public void ForceStop()
        {
            if (_working)
            {
                cts.Cancel();
            }
            else
            {
                Status = "Engine is not working!";
            }
        }


        private async Task<Map> GenerateMap(CancellationToken token)
        {
            int width = Width;
            int height = Height;
            int roughness = Roughness;

            if (Seed == null)
                _seed = new Random().Next();

            PerlinNoiseGenerator pg = PerlinNoiseGenerator.Create((int)Seed, token);
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
