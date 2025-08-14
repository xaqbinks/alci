using UnityEngine;

/// <summary>
/// This class holds the HLSL code for a simple holographic UI shader.
/// To use, create a new Unlit Shader in Unity and paste the code from the
/// 'shaderCode' constant into that file.
/// </summary>
public class HologramShader : MonoBehaviour
{
    public const string shaderCode = @"
Shader ""Unlit/Hologram""
{
    Properties
    {
        _MainTex ("Texture", 2D) = ""white"" {}
        _TintColor ("Tint Color", Color) = (0, 1, 1, 0.5)
        _ScanlineSpeed ("Scanline Speed", Float) = 10
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.1
        _GlowIntensity ("Glow Intensity", Float) = 2
    }
    SubShader
    {
        Tags { ""Queue""=""Transparent"" ""RenderType""=""Transparent"" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _TintColor;
            float _ScanlineSpeed;
            float _ScanlineIntensity;
            float _GlowIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(UnityWorldSpaceViewDir(mul(unity_ObjectToWorld, v.vertex).xyz));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Main texture sample
                fixed4 col = tex2D(_MainTex, i.uv) * _TintColor;

                // Scanline effect
                float scanline = sin((i.uv.y + _Time.y * _ScanlineSpeed) * 200.0) * _ScanlineIntensity;
                col.rgb += scanline;

                // Fresnel/Glow effect
                float fresnel = 1.0 - saturate(dot(i.worldNormal, i.viewDir));
                fresnel = pow(fresnel, _GlowIntensity);
                col.rgb += fresnel * _TintColor.rgb;

                // Final alpha
                col.a *= _TintColor.a;

                return col;
            }
            ENDCG
        }
    }
}
";
}
