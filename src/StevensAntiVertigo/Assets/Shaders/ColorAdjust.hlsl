sampler2D uImage0 : register(s0);

float brightness;
float contrast;
float gamma;
float vibrance;

struct VertexShaderOutput
{
    float4 pos : POSITION;
    float4 color : COLOR0;
    float2 uv : TEXCOORD0;
};

float4 MainPixelShader(VertexShaderOutput input) : COLOR0
{
    float4 sampleColor = tex2D(uImage0, input.uv);

    float3 c = (sampleColor.rgb - 0.5) * (contrast * 2.0) + 0.5;
    c += (brightness - 0.5);
    c = max(c, 0.0);
    c = pow(c, 1.0 / max(gamma, 0.01));

    float3 lumCoeff = float3(0.212656, 0.715158, 0.072186);
    float luma = dot(c, lumCoeff);
    float maxColor = max(c.r, max(c.g, c.b));
    float minColor = min(c.r, min(c.g, c.b));
    float sat = maxColor - minColor;
    float effect = (vibrance - 0.5) * 2.0 * (1.0 - sat);
    c = lerp(float3(luma, luma, luma), c, 1.0 + effect);

    return float4(saturate(c), sampleColor.a);
}

technique t0
{
    pass p0
    {
        PixelShader = compile ps_3_0 MainPixelShader();
    }
}
