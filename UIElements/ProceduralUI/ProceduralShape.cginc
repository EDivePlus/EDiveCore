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

// corners: x = top right, y = bottom right, z = top left, w = bottom left; already clamped to the half size
float ShapeDistance(float2 p, float2 halfSize, float4 corners, float style, float join)
{
    float2 side = p.x > 0.0 ? corners.xy : corners.zw;
    float c = p.y > 0.0 ? side.x : side.y;
    float2 q = abs(p) - halfSize;
    // A ternary would run both corner functions
    float d;
    [branch]
    if (style < 0.5)
        d = RoundCornerDistance(q, c, join);
    else
        d = ChamferCornerDistance(q, c, join);
    return d;
}

// Three 16-bit values from two floats, matching VertexPacking.PackTriple
void UnpackTriple(float2 packed, out float a, out float b, out float c)
{
    float bLow = floor(packed.x / 65536.0);
    a = packed.x - bLow * 65536.0;
    c = floor(packed.y / 256.0);
    b = (packed.y - c * 256.0) * 256.0 + bLow;
}

// 16-bit fixed point in 1/16 px steps, matching VertexPacking.FixedPixel and FixedSignedPixel
float FixedToPixel(float q) { return q / 16.0; }
float FixedToSignedPixel(float q) { return q / 16.0 - 2048.0; }

// 16-bit turn fraction, matching VertexPacking.FixedAngle
float FixedToAngle(float q) { return q / 65535.0 * SHAPE_TWO_PI; }

// Vertex position over its quad in 1/255 steps: x + y * 256, matching VertexPacking.PackGrid
float2 DecodeGrid(float code)
{
    float y = floor(code / 256.0);
    return float2(code - y * 256.0, y) / 255.0;
}

