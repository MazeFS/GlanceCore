sampler2D input : register(s0);
float amount : register(c0);

float4 main(float2 uv : TEXCOORD) : COLOR
{
    float2 dir = uv - 0.5;
    float dist = length(dir);
    float distortion = pow(dist * 1.0, 2.0);
    float2 refractedUV = uv - (dir * distortion * amount);
    
    float4 color = tex2D(input, refractedUV) * 0.20;
    float step = 0.003;
    
    color += tex2D(input, refractedUV + float2(-step, -step)) * 0.10;
    color += tex2D(input, refractedUV + float2(0, -step)) * 0.10;
    color += tex2D(input, refractedUV + float2(step, -step)) * 0.10;
    color += tex2D(input, refractedUV + float2(-step, 0)) * 0.10;
    color += tex2D(input, refractedUV + float2(step, 0)) * 0.10;
    color += tex2D(input, refractedUV + float2(-step, step)) * 0.10;
    color += tex2D(input, refractedUV + float2(0, step)) * 0.10;
    color += tex2D(input, refractedUV + float2(step, step)) * 0.10;
    
    return color;
}