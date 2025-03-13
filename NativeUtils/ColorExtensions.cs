using UnityEngine;

namespace EDIVE.NativeUtils
{
    public static class ColorExtensions
    {
        private const float ONE_OVER_255 = 1.0f / 255.0f;

        public static Color HexStringToColor(string hex)
        {
            hex = hex.Replace("#", "");
            byte a = 255;
          
            var r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            var g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            var b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
         
            if (hex.Length == 8) 
                a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);

            return new Color(r * ONE_OVER_255, g * ONE_OVER_255, b * ONE_OVER_255, a * ONE_OVER_255);
        }
        
        public static Color WithR(this Color c, float r)
        {
            c.r = r;
            return c;
        }

        public static Color WithG(this Color c, float g)
        {
            c.g = g;
            return c;
        }

        public static Color WithB(this Color c, float b)
        {
            c.b = b;
            return c;
        }

        public static Color WithA(this Color c, float a)
        {
            c.a = a;
            return c;
        }

        public static Color WithRGB(this Color c, float r, float g, float b)
        {
            c.r = r;
            c.g = g;
            c.b = b;
            return c;
        }
        
        public static Color MultiplyComponents(this Color c, float r, float g, float b)
        {
            c.r *= r;
            c.g *= g;
            c.b *= b;
            return c;
        }
        
        public static Color MultiplyComponents(this Color c, float r, float g, float b, float a)
        {
            c.r *= r;
            c.g *= g;
            c.b *= b;
            c.a *= a;
            return c;
        }
        
        public static Color MultiplyRGB(this Color c, float scale)
        {
            c.r *= scale;
            c.g *= scale;
            c.b *= scale;
            return c;
        }

        public static Color MultiplyRGBA(this Color c, float scale)
        {
            c.r *= scale;
            c.g *= scale;
            c.b *= scale;
            c.a *= scale;
            return c;
        }
        
        public static Color Clamp01(this Color c)
        {
            c.r = Mathf.Clamp01(c.r);
            c.g = Mathf.Clamp01(c.g);
            c.b = Mathf.Clamp01(c.b);
            c.a = Mathf.Clamp01(c.a);
            return c;
        }
    }
}
