Shader "Toon Water"
{
    Properties
    {
        // color/depth/foam/noise controls
        _DepthGradientShallow("Depth Gradient Shallow", Color) = (0.325, 0.807, 0.971, 0.725)
        _DepthGradientDeep("Depth Gradient Deep", Color) = (0.086, 0.407, 1, 0.749)
        _DepthMaxDistance("Depth Maximum Distance", Float) = 1
        _FoamColor("Foam Color", Color) = (1,1,1,1)
        _SurfaceNoise("Surface Noise", 2D) = "white" {}
        _SurfaceNoiseScroll("Surface Noise Scroll Amount", Vector) = (0.03, 0.03, 0, 0)
        _SurfaceNoiseCutoff("Surface Noise Cutoff", Range(0, 1)) = 0.777
        _SurfaceDistortion("Surface Distortion", 2D) = "white" {}
        _SurfaceDistortionAmount("Surface Distortion Amount", Range(0, 1)) = 0.27
        _FoamMaxDistance("Foam Maximum Distance", Float) = 0.4
        _FoamMinDistance("Foam Minimum Distance", Float) = 0.04

        _Wave1Amplitude("Wave1 Amplitude", Range(0, 1)) = 0.15
        _Wave1Frequency("Wave1 Frequency", Range(0, 15)) = 3.0
        _Wave1Speed("Wave1 Speed", Range(0, 10)) = 1.2
        _Wave1Direction("Wave1 Direction (XY on XZ)", Vector) = (1, 0, 0, 0)

        _Wave2Amplitude("Wave2 Amplitude", Range(0, 1)) = 0.08
        _Wave2Frequency("Wave2 Frequency", Range(0, 15)) = 5.5
        _Wave2Speed("Wave2 Speed", Range(0, 10)) = 1.8
        _Wave2Direction("Wave2 Direction (XY on XZ)", Vector) = (0.5, 0.86, 0, 0)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #define SMOOTHSTEP_AA 0.01

            struct appdata
            {
                float4 vertex : POSITION;
                float4 uv     : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex         : SV_POSITION;
                float2 noiseUV        : TEXCOORD0;
                float2 distortUV      : TEXCOORD1;
                float4 screenPosition : TEXCOORD2;
                float3 viewNormal     : NORMAL;
            };

            sampler2D _SurfaceNoise;      float4 _SurfaceNoise_ST;
            sampler2D _SurfaceDistortion; float4 _SurfaceDistortion_ST;

            float4 _DepthGradientShallow, _DepthGradientDeep, _FoamColor;
            float  _DepthMaxDistance, _FoamMaxDistance, _FoamMinDistance;
            float  _SurfaceNoiseCutoff, _SurfaceDistortionAmount;
            float2 _SurfaceNoiseScroll;

            float  _Wave1Amplitude, _Wave1Frequency, _Wave1Speed;
            float4 _Wave1Direction; // xy used
            float  _Wave2Amplitude, _Wave2Frequency, _Wave2Speed;
            float4 _Wave2Direction; // xy used

            // Camera textures
            sampler2D _CameraDepthTexture;
            sampler2D _CameraNormalsTexture;

            float4 alphaBlend(float4 top, float4 bottom)
            {
                float3 color = (top.rgb * top.a) + (bottom.rgb * (1 - top.a));
                float   a    = top.a + bottom.a * (1 - top.a);
                return float4(color, a);
            }

            // Vertex with WORLD-SPACE sine displacement on Y
            v2f vert (appdata v)
            {
                v2f o;

                // Object = World
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                // Normalize XZ directions for the two waves
                float2 d1 = normalize(_Wave1Direction.xy);
                float2 d2 = normalize(_Wave2Direction.xy);

                // Phases 
                float p1 = dot(d1, worldPos.xz) * _Wave1Frequency + _Time.y * _Wave1Speed;
                float p2 = dot(d2, worldPos.xz) * _Wave2Frequency + _Time.y * _Wave2Speed;

                // Height offsets
                float h  = sin(p1) * _Wave1Amplitude + sin(p2) * _Wave2Amplitude;

                // Apply displacement along world Y
                worldPos.y += h;

                // World -> Clip
                o.vertex         = UnityWorldToClipPos(float4(worldPos, 1));
                o.screenPosition = ComputeScreenPos(o.vertex);

                // Pass UVs through (unchanged)
                o.distortUV = TRANSFORM_TEX(v.uv, _SurfaceDistortion);
                o.noiseUV   = TRANSFORM_TEX(v.uv, _SurfaceNoise);

                // Keep using the built-in view normal
                o.viewNormal = COMPUTE_VIEW_NORMAL;

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // Depth behind current pixel 
                float existingDepth01    = tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPosition)).r;
                float existingDepthLinear = LinearEyeDepth(existingDepth01);

                // Distance between water surface and object behind it 
                float depthDifference = existingDepthLinear - i.screenPosition.w;

                // Depth-based water color
                float waterDepth01 = saturate(depthDifference / _DepthMaxDistance);
                float4 waterColor  = lerp(_DepthGradientShallow, _DepthGradientDeep, waterDepth01);

                // Normal difference foam bias
                float3 existingNormal = tex2Dproj(_CameraNormalsTexture, UNITY_PROJ_COORD(i.screenPosition));
                float   normalDot     = saturate(dot(existingNormal, i.viewNormal));
                float   foamDistance  = lerp(_FoamMaxDistance, _FoamMinDistance, normalDot);
                float   foamDepth01   = saturate(depthDifference / foamDistance);

                // Distorted scrolling noise
                float surfaceNoiseCutoff = foamDepth01 * _SurfaceNoiseCutoff;
                float2 distortSample     = (tex2D(_SurfaceDistortion, i.distortUV).xy * 2 - 1) * _SurfaceDistortionAmount;
                float2 noiseUV           = float2(
                    (i.noiseUV.x + _Time.y * _SurfaceNoiseScroll.x) + distortSample.x,
                    (i.noiseUV.y + _Time.y * _SurfaceNoiseScroll.y) + distortSample.y
                );
                float surfaceNoiseSample = tex2D(_SurfaceNoise, noiseUV).r;

                // AA’d threshold
                float surfaceNoise = smoothstep(surfaceNoiseCutoff - SMOOTHSTEP_AA,
                                                surfaceNoiseCutoff + SMOOTHSTEP_AA,
                                                surfaceNoiseSample);

                float4 foamCol = _FoamColor;
                foamCol.a *= surfaceNoise;

                return alphaBlend(foamCol, waterColor);
            }
            ENDCG
        }
    }
}
