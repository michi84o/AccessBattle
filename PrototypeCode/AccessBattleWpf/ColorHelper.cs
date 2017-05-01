using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattleWpf
{
    public static class ColorHelper
    {
        public static void RgbToHsv(byte r, byte g, byte b, out double h, out double s, out double v)
        {            
            int max = r;
            if (g > max) max = g;
            if (b > max) max = b;
            int min = r;
            if (g < min) min = g;
            if (b < min) min = b;            

            if (max - min == 0) h = 0; // White, Grey, Black
            else if (max == r) h = ((1.0 * g - b) / ((1.0 * max - min)));
            else if (max == g) h = 2.0 + ((1.0 * b - r) / (1.0 * max - min));
            else /*(max == b)*/ h = 4.0 + ((1.0 * r - g) / (1.0 * max - min));

            h = h * 60;
            if (h < 0) h = h + 360;

            s = (max == 0) ? 0 : 1.0 - (1.0 * min / max);
            v = max / 255.0;
        }

        public static void HsvToRgb(double h, double s, double v, out byte r, out byte g, out byte b)
        {
            var hi = (int)((Math.Floor(h / 60)) % 6 + .5);
            var f = h / 60 - Math.Floor(h / 60);

            v = v * 255;
            var vi = (byte)(v+.5);
            var p = (byte)(v * (1 - s) + .5);
            var q = (byte)(v * (1 - f * s) + .5);
            var t = (byte)(v * (1 - (1 - f) * s) + .5);

            if (hi == 0) { r = vi; g = t; b = p; }
            else if (hi == 1) { r = q; g = vi; b = p; }
            else if (hi == 2) { r = p; g = vi; b = t; }
            else if (hi == 3) { r = p; g = q; b = vi; }
            else if (hi == 4) { r = t; g = p; b = vi; }
            else { r = vi; g = p; b = q; }
        }
    }
}
