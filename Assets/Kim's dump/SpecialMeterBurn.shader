Shader "Custom/SpecialMeterBurn"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _GradientTex ("Color Gradient", 2D) = "white" {}
        _ScrollSpeed ("Scroll Speed", Vector) = (0.5, 0.2, 0, 0)
        _BurnIntensity ("Burn Intensity", Range(0, 5)) = 1.0
        _FillAmount ("Fill Amount", Range(0, 1)) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            sampler2D _GradientTex;
            float4 _ScrollSpeed;
            float _BurnIntensity;
            float _FillAmount;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // 1. Handle the "Fill" logic (simple horizontal cutoff)
                if (i.uv.x > _FillAmount) discard;

                // 2. Animate the Noise
                float2 scrollingUV = i.uv + (_Time.y * _ScrollSpeed.xy);
                float noise = tex2D(_NoiseTex, scrollingUV).r;

                // 3. Combine Noise with the Gradient
                // We use the noise to "wiggle" the lookup on the gradient
                float gradientLookup = i.uv.x + (noise * 0.2 * _BurnIntensity);
                fixed4 col = tex2D(_GradientTex, float2(gradientLookup, 0.5));

                // 4. Add "Heat" (Brightness) based on noise
                col.rgb += noise * 0.3 * _BurnIntensity;
                
                // Keep the alpha from the original sprite if needed
                col.a = tex2D(_MainTex, i.uv).a;

                return col;
            }
            ENDCG
        }
    }
}