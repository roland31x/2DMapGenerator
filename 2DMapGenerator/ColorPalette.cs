using System;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace _2DMapGenerator
{
    public abstract class ColorPalette
    {
        protected float Scale(float number)
        {
            return (number + 1) / 2;
        }
        public abstract Color GetColor(float number);
    }

    public class GrayscalePalette : ColorPalette
    {
        public override Color GetColor(float number)
        {
            number = Scale(number);
            byte val = (byte)(number * 255);
            return Color.FromArgb(255, val, val, val);
        }
    }
    
    public class HeightMapPalette : ColorPalette
    {
        public override Color GetColor(float number)
        {
            number = Scale(number);

            if (number > 0.7)
            {
                // Interpolating between yellow and dark brown
                byte r = 255;
                byte g = (byte)(255 * (number / 0.3));
                byte b = 0;
                return Color.FromArgb(255, r, g, b);
            }
            else if (number > 0.5)
            {
                // Darkening the green towards yellow
                byte r = (byte)(255 * ((0.7 - number) / 0.4));
                byte g = (byte)(100 + 155 * ((0.7 - number) / 0.4)); // Dark green
                byte b = 0;
                return Color.FromArgb(255, r, g, b);
            }
            else
            {
                // Interpolating between pale blue ocean color
                byte r = 173;
                byte g = (byte)(255 - 127.5 * ((number - 0.7) / 0.3));
                byte b = 255;
                return Color.FromArgb(255, r, g, b);
            }
        }
    }

    public class AntiquePalette : ColorPalette
    {
        public override Color GetColor(float number)
        {
            number = Scale(number);
            if (number < 0.6)
            {
                return Color.FromArgb(255, 0, 0, 255);
            }
            else
            {
                return Color.FromArgb(255, 0, 255, 0);
            }
        }
    }
}
