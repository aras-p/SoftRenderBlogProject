#include <metal_stdlib>
#include <simd/simd.h>

using namespace metal;

typedef struct
{
    float3 position [[attribute(0)]];
    float2 texCoord [[attribute(1)]];
} Vertex;

typedef struct
{
    float4 position [[position]];
    float2 texCoord;
} ColorInOut;

vertex ColorInOut vertexShader(Vertex in [[stage_in]])
{
    ColorInOut out;
    float4 position = float4(in.position.x, -in.position.z, 0.0, 1.0);
    out.position = position;
    out.texCoord = in.texCoord;
    return out;
}

fragment half4 fragmentShader(ColorInOut in [[stage_in]], texture2d<half> colorMap [[texture(0)]])
{
    constexpr sampler colorSampler(mip_filter::nearest, mag_filter::nearest, min_filter::nearest);
    return colorMap.sample(colorSampler, in.texCoord.xy);
}
