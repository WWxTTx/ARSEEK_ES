
Shader "Ciconia Studio/CS_Ghost/URP/CS_Advanced Ghost"
{
    Properties
    {
        [Header(Main)]
        _Color("Color", Color) = (0, 0, 0, 1)
        _MainTex("Base Color", 2D) = "white" {}
        _Opacity("Opacity", Range(0, 1)) = 0.5

        [Header(Fresnel)]
        [HDR] _FresnelColor("Fresnel Color", Color) = (0.69, 1, 0.98, 1)
        _FresnelPower("Fresnel Power", Float) = 4
        _FresnelIntensity("Fresnel Intensity", Float) = 4
        _FresnelBias("Bias", Range(0, 1)) = 0

        [Header(Flicker)]
        _FlickerSpeed("Flicker Speed", Float) = 1
        _FlickerMin("Min Brightness", Range(0, 1)) = 0.3
        _FlickerMax("Max Brightness", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

        HLSLINCLUDE
        #pragma target 3.0
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Color;
            float4 _FresnelColor;
            half  _Opacity;
            half  _FresnelPower;
            half  _FresnelIntensity;
            half  _FresnelBias;
            half  _FlickerSpeed;
            half  _FlickerMin;
            half  _FlickerMax;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            float2 uv         : TEXCOORD0;
            float2 lightmapUV : TEXCOORD1;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv         : TEXCOORD0;
            float3 normalWS   : TEXCOORD2;
            float3 viewDirWS  : TEXCOORD3;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        Varyings SharedVert(Attributes input)
        {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);

            VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
            VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

            output.positionCS = vertexInput.positionCS;
            output.normalWS = normalInput.normalWS;
            output.viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
            output.uv = TRANSFORM_TEX(input.uv, _MainTex);

            return output;
        }
        ENDHLSL

        // Pass 0: depth prime for multi-ghost occlusion. Rendered by GhostDepthPrepassFeature
        // (a custom ScriptableRendererFeature) BEFORE the transparent pass, so all ghost
        // depth is written before any ghost is shaded. Then this pass is matched in the
        // forward pass with ZTest Equal to render only the front-most surface.
        // Uses a custom LightMode tag so URP never renders this pass itself.
        Pass
        {
            Name "GhostDepthPrime"
            Tags { "LightMode" = "GhostDepthPrime" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex SharedVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            half4 DepthFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return 0;
            }
            ENDHLSL
        }

        // Pass 1: shade only fragments matching the primed depth, so overlapping ghosts
        // resolve to the nearest surface instead of blending together.
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Equal
            Cull Back

            HLSLPROGRAM
            #pragma vertex SharedVert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 N = normalize(input.normalWS);
                float3 V = SafeNormalize(input.viewDirWS);

                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;

                float NdotV = dot(N, V);
                float fresnel = _FresnelBias + _FresnelIntensity * pow(max(1.0 - abs(NdotV), 0.0001), _FresnelPower);
                float flicker = lerp(_FlickerMin, _FlickerMax, sin(_Time.y * _FlickerSpeed) * 0.5 + 0.5);
                float fresnelMask = saturate(fresnel * flicker);

                float3 finalColor = baseColor.rgb + _FresnelColor.rgb * fresnelMask;

                #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
                    finalColor = MixFog(finalColor, input.positionCS.w);
                #endif

                return half4(finalColor, _Opacity);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            ShadowVaryings vert(ShadowAttributes input)
            {
                ShadowVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, GetMainLight().direction));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                return output;
            }

            half4 frag(ShadowVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return 0;
            }
            ENDHLSL
        }
    }
}
