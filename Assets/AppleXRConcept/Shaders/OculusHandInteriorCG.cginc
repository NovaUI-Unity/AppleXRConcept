/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

#pragma vertex baseVertex
#pragma fragment baseFragment
#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight

#pragma prefer_hlslcc gles
#pragma exclude_renderers d3d11_9x
#pragma target 2.0

struct VertexInput
{
    float4 vertex : POSITION;
    half3 normal : NORMAL;
    half4 vertexColor : COLOR;
    float4 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 vertex : SV_POSITION;
    float4 worldPos : TEXCOORD1;
    float3 worldNormal : TEXCOORD2;
    half4 glowColor : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

float lengthsq(float3 f)
{
    return dot(f, f);
}

VertexOutput baseVertex(VertexInput v)
{
    VertexOutput o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    UNITY_TRANSFER_INSTANCE_ID(v, o);

    o.worldPos = mul(unity_ObjectToWorld, v.vertex);
    o.worldNormal = UnityObjectToWorldNormal(v.normal);
    o.vertex = UnityObjectToClipPos(v.vertex);

    half4 maskPixelColor = tex2Dlod(_FingerGlowMask, v.texcoord);
    int glowMaskR = maskPixelColor.r * 255;

    int thumbMask = (glowMaskR >> 3) & 0x1;
    int indexMask = (glowMaskR >> 4) & 0x1;
    int middleMask = (glowMaskR >> 5) & 0x1;
    int ringMask = (glowMaskR >> 6) & 0x1;
    int pinkyMask = (glowMaskR >> 7) & 0x1;

    half glowIntensity = saturate(
        maskPixelColor.g *
        (thumbMask * _ThumbGlowValue
            + indexMask * _IndexGlowValue
            + middleMask * _MiddleGlowValue
            + ringMask * _RingGlowValue
            + pinkyMask * _PinkyGlowValue));

    half4 glow = glowIntensity * _FingerGlowColor;
    o.glowColor.rgb = glow.rgb;
    o.glowColor.a = saturate(maskPixelColor.a + _WristFade) * _Opacity;
    o.glowColor.a *= step(0.03, o.glowColor.a);
    return o;
}

half4 baseFragment(VertexOutput i) : SV_Target
{
    float3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));

    float3 boundsPos = mul(_WorldToLocal, i.worldPos).xyz;
    float3 closestPoint = clamp(boundsPos, -_Extents, _Extents);
    float maxProximity = _Proximity * _Proximity;
    float normalizedDistance = clamp(lengthsq(closestPoint - boundsPos), 0, maxProximity) / maxProximity;
    float proximityStrength = 3 * (1 - normalizedDistance) + 1;

    float fresnelNdot = dot(i.worldNormal, worldViewDir);
    float fresnelBase = 1 + 0.5 * (1 - fresnelNdot);
    float fresnel = pow(fresnelBase, _FresnelStrength * proximityStrength) - 1;
    float4 color = lerp(_MainColor, _FresnelColor, fresnel);

    half3 glowColor = saturate(color + i.glowColor.rgb);
    
    return half4(glowColor, color.a * i.glowColor.a);
}
