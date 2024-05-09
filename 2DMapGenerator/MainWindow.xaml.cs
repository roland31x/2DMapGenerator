using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Control;

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
        public MainWindow()
        {
            this.InitializeComponent();
            this.engine.InfoEvent += OnInfoEvent;
            this.engine.GenerationStarted += OnGenerationStarted;
            this.engine.GenerationFinished += OnGenerationFinished;
        }

        private void OnGenerationFinished(object sender, EventArgs e)
        {
            GenerateButton.Visibility = Visibility.Visible;
            StopButton.Visibility = Visibility.Collapsed;
            SeedBox.IsEnabled = true;
            HeightBox.IsEnabled = true;
            WidthBox.IsEnabled = true;
            //RenderMap(engine.GeneratedMap);
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
            InfoBlock.Text = e.Info;
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
    }
}
