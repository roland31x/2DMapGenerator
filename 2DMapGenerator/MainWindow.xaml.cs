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

        private List<Food> foodItems = new List<Food>();
        private List<Human> humans = new List<Human>();
        private DispatcherTimer humanUpdateTimer;

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

            humanUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100) // Update every 100ms
            };
            humanUpdateTimer.Tick += UpdateHumans;

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

        private void UpdateHumans(object sender, object e)
        {
            if (engine.GeneratedMap == null || humans.Count == 0)
                return;

            List<Human> newHumans = new List<Human>();
            List<Human> deadHumans = new List<Human>();
            List<Food> consumedFood = new List<Food>();

            foreach (var human in humans)
            {
                human.Move(engine.GeneratedMap);

                // Check if human eats food
                foreach (var food in foodItems)
                {
                    if ((int)human.Position.x == (int)food.Position.x &&
                        (int)human.Position.y == (int)food.Position.y)
                    {
                        human.Eat();
                        consumedFood.Add(food);
                        break;
                    }
                }

                // Check if human is alive
                if (!human.IsAlive())
                {
                    deadHumans.Add(human);
                    continue;
                }

                // Check for reproduction
                foreach (var otherHuman in humans)
                {
                    if (human != otherHuman &&
                        (int)human.Position.x == (int)otherHuman.Position.x &&
                        (int)human.Position.y == (int)otherHuman.Position.y)
                    {
                        // Reproduce only on land
                        int x = (int)Math.Clamp(human.Position.x, 0, engine.GeneratedMap.Width - 1);
                        int y = (int)Math.Clamp(human.Position.y, 0, engine.GeneratedMap.Height - 1);
                        if (engine.GeneratedMap[x, y] >= 0.3f && engine.GeneratedMap[x, y] <= 0.7f)
                        {
                            newHumans.Add(new Human(human.Position));
                        }
                    }
                }
            }

            // Remove dead humans
            foreach (var dead in deadHumans)
            {
                humans.Remove(dead);
            }

            // Remove consumed food
            foreach (var food in consumedFood)
            {
                foodItems.Remove(food);
            }

            // Add new humans
            humans.AddRange(newHumans);

            RenderHumansAndFood();
        }

        private async void RenderHumansAndFood()
        {
            if (engine.GeneratedMap == null)
                return;

            int width = engine.GeneratedMap.Width;
            int height = engine.GeneratedMap.Height;
            byte[] pixelData = new byte[width * height * 4];

            // Render the terrain first
            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    Color color = selected.GetColor(engine.GeneratedMap[x, y]);
                    int pixelIndex = (y * width + x) * 4;

                    pixelData[pixelIndex] = color.B;     // Blue
                    pixelData[pixelIndex + 1] = color.G; // Green
                    pixelData[pixelIndex + 2] = color.R; // Red
                    pixelData[pixelIndex + 3] = color.A; // Alpha
                }
            });

            // Overlay food (green dots)
            foreach (var food in foodItems)
            {
                int x = (int)food.Position.x;
                int y = (int)food.Position.y;

                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    int pixelIndex = (y * width + x) * 4;
                    pixelData[pixelIndex] = 0;   // Blue
                    pixelData[pixelIndex + 1] = 255; // Green
                    pixelData[pixelIndex + 2] = 0;   // Red
                    pixelData[pixelIndex + 3] = 255; // Alpha
                }
            }

            // Overlay humans (red dots)
            foreach (var human in humans)
            {
                int x = (int)human.Position.x;
                int y = (int)human.Position.y;

                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    int pixelIndex = (y * width + x) * 4;
                    pixelData[pixelIndex] = 255;     // Blue
                    pixelData[pixelIndex + 1] = 0;   // Green
                    pixelData[pixelIndex + 2] = 0;   // Red
                    pixelData[pixelIndex + 3] = 255; // Alpha
                }
            }

            // Create and display the combined image
            WriteableBitmap bitmap = new WriteableBitmap(width, height);
            using (Stream stream = bitmap.PixelBuffer.AsStream())
            {
                await stream.WriteAsync(pixelData, 0, pixelData.Length);
            }

            MapImg.Source = bitmap;
        }


        private void HumanSimulationButton_Click(object sender, RoutedEventArgs e)
        {
            if (engine.GeneratedMap == null)
            {
                InfoBlock.Text = "No map generated! Generate a map first.";
                return;
            }

            // Generate humans and food
            humans.Clear();
            GenerateFood(engine.GeneratedMap, 50); // Add 50 food items

            Random random = new Random();
            int humanCount = 50; // Number of humans to simulate
            for (int i = 0; i < humanCount; i++)
            {
                humans.Add(new Human(
                    new Vector2(
                        random.Next(0, engine.GeneratedMap.Width),
                        random.Next(0, engine.GeneratedMap.Height)
                    )
                ));
            }

            // Start the human simulation timer
            humanUpdateTimer.Start();
            InfoBlock.Text = "Human simulation started.";
            RenderHumansAndFood(); // Initial render
        }


        private void GenerateFood(Map map, int count)
        {
            Random random = new Random();
            foodItems.Clear();

            for (int i = 0; i < count; i++)
            {
                int x, y;
                do
                {
                    x = random.Next(0, map.Width);
                    y = random.Next(0, map.Height);
                }
                while (map[x, y] < 0.3f || map[x, y] > 0.7f); // Only place food on plains

                foodItems.Add(new Food(new Vector2(x, y)));
            }
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
