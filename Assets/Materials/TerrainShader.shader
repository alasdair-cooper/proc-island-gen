// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/TerrainShader"
{
    Properties
    {
        _Tess("Tessellation", Range(1,32)) = 5
        _TessDst("Tessellation Distance", Float) = 100

        _LowThreshold("Lower Threshold", Float) = 0.2
        _MidThreshold("Medium Threshold", Float) = 0.6
        _HighThreshold("Higher Threshold", Float) = 0.8

        _MainTex1("1 Base (RGB)", 2D) = "white" {}
        _DispTex1("1 Disp Texture", 2D) = "gray" {}
        _NormalMap1("1 Normalmap", 2D) = "bump" {}
        _MainTex2("2 Base (RGB)", 2D) = "white" {}
        _DispTex2("2 Disp Texture", 2D) = "gray" {}
        _NormalMap2("2 Normalmap", 2D) = "bump" {}
        _MainTex3("3 Base (RGB)", 2D) = "white" {}
        _DispTex3("3 Disp Texture", 2D) = "gray" {}
        _NormalMap3("3 Normalmap", 2D) = "bump" {}
        _MainTex4("4 Base (RGB)", 2D) = "white" {}
        _DispTex4("4 Disp Texture", 2D) = "gray" {}
        _NormalMap4("4 Normalmap", 2D) = "bump" {}

        _Disp1("1 Displacement", Range(0, 100)) = 1
        _Disp2("2 Displacement", Range(0, 100)) = 1
        _Disp3("3 Displacement", Range(0, 100)) = 1
        _Disp4("4 Displacement", Range(0, 100)) = 1

        _Tiling1("1 Tiling", Float) = 1
        _Tiling2("2 Tiling", FLoat) = 1
        _Tiling3("3 Tiling", Float) = 1
        _Tiling4("4 Tiling", Float) = 1

        _Spec("Specular", Range(0, 1.0)) = 0
        _Gloss("Gloss", Range(0, 1.0)) = 0

        _Color("Color", color) = (1,1,1,0)
        _SpecColor("Spec color", color) = (0.5,0.5,0.5,0.5)

        _VerticalScale("Vertical scale", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf BlinnPhong addshadow fullforwardshadows vertex:vert tessellate:tessDistance nolightmap
        #pragma target 4.6
        #include "Tessellation.cginc"

            // https://github.com/keijiro/NoiseShader/blob/8de41c5f3e1e088eb32811470d8af9ed6861f1c/LICENSE
            #include "Packages/jp.keijiro.noiseshader/Shader/ClassicNoise2D.hlsl"

            struct appdata {
                float4 vertex : POSITION;
                float4 tangent : TANGENT;
                float3 normal : NORMAL;
                float2 texcoord : TEXCOORD0;

                fixed4 color : COLOR;
            };

            float _Tess;
            float _TessDst;
            float _TilingAmount;

            float _LowThreshold;
            float _MidThreshold;
            float _HighThreshold;

            float _VerticalScale;

            float4 tessDistance(appdata v0, appdata v1, appdata v2) {
                float minDist = 1;
                float maxDist = _TessDst;
                return UnityDistanceBasedTess(v0.vertex, v1.vertex, v2.vertex, minDist, maxDist, _Tess);
            }

            sampler2D _DispTex1;
            sampler2D _DispTex2;
            sampler2D _DispTex3;
            sampler2D _DispTex4;

            float _Disp1;
            float _Disp2;
            float _Disp3;
            float _Disp4;

            float _Tiling1;
            float _Tiling2;
            float _Tiling3;
            float _Tiling4;

            float _Spec;
            float _Gloss;

            sampler2D _MainTex1;
            sampler2D _MainTex2;
            sampler2D _MainTex3;
            sampler2D _MainTex4;
            sampler2D _NormalMap1;
            sampler2D _NormalMap2;
            sampler2D _NormalMap3;
            sampler2D _NormalMap4;
            fixed4 _Color;

            struct Input {
                float2 uv_MainTex1;
                float2 uv_MainTex2;
                float2 uv_MainTex3;
                float2 uv_MainTex4;
                float3 worldNormal;
                float3 worldPos;
                float4 color : COLOR;
            };

            float4 _MainTex01_ST;

            void vert(inout appdata v)
            {
                float slope = 1 - normalize(v.normal).y;
                float d;

                float3 wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float2 pos = wpos.xz;

                float d1 = tex2Dlod(_DispTex1, float4(pos * (1 / _Tiling1), 0, 0)).r * _Disp1;
                float d2 = tex2Dlod(_DispTex2, float4(pos * (1/ _Tiling2), 0, 0)).r * _Disp2;
                float d3 = tex2Dlod(_DispTex3, float4(pos * (1 / _Tiling3), 0, 0)).r * _Disp3;
                float d4 = tex2Dlod(_DispTex4, float4(pos * (1 / _Tiling4), 0, 0)).r * _Disp4;

                if (slope == 0) {
                    d = d1;
                }
                else if (slope < _LowThreshold && slope > 0) {
                    d = lerp(d1, d2, slope / _LowThreshold);
                }
                else if (slope >= _LowThreshold && slope < _MidThreshold) {
                    d = lerp(d2, d3, (slope - _LowThreshold) / (_MidThreshold - _LowThreshold));
                }
                else if (slope >= _MidThreshold && slope < _HighThreshold) {
                    d = lerp(d3, d4, (slope - _MidThreshold) / (_HighThreshold - _MidThreshold));
                }
                else {
                    d = d4;
                }

                float dist = distance(wpos, _WorldSpaceCameraPos);

                v.vertex.y += v.normal * d * (1 - (min(dist, _TessDst) / _TessDst));
                v.color = float4(pos.x, pos.y, 0, slope);
                //v.vertex.y *= ((ClassicNoise(float2(v.vertex.x, v.vertex.z)) * 2) - 1) * _VerticalScale;

                //float2 worldXY = mul(unity_ObjectToWorld, v.vertex).xy;
                //v.texcoord = TRANSFORM_TEX(worldXY, _MainTex01);
            }

            void surf(Input IN, inout SurfaceOutput o) 
            {
                float slope = IN.color.w;
                float normal = 1;
                half4 c;

                float2 pos = IN.color.xy;

                float3 normal1 = UnpackNormal(tex2D(_NormalMap1, pos * (1 / _Tiling1)));
                float3 normal2 = UnpackNormal(tex2D(_NormalMap2, pos * (1 / _Tiling2)));
                float3 normal3 = UnpackNormal(tex2D(_NormalMap3, pos * (1 / _Tiling3)));
                float3 normal4 = UnpackNormal(tex2D(_NormalMap4, pos * (1 / _Tiling4)));
                //float3 normal1 = UnpackNormal(tex2D(_NormalMap1, IN.uv_MainTex1 * _Tiling1));
                //float3 normal2 = UnpackNormal(tex2D(_NormalMap1, IN.uv_MainTex2 * _Tiling2));
                //float3 normal3 = UnpackNormal(tex2D(_NormalMap1, IN.uv_MainTex3 * _Tiling3));
                //float3 normal4 = UnpackNormal(tex2D(_NormalMap1, IN.uv_MainTex4 * _Tiling4));

                half4 cMainTex1 = tex2Dlod(_MainTex1, float4(pos * (1 / _Tiling1), 0, 0));
                half4 cMainTex2 = tex2Dlod(_MainTex2, float4(pos * (1 / _Tiling2), 0, 0));
                half4 cMainTex3 = tex2Dlod(_MainTex3, float4(pos * (1 / _Tiling3), 0, 0));
                half4 cMainTex4 = tex2Dlod(_MainTex4, float4(pos * (1 / _Tiling4), 0, 0));

                if (slope == 0) {
                    c = cMainTex1;
                    normal = normal1;
                }
                else if (slope < _LowThreshold && slope > 0) {
                    c = lerp(cMainTex1, cMainTex2, slope / _LowThreshold);
                    normal = lerp(normal1, normal2, slope / _LowThreshold);
                }
                else if (slope >= _LowThreshold && slope < _MidThreshold) {
                    c = lerp(cMainTex2, cMainTex3, (slope - _LowThreshold) / (_MidThreshold - _LowThreshold));
                    normal = lerp(normal2, normal3, (slope - _LowThreshold) / (_MidThreshold - _LowThreshold));
                }
                else if (slope >= _MidThreshold && slope < _HighThreshold) {
                    c = lerp(cMainTex3, cMainTex4, (slope - _MidThreshold) / (_HighThreshold - _MidThreshold));
                    normal = lerp(normal3, normal4, (slope - _MidThreshold) / (_HighThreshold - _MidThreshold));
                }
                else {
                    c = cMainTex4;
                    normal = normal4;
                }

                c = c * _Color;
                o.Albedo = c.rgb;
                o.Specular = _Spec;
                o.Gloss = _Gloss;
                o.Normal = normal;
            }
            ENDCG
        }
        FallBack "Diffuse"
}