// Subtle dotted AR plane: white dots at 15% opacity on a transparent plane.
// ARPlaneMeshVisualizer writes plane-space coordinates (in metres) into UV0,
// so dividing by _Spacing gives one dot every _Spacing metres on any plane, vertical too.
Shader "FindMe/PlaneDots"
{
    Properties
    {
        _Color ("Dot color", Color) = (1, 1, 1, 0.15)
        _Spacing ("Dot spacing (m)", Float) = 0.1
        _Radius ("Dot radius (0-0.5 of a cell)", Range(0.01, 0.5)) = 0.08
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off  // visible from below too (e.g. looking up at a shelf)

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Spacing;
                float _Radius;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert (Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv / _Spacing;  // 1 unit = 1 dot cell
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float d = length(frac(i.uv) - 0.5);                 // distance from cell centre
                float aa = fwidth(d);                               // soft edge, ~1 pixel wide
                half mask = 1 - smoothstep(_Radius - aa, _Radius + aa, d);
                return half4(_Color.rgb, _Color.a * mask);
            }
            ENDHLSL
        }
    }
}
