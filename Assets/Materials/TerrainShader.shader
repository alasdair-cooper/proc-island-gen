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

        _BottomCapThreshold("Bottom Cap Threshold", Float) = 0.8

        _LowThresholdWidth("Lower Threshold Border Width", Float) = 0.1
        _MidThresholdWidth("Medium Threshold Border Width", Float) = 0.1
        _HighThresholdWidth("Higher Threshold Border Width", Float) = 0.1

        _BottomCapThresholdWidth("Bottom Cap Threshold Border Width", Float) = 0.1

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

        _MainTexBottom("Bottom Cap Base (RGB)", 2D) = "white" {}
        _DispTexBottom("Bottom Cap Disp Texture", 2D) = "gray" {}
        _NormalMapBottom("Bottom Cap Normalmap", 2D) = "bump" {}

        _WetnessTex("Wetness (RGB)", 2D) = "white" {}
        _WetnessMagnifier("Wetness Magnifier", Float) = 1

        _Disp1("1 Displacement", Range(0, 100)) = 1
        _Disp2("2 Displacement", Range(0, 100)) = 1
        _Disp3("3 Displacement", Range(0, 100)) = 1
        _Disp4("4 Displacement", Range(0, 100)) = 1

        _DispBottom("Bottom Cap Displacement", Range(0, 100)) = 1

        _Tiling1("1 Tiling", Float) = 1
        _Tiling2("2 Tiling", FLoat) = 1
        _Tiling3("3 Tiling", Float) = 1
        _Tiling4("4 Tiling", Float) = 1

        _TilingBottom("Bottom Cap Tiling", Float) = 1

        _Spec("Specular", Range(0, 1.0)) = 0
        _Gloss("Gloss", Range(0, 1.0)) = 0

        _Color("Color", color) = (1,1,1,0)
        _SpecColor("Spec color", color) = (0.5,0.5,0.5,0.5)

        _VerticalScale("Vertical scale", Float) = 1

        _DebugMode("Debug mode", Int) = 0
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

        float _BottomCapThreshold;

        float _LowThresholdWidth;
        float _MidThresholdWidth;
        float _HighThresholdWidth;

        float _BottomCapThresholdWidth;

        float _VerticalScale;

        float _DebugMode;

        float4 tessDistance(appdata v0, appdata v1, appdata v2) {
            float minDist = 1;
            float maxDist = _TessDst;
            return UnityDistanceBasedTess(v0.vertex, v1.vertex, v2.vertex, minDist, maxDist, _Tess);
        }

        sampler2D _DispTex1;
        sampler2D _DispTex2;
        sampler2D _DispTex3;
        sampler2D _DispTex4;

        sampler2D _DispTexBottom;

        float _Disp1;
        float _Disp2;
        float _Disp3;
        float _Disp4;

        float _DispBottom;

        float _Tiling1;
        float _Tiling2;
        float _Tiling3;
        float _Tiling4;

        float _TilingBottom;

        float _Spec;
        float _Gloss;

        sampler2D _MainTex1;
        sampler2D _MainTex2;
        sampler2D _MainTex3;
        sampler2D _MainTex4;

        sampler2D _WetnessTex;

        float _WetnessMagnifier;

        sampler2D _MainTexBottom;

        sampler2D _NormalMap1;
        sampler2D _NormalMap2;
        sampler2D _NormalMap3;
        sampler2D _NormalMap4;

        sampler2D _NormalMapBottom;

        fixed4 _Color;

        struct Input {
            float2 uv_MainTex1;
            float2 uv_MainTex2;
            float2 uv_MainTex3;
            float2 uv_MainTex4;
            float2 uv_MainTexBottom;
            float3 worldNormal;
            float3 worldPos;
            float4 color : COLOR;
        };

        //float4 _MainTex01_ST;

        // Partial Derivative Blending from https://blog.selfshadow.com/publications/blending-in-detail/
        float3 normal_lerp(float3 n1, float3 n2, float t)
        {
            return normalize(float3(lerp(n1.xy / n1.z, n2.xy / n2.z, t), 1));
        }

        // Reoriented Normal Mapping from https://blog.selfshadow.com/publications/blending-in-detail/
        // n1 is the base and n2 is the detail
        float3 normal_blend(float3 n1, float3 n2)
        {
            n1 += float3(0, 0, 1);
            n2 *= float3(-1, -1, 1);
            return n1 * dot(n1, n2) / n1.z - n2;
        }

        void vert(inout appdata v)
        {
            float slope = 1 - normalize(v.normal).y;
            float d;

            float3 wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
            float2 pos = wpos.xz;

            float2 scaledPos1 = pos * (1 / _Tiling1);
            float2 scaledPos2 = pos * (1 / _Tiling2);
            float2 scaledPos3 = pos * (1 / _Tiling3);
            float2 scaledPos4 = pos * (1 / _Tiling4);

            float2 scaledPosBottom = pos * (1 / _TilingBottom);

            float d1 = lerp(-1, 1, tex2Dlod(_DispTex1, float4(scaledPos1, 0, 0)).r) * _Disp1;
            float d2 = lerp(-1, 1, tex2Dlod(_DispTex2, float4(scaledPos2, 0, 0)).r) * _Disp2;
            float d3 = lerp(-1, 1, tex2Dlod(_DispTex3, float4(scaledPos3, 0, 0)).r) * _Disp3;
            float d4 = lerp(-1, 1, tex2Dlod(_DispTex4, float4(scaledPos4, 0, 0)).r) * _Disp4;

            float dBottom = lerp(-1, 1, tex2Dlod(_DispTexBottom, float4(scaledPosBottom, 0, 0)).r) * _DispBottom;

            float _LowThresholdRadius = _LowThresholdWidth / 2;
            float _MidThresholdRadius = _MidThresholdWidth / 2;
            float _HighThresholdRadius = _HighThresholdWidth / 2;

            if (slope < _LowThreshold - _LowThresholdWidth / 2)
            {
                d = d1;
            }
            else if (slope >= _LowThreshold - _LowThresholdRadius && slope < _LowThreshold + _LowThresholdRadius)
            {
                d = lerp(d1, d2, (slope - _LowThreshold + _LowThresholdRadius) / _LowThresholdWidth);
            }
            else if (slope >= _LowThreshold + _LowThresholdRadius && slope < _MidThreshold - _MidThresholdRadius)
            {
                d = d2;
            }
            else if (slope >= _MidThreshold - _MidThresholdRadius && slope < _MidThreshold + _MidThresholdRadius)
            {
                d = lerp(d2, d3, (slope - _MidThreshold + _MidThresholdRadius) / _MidThresholdWidth);
            }
            else if (slope >= _MidThreshold + _MidThresholdRadius && slope < _HighThreshold - _HighThresholdRadius)
            {
                d = d3;
            }
            else if (slope >= _HighThreshold - _HighThresholdRadius && slope < _HighThreshold + _HighThresholdRadius)
            {
                d = lerp(d3, d4, (slope - _HighThreshold + _HighThresholdRadius) / _HighThresholdWidth);
            }
            else
            {
                d = d4;
            }

          /*  if (slope < _LowThreshold && slope > 0) 
            {
                d = lerp(d1, d2, slope / _LowThreshold);
            }
            else if (slope >= _LowThreshold && slope < _MidThreshold) 
            {
                d = lerp(d2, d3, (slope - _LowThreshold) / (_MidThreshold - _LowThreshold));
            }
            else if (slope >= _MidThreshold && slope < _HighThreshold)
            {
                d = lerp(d3, d4, (slope - _MidThreshold) / (_HighThreshold - _MidThreshold));
            }*/

            if (wpos.y >= _BottomCapThreshold - _BottomCapThresholdWidth / 2 && wpos.y < _BottomCapThreshold + _BottomCapThresholdWidth / 2)
            {
                d = lerp(dBottom, d, (wpos.y - (_BottomCapThreshold - _BottomCapThresholdWidth / 2)) / _BottomCapThresholdWidth);
            }
            else if (wpos.y < _BottomCapThreshold - _BottomCapThresholdWidth / 2)
            {
                d = dBottom;
            }

            float dist = distance(wpos, _WorldSpaceCameraPos);

            v.vertex.xyz += normalize(v.normal) * d * (1 - (min(dist, _TessDst) / _TessDst));

            v.color = float4(wpos.x, wpos.y, wpos.z, slope);
        }

        void surf(Input IN, inout SurfaceOutput o) 
        {
            float slope = IN.color.w;
            float3 normal;
            half4 c;

            float3 wpos = IN.color.xyz;
            float2 pos = wpos.xz;

            float2 scaledPos1 = pos * (1 / _Tiling1);
            float2 scaledPos2 = pos * (1 / _Tiling2);
            float2 scaledPos3 = pos * (1 / _Tiling3);
            float2 scaledPos4 = pos * (1 / _Tiling4);

            float2 scaledPosBottom = pos * (1 / _TilingBottom);

            float3 normal1 = UnpackNormal(tex2D(_NormalMap1, scaledPos1));
            float3 normal2 = UnpackNormal(tex2D(_NormalMap2, scaledPos2));
            float3 normal3 = UnpackNormal(tex2D(_NormalMap3, scaledPos3));
            float3 normal4 = UnpackNormal(tex2D(_NormalMap4, scaledPos4));

            float3 normalBottom = UnpackNormal(tex2D(_NormalMapBottom, scaledPosBottom));

            half4 cMainTex1 = tex2Dlod(_MainTex1, float4(scaledPos1, 0, 0));
            half4 cMainTex2 = tex2Dlod(_MainTex2, float4(scaledPos2, 0, 0));
            half4 cMainTex3 = tex2Dlod(_MainTex3, float4(scaledPos3, 0, 0));
            half4 cMainTex4 = tex2Dlod(_MainTex4, float4(scaledPos4, 0, 0));

            //half4 wetnessTex = tex2Dlod(_WetnessTex, float4(scaledPos4, 0, 0));

            half4 cMainTexBottom = tex2Dlod(_MainTexBottom, float4(scaledPosBottom, 0, 0));

            float _LowThresholdRadius = _LowThresholdWidth / 2;
            float _MidThresholdRadius = _MidThresholdWidth / 2;
            float _HighThresholdRadius = _HighThresholdWidth / 2;

            if (slope < _LowThreshold - _LowThresholdWidth / 2)
            {
                c = cMainTex1;
                normal = normal1;
            }
            else if (slope >= _LowThreshold - _LowThresholdRadius && slope < _LowThreshold + _LowThresholdRadius)
            {
                c = lerp(cMainTex1, cMainTex2, (slope - _LowThreshold + _LowThresholdRadius) / _LowThresholdWidth);
                normal = normal_lerp(normal1, normal2, (slope - _LowThreshold + _LowThresholdRadius) / _LowThresholdWidth);
            }
            else if (slope >= _LowThreshold + _LowThresholdRadius && slope < _MidThreshold - _MidThresholdRadius)
            {
                c = cMainTex2;
                normal = normal2;
            }
            else if (slope >= _MidThreshold - _MidThresholdRadius && slope < _MidThreshold + _MidThresholdRadius)
            {
                c = lerp(cMainTex2, cMainTex3, (slope - _MidThreshold + _MidThresholdRadius) / _MidThresholdWidth);
                normal = normal_lerp(normal2, normal3, (slope - _MidThreshold + _MidThresholdRadius) / _MidThresholdWidth);
            }
            else if (slope >= _MidThreshold + _MidThresholdRadius && slope < _HighThreshold - _HighThresholdRadius)
            {
                c = cMainTex3;
                normal = normal3;
            }
            else if (slope >= _HighThreshold - _HighThresholdRadius && slope < _HighThreshold + _HighThresholdRadius)
            {
                c = lerp(cMainTex3, cMainTex4, (slope - _HighThreshold + _HighThresholdRadius) / _HighThresholdWidth);
                normal = normal_lerp(normal3, normal4, (slope - _HighThreshold + _HighThresholdRadius) / _HighThresholdWidth);
            }
            else
            {
                c = cMainTex4;
                normal = normal4;
            }

            if (wpos.y >= _BottomCapThreshold - _BottomCapThresholdWidth / 2 && wpos.y < _BottomCapThreshold + _BottomCapThresholdWidth / 2)
            {
                c = lerp(cMainTexBottom, c, (wpos.y - _BottomCapThreshold + _BottomCapThresholdWidth / 2) / _BottomCapThresholdWidth);
                normal = normal_lerp(normalBottom, normal, (wpos.y - _BottomCapThreshold + _BottomCapThresholdWidth / 2) / _BottomCapThresholdWidth);
            }
            else if (wpos.y < _BottomCapThreshold - _BottomCapThresholdWidth / 2)
            {
                c = cMainTexBottom;
                normal = normalBottom;
            }

           /* if (slope < _LowThreshold)
            {
                c = lerp(cMainTex1, cMainTex2, slope / _LowThreshold);
                normal = normal_lerp(normal1, normal2, slope / _LowThreshold);
            }
            else if (slope >= _LowThreshold && slope < _MidThreshold)
            {
                c = lerp(cMainTex2, cMainTex3, (slope - _LowThreshold) / (_MidThreshold - _LowThreshold));
                normal = normal_lerp(normal2, normal3, (slope - _LowThreshold) / (_MidThreshold - _LowThreshold));
            }
            else if (slope >= _MidThreshold && slope < _HighThreshold)
            {
                c = lerp(cMainTex3, cMainTex4, (slope - _MidThreshold) / (_HighThreshold - _MidThreshold));
                normal = normal_lerp(normal3, normal4, (slope - _MidThreshold) / (_HighThreshold - _MidThreshold));
            }
            else {
                c = cMainTex4;
                normal = normal4;
            }*/

            normal = BlendNormals(o.Normal, normal);

            if (_DebugMode == 0)
            {
                o.Albedo = c.rgb;
            }
            else if (_DebugMode == 1)
            {
                o.Albedo = normal;
            }
            else if (_DebugMode == 2)
            {
                o.Albedo = slope;
            }

            c = c * _Color;
            o.Specular = _Spec;
            o.Gloss = _Gloss;
            //o.Normal = normal;
        }
        ENDCG
    }
    FallBack "Diffuse"
}