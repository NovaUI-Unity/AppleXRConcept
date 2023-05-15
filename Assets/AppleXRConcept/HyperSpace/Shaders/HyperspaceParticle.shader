Shader "Demo/HyperspaceParticle"
{
    Properties
    {
        _TintColor ("Color", Color) = (1, 1, 1)
        _NearCameraClipDistance ("Near Camera Clip Distance", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

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
                fixed4 color : COLOR;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _TintColor;
            float _NearCameraClipDistance;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 viewPos = UnityObjectToViewPos(v.vertex);
                float distanceToCamera = length(viewPos);
                if (distanceToCamera < _NearCameraClipDistance)
                {
                    o.vertex = 0;
                }
                else {
                    o.vertex = UnityViewToClipPos(viewPos);
                }
                
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = i.color;

                float2 uv = (2 * i.uv - 1.0);
                float2 absUV = abs(uv);
                float2 foo = smoothstep(1.0, 0, absUV);
                c.a *= foo.x * foo.y;
                return c;
            }
            ENDCG

        }
    }
}
