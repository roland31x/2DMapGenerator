using Microsoft.UI.Input;
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
using Windows.Storage;
using Windows.Storage.Pickers;
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
        GenerationEngine engine;
        ExportEngine exporter;
        bool held = false;
        bool hovered = false;
        double heldx = 0;
        double heldy = 0;
        double horzoff = 0;
        double vertoff = 0;

        ColorPalette selected = new GrayscalePalette();

        public MainWindow()
        {
            this.InitializeComponent();
            this.engine = GenerationEngine.Singleton;
            this.exporter = ExportEngine.Singleton;
            //this.engine.InfoEvent += OnInfoEvent;
            this.engine.GenerationStarted += OnGenerationStarted;
            this.engine.GenerationFinished += OnGenerationFinished;
            (this.ColorBox.Items[0] as ComboBoxItem).Tag = new GrayscalePalette();
            (this.ColorBox.Items[1] as ComboBoxItem).Tag = new SimpleTerrainPalette();
            (this.ColorBox.Items[2] as ComboBoxItem).Tag = new HeightMapPalette();
            (this.ColorBox.Items[3] as ComboBoxItem).Tag = new TemperaturePalette();
            this.ColorBox.SelectedIndex = 0;

            Binding binding = new Binding();
            binding.Source = engine;
            binding.Path = new PropertyPath("Status");
            binding.Mode = BindingMode.TwoWay;
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            InfoBlock.SetBinding(TextBox.TextProperty, binding);
        }

        private async void OnGenerationFinished(object sender, EventArgs e)
        {
            GenerateButton.Visibility = Visibility.Visible;
            StopButton.Visibility = Visibility.Collapsed;
            SeedBox.IsEnabled = true;
            HeightBox.IsEnabled = true;
            WidthBox.IsEnabled = true;
            RoughnessBox.IsEnabled = true;
            SeedBox.Text = engine.Seed.ToString();

            if (engine.ValidGeneration)
            {
                ExportButton.Visibility = Visibility.Visible;
                await RenderMap(engine.GeneratedMap);
                CalcZoom();
            }  
            else
                ExportButton.Visibility = Visibility.Collapsed;

            
            
        }
        private void CalcZoom()
        {
            float minzoom = 600f / Math.Min(engine.GeneratedMap.Width, engine.GeneratedMap.Height);

            if (minzoom > 1)
            {
                ZoomModeBox.SelectedIndex = 1;
                ZoomSliderSup.Minimum = minzoom;
                ZoomSliderSup.Value = minzoom;
            }
            else
            {
                ZoomModeBox.SelectedIndex = ZoomModeBox.SelectedIndex;
                ZoomSliderSub.Minimum = minzoom;
                ZoomSliderSup.Minimum = 1;
            }
        }

        private async Task RenderMap(Map map)
        {
            if(map == null)
                return;

            int height = map.Height;
            int width = map.Width;
            byte[] pixelData = new byte[width * height * 4];

            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    Color color = selected.GetColor(map[x, y]);
                    int pixelIndex = (y * width + x) * 4;
                    pixelData[pixelIndex] = color.B;
                    pixelData[pixelIndex + 1] = color.G;
                    pixelData[pixelIndex + 2] = color.R;
                    pixelData[pixelIndex + 3] = color.A;
                }
            });

            WriteableBitmap bitmap = new WriteableBitmap(width, height);
            using (Stream stream = bitmap.PixelBuffer.AsStream())
            {
                await stream.WriteAsync(pixelData, 0, pixelData.Length);
            }
            MapImg.Source = bitmap;
            MapImg.Height = height;
            MapImg.Width = width;        

        }

        private void OnGenerationStarted(object sender, EventArgs e)
        {
            GenerateButton.Visibility = Visibility.Collapsed;
            StopButton.Visibility = Visibility.Visible;
            SeedBox.IsEnabled = false;
            HeightBox.IsEnabled = false;
            WidthBox.IsEnabled = false;
            RoughnessBox.IsEnabled = false;
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
            if(SeedBox.Text.Length == 0)
            {
                engine.Seed = null;
                return;
            }
            if (int.TryParse(SeedBox.Text, out int seed))
            {
                if (seed == engine.Seed)
                    return;
                engine.Seed = seed;
            }
        }

        private void ZoomSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (MapContainer == null)
                return;

            MapContainer.ZoomToFactor((float)e.NewValue);

            if(ZoomInfo == null)
                return;
            ZoomInfo.Text = $"Zoom is {Math.Round(e.NewValue,3)}x";
        }

        private void ZoomModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(ZoomSliderSub == null || ZoomSliderSup == null)
                return;

            if((string)(ZoomModeBox.SelectedItem as ComboBoxItem).Tag == "Out")
            {
                if (ZoomSliderSup.Minimum > 1)
                {
                    ZoomModeBox.SelectedIndex = 1;
                    return;
                }
                ZoomSliderSub.Visibility = Visibility.Visible;
                ZoomSliderSup.Visibility = Visibility.Collapsed;
            }
            else
            {
                ZoomSliderSub.Visibility = Visibility.Collapsed;
                ZoomSliderSup.Visibility = Visibility.Visible;
            }

            if(ZoomSliderSub.Visibility == Visibility.Visible)
                ZoomSliderSub.Value = ZoomSliderSub.Value;
            else
                ZoomSliderSup.Value = ZoomSliderSup.Value;
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


        private async void ColorBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.selected = (ColorPalette)(ColorBox.SelectedItem as ComboBoxItem).Tag;
            await RenderMap(engine.GeneratedMap);
        }

        private void RoughnessBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(int.TryParse(RoughnessBox.Text, out int roughness))
            {
                engine.Roughness = roughness;
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
            MainGrid.InputCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeAll);
        }

        private void MapContainer_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            held = false;
            if (hovered)
                MainGrid.InputCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
            else
                MainGrid.InputCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);

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

        private void MapContainer_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            hovered = true;
            MainGrid.InputCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        }

        private void MapContainer_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            hovered = false;
            MainGrid.InputCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            Overlay.Visibility = Visibility.Visible;
            ExportPanel.Visibility = Visibility.Visible;
        }

        private async void PathSelector_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop; 
            picker.FileTypeFilter.Add("*");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            
            StorageFolder folder = await picker.PickSingleFolderAsync();

            if(folder == null)
                return;

            OutputPathBox.Text = folder.Path;

        }

        private async void ExportStart_Click(object sender, RoutedEventArgs e)
        {
            bool ready = true;
            if(ExportImageCheck.IsChecked == false && ExportObjectCheck.IsChecked == false)
                ready = false;
            if(!Directory.Exists(OutputPathBox.Text))
                ready = false;

            if (!ready)
            {
                Flyout flyout = new Flyout();
                flyout.Content = new TextBlock() { Text = "Please select a valid output path and at least one export option" };
                flyout.ShowAt(MainGrid);
                return;
            }

            ExportStart.IsEnabled = false;
            ExportClose.IsEnabled = false;
            ExportImageCheck.IsEnabled = false;
            ExportObjectCheck.IsEnabled = false;
            PathSelector.IsEnabled = false;
            ExportStart.Content = "Exporting...";

            string path = OutputPathBox.Text;
            path += "\\Seed" + engine.Seed;

            int number = 0;
            if(Directory.Exists(path))
            {
                while (Directory.Exists(path + "_" + number))
                    number++;
                path += "_" + number;
            }

            Directory.CreateDirectory(path);
            try
            {
                bool expImg = ExportImageCheck.IsChecked == true ? true : false;
                bool expObj = ExportObjectCheck.IsChecked == true ? true : false;
                Map toExp = engine.GeneratedMap;
                ColorPalette colors = this.selected;

                await Task.Run(() => exporter.Export(expImg, expObj, path, toExp, colors));

                Flyout success = new Flyout();
                success.Content = new TextBlock() { Text = "Exported successfully to: " + path };
                success.ShowAt(MainGrid);
            }
            catch (Exception ex)
            {
                Flyout flyout = new Flyout();
                flyout.Content = new TextBlock() { Text = "Error exporting: " + ex.Message };
                flyout.ShowAt(MainGrid);
            }   

            ExportStart.IsEnabled = true;
            ExportClose.IsEnabled = true;
            ExportImageCheck.IsEnabled = true;
            ExportObjectCheck.IsEnabled = true;
            PathSelector.IsEnabled = true;
            ExportStart.Content = "Export";
            Overlay.Visibility = Visibility.Collapsed;
            ExportPanel.Visibility = Visibility.Collapsed;   

        }

        private void ExportClose_Click(object sender, RoutedEventArgs e)
        {
            Overlay.Visibility = Visibility.Collapsed;
            ExportPanel.Visibility = Visibility.Collapsed;
        }
    }

    public class CustomGrid : Grid
    {
        public InputCursor InputCursor
        {
            get => ProtectedCursor;
            set => ProtectedCursor = value;
        }
    }
}
