Shader "TechTalk/InspectionShell"
{
    Properties { _Opacity("Face opacity", Range(0,1)) = .14 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            // Keep the opaque parts' depth. Composite front faces over visible internals.
            ZTest LEqual
            ZWrite Off
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Opacity;
            CBUFFER_END
            struct Vertex { float4 position : POSITION; float3 normal : NORMAL; };
            struct Varyings { float4 position : SV_POSITION; float3 normal : TEXCOORD0; float3 world : TEXCOORD1; };

            Varyings Vert(Vertex v)
            {
                Varyings o;
                o.position = TransformObjectToHClip(v.position.xyz);
                o.world = TransformObjectToWorld(v.position.xyz);
                o.normal = TransformObjectToWorldNormal(v.normal);
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float3 view = GetWorldSpaceNormalizeViewDir(i.world);
                float rim = pow(1 - saturate(dot(normalize(i.normal),view)), 3);
                return half4(.035 + rim * .18,.54 + rim * .36,.9,saturate(_Opacity + rim * .46));
            }
            ENDHLSL
        }
    }
}
