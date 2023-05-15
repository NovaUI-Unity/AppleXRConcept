/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

#pragma vertex outlineVertex
#pragma fragment outlineFragment
#pragma multi_compile_local __ CONFIDENCE

#pragma prefer_hlslcc gles
#pragma exclude_renderers d3d11_9x
#pragma target 2.0

//

struct OutlineVertexInput
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 texcoord : TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct OutlineVertexOutput
{
    float4 vertex : SV_POSITION;
    half4 glowColor : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

float3 GreaterThanOrEqual(float3 lhs, float3 rhs)
{
    return step(rhs, lhs);
}

float GreaterThanOrEqual(float lhs, float rhs)
{
    return step(rhs, lhs);
}

OutlineVertexOutput outlineVertex(OutlineVertexInput v)
{
    OutlineVertexOutput o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    UNITY_TRANSFER_INSTANCE_ID(v, o);

    v.vertex.xyz += v.normal * _OutlineWidth;
    o.vertex = UnityObjectToClipPos(v.vertex);
    
    half4 maskPixelColor = tex2Dlod(_FingerGlowMask, v.texcoord);

    o.glowColor.rgb = _OutlineColor;
    o.glowColor.a = saturate(maskPixelColor.a + _WristFade) * _OutlineColor.a * _OutlineOpacity;
    o.glowColor.a *= GreaterThanOrEqual(o.glowColor.a, 0.03);

    return o;
}

half4 outlineFragment(OutlineVertexOutput i) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    return i.glowColor;
}
