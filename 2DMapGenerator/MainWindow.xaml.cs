using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using static _2DMapGenerator.Human;

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
        Random random = new Random();
        bool held = false;
        bool hovered = false;
        double heldx = 0;
        double heldy = 0;
        double horzoff = 0;
        double vertoff = 0;

        private List<Food> foodItems = new List<Food>();
        private List<Human> humans = new List<Human>();
        private List<Tribe> tribes = new List<Tribe>();
        private DispatcherTimer humanUpdateTimer;

        ColorPalette selected = new SimpleTerrainPalette();

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
            this.ColorBox.SelectedIndex = 2;

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
            if (map == null)
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
            if (SeedBox.Text.Length == 0)
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

            if (ZoomInfo == null)
                return;
            ZoomInfo.Text = $"Zoom is {Math.Round(e.NewValue, 3)}x";
        }

        private void ZoomModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ZoomSliderSub == null || ZoomSliderSup == null)
                return;

            if ((string)(ZoomModeBox.SelectedItem as ComboBoxItem).Tag == "Out")
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

            if (ZoomSliderSub.Visibility == Visibility.Visible)
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
            if (int.TryParse(RoughnessBox.Text, out int roughness))
            {
                engine.Roughness = roughness;
            }
        }

        private void HeightBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(HeightBox.Text, out int height))
            {
                engine.Height = height;
            }
        }

        private void WidthBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(WidthBox.Text, out int width))
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

            if (folder == null)
                return;

            OutputPathBox.Text = folder.Path;

        }

        private async void ExportStart_Click(object sender, RoutedEventArgs e)
        {
            bool ready = true;
            if (ExportImageCheck.IsChecked == false && ExportObjectCheck.IsChecked == false)
                ready = false;
            if (!Directory.Exists(OutputPathBox.Text))
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
            if (Directory.Exists(path))
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
                if (human == null)
                    continue;

                if (!human.IsAlive())
                {
                    deadHumans.Add(human);
                    continue;
                }

                // Random decision: sometimes farm, sometimes move
                if (random.NextDouble() < 0.1)  // 10% chance to farm
                {
                    human.Farm(engine.GeneratedMap);
                }
                else
                {
                    human.Move(engine.GeneratedMap, foodItems, humans);
                }

                // Check if human eats food (as before)
                foreach (var food in foodItems.ToList())
                {
                    if (Math.Abs(human.Position.x - food.Position.x) <= 1 &&
                        Math.Abs(human.Position.y - food.Position.y) <= 1)
                    {
                        human.Eat();
                        consumedFood.Add(food);
                        break;
                    }
                }

                // Check for reproduction, theft, etc. as before…
                // For example, if a human is low on money, they might attempt theft:
                if (human.Money < 20 && random.NextDouble() < 0.05)
                {
                    // Find a nearby target for theft
                    Human target = humans.FirstOrDefault(h => h != human &&
                                Math.Abs(h.Position.x - human.Position.x) < 2 &&
                                Math.Abs(h.Position.y - human.Position.y) < 2 &&
                                h.Money > 0);
                    if (target != null)
                    {
                        human.Steal(target);
                    }
                }

                // Reproduction logic (unchanged)
                if (!consumedFood.Any(food =>
                    Math.Abs(human.Position.x - food.Position.x) <= 1 &&
                    Math.Abs(human.Position.y - food.Position.y) <= 1))
                {
                    foreach (var otherHuman in humans)
                    {
                        if (!human.CanReproduce(otherHuman)) continue;

                        if ((int)human.Position.x == (int)otherHuman.Position.x &&
                            (int)human.Position.y == (int)otherHuman.Position.y)
                        {
                            int x = (int)Math.Clamp(human.Position.x, 0, engine.GeneratedMap.Width - 1);
                            int y = (int)Math.Clamp(human.Position.y, 0, engine.GeneratedMap.Height - 1);
                            if (engine.GeneratedMap[x, y] >= 0.3f && engine.GeneratedMap[x, y] <= 0.7f)
                            {
                                newHumans.Add(human.Reproduce(otherHuman, engine.GeneratedMap));
                                break;
                            }
                        }
                    }
                }
            }

            humans = humans.Except(deadHumans).ToList();
            foodItems = foodItems.Except(consumedFood).ToList();
            if (newHumans.Count + humans.Count < 500)
                humans.AddRange(newHumans);

            RenderHumansAndFood();
            DisplayTraitStatistics();
        }

        private void DisplayTraitStatistics()
        {
            float avgSpeed = humans.Count > 0 ? humans.Where(h => h != null).Average(h => h.Speed) : 0;
            float avgLifespan = humans.Count > 0 ? humans.Where(h => h != null).Average(h => h.Lifespan) : 0;
            float avgEnergyEfficiency = humans.Count > 0 ? humans.Where(h => h != null).Average(h => h.EnergyEfficiency) : 0;

            string traitStats = $"Avg Speed: {avgSpeed:F2}, Avg Lifespan: {avgLifespan:F2}, Avg Efficiency: {avgEnergyEfficiency:F2}";

            //string societyReport = SocietyReporter.GenerateReport(tribes, humans);
            InfoBlock.Text = traitStats;// + "\n" + societyReport;
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
                    if (x >= 0 && x < engine.GeneratedMap.Width && y >= 0 && y < engine.GeneratedMap.Height)
                    {
                        Color color = selected.GetColor(engine.GeneratedMap[x, y]);
                        int pixelIndex = (y * width + x) * 4;

                        pixelData[pixelIndex] = color.B;     // Blue
                        pixelData[pixelIndex + 1] = color.G; // Green
                        pixelData[pixelIndex + 2] = color.R; // Red
                        pixelData[pixelIndex + 3] = color.A; // Alpha
                    }
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

            // Overlay humans with tribe-specific colors
            // Overlay humans with tribe-specific colors
            foreach (var human in humans)
            {
                if (human == null || !human.IsAlive())
                    continue;

                int x = (int)human.Position.x;
                int y = (int)human.Position.y;

                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    int pixelIndex = (y * width + x) * 4;

                    // If the human’s tribe is null, use a default color.
                    Color tribeColor = (human.Tribe != null)
                        ? human.Tribe.TribeColor
                        : Color.FromArgb(255, 128, 128, 128);

                    // Adjust color based on gender
                    if (human.HumanGender == Human.Gender.Male)
                    {
                        pixelData[pixelIndex] = (byte)(tribeColor.B * 0.7);
                        pixelData[pixelIndex + 1] = (byte)(tribeColor.G * 0.7);
                        pixelData[pixelIndex + 2] = (byte)(tribeColor.R * 0.7);
                    }
                    else
                    {
                        pixelData[pixelIndex] = (byte)(tribeColor.B * 1.3);
                        pixelData[pixelIndex + 1] = (byte)(tribeColor.G * 1.3);
                        pixelData[pixelIndex + 2] = (byte)(tribeColor.R * 1.3);
                    }
                    pixelData[pixelIndex + 3] = 255;
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

        private void CreateTribes(int numberOfTribes)
        {
            Random random = new Random();

            for (int i = 0; i < numberOfTribes; i++)
            {
                // Select a random leader from existing humans
                Human leader = humans[random.Next(humans.Count)];

                // Create a new tribe and assign the leader
                Tribe tribe = new Tribe($"Tribe_{i + 1}", leader);
                tribes.Add(tribe);

                // Assign leader's tribe
                leader.Tribe = tribe;
            }

            // Distribute humans to tribes
            foreach (var human in humans)
            {
                if (human.Tribe == null) // Assign only unassigned humans
                {
                    Tribe randomTribe = tribes[random.Next(tribes.Count)];
                    randomTribe.AddMember(human);
                    human.Tribe = randomTribe;
                }
            }

            // Assign colors to tribes
            Tribe.AssignTribeColors(tribes);
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
            GenerateFood(engine.GeneratedMap, 500); // Add 500 food items

            Random random = new Random();
            int humanCount = 50; // Number of humans to simulate
            int attempts = 0;

            for (int i = 0; i < humanCount; i++)
            {
                int x, y;
                do
                {
                    x = random.Next(0, engine.GeneratedMap.Width);
                    y = random.Next(0, engine.GeneratedMap.Height);
                    attempts++;

                    // Safety limit to prevent infinite loop in case of insufficient land
                    if (attempts > 1000)
                    {
                        InfoBlock.Text = "Not enough land to place all humans!";
                        return;
                    }
                }
                while (engine.GeneratedMap[x, y] < 0.3f || engine.GeneratedMap[x, y] > 0.7f);

                // Create a new human at the valid land position
                Gender randomGender = (random.NextDouble() < 0.5) ? Gender.Male : Gender.Female;
                humans.Add(new Human(new Vector2(x, y), randomGender, tribes));
            }
            CreateTribes(2);

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
                int attempts = 0;
                do
                {
                    x = random.Next(0, map.Width);
                    y = random.Next(0, map.Height);
                    attempts++;
                    if (attempts > 1000) break; // Safety limit
                } while (map[x, y] < 0.3f || map[x, y] > 0.7f);

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