// Geometry shared by every layer, matching SDFGraphic.BuildGeometryVertex.
// uv is the vertex position over the rect, size is in px, arc is apex (xy) and start / end angle (zw).
void DecodeGeometry(float4 uv0, float4 uv1, out float2 uv, out float2 size, out float4 roundness, out float4 arc, out float cornerRadius)
{
    float grid, w, h;
    UnpackTriple(uv0.xy, grid, w, h);
    float r0, r1, r2;
    UnpackTriple(uv0.zw, r0, r1, r2);
    float r3, apexX, apexY;
    UnpackTriple(uv1.xy, r3, apexX, apexY);
    float start, sweep, corner;
    UnpackTriple(uv1.zw, start, sweep, corner);

    uv = DecodeGrid(grid);
    size = float2(FixedToPixel(w), FixedToPixel(h));
    roundness = float4(FixedToPixel(r0), FixedToPixel(r1), FixedToPixel(r2), FixedToPixel(r3));
    float startAngle = FixedToAngle(start);
    arc = float4(FixedToSignedPixel(apexX), FixedToSignedPixel(apexY), startAngle, startAngle + FixedToAngle(sweep));
    cornerRadius = FixedToPixel(corner);
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

// One color from two floats, matching VertexPacking.PackColor
half4 UnpackColor(float2 packed)
{
    float3 rgb = UnpackBytes(packed.x);
    return PackedToWorkingSpace(half4(rgb, packed.y) / 255.0);
}

// fill = radialSize * 100 + mode * 4096 + hasTexture * 32768, matching SDFGraphic.BuildFillVertex
// mode: 0 = none, 1 = vertical, 2 = horizontal, 3 = radial cover, 4 = radial fit, 5 = ellipse
void DecodeFill(float raw, out float mode, out float radialSize, out float hasTexture)
{
    hasTexture = floor(raw / 32768.0);
    raw -= hasTexture * 32768.0;
    mode = floor(raw / 4096.0);
    radialSize = (raw - mode * 4096.0) / 100.0;
}

// Blend factor from the fill color toward the gradient color; uv spans the rect from 0 to 1.
// Branch free: callers skip it for mode 0, and flow control nested in their branch crashes the D3D compiler.
float GradientFactor(float mode, float2 uv, float2 size, float radialSize)
{
    float2 centered = uv - 0.5;
    float axial = mode < 1.5 ? 1.0 - uv.y : uv.x;
    float refDim = mode < 3.5 ? max(size.x, size.y) : min(size.x, size.y);
    float radial = mode > 4.5 ? length(centered) * 2.0 : length(centered * size) / (refDim * 0.5);
    radial /= max(radialSize, 0.0001);
    return saturate(mode < 2.5 ? axial : radial);
}

// Gradient of a field over shape space, from screen space derivatives of the field and of the shape position
float2 ShapeGradient(float field, float2 p)
{
    float2 dpx = ddx(p);
    float2 dpy = ddy(p);
    float2 df = float2(ddx(field), ddy(field));
    float det = dpx.x * dpy.y - dpx.y * dpy.x;
    if (abs(det) < 1e-8)
        return float2(1.0, 0.0);
    return float2(dpy.y * df.x - dpx.y * df.y, dpx.x * df.y - dpy.x * df.x) / det;
}

// Cosine of the angle between the edge normals of two fields at this pixel
float EdgeCosine(float a, float b, float2 p)
{
    float2 ga = ShapeGradient(a, p);
    float2 gb = ShapeGradient(b, p);
    return dot(ga, gb) / max(length(ga) * length(gb), 1e-6);
}

// Intersection of two distance fields. cosTheta is the cosine of the angle between their edge normals, which
// keeps the corner exact at any angle. The corner is rounded by r, or offset outside with the join when sharp:
// round is the true distance, miter extends both edges, bevel cuts the miter where the round would end.
float JoinedMax(float a, float b, float r, float cosTheta, float join)
{
    float2 q = float2(a, b) + r;
    float2 o = max(q, 0.0);
    float outside;
    if (r > 0.0 || join < 0.5)
    {
        bool corner = q.x >= q.y * cosTheta && q.y >= q.x * cosTheta && max(q.x, q.y) > 0.0;
        float sinSq = max(1.0 - cosTheta * cosTheta, 1e-4);
        outside = corner
            ? sqrt(max(q.x * q.x + q.y * q.y - 2.0 * q.x * q.y * cosTheta, 0.0) / sinSq)
            : max(o.x, o.y);
    }
    else if (join < 1.5)
    {
        outside = max(o.x, o.y);
    }
    else
    {
        // The cut is a third edge along the bisector; the unclamped distances keep it from reaching past the corner
        float cosHalf = max(sqrt(max(0.5 + 0.5 * cosTheta, 0.0)), 1e-3);
        outside = max(max(o.x, o.y), (q.x + q.y) / (2.0 * cosHalf));
    }
    return min(max(q.x, q.y), 0.0) + outside - r;
}

// arc: xy = apex in sdf space, zw = start and end angle; edge padding is already folded into the apex.
bool ArcIsFull(float4 arc)
{
    return arc.w - arc.z >= SHAPE_TWO_PI - 0.0001;
}

// Per vertex part of the sector: a wedge sweeping clockwise from north between the arc angles, apex rounded by r.
// Sweeps past a half turn are the complement of the remaining wedge, so the direction flips and the sign turns negative.
// Shrinking the wedge by r moves the apex along the bisector; growing it back rounds the apex.
//   sectorA: xy = shifted apex, zw = bisector direction
//   sectorB: xy = sin and cos of the half sweep, z = apex radius, w = sign
void PrepareSector(float4 arc, float r, out float4 sectorA, out float4 sectorB)
{
    float halfSweep = (arc.w - arc.z) * 0.5;
    float center = (arc.z + arc.w) * 0.5;
    float2 up;
    sincos(center, up.x, up.y);
    float sgn = 1.0;
    if (halfSweep > SHAPE_HALF_PI)
    {
        up = -up;
        halfSweep = SHAPE_PI - halfSweep;
        sgn = -1.0;
    }
    float2 sc;
    sincos(halfSweep, sc.x, sc.y);
    sectorA = float4(arc.xy + up * (r / max(sc.x, 0.0001)), up);
    sectorB = float4(sc, r, sgn);
}

// Per pixel part of the sector, see PrepareSector
float SectorDistance(float2 p, float4 sectorA, float4 sectorB)
{
    float2 d = p - sectorA.xy;
    float2 up = sectorA.zw;
    float2 q = float2(abs(dot(d, float2(up.y, -up.x))), dot(d, up));
    float2 sc = sectorB.xy;
    float m = length(q - sc * max(dot(q, sc), 0.0));
    return sectorB.w * (m * sign(sc.y * q.x - sc.x * q.y) - sectorB.z);
}

// shadow = round(shadowSize) + round(shadowBlur) * 4096.
// Rounded first: perspective interpolation drifts the value slightly and floor would turn 0 into a 4096 px shadow.
void DecodeShadow(float raw, out float shadowSize, out float shadowBlur)
{
    float info = round(max(raw, 0.0));
    shadowBlur = floor(info / 4096.0);
    shadowSize = info - shadowBlur * 4096.0;
}

// layer = frameWidth + framePlacement * 4096 + frameMode * 16384 + cornerShape * 32768 + sharpApex * 262144 + layer * 524288,
// matching SDFGraphic.EncodeLayer. Layers: 0 = fill, 1 = outer shadow, 2 = inner shadow, 3 = outline, 4 = solid fill
void DecodeLayer(float raw, out float layer, out float sharpApex, out float cornerShape, out bool frameMode, out float framePlacement, out float frameWidth)
{
    raw = round(raw);
    layer = floor(raw / 524288.0);
    raw -= layer * 524288.0;
    sharpApex = floor(raw / 262144.0);
    raw -= sharpApex * 262144.0;
    cornerShape = floor(raw / 32768.0);
    raw -= cornerShape * 32768.0;
    float frame = floor(raw / 16384.0);
    raw -= frame * 16384.0;
    frameMode = frame > 0.5;
    framePlacement = floor(raw / 4096.0);
    frameWidth = raw - framePlacement * 4096.0;
}

// Placement: 0 = inside, 1 = center, 2 = outside. How far a band of the given width reaches past the edge.
float PlacementOuterExtent(float placement, float width)
{
    return placement < 0.5 ? 0.0 : placement < 1.5 ? width * 0.5 : width;
}

// Shadow offset, 12 bits per axis in 1/4 px steps; matches VertexPacking.PackOffsets
float2 DecodeOffsets(float raw)
{
    float y = floor(raw / 4096.0);
    return float2(raw - y * 4096.0, y) / 4.0 - 512.0;
}

#endif
