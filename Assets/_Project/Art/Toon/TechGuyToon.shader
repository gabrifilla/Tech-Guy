Shader "Tech Guy/Illustrated Toon"
{
    Properties
    {
        [MainTexture] _BaseMap("Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Pigment", Color) = (1,1,1,1)
        _ShadowColor("Cool shadow tint", Color) = (.32,.43,.48,1)
        _HighlightColor("Warm light tint", Color) = (1.05,1.0,.88,1)
        _ShadowThreshold("Light band", Range(0,1)) = .48
        _BandSoftness("Band edge softness", Range(.001,.2)) = .035
        _PigmentStrength("Pigment variation", Range(0,.2)) = .045
        _OutlineColor("Ink", Color) = (.018,.031,.034,1)
        _OutlineWidth("Ink width (world metres)", Range(0,.08)) = .018
        _EmissionMap("Emission", 2D) = "white" {}
        [HDR] _EmissionColor("Emission color", Color) = (0,0,0,1)
        [HideInInspector] _Cull("Cull", Float) = 2
        [HideInInspector] _Cutoff("Alpha cutoff", Float) = .5
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor, _ShadowColor, _HighlightColor, _OutlineColor, _EmissionColor;
            float _ShadowThreshold, _BandSoftness, _PigmentStrength, _OutlineWidth, _Cutoff;
        CBUFFER_END
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
        ENDHLSL

        Pass
        {
            Name "Ink"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            HLSLPROGRAM
            #pragma vertex InkVertex
            #pragma fragment InkFragment
            #pragma multi_compile_instancing
            struct InkAttributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct InkVaryings { float4 positionCS:SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
            InkVaryings InkVertex(InkAttributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                InkVaryings output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 position = TransformObjectToWorld(input.positionOS.xyz);
                position += TransformObjectToWorldNormal(input.normalOS) * _OutlineWidth;
                output.positionCS = TransformWorldToHClip(position);
                return output;
            }
            half4 InkFragment(InkVaryings input):SV_Target { return _OutlineColor; }
            ENDHLSL
        }
        Pass
        {
            Name "IllustratedLight"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            HLSLPROGRAM
            #pragma vertex ToonVertex
            #pragma fragment ToonFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Attributes
            { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                float3 positionWS:TEXCOORD0;
                half3 normalWS:TEXCOORD1;
                float2 uv:TEXCOORD2;
                half fog:TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings ToonVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = p.positionCS;
                output.positionWS = p.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fog = ComputeFogFactor(p.positionCS.z);
                return output;
            }
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float Pigment(float2 p)
            {
                float2 cell=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(Hash(cell),Hash(cell+float2(1,0)),f.x),
                    lerp(Hash(cell+float2(0,1)),Hash(cell+1),f.x),f.y);
            }
            half4 ToonFragment(Varyings input):SV_Target
            {
                half3 normal = normalize(input.normalWS);
                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half diffuse = saturate(dot(normal,light.direction));
                half band = smoothstep(_ShadowThreshold-_BandSoftness,_ShadowThreshold+_BandSoftness,diffuse);
                half lit = band * light.shadowAttenuation;
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).rgb * _BaseColor.rgb;
                float grain = Pigment(input.positionWS.xz*1.7 + input.positionWS.y*.37);
                albedo *= 1 + (grain-.5)*_PigmentStrength;
                half3 shade = lerp(_ShadowColor.rgb,_HighlightColor.rgb,lit);
                half3 color = albedo * shade * (.55 + light.color*.65 + SampleSH(normal)*.2);
                #ifdef _ADDITIONAL_LIGHTS
                uint count = GetAdditionalLightsCount();
                for (uint i=0; i<count; i++)
                {
                    Light extra = GetAdditionalLight(i,input.positionWS);
                    half edge = smoothstep(.35,.45,saturate(dot(normal,extra.direction)));
                    color += albedo * extra.color * extra.distanceAttenuation * edge * .3;
                }
                #endif
                color += SAMPLE_TEXTURE2D(_EmissionMap,sampler_EmissionMap,input.uv).rgb * _EmissionColor.rgb;
                return half4(MixFog(color,input.fog),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Back
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R Cull Back
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On Cull Back
            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
