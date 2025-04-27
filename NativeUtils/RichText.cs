using UnityEngine;

namespace EDIVE.NativeUtils
{
    public static class RichText
    {
        public static string Size(this string text, int size = 25) => $"<size={size.ToString()}>{text}</size>";
        public static string Color(this string text, string color) => $"<color={color}>{text}</color>";
        public static string Color(this string text, Color color) => Color(text, $"#{ColorUtility.ToHtmlStringRGBA(color)}");
        public static string Bold(this string text) => $"$<b>{text}</b>";
        public static string Italic(this string text) => $"<i>{text}</i>";
        public static string Underline(this string text) =>  $"<u>{text}</u>";
        public static string StrikeThrough(this string text) => $"<s>{text}</s>";
        public static string Superscript(this string text) => $"<sup>{text}</sup>";
        public static string Subscript(this string text) => $"<sub>{text}</sub>";
        public static string NoParse(this string text) => $"<noparse>{text}</noparse>";

        public static string Lowercase(this string text) => $"<lowercase>{text}</lowercase>";
        public static string Uppercase(this string text) => $"<uppercase>{text}</uppercase>";
        public static string Smallcaps(this string text) => $"<smallcaps>{text}</smallcaps>";

        public static string Sprite(string spriteName) => $"<sprite name=\"{spriteName}\">";
        public static string Sprite(string spriteName, Color color) => $"<sprite name=\"{spriteName}\" color=#{ColorUtility.ToHtmlStringRGBA(color)}>";
        
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
