#ifndef EDIVE_PROCEDURAL_SHAPE_INCLUDED
#define EDIVE_PROCEDURAL_SHAPE_INCLUDED

#define SQRT_HALF 0.70710678
#define SHAPE_PI 3.14159265
#define SHAPE_HALF_PI 1.57079633
#define SHAPE_TWO_PI 6.28318531

// cornerShape = style + join * 2
//   style: 0 = round, 1 = chamfer
//   join:  0 = round, 1 = miter, 2 = bevel (outer offsets only, the shape itself is unchanged)
void DecodeCornerShape(float cornerShape, out float style, out float join)
{
    join = floor(cornerShape / 2.0 + 0.001);
    style = cornerShape - join * 2.0;
}

float SegmentDistance(float2 p, float2 a, float2 b)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = saturate(dot(pa, ba) / dot(ba, ba));
    return length(pa - ba * h);
}

// q = abs(p) - halfSize: the corner sits at the origin and the interior is negative
float RoundCornerDistance(float2 q, float c, float join)
{
    float2 qc = q + c;
    float2 o = max(qc, 0.0);
    float outside;
    if (c > 0.0 || join < 0.5)
        outside = length(o);
    else if (join < 1.5)
        outside = max(o.x, o.y);
    else
        outside = max(max(o.x, o.y), (o.x + o.y) * SQRT_HALF);
    return min(max(qc.x, qc.y), 0.0) + outside - c;
}

float ChamferCornerDistance(float2 q, float c, float join)
{
    if (c <= 0.0)
        return RoundCornerDistance(q, 0.0, join);

    // Edge lines x = 0, y = 0 and the chamfer x + y = -c; their max is exact inside and a miter offset outside
    float lineMax = max(max(q.x, q.y), (q.x + q.y + c) * SQRT_HALF);
    if (lineMax <= 0.0 || (join > 0.5 && join < 1.5))
        return lineMax;

    if (join > 1.5)
    {
        // Bevel: cut through each chamfer vertex along the bisector of its edge normals
        float2 n = normalize(float2(1.0 + SQRT_HALF, SQRT_HALF));
        float cutX = dot(q - float2(0.0, -c), n);
        float cutY = dot(q - float2(-c, 0.0), n.yx);
        return max(lineMax, max(cutX, cutY));
    }

    // Round join: exact distance to the three boundary pieces of this corner
    float2 vertexX = float2(0.0, -c);
    float2 vertexY = float2(-c, 0.0);
    float edgeX = q.y <= -c ? abs(q.x) : length(q - vertexX);
    float edgeY = q.x <= -c ? abs(q.y) : length(q - vertexY);
    return min(min(edgeX, edgeY), SegmentDistance(q, vertexX, vertexY));
}

// corners: x = top right, y = bottom right, z = top left, w = bottom left
float ShapeDistance(float2 p, float2 halfSize, float4 corners, float style, float join)
{
    float2 side = p.x > 0.0 ? corners.xy : corners.zw;
    float c = p.y > 0.0 ? side.x : side.y;
    c = min(c, min(halfSize.x, halfSize.y));
    float2 q = abs(p) - halfSize;
    return style < 0.5 ? RoundCornerDistance(q, c, join) : ChamferCornerDistance(q, c, join);
}

// Three bytes per float: b0 + b1 * 256 + b2 * 65536. Power-of-two divisions are exact, so no epsilon.
float3 UnpackBytes(float packed)
{
    float b2 = floor(packed / 65536.0);
    packed -= b2 * 65536.0;
    float b1 = floor(packed / 256.0);
    return float3(packed - b1 * 256.0, b1, b2);
}

// Packed colors are gamma bytes that bypass the canvas conversion, so they are brought to the working color space here
half4 PackedToWorkingSpace(half4 color)
{
#ifndef UNITY_COLORSPACE_GAMMA
    color.rgb = UIGammaToLinear(color.rgb);
#endif
    return color;
}

// Outline, shadow and gradient color from four floats, matching VertexPacking.PackColors
void UnpackColors(float4 packed, out half4 outline, out half4 shadow, out half4 gradient)
{
    float3 x = UnpackBytes(packed.x);
    float3 y = UnpackBytes(packed.y);
    float3 z = UnpackBytes(packed.z);
    float3 w = UnpackBytes(packed.w);
    outline = PackedToWorkingSpace(half4(x.x, x.y, x.z, y.x) / 255.0);
    shadow = PackedToWorkingSpace(half4(y.y, y.z, z.x, z.y) / 255.0);
    gradient = PackedToWorkingSpace(half4(z.z, w.x, w.y, w.z) / 255.0);
}

