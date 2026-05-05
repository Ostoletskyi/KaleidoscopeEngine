Shader "KaleidoscopeEngine/KaleidoscopeMirror"
{
    Properties
    {
        _SourceTex ("Source Texture", 2D) = "black" {}
        _SourceTexB ("Secondary Source Texture", 2D) = "black" {}
        _PreviewSource ("Preview Source", Float) = 0
        _RenderPixelationFactor ("Render Pixelation Factor", Float) = 1
        colorDepthMode ("colorDepthMode", Float) = 10
        colorSteps ("colorSteps", Float) = 16777216
        paletteQuantizationStrength ("paletteQuantizationStrength", Float) = 0
        _SegmentCount ("Segment Count", Float) = 8
        _Rotation ("Rotation", Float) = 0
        _Zoom ("Zoom", Float) = 1
        _CenterOffset ("Center Offset", Vector) = (0, 0, 0, 0)
        _RadialDistortion ("Radial Distortion", Float) = 0
        _EdgeSoftness ("Edge Softness", Float) = 0.02
        _UseMirrorAngleMode ("Use Mirror Angle Mode", Float) = 1
        _MirrorAngleDegrees ("Mirror Angle Degrees", Float) = 60
        _PrismMode ("Prism Mode", Float) = 1
        _SeamSoftness ("Seam Softness", Float) = 0.025
        _SeamAlignmentOffset ("Seam Alignment Offset", Float) = 0
        _SeamChromaticAberrationEnabled ("Seam Chromatic Aberration Enabled", Float) = 0
        _SeamChromaticAberration ("Seam Chromatic Aberration", Float) = 0.0012
        _ShowSectorBoundaries ("Show Sector Boundaries", Float) = 0
        _BoundaryDebugColor ("Boundary Debug Color", Color) = (1, 0.25, 0.08, 1)
        _CenterScale ("Center Scale", Float) = 1.05
        _CenterBrightness ("Center Brightness", Float) = 1.08
        _CenterVignette ("Center Vignette", Float) = 0.12
        _CenterStabilization ("Center Stabilization", Float) = 0.72
        _CenterMaskEnabled ("Center Mask Enabled", Float) = 1
        _CenterMaskMode ("Center Mask Mode", Float) = 0
        _CenterMaskRadius ("Center Mask Radius", Float) = 0.18
        _CenterExposure ("Center Exposure", Float) = 0.86
        _CenterFalloff ("Center Falloff", Float) = 0.34
        _CenterContrast ("Center Contrast", Float) = 0.95
        _CenterGradientStrength ("Center Gradient Strength", Float) = 0.22
        _CenterDetailBoost ("Center Detail Boost", Float) = 0.18
        _CenterBloomLimit ("Center Bloom Limit", Float) = 1.08
        _CenterCleanEnabled ("Center Clean Enabled", Float) = 0
        _CenterCleanRadius ("Center Clean Radius", Float) = 0.095
        _CenterCleanFeather ("Center Clean Feather", Float) = 0.055
        _CenterReconstructFromTexture ("Center Reconstruct From Texture", Float) = 1
        _CenterPatternContinuation ("Center Pattern Continuation", Float) = 0.92
        _CenterFillMode ("Center Fill Mode", Float) = 2
        _CenterWorkRadius ("Center Work Radius", Float) = 0.095
        _CenterWorkFeather ("Center Work Feather", Float) = 0.055
        _CenterBlendStrength ("Center Blend Strength", Float) = 0.78
        _CenterContinuationStrength ("Center Continuation Strength", Float) = 0.76
        _CenterDetailAmount ("Center Detail Amount", Float) = 0.18
        _CenterSampleScale ("Center Sample Scale", Float) = 1
        _CenterReconstructionQuality ("Center Reconstruction Quality", Float) = 0.7
        _CenterOnlyDebugMode ("Center Only Debug Mode", Float) = 0
        _CenterMaskPreview ("Center Mask Preview", Float) = 0
        _PhysicalCenterArtifacts ("Physical Center Artifacts", Float) = 1
        _CenterArtifactOverrideEnabled ("Center Artifact Override Enabled", Float) = 0
        _CenterArtifactOverrideRadius ("Center Artifact Override Radius", Float) = 0.115
        _CenterArtifactOverrideMode ("Center Artifact Override Mode", Float) = 0
        _OpticalDensity ("Optical Density", Float) = 0.76
        _VisualNoiseAmount ("Visual Noise Amount", Float) = 0.045
        _ForegroundWeight ("Foreground Weight", Float) = 0.72
        _MidgroundWeight ("Midground Weight", Float) = 0.9
        _BackgroundWeight ("Background Weight", Float) = 0.42
        _DepthFadeStrength ("Depth Fade Strength", Float) = 0.16
        _OpticalDepthStrength ("Optical Depth Strength", Float) = 0.24
        _SourceOverscanFactor ("Source Overscan Factor", Float) = 1.2
        _EdgeRecursionBlend ("Edge Recursion Blend", Float) = 0.5
        _CenterConvergenceStrength ("Center Convergence Strength", Float) = 0.6
        _RadialContinuation ("Radial Continuation", Float) = 0.5
        _CenterRecursionBlend ("Center Recursion Blend", Float) = 0.44
        _InnerPatternPropagation ("Inner Pattern Propagation", Float) = 0.4
        _SeamSmoothingQuality ("Seam Smoothing Quality", Float) = 1.25
        _OpticalDistortionQuality ("Optical Distortion Quality", Float) = 1.08
        _SeamBlendStrength ("Seam Blend Strength", Float) = 0.8
        _SeamFeatherWidth ("Seam Feather Width", Float) = 0.04
        _ContinuityCorrection ("Continuity Correction", Float) = 0.32
        _RadialEdgeSoftness ("Radial Edge Softness", Float) = 0.035
        _SeamAntialiasingEnabled ("Seam Antialiasing Enabled", Float) = 1
        _SeamAAWidth ("Seam AA Width", Float) = 0.012
        _SeamLineSuppression ("Seam Line Suppression", Float) = 0.72
        _HighSpeedSpinAAEnabled ("High Speed Spin AA Enabled", Float) = 1
        _HighSpeedSeamSoftening ("High Speed Seam Softening", Float) = 0
        _MaskEnabled ("Mask Enabled", Float) = 1
        _MaskMode ("Mask Mode", Float) = 1
        _MaskRadius ("Mask Radius", Float) = 0.72
        _MaskSoftness ("Mask Softness", Float) = 0.16
        _MaskDarkness ("Mask Darkness", Float) = 0.58
        _HexMaskRotation ("Hex Mask Rotation", Float) = 0
        _VignetteEnabled ("Vignette Enabled", Float) = 1
        _VignetteStrength ("Vignette Strength", Float) = 0.12
        _VignetteSoftness ("Vignette Softness", Float) = 0.72
        _EdgeDarkening ("Edge Darkening", Float) = 0.08
        _OpticalMaskFeather ("Optical Mask Feather", Float) = 0.05
        _LensImperfectionStrength ("Lens Imperfection Strength", Float) = 0.006
        _RubyWeight ("Ruby Weight", Float) = 0.9
        _EmeraldWeight ("Emerald Weight", Float) = 1.18
        _OpalWeight ("Opal Weight", Float) = 1.14
        _QuartzWeight ("Quartz Weight", Float) = 1.02
        _SaturationCompression ("Saturation Compression", Float) = 0.16
        _HighlightColorBias ("Highlight Color Bias", Color) = (1, 0.92, 0.84, 1)
        _Brightness ("Brightness", Float) = 1.16
        _Contrast ("Contrast", Float) = 1.16
        _Saturation ("Saturation", Float) = 1.22
        _Vibrance ("Vibrance", Float) = 1.15
        _Gamma ("Gamma", Float) = 0.95
        _BlackLevel ("Black Level", Float) = 0.02
        _WhiteLevel ("White Level", Float) = 1.1
        _Sharpness ("Sharpness", Float) = 0.15
        _OrganicTime ("Organic Time", Float) = 0
        _WobbleEnabled ("Wobble Enabled", Float) = 1
        _WobbleStrength ("Wobble Strength", Float) = 0.0035
        _WobbleSpeed ("Wobble Speed", Float) = 0.8
        _BreathingEnabled ("Breathing Enabled", Float) = 1
        _BreathingAmplitude ("Breathing Amplitude", Float) = 0.006
        _BreathingSpeed ("Breathing Speed", Float) = 0.28
        _CenterDriftEnabled ("Center Drift Enabled", Float) = 1
        _CenterDriftStrength ("Center Drift Strength", Float) = 0.0025
        _CenterDriftSpeed ("Center Drift Speed", Float) = 0.22
        _SegmentVariationEnabled ("Segment Variation Enabled", Float) = 1
        _SegmentAngleVariation ("Segment Angle Variation", Float) = 0.003
        _SegmentBrightnessVariation ("Segment Brightness Variation", Float) = 0.018
        _TemporalDriftEnabled ("Temporal Drift Enabled", Float) = 1
        _DriftSpeed ("Drift Speed", Float) = 0.08
        _DriftAmount ("Drift Amount", Float) = 0.003
        _AsymmetryEnabled ("Asymmetry Enabled", Float) = 1
        _AsymmetryStrength ("Asymmetry Strength", Float) = 0.003
        _TemporalDriftAmount ("Temporal Drift Amount", Float) = 0.0025
        _RotationalDrift ("Rotational Drift", Float) = 0.0015
        _ScaleDrift ("Scale Drift", Float) = 0.0025
        _OpticalBreathingAmount ("Optical Breathing Amount", Float) = 0.004
        _DirtyGlassEnabled ("Dirty Glass Enabled", Float) = 1
        _DirtyGlassStrength ("Dirty Glass Strength", Float) = 0.01
        _DirtyGlassScale ("Dirty Glass Scale", Float) = 120
        _DirectTextureSource ("Direct Texture Source", Float) = 0
        _AnimatedImageSource ("Animated Image Source", Float) = 0
        _SourceUvShake ("Source UV Shake", Vector) = (0, 0, 0, 0)
        imageScrollSpeed ("imageScrollSpeed", Float) = 0.035
        imageZoomSpeed ("imageZoomSpeed", Float) = 0.08
        imageRotationSpeed ("imageRotationSpeed", Float) = 0.025
        imageChangeInterval ("imageChangeInterval", Float) = 30
        imageTransitionDuration ("imageTransitionDuration", Float) = 2
        imageTransitionMode ("imageTransitionMode", Float) = 0
        _ImageSourceOffset ("Image Source Offset", Vector) = (0, 0, 0, 0)
        _ImageSourceZoom ("Image Source Zoom", Float) = 1
        _ImageSourceRotation ("Image Source Rotation", Float) = 0
        _ImageTransitionProgress ("Image Transition Progress", Float) = 0
        _ImageMobiusDrift ("Image Mobius Drift", Float) = 0.018
    }

    SubShader
    {
        Tags
        {
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
            sampler2D _SourceTexB;

            float _PreviewSource;
            float _RenderPixelationFactor;
            float colorDepthMode;
            float colorSteps;
            float paletteQuantizationStrength;
            float _SegmentCount;
            float _Rotation;
            float _Zoom;
            float4 _CenterOffset;
            float _RadialDistortion;
            float _EdgeSoftness;
            float _UseMirrorAngleMode;
            float _MirrorAngleDegrees;
            float _PrismMode;
            float _SeamSoftness;
            float _SeamAlignmentOffset;
            float _SeamChromaticAberrationEnabled;
            float _SeamChromaticAberration;
            float _ShowSectorBoundaries;
            float4 _BoundaryDebugColor;
            float _CenterScale;
            float _CenterBrightness;
            float _CenterVignette;
            float _CenterStabilization;
            float _CenterMaskEnabled;
            float _CenterMaskMode;
            float _CenterMaskRadius;
            float _CenterExposure;
            float _CenterFalloff;
            float _CenterContrast;
            float _CenterGradientStrength;
            float _CenterDetailBoost;
            float _CenterBloomLimit;
            float _CenterCleanEnabled;
            float _CenterCleanRadius;
            float _CenterCleanFeather;
            float _CenterReconstructFromTexture;
            float _CenterPatternContinuation;
            float _CenterFillMode;
            float _CenterWorkRadius;
            float _CenterWorkFeather;
            float _CenterBlendStrength;
            float _CenterContinuationStrength;
            float _CenterDetailAmount;
            float _CenterSampleScale;
            float _CenterReconstructionQuality;
            float _CenterOnlyDebugMode;
            float _CenterMaskPreview;
            float _PhysicalCenterArtifacts;
            float _CenterArtifactOverrideEnabled;
            float _CenterArtifactOverrideRadius;
            float _CenterArtifactOverrideMode;
            float _OpticalDensity;
            float _VisualNoiseAmount;
            float _ForegroundWeight;
            float _MidgroundWeight;
            float _BackgroundWeight;
            float _DepthFadeStrength;
            float _OpticalDepthStrength;
            float _SourceOverscanFactor;
            float _EdgeRecursionBlend;
            float _CenterConvergenceStrength;
            float _RadialContinuation;
            float _CenterRecursionBlend;
            float _InnerPatternPropagation;
            float _SeamSmoothingQuality;
            float _OpticalDistortionQuality;
            float _SeamBlendStrength;
            float _SeamFeatherWidth;
            float _ContinuityCorrection;
            float _RadialEdgeSoftness;
            float _SeamAntialiasingEnabled;
            float _SeamAAWidth;
            float _SeamLineSuppression;
            float _HighSpeedSpinAAEnabled;
            float _HighSpeedSeamSoftening;
            float _MaskEnabled;
            float _MaskMode;
            float _MaskRadius;
            float _MaskSoftness;
            float _MaskDarkness;
            float _HexMaskRotation;
            float _VignetteEnabled;
            float _VignetteStrength;
            float _VignetteSoftness;
            float _EdgeDarkening;
            float _OpticalMaskFeather;
            float _LensImperfectionStrength;
            float _RubyWeight;
            float _EmeraldWeight;
            float _OpalWeight;
            float _QuartzWeight;
            float _SaturationCompression;
            float4 _HighlightColorBias;
            float _Brightness;
            float _Contrast;
            float _Saturation;
            float _Vibrance;
            float _Gamma;
            float _BlackLevel;
            float _WhiteLevel;
            float _Sharpness;
            float _OrganicTime;
            float _WobbleEnabled;
            float _WobbleStrength;
            float _WobbleSpeed;
            float _BreathingEnabled;
            float _BreathingAmplitude;
            float _BreathingSpeed;
            float _CenterDriftEnabled;
            float _CenterDriftStrength;
            float _CenterDriftSpeed;
            float _SegmentVariationEnabled;
            float _SegmentAngleVariation;
            float _SegmentBrightnessVariation;
            float _TemporalDriftEnabled;
            float _DriftSpeed;
            float _DriftAmount;
            float _AsymmetryEnabled;
            float _AsymmetryStrength;
            float _TemporalDriftAmount;
            float _RotationalDrift;
            float _ScaleDrift;
            float _OpticalBreathingAmount;
            float _DirtyGlassEnabled;
            float _DirtyGlassStrength;
            float _DirtyGlassScale;
            float _DirectTextureSource;
            float _AnimatedImageSource;
            float4 _SourceUvShake;
            float imageScrollSpeed;
            float imageZoomSpeed;
            float imageRotationSpeed;
            float imageChangeInterval;
            float imageTransitionDuration;
            float imageTransitionMode;
            float4 _ImageSourceOffset;
            float _ImageSourceZoom;
            float _ImageSourceRotation;
            float _ImageTransitionProgress;
            float _ImageMobiusDrift;

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
                color = max(0.0, color - _BlackLevel);
                color /= max(0.001, _WhiteLevel - _BlackLevel);
                color *= _Brightness;
                float gammaValue = max(0.01, _Gamma);
                color = pow(max(0.0, color), float3(gammaValue, gammaValue, gammaValue));
                color = (color - 0.5) * _Contrast + 0.5;
                float luminance = dot(color, float3(0.2126, 0.7152, 0.0722));
                float maxChannel = max(color.r, max(color.g, color.b));
                float minChannel = min(color.r, min(color.g, color.b));
                float chroma = saturate(maxChannel - minChannel);
                float vibranceBoost = 1.0 + max(0.0, _Vibrance - 1.0) * (1.0 - chroma);
                color = lerp(luminance.xxx, color, _Saturation * vibranceBoost);
                color = lerp(color, luminance.xxx + (color - luminance.xxx) * (1.0 + _Sharpness), saturate(_Sharpness));
                return saturate(color);
            }

            float Hash11(float value)
            {
                return frac(sin(value * 127.1 + 19.19) * 43758.5453);
            }

            float Hash21(float2 value)
            {
                return frac(sin(dot(value, float2(127.1, 311.7))) * 43758.5453);
            }

            float2 Rotate2D(float2 value, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                return float2(value.x * c - value.y * s, value.x * s + value.y * c);
            }

            float2 MirrorRepeatUV(float2 uv)
            {
                return 1.0 - abs(frac(uv) * 2.0 - 1.0);
            }

            float2 TransformDirectImageUV(float2 uv, float transitionOffset)
            {
                float2 centered = uv - 0.5;
                float mobius = sin((centered.x - centered.y) * 9.0 + _OrganicTime * 0.37) * _ImageMobiusDrift;
                centered += float2(centered.y, centered.x) * mobius;
                centered = Rotate2D(centered, _ImageSourceRotation);
                centered /= max(0.001, _ImageSourceZoom);
                return centered + 0.5 + _ImageSourceOffset.xy + float2(0.0, transitionOffset);
            }

            float4 SampleDirectImageSource(float2 uv)
            {
                float progress = saturate(_ImageTransitionProgress);
                float easedProgress = smoothstep(0.0, 1.0, progress);
                float mode = imageTransitionMode;

                float2 uvA = TransformDirectImageUV(uv, 0.0);
                float2 uvB = TransformDirectImageUV(uv, 0.0);

                if (mode >= 1.5 && mode < 2.5)
                {
                    uvA = TransformDirectImageUV(uv, easedProgress);
                    uvB = TransformDirectImageUV(uv, easedProgress - 1.0);
                }
                else if (mode >= 2.5)
                {
                    float roll = easedProgress * 0.5;
                    uvA = TransformDirectImageUV(uv, roll);
                    uvB = TransformDirectImageUV(uv, roll - 0.5);
                }

                float4 a = tex2D(_SourceTex, MirrorRepeatUV(uvA));
                if (progress <= 0.0001)
                {
                    return a;
                }

                float4 b = tex2D(_SourceTexB, MirrorRepeatUV(uvB));
                if (mode >= 0.5 && mode < 1.5)
                {
                    float dissolve = Hash21(floor(uv * 260.0));
                    float dissolveMask = smoothstep(easedProgress - 0.08, easedProgress + 0.08, dissolve);
                    return lerp(a, b, dissolveMask);
                }

                return lerp(a, b, easedProgress);
            }

            float4 SampleSource(float2 uv)
            {
                uv += _SourceUvShake.xy;
                float pixelation = max(1.0, _RenderPixelationFactor);
                if (pixelation > 1.001)
                {
                    float2 pixelGrid = max(float2(16.0, 16.0), _ScreenParams.xy / pixelation);
                    uv = (floor(uv * pixelGrid) + 0.5) / pixelGrid;
                }

                if (_DirectTextureSource > 0.5 && _AnimatedImageSource > 0.5)
                {
                    return SampleDirectImageSource(uv);
                }

                float2 centered = uv - 0.5;
                float radius = length(centered);
                float2 direction = normalize(centered + float2(0.0001, 0.0001));
                float2 overscannedUv = 0.5 + centered / max(1.0, _SourceOverscanFactor);
                float2 mirroredUv = MirrorRepeatUV(uv);
                float2 inwardUv = 0.5 + direction * min(radius, 0.48) * 0.72;
                float2 recursiveUv = lerp(mirroredUv, inwardUv, 0.38);
                float edgeBlend = smoothstep(0.44, 0.72, radius) * _EdgeRecursionBlend;
                float2 finalUv = lerp(saturate(overscannedUv), recursiveUv, edgeBlend);
                return tex2D(_SourceTex, saturate(finalUv));
            }

            float3 ApplyColorDepthQuantization(float3 color)
            {
                float strength = saturate(paletteQuantizationStrength);
                if (strength <= 0.0001 || colorDepthMode >= 10.0)
                {
                    return color;
                }

                float steps = max(2.0, colorSteps);
                float3 sourceColor = saturate(color);
                float luminance = dot(sourceColor, float3(0.2126, 0.7152, 0.0722));
                float3 quantized;

                if (steps <= 2.5)
                {
                    quantized = luminance >= 0.5
                        ? float3(0.96, 0.92, 0.82)
                        : float3(0.025, 0.028, 0.034);
                }
                else
                {
                    float channelLevels = max(2.0, floor(pow(steps, 0.3333333) + 0.5));
                    quantized = floor(sourceColor * (channelLevels - 1.0) + 0.5) / (channelLevels - 1.0);

                    if (steps <= 16.5)
                    {
                        float lumaLevels = max(2.0, steps);
                        float lumaQuantized = floor(luminance * (lumaLevels - 1.0) + 0.5) / (lumaLevels - 1.0);
                        float chromaPreservation = saturate((steps - 2.0) / 14.0);
                        quantized = lerp(lumaQuantized.xxx, quantized, chromaPreservation);
                    }
                }

                return lerp(color, quantized, strength);
            }

            float3 ApplyPaletteHierarchy(float3 color)
            {
                float luminance = max(0.0001, dot(color, float3(0.2126, 0.7152, 0.0722)));
                float maxChannel = max(color.r, max(color.g, color.b));
                float minChannel = min(color.r, min(color.g, color.b));
                float saturation = saturate((maxChannel - minChannel) / max(0.0001, maxChannel));

                float ruby = saturate((color.r - max(color.g, color.b)) * 2.6);
                float emerald = saturate((color.g - max(color.r, color.b)) * 2.2);
                float opal = saturate((1.0 - saturation) * smoothstep(0.35, 0.9, luminance));
                float quartz = saturate((1.0 - saturation) * smoothstep(0.68, 1.15, luminance));

                float paletteWeight =
                    ruby * (_RubyWeight - 1.0) +
                    emerald * (_EmeraldWeight - 1.0) +
                    opal * (_OpalWeight - 1.0) +
                    quartz * (_QuartzWeight - 1.0);
                color *= max(0.0, 1.0 + paletteWeight);

                float compressedSaturation = saturation / (1.0 + saturation * _SaturationCompression);
                float3 normalizedColor = lerp(luminance.xxx, color, saturate(compressedSaturation / max(0.0001, saturation)));
                float highlightMask = smoothstep(0.72, 1.12, luminance);
                normalizedColor = lerp(normalizedColor, normalizedColor * _HighlightColorBias.rgb, highlightMask * 0.18);
                return max(0.0, normalizedColor);
            }

            float3 ApplyDirtyGlass(float3 color, float2 uv)
            {
                if (_DirtyGlassEnabled < 0.5 || _DirtyGlassStrength <= 0.0)
                {
                    return color;
                }

                float strength = _DirtyGlassStrength + _LensImperfectionStrength;
                float2 glassUv = uv * max(1.0, _DirtyGlassScale);
                float2 cell = floor(glassUv);
                float cellSeed = dot(cell, float2(1.0, 57.0));
                float dust = smoothstep(0.87, 1.0, Hash11(cellSeed));
                float grain = Hash11(dot(floor(glassUv * 2.7), float2(13.0, 97.0))) * 0.5;
                float smudge = sin(uv.x * 18.7 + uv.y * 11.3 + _OrganicTime * 0.015) * 0.5 + 0.5;
                float glassMask = dust * 0.7 + grain * 0.08 + smudge * 0.12;
                return color * (1.0 - saturate(glassMask * strength));
            }

            float OpticalMask(float2 uv)
            {
                if (_MaskEnabled < 0.5 || _MaskMode < 0.5)
                {
                    return 1.0;
                }

                float2 centeredUv = uv - 0.5;
                float radius = length(centeredUv);
                float softness = _MaskSoftness + _OpticalMaskFeather;
                if (_MaskMode < 1.5)
                {
                    return 1.0 - smoothstep(_MaskRadius - softness, _MaskRadius, radius);
                }

                if (_MaskMode < 2.5)
                {
                    float2 p = abs(Rotate2D(centeredUv, _HexMaskRotation));
                    float hexDistance = max(p.y, p.x * 0.8660254 + p.y * 0.5);
                    return 1.0 - smoothstep(_MaskRadius - softness, _MaskRadius, hexDistance);
                }

                return 1.0 - smoothstep(_MaskRadius - softness, _MaskRadius, radius) * _MaskDarkness;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                if (_PreviewSource > 0.5)
                {
                    float4 preview = tex2D(_SourceTex, input.uv);
                    preview.rgb = ApplyColor(preview.rgb);
                    preview.a = 1.0;
                    return preview;
                }

                const float TwoPi = 6.28318530718;
                float angleDrivenSegments = round(360.0 / max(1.0, _MirrorAngleDegrees));
                float segments = _UseMirrorAngleMode > 0.5 ? angleDrivenSegments : round(_SegmentCount);
                segments = clamp(segments, 1.0, 64.0);
                float wedge = TwoPi / segments;
                float organicTime = _OrganicTime;
                float centerMaskActive = (_CenterMaskEnabled > 0.5 && _DirectTextureSource < 0.5) ? 1.0 : 0.0;
                float effectiveCenterMaskRadius = lerp(0.0001, max(0.0001, _CenterMaskRadius), centerMaskActive);

                float2 organicCenter = _CenterOffset.xy;
                if (_CenterDriftEnabled > 0.5)
                {
                    float driftTime = organicTime * _CenterDriftSpeed;
                    organicCenter += float2(
                        sin(driftTime * 1.31 + 0.7),
                        cos(driftTime * 1.07 + 2.1)) * _CenterDriftStrength;
                }

                float breathing = 1.0;
                if (_BreathingEnabled > 0.5)
                {
                    breathing += sin(organicTime * _BreathingSpeed * TwoPi) * _BreathingAmplitude;
                }
                breathing += sin(organicTime * 0.19 * TwoPi + 1.7) * _OpticalBreathingAmount;

                float2 centered = (input.uv - 0.5 - organicCenter) / max(0.001, _Zoom * breathing);
                float radius = length(centered);
                float screenRadius = length(input.uv - 0.5);
                float centerInfluence = centerMaskActive * (1.0 - smoothstep(effectiveCenterMaskRadius * 0.35, effectiveCenterMaskRadius, radius));
                float2 stabilizedCentered = (input.uv - 0.5 - _CenterOffset.xy) / max(0.001, _Zoom * breathing);
                centered = lerp(centered, stabilizedCentered, centerInfluence * _CenterStabilization);
                radius = lerp(radius, length(centered) / max(0.001, _CenterScale), centerInfluence);
                float angle = atan2(centered.y, centered.x) + _Rotation + _SeamAlignmentOffset;

                if (_WobbleEnabled > 0.5)
                {
                    float slowWobble = sin(organicTime * _WobbleSpeed + radius * 9.5);
                    float mechanicalWobble = sin(organicTime * _WobbleSpeed * 0.57 + angle * 2.0) * 0.35;
                    angle += (slowWobble + mechanicalWobble) * _WobbleStrength;
                }

                if (_TemporalDriftEnabled > 0.5)
                {
                    float driftAmount = _DriftAmount + _TemporalDriftAmount;
                    angle += sin(organicTime * _DriftSpeed + radius * 4.0) * driftAmount;
                    radius += cos(organicTime * _DriftSpeed * 0.73 + angle) * driftAmount * 0.25;
                }

                if (_AsymmetryEnabled > 0.5)
                {
                    angle += sin(organicTime * 0.17 + radius * 5.0) * _RotationalDrift;
                }

                angle = fmod(angle + TwoPi * 8.0, TwoPi);
                float sector = floor(angle / wedge);
                float localAngle = angle - sector * wedge;
                float segmentSeed = Hash11(sector + 1.0);
                float segmentSeedB = Hash11(sector + 17.0);
                float segmentSeedC = Hash11(sector + 31.0);

                bool mirrored = fmod(sector, 2.0) >= 1.0;
                if (mirrored)
                {
                    localAngle = wedge - localAngle;
                }

                if (_SegmentVariationEnabled > 0.5)
                {
                    float angleVariation = (segmentSeed - 0.5) * 2.0 * (_SegmentAngleVariation + _AsymmetryStrength);
                    localAngle = clamp(localAngle + angleVariation, 0.0, wedge);
                }

                float edgeDistance = min(localAngle, wedge - localAngle) / max(0.0001, wedge);
                float seamAA = _SeamAntialiasingEnabled > 0.5 ? _SeamAAWidth + _HighSpeedSeamSoftening : 0.0;
                float seamFeather = max(max(0.0001, _SeamSoftness + _SeamFeatherWidth + seamAA), fwidth(edgeDistance) * (2.2 + _SeamSmoothingQuality));
                float seamMask = smoothstep(0.0, seamFeather, edgeDistance);
                localAngle = lerp(wedge * 0.5, localAngle, lerp(1.0, seamMask, _SeamBlendStrength * (1.0 - _SeamLineSuppression * 0.35)));

                float normalizedAngle = localAngle / wedge;
                float foldedAngle = (normalizedAngle - 0.5) * wedge;
                float distortedRadius = radius * (1.0 + _RadialDistortion * _OpticalDistortionQuality * radius * radius);
                if (_WobbleEnabled > 0.5)
                {
                    distortedRadius += sin(organicTime * _WobbleSpeed * 0.83 + sector * 1.7) * _WobbleStrength * 0.25;
                }
                if (_AsymmetryEnabled > 0.5)
                {
                    distortedRadius *= 1.0 + (segmentSeedC - 0.5) * 2.0 * _ScaleDrift;
                }

                float2 sampleVector = float2(cos(foldedAngle), sin(foldedAngle)) * distortedRadius;
                float2 sampleUV = sampleVector + 0.5 + organicCenter;
                if (_TemporalDriftEnabled > 0.5)
                {
                    float driftAmount = _DriftAmount + _TemporalDriftAmount;
                    sampleUV += float2(
                        sin(organicTime * _DriftSpeed * 1.7 + radius * 6.0),
                        cos(organicTime * _DriftSpeed * 1.3 + angle * 2.0)) * driftAmount * 0.35;
                }

                float2 rayDirection = normalize(sampleVector + float2(0.0001, 0.0001));
                float physicalInnerContinuity = (1.0 - smoothstep(effectiveCenterMaskRadius * 0.3, effectiveCenterMaskRadius * 1.8, radius)) * _CenterConvergenceStrength;
                float innerContinuity = lerp(physicalInnerContinuity, _CenterConvergenceStrength, _DirectTextureSource);
                float continuationRadius = lerp(max(distortedRadius, effectiveCenterMaskRadius * (0.52 + _RadialContinuation)), distortedRadius, _DirectTextureSource);
                float2 continuationUV = 0.5 + rayDirection * continuationRadius + organicCenter;
                sampleUV = lerp(sampleUV, continuationUV, innerContinuity * 0.38);

                float4 color = SampleSource(sampleUV);
                float centerWorkMask = _CenterWorkRadius > 0.0001
                    ? 1.0 - smoothstep(max(0.0001, _CenterWorkRadius), max(0.0002, _CenterWorkRadius + _CenterWorkFeather), screenRadius)
                    : 0.0;
                float cleanCenterMask = (_CenterCleanEnabled > 0.5 || (_DirectTextureSource > 0.5 && _PhysicalCenterArtifacts < 0.5))
                    ? centerWorkMask
                    : 0.0;
                if (cleanCenterMask > 0.0001 && _CenterReconstructFromTexture > 0.5 && _CenterFillMode > 0.5)
                {
                    float quality = saturate(_CenterReconstructionQuality);
                    float sampleScale = max(0.25, _CenterSampleScale);
                    float continuationStrength = saturate(_CenterContinuationStrength);
                    float reconstructRadius = max(distortedRadius * sampleScale, _CenterWorkRadius * (0.85 + continuationStrength * sampleScale));
                    float2 reconstructUV = 0.5 + rayDirection * reconstructRadius + organicCenter;
                    float2 reconstructUVB = 0.5 + Rotate2D(rayDirection, wedge * 0.5) * reconstructRadius + organicCenter;
                    float2 reconstructUVC = 0.5 + Rotate2D(rayDirection, -wedge * 0.5) * reconstructRadius * (1.0 + 0.1 * quality) + organicCenter;
                    float2 reconstructUVD = 0.5 + rayDirection * reconstructRadius * (1.0 + 0.28 * quality) + organicCenter;
                    float3 sampledFill = SampleSource(sampleUV).rgb;
                    float3 radialFill = SampleSource(reconstructUV).rgb;
                    float3 mirrorFill = lerp(radialFill, SampleSource(reconstructUVB).rgb, 0.5);
                    mirrorFill = lerp(mirrorFill, SampleSource(reconstructUVC).rgb, quality * 0.35);
                    float3 sourceResampleFill = lerp(mirrorFill, SampleSource(reconstructUVD).rgb, quality * 0.45);
                    float3 softBlendFill = lerp(sampledFill, sourceResampleFill, saturate(0.45 + quality * 0.35));
                    float3 reconstructed = sampledFill;
                    reconstructed = _CenterFillMode >= 1.5 && _CenterFillMode < 2.5 ? mirrorFill : reconstructed;
                    reconstructed = _CenterFillMode >= 2.5 && _CenterFillMode < 3.5 ? sourceResampleFill : reconstructed;
                    reconstructed = _CenterFillMode >= 3.5 && _CenterFillMode < 4.5 ? radialFill : reconstructed;
                    reconstructed = _CenterFillMode >= 4.5 ? softBlendFill : reconstructed;
                    float continuation = saturate(lerp(_CenterPatternContinuation, 1.0, quality * 0.2));
                    float centerRepairWeight = saturate(cleanCenterMask * continuation * _CenterBlendStrength * lerp(0.65, 1.0, _CenterDetailAmount));
                    color.rgb = lerp(color.rgb, reconstructed, centerRepairWeight);
                }
                if (_CenterMaskPreview > 0.5)
                {
                    float3 previewColor = lerp(color.rgb, float3(0.1, 0.75, 1.0), centerWorkMask * 0.72);
                    color.rgb = _CenterOnlyDebugMode > 0.5 ? lerp(color.rgb, previewColor, saturate(centerWorkMask + 0.12)) : previewColor;
                }
                if (_CenterArtifactOverrideEnabled > 0.5 && _DirectTextureSource > 0.5)
                {
                    float overrideMask = 1.0 - smoothstep(
                        max(0.0001, _CenterArtifactOverrideRadius),
                        max(0.0002, _CenterArtifactOverrideRadius + _CenterCleanFeather),
                        screenRadius);
                    overrideMask *= centerWorkMask;
                    float overrideRadius = max(distortedRadius, _CenterArtifactOverrideRadius * 1.05);
                    float3 cleanSource = SampleSource(0.5 + rayDirection * overrideRadius + organicCenter).rgb;
                    color.rgb = lerp(color.rgb, cleanSource, overrideMask);
                }
                float sourceEdgeMask = smoothstep(0.42, 0.72, length(sampleUV - 0.5)) * _EdgeRecursionBlend;
                if (sourceEdgeMask > 0.0001)
                {
                    float2 inwardEdgeUV = 0.5 + (sampleUV - 0.5) * 0.58;
                    float2 rotatedEdgeUV = 0.5 + Rotate2D(sampleUV - 0.5, wedge * 0.5) * 0.64;
                    float3 edgeFill = lerp(SampleSource(inwardEdgeUV).rgb, SampleSource(rotatedEdgeUV).rgb, 0.35);
                    color.rgb = lerp(color.rgb, edgeFill, sourceEdgeMask * 0.45);
                }

                float density = saturate(_OpticalDensity);
                float2 tangent = normalize(float2(-sin(foldedAngle), cos(foldedAngle)) + float2(0.0001, 0.0001));
                float2 radial = normalize(sampleVector + float2(0.0001, 0.0001));
                float jitter = Hash21(float2(sector, floor(radius * 48.0))) - 0.5;
                float2 parallax = (radial * 0.018 + tangent * jitter * 0.014) * _OpticalDepthStrength;
                float4 foreground = SampleSource(sampleUV + parallax * 0.8);
                float4 midground = SampleSource(0.5 + (sampleUV - 0.5) * (1.0 - 0.045 * density) - tangent * 0.012 * density);
                float4 background = SampleSource(0.5 + (sampleUV - 0.5) * (1.0 + 0.075 * density) + radial * 0.01);
                float fgWeight = _ForegroundWeight * density * (1.0 - _DepthFadeStrength * radius);
                float midWeight = _MidgroundWeight * density;
                float bgWeight = _BackgroundWeight * density * (0.65 + radius * 0.5);
                float totalWeight = 1.0 + fgWeight + midWeight + bgWeight;
                color.rgb = (color.rgb + foreground.rgb * fgWeight + midground.rgb * midWeight + background.rgb * bgWeight) / max(0.0001, totalWeight);

                float centerPropagationMask = centerMaskActive * (1.0 - smoothstep(effectiveCenterMaskRadius * 0.25, effectiveCenterMaskRadius * 1.65, radius)) * _CenterRecursionBlend;
                if (centerPropagationMask > 0.0001)
                {
                    float propagationRadius = effectiveCenterMaskRadius * (0.72 + _RadialContinuation * 0.7) + radius * _InnerPatternPropagation;
                    float2 propagatedUV = 0.5 + rayDirection * propagationRadius + organicCenter;
                    float2 propagatedUVB = 0.5 + Rotate2D(rayDirection, wedge * 0.5) * propagationRadius + organicCenter;
                    float3 propagatedColor = (SampleSource(propagatedUV).rgb + SampleSource(propagatedUVB).rgb) * 0.5;
                    color.rgb = lerp(color.rgb, propagatedColor, saturate(centerPropagationMask * _InnerPatternPropagation));
                }

                if (_SeamChromaticAberrationEnabled > 0.5 && _SeamChromaticAberration > 0.0)
                {
                    float2 chromaOffset = tangent * _SeamChromaticAberration * (1.0 - seamMask);
                    float3 redSample = SampleSource(sampleUV + chromaOffset).rgb;
                    float3 blueSample = SampleSource(sampleUV - chromaOffset).rgb;
                    color.r = redSample.r;
                    color.b = blueSample.b;
                }

                float continuityBlend = (1.0 - seamMask) * _ContinuityCorrection * _SeamBlendStrength;
                if (continuityBlend > 0.0001)
                {
                    float2 continuityVector = float2(cos(0.0), sin(0.0)) * distortedRadius;
                    float2 continuityUV = continuityVector + 0.5 + organicCenter;
                    color.rgb = lerp(color.rgb, SampleSource(continuityUV).rgb, continuityBlend);
                }

                float edgeMask = smoothstep(0.0, max(0.0001, _EdgeSoftness + _RadialEdgeSoftness), edgeDistance);
                color.rgb *= lerp(0.82, 1.0, edgeMask);
                if (_SegmentVariationEnabled > 0.5)
                {
                    color.rgb *= 1.0 + (segmentSeedB - 0.5) * 2.0 * _SegmentBrightnessVariation;
                }

                color.rgb = ApplyPaletteHierarchy(color.rgb);

                float centerLift = 1.0 + centerInfluence * (_CenterBrightness - 1.0);
                float centerShade = 1.0 - centerMaskActive * smoothstep(effectiveCenterMaskRadius, effectiveCenterMaskRadius * 2.2, screenRadius) * _CenterVignette;
                color.rgb *= centerLift * centerShade;
                float centerWeight = centerMaskActive * (1.0 - smoothstep(0.0, max(0.001, _CenterFalloff), screenRadius));
                float centerNoise = Hash21(floor(input.uv * 190.0));
                float centerWave = sin(screenRadius * 58.0 - organicTime * 0.9) * 0.5 + 0.5;
                float centerDetail = (centerNoise - 0.5) * 0.5 + centerWave * 0.25;
                float centerGradient = 1.0 - smoothstep(0.02, max(0.03, _CenterFalloff), screenRadius);
                float3 centerColor = color.rgb * _CenterExposure;
                float centerLum = dot(centerColor, float3(0.2126, 0.7152, 0.0722));
                centerColor = (centerColor - centerLum) * _CenterContrast + centerLum;
                centerColor += (_HighlightColorBias.rgb - 0.5) * _CenterGradientStrength * centerGradient;
                centerColor += centerDetail.xxx * _CenterDetailBoost * centerWeight;
                centerColor = min(centerColor, float3(_CenterBloomLimit, _CenterBloomLimit, _CenterBloomLimit));
                color.rgb = lerp(color.rgb, max(0.0, centerColor), saturate(centerWeight));
                float mosaicNoise = (Hash21(floor(input.uv * lerp(90.0, 260.0, density))) - 0.5) * _VisualNoiseAmount * density;
                color.rgb += mosaicNoise.xxx;
                color.rgb = ApplyColor(color.rgb);
                color.rgb = ApplyDirtyGlass(color.rgb, input.uv);
                if (_ShowSectorBoundaries > 0.5 && edgeDistance < max(0.001, _SeamSoftness * 0.45))
                {
                    color.rgb = lerp(color.rgb, _BoundaryDebugColor.rgb, 0.85);
                }

                float mask = OpticalMask(input.uv);
                if (_VignetteEnabled > 0.5)
                {
                    float vignette = 1.0 - smoothstep(_VignetteSoftness, 0.95, screenRadius * 1.45);
                    color.rgb *= lerp(1.0 - _EdgeDarkening, 1.0, vignette);
                    color.rgb = lerp(color.rgb, color.rgb * (1.0 - _VignetteStrength), smoothstep(0.38, 0.86, screenRadius));
                }

                color.rgb = lerp(color.rgb * (1.0 - _MaskDarkness), color.rgb, mask);
                color.rgb = ApplyColorDepthQuantization(color.rgb);
                color.a = 1.0;
                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}
