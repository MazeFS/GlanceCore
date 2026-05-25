sampler2D input : register(s0);
float pixelSize : register(c0);
float time : register(c1);

float4 main(float2 uv : TEXCOORD) : COLOR
{
    float2 d = float2(pixelSize, pixelSize);
    float2 coord = d * floor(uv / d);
    float4 color = tex2D(input, coord);
    
    float scanline = sin(uv.y * 800.0 + time * 5.0) * 0.05;
    color.rgb -= scanline;
    
    return color;
}