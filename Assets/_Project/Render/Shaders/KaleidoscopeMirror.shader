Shader "KaleidoscopeEngine/KaleidoscopeMirror"
{
    Properties
    {
        _SourceTex ("Source Texture", 2D) = "black" {}
        _SegmentCount ("Segment Count", Float) = 8
        _Rotation ("Rotation", Float) = 0
        _Zoom ("Zoom", Float) = 1
        _CenterOffset ("Center Offset", Vector) = (0, 0, 0, 0)
        _RadialDistortion ("Radial Distortion", Float) = 0
        _EdgeSoftness ("Edge Softness", Float) = 0.02
        _Brightness ("Brightness", Float) = 1
        _Contrast ("Contrast", Float) = 1
        _Saturation ("Saturation", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "HDRenderPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend One Zero

        Pass
        {
            Name "KaleidoscopeMirror"

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "UnityCG.cginc"

            sampler2D _SourceTex;

            float _SegmentCount;
            float _Rotation;
            float _Zoom;
            float4 _CenterOffset;
            float _RadialDistortion;
            float _EdgeSoftness;
            float _Brightness;
            float _Contrast;
            float _Saturation;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.positionOS);
                output.uv = input.uv;
                return output;
            }

            float3 ApplyColor(float3 color)
            {
                color *= _Brightness;
                color = (color - 0.5) * _Contrast + 0.5;
                float luminance = dot(color, float3(0.2126, 0.7152, 0.0722));
                color = lerp(luminance.xxx, color, _Saturation);
                return saturate(color);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                const float TwoPi = 6.28318530718;
                float segments = max(1.0, round(_SegmentCount));
                float wedge = TwoPi / segments;

                float2 centered = (input.uv - 0.5 - _CenterOffset.xy) / max(0.001, _Zoom);
                float radius = length(centered);
                float angle = atan2(centered.y, centered.x) + _Rotation;

                angle = fmod(angle + TwoPi * 8.0, TwoPi);
                float sector = floor(angle / wedge);
                float localAngle = angle - sector * wedge;

                bool mirrored = fmod(sector, 2.0) >= 1.0;
                if (mirrored)
                {
                    localAngle = wedge - localAngle;
                }

                float normalizedAngle = localAngle / wedge;
                float foldedAngle = (normalizedAngle - 0.5) * wedge;
                float distortedRadius = radius * (1.0 + _RadialDistortion * radius * radius);
                float2 sampleVector = float2(cos(foldedAngle), sin(foldedAngle)) * distortedRadius;
                float2 sampleUV = sampleVector + 0.5 + _CenterOffset.xy;

                float4 color = tex2D(_SourceTex, saturate(sampleUV));

                float edgeDistance = min(localAngle, wedge - localAngle) / max(0.0001, wedge);
                float edgeMask = smoothstep(0.0, max(0.0001, _EdgeSoftness), edgeDistance);
                color.rgb *= lerp(0.82, 1.0, edgeMask);
                color.rgb = ApplyColor(color.rgb);
                color.a = 1.0;
                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}
