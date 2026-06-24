using System;
using System.Globalization;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.NativeUtils
{
    public static class RichText
    {
        public static string Size(this string text, int size = 25) => $"<size={size}>{text}</size>";
        public static string Color(this string text, string color) => $"<color={color}>{text}</color>";
        public static string Color(this string text, Color color) => text.Color($"#{ColorUtility.ToHtmlStringRGBA(color)}");
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

        public static string Pos(this string text, float value, RichTextUnit richTextUnit = RichTextUnit.Pixels) => $"<pos={richTextUnit.Format(value)}>{text}";
        public static string Pos(this string text, RichTextUnitField value) => text.Pos(value.Value, value.Unit);
        public static string Indent(this string text, float value, RichTextUnit richTextUnit = RichTextUnit.Pixels) => $"<indent={richTextUnit.Format(value)}>{text}</indent>";
        public static string Indent(this string text, RichTextUnitField value) => text.Indent(value.Value, value.Unit);
        public static string Space(float value, RichTextUnit richTextUnit = RichTextUnit.Pixels) => $"<space={richTextUnit.Format(value)}>";
        public static string Space(RichTextUnitField value) => Space(value.Value, value.Unit);
        public static string LineHeight(float value, RichTextUnit richTextUnit = RichTextUnit.Pixels) => $"<line-height={richTextUnit.Format(value)}>";
        public static string LineHeight(RichTextUnitField value) => LineHeight(value.Value, value.Unit);
        public static string VOffset(this string text, float value, RichTextUnit richTextUnit = RichTextUnit.Pixels) => $"<voffset={richTextUnit.Format(value)}>{text}</voffset>";
        public static string VOffset(this string text, RichTextUnitField value) => text.VOffset(value.Value, value.Unit);
        public static string CSpace(this string text, float value, RichTextUnit richTextUnit = RichTextUnit.Pixels) => $"<cspace={richTextUnit.Format(value)}>{text}</cspace>";
        public static string CSpace(this string text, RichTextUnitField value) => text.CSpace(value.Value, value.Unit);
        public static string MSpace(this string text, float value, RichTextUnit richTextUnit = RichTextUnit.Pixels) => $"<mspace={richTextUnit.Format(value)}>{text}</mspace>";
        public static string MSpace(this string text, RichTextUnitField value) => text.MSpace(value.Value, value.Unit);
        public static string Width(this string text, float value, RichTextUnit richTextUnit = RichTextUnit.Pixels) => $"<width={richTextUnit.Format(value)}>{text}</width>";
        public static string Width(this string text, RichTextUnitField value) => text.Width(value.Value, value.Unit);
        public static string Margin(this string text, float value, RichTextUnit richTextUnit = RichTextUnit.Pixels) => $"<margin={richTextUnit.Format(value)}>{text}</margin>";
        public static string Margin(this string text, RichTextUnitField value) => text.Margin(value.Value, value.Unit);
        public static string Align(this string text, string alignment) => $"<align={alignment}>{text}</align>";
        public static string NoBreak(this string text) => $"<nobr>{text}</nobr>";
        public static string Rotate(this string text, float degrees) => $"<rotate={degrees.ToString(CultureInfo.InvariantCulture)}>{text}</rotate>";
        public static string Alpha(this string text, string hexByte) => $"<alpha=#{hexByte}>{text}<alpha=#FF>";
    
        public static string Mark(this string text, string color) => $"<mark=#{color}>{text}</mark>";
        public static string Mark(this string text, Color color) => text.Mark(ColorUtility.ToHtmlStringRGBA(color));
    
        public static string Font(this string text, string fontAsset) => $"<font=\"{fontAsset}\">{text}</font>";
        public static string Style(this string text, string styleName) => $"<style=\"{styleName}\">{text}</style>";
        public static string Gradient(this string text, string gradientName) => $"<gradient=\"{gradientName}\">{text}</gradient>";
        public static string Link(this string text, string id) => $"<link=\"{id}\">{text}</link>";
    
        public static string Aqua(this string text) => text.Color("#00ffffff");
        public static string Black(this string text) => text.Color("#000000ff");
        public static string Blue(this string text) => text.Color("#0000ffff");
        public static string Brown(this string text) => text.Color("#a52a2aff");
        public static string Cyan(this string text) => text.Color("#00ffffff");
        public static string DarkBlue(this string text) => text.Color("#0000a0ff");
        public static string Fuchsia(this string text) => text.Color("#ff00ffff");
        public static string Green(this string text) => text.Color("#008000ff");
        public static string Grey(this string text) => text.Color("#808080ff");
        public static string LightBlue(this string text) => text.Color("#add8e6ff");
        public static string Lime(this string text) => text.Color("#00ff00ff");
        public static string Magenta(this string text) => text.Color("#ff00ffff");
        public static string Maroon(this string text) => text.Color("#800000ff");
        public static string Navy(this string text) => text.Color("#000080ff");
        public static string Olive(this string text) => text.Color("#808000ff");
        public static string Orange(this string text) => text.Color("#ffa500ff");
        public static string Purple(this string text) => text.Color("#800080ff");
        public static string Red(this string text) => text.Color("#ff0000ff");
        public static string Silver(this string text) => text.Color("#c0c0c0ff");
        public static string Teal(this string text) => text.Color("#008080ff");
        public static string White(this string text) => text.Color("#ffffffff");
        public static string Yellow(this string text) => text.Color("#ffff00ff");
    }
    
    public enum RichTextUnit
    {
        [Tooltip("\"em\" Font units, scales with the current font size")]
        Em,
        [Tooltip("\"px\" Absolute, independent of font size")]
        Pixels,
        [Tooltip("\"%\" Relative to the text container")]
        Percent
    }
    
    public static class RichTextUnitExtensions
    {
        public static string Format(this RichTextUnit richTextUnit, float value)
        {
            var n = value.ToString(CultureInfo.InvariantCulture);
            return richTextUnit switch
            {
                RichTextUnit.Em => n + "em",
                RichTextUnit.Pixels => n + "px",
                RichTextUnit.Percent => n + "%",
                _ => n
            };
        }
    }
    
    [InlineProperty]
    [Serializable]
    public struct RichTextUnitField
    {
        [HorizontalGroup]
        [HideLabel]
        [SerializeField]
        private float _Value;
        
        [HorizontalGroup(80)]
        [HideLabel]
        [SerializeField]
        private RichTextUnit _Unit;
        
        public float Value => _Value;
        public RichTextUnit Unit => _Unit;
        
        public RichTextUnitField(float value, RichTextUnit unit)
        {
            _Value = value;
            _Unit = unit;
        }
    }
}

