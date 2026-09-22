// Author: Michal Petr
// Created: 22.09.2026

using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;

namespace EDIVE.UIElements.ProceduralUI
{
    public enum FillMode
    {
        [IconLabelText(FontAwesomeEditorIconType.SquareSolid, "Filled")]
        Filled = 0,

        [IconLabelText(FontAwesomeEditorIconType.SquareXmarkRegular, "No Fill")]
        NoFill = 1
    }

    public enum ShapeStyle
    {
        [IconLabelText(FontAwesomeEditorIconType.CircleSolid, "Round")]
        Round = 0,

        [IconLabelText(FontAwesomeEditorIconType.OctagonSolid, "Chamfer")]
        Chamfer = 1
    }

    // How outline, shadow, frame and glow wrap a sharp corner
    public enum CornerJoin
    {
        [IconLabelText(FontAwesomeEditorIconType.CircleRegular, "Round")]
        Round = 0,

        [IconLabelText(FontAwesomeEditorIconType.SquareRegular, "Miter")]
        Miter = 1,

        [IconLabelText(FontAwesomeEditorIconType.DiamondRegular, "Bevel")]
        Bevel = 2
    }

    // Declared in button order; PerCorner stays 0 so it is the default and matches older data
    public enum RoundnessMode
    {
        [IconLabelText(FontAwesomeEditorIconType.CircleSolid, "Circle")]
        Circle = 2,

        [IconLabelText(FontAwesomeEditorIconType.SquareSolid, "Uniform")]
        Uniform = 1,

        [IconLabelText(FontAwesomeEditorIconType.Grid2Solid, "Per Corner")]
        PerCorner = 0
    }

    public enum EdgePlacement
    {
        Inside,
        Center,
        Outside
    }

    public enum GradientType
    {
        None,
        Vertical,
        Horizontal,
        Radial,
        Gradient
    }

    public enum RadialMode
    {
        CircleCover,
        CircleFit,
        Ellipse
    }

    public enum ArcFillOrigin
    {
        Start,
        End,
        Center
    }
}
