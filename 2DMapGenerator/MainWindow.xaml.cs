using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media.Control;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace _2DMapGenerator
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        GenerationEngine engine = GenerationEngine.Singleton;
        bool held = false;
        double heldx = 0;
        double heldy = 0;
        double horzoff = 0;
        double vertoff = 0;
        public MainWindow()
        {
            this.InitializeComponent();
            this.engine.InfoEvent += OnInfoEvent;
            this.engine.GenerationStarted += OnGenerationStarted;
            this.engine.GenerationFinished += OnGenerationFinished;
        }

        private async void OnGenerationFinished(object sender, EventArgs e)
        {
            GenerateButton.Visibility = Visibility.Visible;
            StopButton.Visibility = Visibility.Collapsed;
            SeedBox.IsEnabled = true;
            HeightBox.IsEnabled = true;
            WidthBox.IsEnabled = true;
            await RenderMap(engine.GeneratedMap);
        }

        private async Task RenderMap(Map map)
        {
            int height = map.Height;
            int width = map.Width;
            byte[] pixelData = new byte[width * height * 4];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {

                    Color color = GetColor(map[x,y]);
                    int pixelIndex = (y * width + x) * 4;
                    pixelData[pixelIndex] = color.B;
                    pixelData[pixelIndex + 1] = color.G;
                    pixelData[pixelIndex + 2] = color.R;
                    pixelData[pixelIndex + 3] = color.A;
                }
            }

            WriteableBitmap bitmap = new WriteableBitmap(width, height);
            using (Stream stream = bitmap.PixelBuffer.AsStream())
            {
                await stream.WriteAsync(pixelData, 0, pixelData.Length);
            }
            MapImg.Source = bitmap;
            MapImg.Height = height;
            MapImg.Width = width;
        }

        Color GetColor(float number)
        {
            if(number != 0)
            {
                int x = 0;
            }
            return new Color()
            {
                R = (byte)(number * 255),
                G = (byte)(number * 255),
                B = (byte)(number * 255),
                A = 255,
            };
        }

        private void OnGenerationStarted(object sender, EventArgs e)
        {
            GenerateButton.Visibility = Visibility.Collapsed;
            StopButton.Visibility = Visibility.Visible;
            SeedBox.IsEnabled = false;
            HeightBox.IsEnabled = false;
            WidthBox.IsEnabled = false;
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            engine.StartGenerate();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            engine.ForceStop();
        }

        private void SeedBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(SeedBox.Text, out int seed))
            {
                engine.Seed = seed;
            }
        }   

        private void OnInfoEvent(object sender, InfoEventArgs e)
        {
            try
            {
                InfoBlock.Text = e.Info;
            }
            catch (Exception ex)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    InfoBlock.Text = e.Info;
                });
            }
        }

        private void HeightBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(int.TryParse(HeightBox.Text, out int height))
            {
                engine.Height = height;
            }
        }

        private void WidthBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(int.TryParse(WidthBox.Text, out int width))
            {
                engine.Width = width;
            }
        }

        private void MapContainer_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            held = true;
            heldx = e.GetCurrentPoint(MapContainer).Position.X;
            heldy = e.GetCurrentPoint(MapContainer).Position.Y;
            horzoff = MapContainer.HorizontalOffset;
            vertoff = MapContainer.VerticalOffset;
        }

        private void MapContainer_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            held = false;
        }

        private void MapContainer_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (held)
            {
                double dx = heldx - e.GetCurrentPoint(MapContainer).Position.X;
                double dy = heldy - e.GetCurrentPoint(MapContainer).Position.Y;
                MapContainer.ScrollToHorizontalOffset(horzoff + dx);
                MapContainer.ScrollToVerticalOffset(vertoff + dy);
            }
        }
    }
}
