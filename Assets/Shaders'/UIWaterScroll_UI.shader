Shader "UI/WaterScroll_UI"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _WaveStrength ("Wave Strength", Float) = 0.01
        _WaveSpeed ("Wave Speed", Float) = 1
        _WaveFrequency ("Wave Frequency", Float) = 10
        _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _WaveStrength;
            float _WaveSpeed;
            float _WaveFrequency;
            fixed4 _Color;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                // Wave distortion (horizontal ripple)
                float wave = sin(o.uv.y * _WaveFrequency + _Time.y * _WaveSpeed) * _WaveStrength;
                o.uv.x += wave;

                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * i.color;
            }
            ENDCG
        }
    }
}
