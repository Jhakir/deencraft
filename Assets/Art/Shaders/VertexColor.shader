// Assets/Art/Shaders/VertexColor.shader
// Simple Lambert-lit shader that reads per-vertex colour as albedo.
// Assign this to WorldMaterial — no texture needed.
Shader "DeenCraft/VertexColor"
{
    Properties
    {
        _MainTex ("Texture (optional)", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 100

        CGPROGRAM
        #pragma surface surf Lambert vertex:vert fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float4 vertColor;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.vertColor   = v.color;
            o.uv_MainTex  = v.texcoord.xy;
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo = IN.vertColor.rgb * tex.rgb;
            o.Alpha  = IN.vertColor.a   * tex.a;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
