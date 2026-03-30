// UISystem/SDFRect
// Forked from Unity UI/Default shader (Unity 6, com.unity.ugui).
// Preserves stencil, masking, and Canvas clipping.
// Adds: SDF rounded rectangle, shadow, outline, state overlay, ripple.
//
// UV channel packing (per vertex, set by SDFRectGraphic.OnPopulateMesh):
//   UV0.xy  = texture UVs (0,0)→(1,1)
//   UV0.zw  = rect half-dimensions in reference pixels (halfW, halfH)
//   UV1.x   = packed corner radii: topLeft (12-bit) | topRight (12-bit)
//   UV1.y   = packed corner radii: bottomRight (12-bit) | bottomLeft (12-bit)
//   UV2.x   = state overlay opacity  [WP-4 StateLayerController]
//   UV2.y   = ripple radius (0–1)    [WP-4 StateLayerController]

Shader "UISystem/SDFRect"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // Shadow (material-level, shared per elevation)
        _ShadowOffset ("Shadow Offset", Vector) = (0, -4, 0, 0)
        _ShadowBlur   ("Shadow Blur", Float) = 8
        _ShadowColor  ("Shadow Color", Color) = (0,0,0,0.25)
        _ShadowEnabled ("Shadow Enabled", Float) = 0

        // Outline
        _OutlineThickness ("Outline Thickness", Float) = 0
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineEnabled ("Outline Enabled", Float) = 0

        // State overlay (base; per-element opacity comes from UV2.x)
        _StateOverlayColor ("State Overlay Color", Color) = (1,1,1,0)

        // Ripple (per-element center from UV; alpha from UV2.y; color shared)
        _RippleColor ("Ripple Color", Color) = (1,1,1,0.24)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float4 texcoord : TEXCOORD0;  // xy=UV, zw=halfSize
                float4 texcoord1: TEXCOORD1;  // xy=packed corner radii
                float4 texcoord2: TEXCOORD2;  // x=overlayOpacity, y=rippleRadius
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex       : SV_POSITION;
                fixed4 color        : COLOR;
                float4 texcoord     : TEXCOORD0;  // xy=UV, zw=halfSize
                float4 texcoord1    : TEXCOORD1;  // xy=packed radii
                float4 texcoord2    : TEXCOORD2;  // x=overlayOpacity, y=rippleRadius
                float4 worldPosition: TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            float4 _ShadowOffset;
            float  _ShadowBlur;
            float4 _ShadowColor;
            float  _ShadowEnabled;

            float  _OutlineThickness;
            float4 _OutlineColor;
            float  _OutlineEnabled;

            float4 _StateOverlayColor;
            float4 _RippleColor;

            // -------------------------------------------------------
            // Unpack two 12-bit values from a single float
            // -------------------------------------------------------
            float2 UnpackRadii(float packed)
            {
                float hi = floor(packed / 4096.0);
                float lo = fmod(packed, 4096.0);
                return float2(hi, lo);
            }

            // -------------------------------------------------------
            // Inigo Quilez sdRoundedBox
            // p  = position relative to rect center
            // b  = half-size of the rect
            // r  = corner radii (x=TL, y=TR, z=BR, w=BL)
            // -------------------------------------------------------
            float sdRoundedBox(float2 p, float2 b, float4 r)
            {
                // Select radius based on quadrant
                r.xy = (p.x > 0.0) ? r.yz : r.xw;
                r.x  = (p.y > 0.0) ? r.x  : r.y;
                float2 q = abs(p) - b + r.x;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r.x;
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord  = v.texcoord;
                OUT.texcoord1 = v.texcoord1;
                OUT.texcoord2 = v.texcoord2;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // --- Unpack UV data ---
                float2 uv       = IN.texcoord.xy;
                float2 halfSize = IN.texcoord.zw;   // rect half-dimensions in ref px

                // Unpack corner radii (12-bit each)
                float2 radiiTR = UnpackRadii(IN.texcoord1.x); // x=TL, y=TR
                float2 radiiBL = UnpackRadii(IN.texcoord1.y); // x=BR, y=BL
                float4 cornerRadii = float4(radiiTR.x, radiiTR.y, radiiBL.x, radiiBL.y);

                // Clamp radii to half the shortest dimension
                float maxR = min(halfSize.x, halfSize.y);
                cornerRadii = clamp(cornerRadii, 0.0, maxR);

                // Position relative to rect center (in ref px)
                float2 pos = (uv - 0.5) * halfSize * 2.0;

                // --- Main SDF ---
                float sdf = sdRoundedBox(pos, halfSize, cornerRadii);

                // Anti-aliased fill alpha
                float fillAlpha = smoothstep(1.0, -1.0, sdf);

                // --- Sample texture ---
                half4 color = (tex2D(_MainTex, uv) + _TextureSampleAdd) * IN.color;
                color.a *= fillAlpha;

                // --- Shadow ---
                if (_ShadowEnabled > 0.5)
                {
                    float2 shadowPos = pos - _ShadowOffset.xy;
                    float shadowSDF  = sdRoundedBox(shadowPos, halfSize, cornerRadii);
                    float shadowAlpha = smoothstep(_ShadowBlur, -_ShadowBlur * 0.5, shadowSDF);
                    shadowAlpha *= _ShadowColor.a * (1.0 - fillAlpha);
                    float4 shadowContrib = float4(_ShadowColor.rgb, shadowAlpha);
                    // Composite shadow under fill
                    color = lerp(shadowContrib, color, color.a > 0.0 ? 1.0 : 0.0);
                    color.a = max(color.a, shadowAlpha);
                }

                // --- Outline ---
                if (_OutlineEnabled > 0.5 && _OutlineThickness > 0.0)
                {
                    float halfT = _OutlineThickness * 0.5;
                    float outlineMask = (abs(sdf) < halfT) ? 1.0 : 0.0;
                    outlineMask *= fillAlpha > 0.0 ? 1.0 : step(sdf, halfT);
                    color.rgb = lerp(color.rgb, _OutlineColor.rgb, outlineMask * _OutlineColor.a);
                }

                // --- State overlay (opacity from UV2.x) ---
                float overlayOpacity = IN.texcoord2.x;
                if (overlayOpacity > 0.001)
                {
                    color.rgb = lerp(color.rgb, _StateOverlayColor.rgb,
                                     overlayOpacity * _StateOverlayColor.a * fillAlpha);
                }

                // --- Ripple (radius from UV2.y; center is element center for now) ---
                float rippleRadius = IN.texcoord2.y;
                if (rippleRadius > 0.001)
                {
                    float2 rippleCenter = float2(0.0, 0.0); // element center
                    float dist = length(pos - rippleCenter);
                    float maxDist = length(halfSize);
                    float rippleMask = smoothstep(rippleRadius * maxDist + 2.0,
                                                  rippleRadius * maxDist - 2.0, dist);
                    rippleMask *= fillAlpha;
                    color.rgb = lerp(color.rgb, _RippleColor.rgb,
                                     rippleMask * _RippleColor.a);
                }

                // --- Canvas clip rect ---
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
