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
            //highest mountains
            if (number >= 1.5)
                return Color.FromArgb(255, 255, 223, 0);
            else
                //deep ocean
                if (number <= -1)
                    return Color.FromArgb(255, 0, 0, 55);

            if (number <= 0.3f)
            {
                // Water colors
                float t = (number + 1) / 1.3f;
                return InterpolateColor(Color.FromArgb(255, 0, 0, 64), Color.FromArgb(255, 0, 191, 255), t);
            }
            else if (number <= 0.7f)
            {
                // Land colors low
                float t = (number - 0.3f) / 0.4f;
                return InterpolateColor(Color.FromArgb(255, 124, 252, 0), Color.FromArgb(255, 34, 139, 34), t);
            }
            else if (number <= 1f)
            {
                // Mountain colors high
                float t = (number - 0.7f) / 0.3f;
                return InterpolateColor(Color.FromArgb(255, 34, 139, 34), Color.FromArgb(255, 255, 223, 0), t);
            }
            else
            {
                // Mountain colors high
                float t = (number - 0.5f) / 0.2f;
                return InterpolateColor(Color.FromArgb(255, 255, 223, 0), Color.FromArgb(255, 153, 101, 21), t);
            }
        }

        private Color InterpolateColor(Color startColor, Color endColor, float t)
        {
            int r = (int)(startColor.R + (endColor.R - startColor.R) * t);
            int g = (int)(startColor.G + (endColor.G - startColor.G) * t);
            int b = (int)(startColor.B + (endColor.B - startColor.B) * t);
            int a = (int)(startColor.A + (endColor.A - startColor.A) * t);

            return Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
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
}
