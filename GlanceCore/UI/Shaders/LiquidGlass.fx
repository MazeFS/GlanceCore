sampler2D input : register(s0);
float amount : register(c0);

float4 main(float2 uv : TEXCOORD) : COLOR
{
    float2 dir = uv - 0.5;
    float dist = length(dir);
    
    // Искажение усиливается к краям (эффект лупы/линзы)
    float distortion = pow(dist * 2.0, 2.0);
    
    float2 refractedUV = uv - (dir * distortion * amount);
    return tex2D(input, refractedUV);
}