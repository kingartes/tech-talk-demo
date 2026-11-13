Shader "TechTalk/PipelineSurface"
{
    Properties
    {
        _Color("Surface", Color) = (.4,.55,.65,1)
        _Tex("UV atlas", 2D) = "white" {}
        _Mode("Mode", Float) = 0
        _Metal("Metal", Range(0,1)) = .85
        _Temp("Sensor temperature", Float) = 52
        _Phase("Clock", Float) = 0
        _Amplitude("Vertex displacement", Float) = 0
        _LightDirection("Light direction", Vector) = (.4,.7,-.5,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_Tex);
            SAMPLER(sampler_Tex);
            CBUFFER_START(UnityPerMaterial)
                float4 _Color, _LightDirection;
                float _Mode, _Metal, _Temp, _Phase, _Amplitude;
            CBUFFER_END

            struct Vertex { float4 position : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; float3 bary : TEXCOORD1; };
            struct Varyings
            {
                float4 position : SV_POSITION;
                float3 normal : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 bary : TEXCOORD2;
                float3 world : TEXCOORD3;
            };

            Varyings Vert(Vertex v)
            {
                Varyings o;
                // Push mesh vertices along a traveling wave, then project them.
                float3 p = v.position.xyz;
                p.y += _Amplitude * sin(4.5 * p.z - _Phase * 18.0);
                o.position = TransformObjectToHClip(p);
                o.world = TransformObjectToWorld(p);
                // Tilt the normal by the wave slope before transforming it to world space.
                v.normal.z -= 4.5 * _Amplitude * cos(4.5 * p.z - _Phase * 18.0) * v.normal.y;
                o.normal = TransformObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                o.bary = v.bary;
                return o;
            }

            float3 Palette(float t)
            {
                float3 cold = float3(.025,.14,.74);
                float3 cool = float3(.0,.76,.83);
                float3 warm = float3(1,.79,.07);
                float3 hot = float3(.94,.048,.02);
                return t < .3333 ? lerp(cold,cool,t*3) :
                    t < .6667 ? lerp(cool,warm,t*3-1) : lerp(warm,hot,t*3-2);
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normal);
                float3 view = GetWorldSpaceNormalizeViewDir(i.world);
                float3 light = normalize(_LightDirection.xyz);
                if (_Mode == 1) // Barycentric distance to the three triangle edges.
                {
                    float3 a = smoothstep(0, 1.15 * fwidth(i.bary), i.bary);
                    float edge = 1 - min(a.x,min(a.y,a.z));
                    return half4(lerp(_Color.rgb * .15, float3(.09,.74,1), edge * .9),1);
                }
                if (_Mode == 2) return half4(_Color.rgb,1);
                if (_Mode == 3) return half4(i.uv, .17, 1);
                if (_Mode == 4) return SAMPLE_TEXTURE2D(_Tex,sampler_Tex,i.uv);
                if (_Mode == 5) return half4(n * .5 + .5, 1);
                if (_Mode == 6) return half4(_Color.rgb * (.14 + .86 * saturate(dot(n,light))), 1);
                if (_Mode == 7)
                {
                    // Synthetic spatial field around the selected sensor, in Celsius.
                    float falloff = exp(-1.65 * abs(i.world.z - .65));
                    float localC = 35 + (_Temp - 35) * falloff;
                    float3 color = Palette(saturate((localC - 30) / 110));
                    return half4(color * (.78 + .22 * saturate(dot(n,light))), 1);
                }

                // Stylized studio reflections for the opening product view.
                float3 r = reflect(-view,n);
                float key = pow(saturate(dot(r,normalize(float3(.35,.85,-.38)))), 12);
                float fill = pow(saturate(dot(r,normalize(float3(-.84,.23,-.40)))), 8);
                float rim = pow(saturate(dot(r,normalize(float3(.12,.32,.94)))), 24);
                float lambert = saturate(dot(n,light));
                float brushing = .92 + .08 * sin(i.uv.y * 170.0 + sin(i.uv.x * 100.0));
                float3 base = _Color.rgb * (.22 + .62 * lambert + .17 * saturate(n.y)) * brushing;
                float3 reflection = key * float3(.85,.92,1) + fill * float3(.23,.41,.75) + rim * float3(.86,.41,.12);
                return half4(base + reflection * lerp(.22,.95,_Metal),1);
            }
            ENDHLSL
        }
    }
}
