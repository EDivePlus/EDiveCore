#ifndef EDIVE_PRISTINE_GRID_INCLUDED
#define EDIVE_PRISTINE_GRID_INCLUDED

// Ben Golus pristine grid. lineWidth is part of a cell.
float PristineGrid(float2 uv, float2 lineWidth)
{
    lineWidth = saturate(lineWidth);
    float4 uvDDXY = float4(ddx(uv), ddy(uv));
    float2 uvDeriv = float2(length(uvDDXY.xz), length(uvDDXY.yw));
    bool2 invertLine = lineWidth > 0.5;
    float2 targetWidth = invertLine ? 1.0 - lineWidth : lineWidth;
    float2 drawWidth = clamp(targetWidth, uvDeriv, 0.5);
    float2 lineAA = max(uvDeriv, 0.000001) * 1.5;
    float2 gridUV = abs(frac(uv) * 2.0 - 1.0);
    gridUV = invertLine ? gridUV : 1.0 - gridUV;
    float2 grid2 = smoothstep(drawWidth + lineAA, drawWidth - lineAA, gridUV);
    grid2 *= saturate(targetWidth / drawWidth);
    grid2 = lerp(grid2, targetWidth, saturate(uvDeriv * 2.0 - 1.0));
    grid2 = invertLine ? 1.0 - grid2 : grid2;
    return lerp(grid2.x, 1.0, grid2.y);
}

#endif // EDIVE_PRISTINE_GRID_INCLUDED
