using System.Globalization;
using UnityEngine;

namespace EDIVE.NativeUtils
{
    public static class RichText
    {
        public enum Unit
        {
            Pixels,  // "px"  - absolute, independent of font size
            Em,      // "em"  - font units, scales with the current font size
            Percent  // "%"   - relative to the text container (or font line-height for LineHeight)
        }
    
        private static string Format(float value, Unit unit)
        {
            var n = value.ToString(CultureInfo.InvariantCulture);
            return unit switch
            {
                Unit.Pixels => n + "px",
                Unit.Em => n + "em",
                Unit.Percent => n + "%",
                _ => n
            };
        }

        public static string Size(this string text, int size = 25) => $"<size={size}>{text}</size>";
        public static string Color(this string text, string color) => $"<color={color}>{text}</color>";
        public static string Color(this string text, Color color) => Color(text, $"#{ColorUtility.ToHtmlStringRGBA(color)}");
        public static string Bold(this string text) => $"<b>{text}</b>";
        public static string Italic(this string text) => $"<i>{text}</i>";
        public static string Underline(this string text) => $"<u>{text}</u>";
        public static string StrikeThrough(this string text) => $"<s>{text}</s>";
        public static string Superscript(this string text) => $"<sup>{text}</sup>";
        public static string Subscript(this string text) => $"<sub>{text}</sub>";
        public static string NoParse(this string text) => $"<noparse>{text}</noparse>";
    
        public static string Lowercase(this string text) => $"<lowercase>{text}</lowercase>";
        public static string Uppercase(this string text) => $"<uppercase>{text}</uppercase>";
        public static string Smallcaps(this string text) => $"<smallcaps>{text}</smallcaps>";
    
        public static string Sprite(string spriteName) => $"<sprite name=\"{spriteName}\">";
        public static string Sprite(string spriteName, Color color) => $"<sprite name=\"{spriteName}\" color=#{ColorUtility.ToHtmlStringRGBA(color)}>";

        public static string Pos(this string text, float value, Unit unit = Unit.Pixels) => $"<pos={Format(value, unit)}>{text}";
        public static string Indent(this string text, float value, Unit unit = Unit.Pixels) => $"<indent={Format(value, unit)}>{text}</indent>";
        public static string Space(float value, Unit unit = Unit.Pixels) => $"<space={Format(value, unit)}>";
        public static string LineHeight(float value, Unit unit = Unit.Pixels) => $"<line-height={Format(value, unit)}>";
        public static string VOffset(this string text, float value, Unit unit = Unit.Pixels) => $"<voffset={Format(value, unit)}>{text}</voffset>";
        public static string CSpace(this string text, float value, Unit unit = Unit.Pixels) => $"<cspace={Format(value, unit)}>{text}</cspace>";
        public static string MSpace(this string text, float value, Unit unit = Unit.Pixels) => $"<mspace={Format(value, unit)}>{text}</mspace>";
        public static string Width(this string text, float value, Unit unit = Unit.Pixels) => $"<width={Format(value, unit)}>{text}</width>";
        public static string Margin(this string text, float value, Unit unit = Unit.Pixels) => $"<margin={Format(value, unit)}>{text}</margin>";
        public static string Align(this string text, string alignment) => $"<align={alignment}>{text}</align>";
        public static string NoBreak(this string text) => $"<nobr>{text}</nobr>";
        public static string Rotate(this string text, float degrees) => $"<rotate={degrees.ToString(CultureInfo.InvariantCulture)}>{text}</rotate>";
        public static string Alpha(this string text, string hexByte) => $"<alpha=#{hexByte}>{text}<alpha=#FF>";
    
        public static string Mark(this string text, string color) => $"<mark=#{color}>{text}</mark>";
        public static string Mark(this string text, Color color) => Mark(text, ColorUtility.ToHtmlStringRGBA(color));
    
        public static string Font(this string text, string fontAsset) => $"<font=\"{fontAsset}\">{text}</font>";
        public static string Style(this string text, string styleName) => $"<style=\"{styleName}\">{text}</style>";
        public static string Gradient(this string text, string gradientName) => $"<gradient=\"{gradientName}\">{text}</gradient>";
        public static string Link(this string text, string id) => $"<link=\"{id}\">{text}</link>";
    
        public static string Aqua(this string text) => Color(text, "#00ffffff");
        public static string Black(this string text) => Color(text, "#000000ff");
        public static string Blue(this string text) => Color(text, "#0000ffff");
        public static string Brown(this string text) => Color(text, "#a52a2aff");
        public static string Cyan(this string text) => Color(text, "#00ffffff");
        public static string DarkBlue(this string text) => Color(text, "#0000a0ff");
        public static string Fuchsia(this string text) => Color(text, "#ff00ffff");
        public static string Green(this string text) => Color(text, "#008000ff");
        public static string Grey(this string text) => Color(text, "#808080ff");
        public static string LightBlue(this string text) => Color(text, "#add8e6ff");
        public static string Lime(this string text) => Color(text, "#00ff00ff");
        public static string Magenta(this string text) => Color(text, "#ff00ffff");
        public static string Maroon(this string text) => Color(text, "#800000ff");
        public static string Navy(this string text) => Color(text, "#000080ff");
        public static string Olive(this string text) => Color(text, "#808000ff");
        public static string Orange(this string text) => Color(text, "#ffa500ff");
        public static string Purple(this string text) => Color(text, "#800080ff");
        public static string Red(this string text) => Color(text, "#ff0000ff");
        public static string Silver(this string text) => Color(text, "#c0c0c0ff");
        public static string Teal(this string text) => Color(text, "#008080ff");
        public static string White(this string text) => Color(text, "#ffffffff");
        public static string Yellow(this string text) => Color(text, "#ffff00ff");
    }
}

