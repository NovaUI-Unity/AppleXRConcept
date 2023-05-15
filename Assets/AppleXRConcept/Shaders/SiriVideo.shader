Shader "AppleXRConcept/SiriVideo"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MinAlpha ("Min Alpha", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _MinAlpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Calculate perceived luma (approximate)
                float luma = dot(float3(0.2126, 0.7152, 0.0722), col.xyz);

                col.a *= lerp(_MinAlpha, 1, luma);

                return col;
            }
            ENDCG
        }
    }
}