// One color from two floats, matching VertexPacking.PackColor
half4 UnpackColor(float2 packed)
{
    float3 rgb = UnpackBytes(packed.x);
    return PackedToWorkingSpace(half4(rgb, packed.y) / 255.0);
}

// fill = radialSize * 100 + mode * 4096 + fillAlpha * 32768 + sharpApex * 8388608
// mode: 0 = none, 1 = vertical, 2 = horizontal, 3 = radial cover, 4 = radial fit, 5 = ellipse
void DecodeFill(float raw, out float mode, out float radialSize, out float fillAlpha, out float sharpApex)
{
    sharpApex = floor(raw / 8388608.0);
    raw -= sharpApex * 8388608.0;
    fillAlpha = floor(raw / 32768.0);
    raw -= fillAlpha * 32768.0;
    mode = floor(raw / 4096.0);
    radialSize = (raw - mode * 4096.0) / 100.0;
    fillAlpha /= 255.0;
}

// Blend factor from the fill color toward the gradient color; uv spans the rect from 0 to 1
float GradientFactor(float mode, float2 uv, float2 size, float radialSize)
{
    if (mode < 0.5)
        return 0.0;
    if (mode < 1.5)
        return saturate(1.0 - uv.y);
    if (mode < 2.5)
        return saturate(uv.x);

    float t;
    if (mode > 4.5)
    {
        t = length(uv - 0.5) * 2.0;
    }
    else
    {
        float refDim = mode < 3.5 ? max(size.x, size.y) : min(size.x, size.y);
        t = length((uv - 0.5) * size) / (refDim * 0.5);
    }
    return saturate(t / max(radialSize, 0.0001));
}

// Intersection of two distance fields with the joining corner rounded by r; exact for perpendicular edges
float RoundedMax(float a, float b, float r)
{
    float2 q = float2(a, b) + r;
    return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r;
}

// Sector with its apex at the origin sweeping clockwise from north between two angles in radians.
// The apex is rounded by r. Sweeps past a half turn are handled as the complement of the remaining wedge.
float SectorDistance(float2 p, float startAngle, float endAngle, float r)
{
    float halfSweep = (endAngle - startAngle) * 0.5;
    float center = (startAngle + endAngle) * 0.5;
    float2 up = float2(sin(center), cos(center));
    float2 q = float2(dot(p, float2(up.y, -up.x)), dot(p, up));

    float sgn = 1.0;
    if (halfSweep > SHAPE_HALF_PI)
    {
        q = -q;
        halfSweep = SHAPE_PI - halfSweep;
        sgn = -1.0;
    }

    // Shrinking the wedge by r moves the apex along the bisector; growing it back rounds the apex
    float2 sc = float2(sin(halfSweep), cos(halfSweep));
    q.y -= r / max(sc.x, 0.0001);
    q.x = abs(q.x);
    float m = length(q - sc * max(dot(q, sc), 0.0));
    float d = m * sign(sc.y * q.x - sc.x * q.y);
    return sgn * (d - r);
}

// arc: xy = apex in sdf space, zw = start and end angle; edge padding is already folded into the apex.
// sharpApex keeps the apex unrounded while the corners against the shape still use cornerRadius.
float ApplyArc(float sdf, float2 p, float4 arc, float cornerRadius, float sharpApex)
{
    if (arc.w - arc.z >= SHAPE_TWO_PI - 0.0001)
        return sdf;

    float sector = SectorDistance(p - arc.xy, arc.z, arc.w, sharpApex > 0.5 ? 0.0 : cornerRadius);
    return RoundedMax(sdf, sector, cornerRadius);
}

// shadow = round(shadowSize) + round(shadowBlur) * 4096
void DecodeShadow(float raw, out float shadowSize, out float shadowBlur)
{
    shadowBlur = floor(raw / 4096.0);
    shadowSize = raw - shadowBlur * 4096.0;
}

// 16-bit fixed point: (value + 2048) * 16, so ±2048 px in 1/16 px steps; matches VertexPacking.PackOffset
float DecodeOffset(float raw)
{
    return raw / 16.0 - 2048.0;
}

#endif
