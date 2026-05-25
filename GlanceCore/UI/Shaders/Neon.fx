sampler2D input : register(s0);
float time : register(c0);
float4 neonColor : register(c1);

float4 main(float2 uv : TEXCOORD) : COLOR
{
    float shift = sin(time * 3.0) * 0.003 + 0.003;
    float4 color;
    color.r = tex2D(input, uv + float2(shift, 0)).r;
    color.g = tex2D(input, uv).g;
    color.b = tex2D(input, uv - float2(shift, 0)).b;
    color.a = tex2D(input, uv).a;
    
    float scanline = sin(uv.y * 600.0 - time * 10.0) * 0.04;
    color.rgb += neonColor.rgb * 0.35;
    color.rgb += scanline;
    
    return color;
}