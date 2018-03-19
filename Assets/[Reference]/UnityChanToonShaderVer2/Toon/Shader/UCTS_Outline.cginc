// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// UCTS_Outline.cginc
// 2017/03/08 N.Kobayashi (Unity Technologies Japan)
// カメラオフセット付きアウトライン（BaseColorライトカラー反映修正版）
// 2017/06/05 PS4対応版
//
            uniform float4 _LightColor0;
            uniform float4 _BaseColor;
            uniform sampler2D _BaseMap; uniform float4 _BaseMap_ST;
            uniform float _Outline_Width;
            uniform float _Farthest_Distance;
            uniform float _Nearest_Distance;
            uniform sampler2D _Outline_Sampler; uniform float4 _Outline_Sampler_ST;
            uniform float4 _Outline_Color;
            uniform fixed _Is_BlendBaseColor;
            uniform fixed _Is_LightColor_Base;
            uniform float _Offset_Z;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                float4 objPos = mul ( unity_ObjectToWorld, float4(0,0,0,1) );
                float3 lightColor = _LightColor0.rgb;
                float2 Set_UV0 = o.uv0;
                float2 node_6326 = Set_UV0;
                float4 _Outline_Sampler_var = tex2Dlod(_Outline_Sampler,float4(TRANSFORM_TEX(node_6326, _Outline_Sampler),0.0,0));
                float Set_Outline_Width = (_Outline_Width*0.001*smoothstep( _Farthest_Distance, _Nearest_Distance, distance(objPos.rgb,_WorldSpaceCameraPos) )*_Outline_Sampler_var.rgb).r;
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - o.pos.xyz);
                float4 viewDirectionVP = mul(UNITY_MATRIX_VP, float4(viewDirection.xyz, 1));
                _Offset_Z = _Offset_Z * -0.1;
                o.pos = UnityObjectToClipPos(float4(v.vertex.xyz + v.normal*Set_Outline_Width,1) );
                o.pos.z = o.pos.z + _Offset_Z*viewDirectionVP.z;
                return o;
            }
            float4 frag(VertexOutput i, float facing : VFACE) : SV_Target{
                float isFrontFace = ( facing >= 0 ? 1 : 0 );
                float faceSign = ( facing >= 0 ? 1 : -1 );
                float4 objPos = mul ( unity_ObjectToWorld, float4(0,0,0,1) );
                float3 lightColor = _LightColor0.rgb;
                float2 Set_UV0 = i.uv0;
                float2 node_6858 = Set_UV0;
                float4 _BaseMap_var = tex2D(_BaseMap,TRANSFORM_TEX(node_6858, _BaseMap));
                float3 node_9970 = (_BaseColor.rgb*_BaseMap_var.rgb);
                float3 Set_BaseColor = lerp( node_9970, (node_9970*_LightColor0.rgb), _Is_LightColor_Base );
                float3 node_2878 = Set_BaseColor;
                float3 Set_Outline_Color = lerp( _Outline_Color.rgb, (_Outline_Color.rgb*node_2878*node_2878), _Is_BlendBaseColor );
                return fixed4(Set_Outline_Color,0);
            }
// UCTS_Outline.cginc ここまで.
