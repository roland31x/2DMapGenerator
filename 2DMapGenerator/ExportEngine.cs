using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;

namespace _2DMapGenerator
{
    public class ExportEngine 
    {
        private static ExportEngine _singleton;   
        public static ExportEngine Singleton
        {
            get
            {
                if (_singleton == null)
                {
                    _singleton = new ExportEngine();
                }
                return _singleton;
            }
        }

        private ExportEngine()
        {

        }

        public async Task Export(bool exportImage, bool exportObject, string path, Map map, ColorPalette selected)
        {
            if (exportImage)
            {
                await ExportImage(path, map, selected);
            }

            if (exportObject)
            {
                await ExportObject(path, map);
            }
        }

        private async Task ExportImage(string path, Map map, ColorPalette colors)
        {
            int done = 0;
            int height = map.Height;
            int width = map.Width;
            byte[] pixelData = new byte[width * height * 4];

            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    Color color = colors.GetColor(map[x, y]);
                    int pixelIndex = (y * width + x) * 4;
                    pixelData[pixelIndex] = color.B;
                    pixelData[pixelIndex + 1] = color.G;
                    pixelData[pixelIndex + 2] = color.R;
                    pixelData[pixelIndex + 3] = color.A;
                    done++;
                }
            });

            File.Create(path + "\\bg.png").Close();
            StorageFile img = await StorageFile.GetFileFromPathAsync(path + "\\bg.png");
            using(IRandomAccessStream stream = await img.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)map.Width, (uint)map.Height, 96, 96, pixelData);
                await encoder.FlushAsync();
            }
        }

        private async Task ExportObject(string path, Map map)
        {
            string objpath = path + "\\map.json";
            
            StringBuilder sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append("  \"width\": " + map.Width + ",\n");
            sb.Append("  \"height\": " + map.Height + ",\n");
            sb.Append("  \"heightmap\": [\n");
            for(int y = 0; y < map.Height; y++)
            {
                sb.Append("    [");
                for(int x = 0; x < map.Width; x++)
                {
                    sb.Append(map[x, y]);
                    if(x < map.Width - 1)
                    {
                        sb.Append(", ");
                    }
                }
                sb.Append(']');
                if(y < map.Height - 1)
                {
                    sb.Append(',');
                }
                sb.Append('\n');
            }

            sb.Append("  ]\n");
            sb.Append("}\n");

            await File.WriteAllTextAsync(objpath, sb.ToString());
        }
    }
}
