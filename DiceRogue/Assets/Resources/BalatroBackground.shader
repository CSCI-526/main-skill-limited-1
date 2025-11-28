Shader "Custom/BalatroBackground"
{
    Properties
    {
        _Color1 ("Color 1", Color) = (0.4,0.7,1,1)
        _Color2 ("Color 2", Color) = (0.1,0.4,0.8,1)
        _Color3 ("Color 3", Color) = (0.8,0.9,1,1)
        _MoveSpeed ("Move Speed", Float) = 0.2
        _SpinSpeed ("Spin Speed", Float) = 0.05
        _NoiseScale ("Noise Scale", Float) = 2.0
        _DistortionAmount ("Distortion Amount", Float) = 0.1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Opaque" }

        Pass
        {
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // 关键：引入 UnityCG，里面有 UnityObjectToClipPos
            #include "UnityCG.cginc"

            float4 _Color1;
            float4 _Color2;
            float4 _Color3;
            float _MoveSpeed;
            float _SpinSpeed;
            float _NoiseScale;
            float _DistortionAmount;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            // --- 简单 hash 噪声 ---
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));

                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp( lerp(a, b, u.x), lerp(c, d, u.x), u.y );
            }

            float2 rotate(float2 uv, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                return float2(
                    c * uv.x - s * uv.y,
                    s * uv.x + c * uv.y
                );
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5;
                float t = _Time.y;

                // 旋转
                uv = rotate(uv, t * _SpinSpeed);

                // 噪声 + 流动
                float2 nUV = uv * _NoiseScale + t * _MoveSpeed;
                float n = noise(nUV);

                // 轻微形变
                uv += (n - 0.5) * _DistortionAmount;

                // 颜色渐变
                float3 col = lerp(_Color1.rgb, _Color2.rgb, smoothstep(0, 1, n));
                col = lerp(col, _Color3.rgb, n * n);

                return float4(col, 1);
            }

            ENDCG
        }
    }
}
