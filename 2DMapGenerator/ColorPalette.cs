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
            return number * 0.5f + 0.5f;
        }
        public abstract Color GetColor(float number);
        protected Color InterpolateColor(Color startColor, Color endColor, float t)
        {
            int r = (int)(startColor.R + (endColor.R - startColor.R) * t);
            int g = (int)(startColor.G + (endColor.G - startColor.G) * t);
            int b = (int)(startColor.B + (endColor.B - startColor.B) * t);

            return Color.FromArgb(255, (byte)r, (byte)g, (byte)b);
        }
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
            /*
            //the values interval should be [-1,1]
            if (number < -1 || number > 1)
                throw new ArgumentOutOfRangeException($"{number} is out of range");
            */

            // Highest mountains
            if (number >= 1)
                return Color.FromArgb(255, 255, 223, 0);

            // Deep ocean
            if (number <= -1)
                return Color.FromArgb(255, 0, 0, 55);

            if (number <= 0.3f)
            {
                // Deep ocean to shallow ocean colors
                float t = (number + 1) / 1.3f;
                return InterpolateColor(Color.FromArgb(255, 0, 0, 64), Color.FromArgb(255, 0, 191, 255), t);
            }
            else if (number <= 0.7f)
            {
                // Low ground to mid ground colors
                float t = (number - 0.3f) / 0.4f;
                return InterpolateColor(Color.FromArgb(255, 124, 252, 0), Color.FromArgb(255, 34, 139, 34), t);
            }
            else
            {
                // High ground to highest mountains colors
                float t = (number - 0.7f) / 0.3f;
                return InterpolateColor(Color.FromArgb(255, 34, 139, 34), Color.FromArgb(255, 255, 223, 0), t);
            }
        }
    }


    public class AntiquePalette : ColorPalette
    {
        public override Color GetColor(float number)
        {
            number = Scale(number);
            if (number < 0.65)
            {
                return Color.FromArgb(255, 0, 0, 255);
            }
            else
            {
                return Color.FromArgb(255, 0, 255, 0);
            }
        }
    }

    public class TemperaturePalette : ColorPalette
    {
        public override Color GetColor(float number)
        {
            /*
            if (number < -1 || number > 1)
                throw new ArgumentOutOfRangeException($"{number} is out of range");
            */

            // Lowest temperatures (coldest)
            if (number <= -1)
                return Color.FromArgb(255, 97, 128, 255);

            if (number >= 1)
                return Color.FromArgb(255, 97, 128, 255); // blue for coldest temperatures


            if (number <= 0.3f)
            {
                // Deep ocean to shallow ocean (cool to warm)
                float t = (number + 1) / 1.3f;
                return InterpolateColor(Color.FromArgb(255, 97, 128, 255), Color.FromArgb(255, 255, 255, 0), t);
            }
            else if (number <= 0.5f)
            {
                // Low ground to mid ground (warm to hot)
                float t = (number - 0.3f) / 0.4f;
                return InterpolateColor(Color.FromArgb(255, 255, 255, 0), Color.FromArgb(255, 255, 0, 0), t);
            }
            else if (number <= 0.7f)
            {
                // Low ground to mid ground (hot to warm)
                float t = (number - 0.3f) / 0.4f;
                return InterpolateColor(Color.FromArgb(255, 255, 0, 0), Color.FromArgb(255, 255, 255, 0), t);
            }
            else
            {
                // High ground to highest mountains (warm to cool)
                float t = (number - 0.7f) / 0.3f;
                return InterpolateColor(Color.FromArgb(255, 255, 255, 0), Color.FromArgb(255, 97, 128, 255), t);
            }

        }


    }
}
