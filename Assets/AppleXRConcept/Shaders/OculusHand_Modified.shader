 /************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

Shader "Interaction/OculusHand_Modified"
{
    Properties
    {
        [Header(General)]
        _MainColor("Main Color", Color) = (0.1960784, 0.2039215, 0.2117647, 1)
        _FresnelColor("Fresnel Color", Color) = (0.1215686, 0.1254902, 0.1294117, 1)
        _FresnelStrength("Fresnel Strength", Range(0 , 10)) = 0.16
        _Opacity("Opacity", Range(0 , 1)) = 0.8

        [Header(Outline)]
        _OutlineColor("Outline Color", Color) = (0.5377358,0.5377358,0.5377358,1)
        _OutlineJointColor("Outline Joint Error Color", Color) = (1,0,0,1)
        _OutlineWidth("Outline Width", Range(0 , 1)) = 0.00134
        _OutlineOpacity("Outline Opacity", Range(0 , 1)) = 0.4

        [Header(Wrist)]
        _WristFade("Wrist Fade", Range(0 , 1)) = 0.5

        [Header(Finger Glow)]
        _FingerGlowColor("Finger Glow Color", Color) = (1,1,1,1)
        _FingerGlowMask("Finger Glow Mask", 2D) = "white" {}
        [Toggle(CONFIDENCE)] _EnableConfidence("Show Low Confidence", Float) = 0

        [Header(Interaction)]
        _Proximity("Proximity", Float) = 100

        [HideInInspector] _ThumbGlowValue("Thumb Glow", Float) = 0
        [HideInInspector] _IndexGlowValue("Index Glow", Float) = 0
        [HideInInspector] _MiddleGlowValue("Middle Glow", Float) = 0
        [HideInInspector] _RingGlowValue("Ring Glow", Float) = 0
        [HideInInspector] _PinkyGlowValue("Pinky Glow", Float) = 0    

        [HideInInspector] _texcoord("", 2D) = "white" {}    
    }

    CGINCLUDE
    #include "Lighting.cginc"
    
    #pragma target 2.0
    
    // CBUFFER named UnityPerMaterial, SRP can cache the material properties between frames and reduce significantly the cost of each drawcall.
    CBUFFER_START(UnityPerMaterial)			
        // General
        uniform float4 _MainColor;
        uniform float4 _FresnelColor;
        uniform float _Opacity;
        uniform float _FresnelStrength;
    
        // Outline
        uniform float4 _OutlineColor;
        uniform half4 _OutlineJointColor;
        uniform float _OutlineWidth;
        uniform float _OutlineOpacity;
    
        // Wrist
        uniform half _WristFade;
    
        // Finger Glow
        uniform sampler2D _FingerGlowMask;
        uniform float4 _FingerGlowColor;
    
        uniform float _ThumbGlowValue;
        uniform float _IndexGlowValue;
        uniform float _MiddleGlowValue;
        uniform float _RingGlowValue;
        uniform float _PinkyGlowValue;
    
        uniform half _JointsGlow[18];

        uniform float _Proximity;
        uniform float3 _Extents;
        uniform float4x4 _WorldToLocal;
    CBUFFER_END
    
    ENDCG

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }
        
        Cull Back
        AlphaToMask Off

        Pass
        {
            Name "Depth"
            ZWrite On
            ColorMask 0             
        }
        
        Pass
        {
            PackageRequirements { "com.unity.render-pipelines.universal": "[10.8.1,10.10.0]" }
            
            Name "Outline-URP-2020"
            Tags { "LightMode" = "LightweightForward" "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
            
            Cull Front
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #include "OculusHandOutlineCG.cginc"            
            ENDCG
        }

        Pass
        {
            PackageRequirements { "com.unity.render-pipelines.universal": "12.1.7" }
            
            Name "Outline-URP-2021+"
            Tags { "LightMode" = "UniversalForwardOnly" "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
            
            Cull Front
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #include "OculusHandOutlineCG.cginc"            
            ENDCG
        }
            
        Pass
        {
            Name "Outline-DRP"
            Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }
            
            Cull Front
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #include "OculusHandOutlineCG.cginc"            
            ENDCG
        }
        
        Pass
        {
            Name "Interior-URP"
            Tags { "LightMode" = "UniversalForward" "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" "IsEmissive" = "true" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            CGPROGRAM
            #include "OculusHandInteriorCG.cginc"
            ENDCG
        } 

        Pass
        {
            Name "Interior-DRP"
            Tags { "RenderType" = "MaskedOutline" "Queue" = "Transparent" "IgnoreProjector" = "True" "IsEmissive" = "true" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            
            CGPROGRAM
            #include "OculusHandInteriorCG.cginc"
            ENDCG
        }
    }
}

