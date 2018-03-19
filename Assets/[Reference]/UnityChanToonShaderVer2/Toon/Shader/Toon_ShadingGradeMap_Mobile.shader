// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "UnityChanToonShader/Mobile/Toon_ShadingGradeMap" {
    Properties {
        [Enum(OFF,0,FRONT,1,BACK,2)] _CullMode("Cull Mode", int) = 2  //OFF/FRONT/BACK
        _BaseMap ("BaseMap", 2D) = "white" {}
        _BaseColor ("BaseColor", Color) = (1,1,1,1)
        [MaterialToggle] _Is_LightColor_Base ("Is_LightColor_Base", Float ) = 1
        _1st_ShadeMap ("1st_ShadeMap", 2D) = "white" {}
        _1st_ShadeColor ("1st_ShadeColor", Color) = (1,1,1,1)
        [MaterialToggle] _Is_LightColor_1st_Shade ("Is_LightColor_1st_Shade", Float ) = 1
        _2nd_ShadeMap ("2nd_ShadeMap", 2D) = "white" {}
        _2nd_ShadeColor ("2nd_ShadeColor", Color) = (1,1,1,1)
        [MaterialToggle] _Is_LightColor_2nd_Shade ("Is_LightColor_2nd_Shade", Float ) = 1
        _NormalMap ("NormalMap", 2D) = "bump" {}
        [MaterialToggle] _Is_NormalMap ("Is_NormalMap", Float ) = 0
        [MaterialToggle] _Set_SystemShadowsToBase ("Set_SystemShadowsToBase", Float ) = 1
        _Tweak_SystemShadowsLevel ("Tweak_SystemShadowsLevel", Range(-0.5, 0.5)) = 0
        _ShadingGradeMap ("ShadingGradeMap", 2D) = "white" {}
        [MaterialToggle] _Is_1st_ShadeColorOnly ("Is_1st_ShadeColorOnly", Float ) = 0
        _1st_ShadeColor_Step ("1st_ShadeColor_Step", Range(0, 1)) = 0.5
        _1st_ShadeColor_Feather ("1st_ShadeColor_Feather", Range(0.0001, 1)) = 0.0001
        _2nd_ShadeColor_Step ("2nd_ShadeColor_Step", Range(0, 1)) = 0.003
        _2nd_ShadeColor_Feather ("2nd_ShadeColor_Feather", Range(0.0001, 1)) = 0.0001
        _HighColor ("HighColor", Color) = (1,1,1,1)
        [MaterialToggle] _Is_LightColor_HighColor ("Is_LightColor_HighColor", Float ) = 1
        [MaterialToggle] _Is_NormalMapToHighColor ("Is_NormalMapToHighColor", Float ) = 0
        _HighColor_Power ("HighColor_Power", Range(0, 1)) = 0
        [MaterialToggle] _Is_SpecularToHighColor ("Is_SpecularToHighColor", Float ) = 0
        [MaterialToggle] _Is_BlendAddToHiColor ("Is_BlendAddToHiColor", Float ) = 0
        [MaterialToggle] _Is_UseTweakHighColorOnShadow ("Is_UseTweakHighColorOnShadow", Float ) = 0
        _TweakHighColorOnShadow ("TweakHighColorOnShadow", Range(0, 1)) = 0
//ハイカラーマスク.
        _Set_HighColorMask ("Set_HighColorMask", 2D) = "white" {}
        _Tweak_HighColorMaskLevel ("Tweak_HighColorMaskLevel", Range(-1, 1)) = 0
        [MaterialToggle] _RimLight ("RimLight", Float ) = 0
        _RimLightColor ("RimLightColor", Color) = (1,1,1,1)
        [MaterialToggle] _Is_LightColor_RimLight ("Is_LightColor_RimLight", Float ) = 1
        [MaterialToggle] _Is_NormalMapToRimLight ("Is_NormalMapToRimLight", Float ) = 0
        _RimLight_Power ("RimLight_Power", Range(0, 1)) = 0.1
        _RimLight_InsideMask ("RimLight_InsideMask", Range(0.0001, 1)) = 0.0001
        [MaterialToggle] _RimLight_FeatherOff ("RimLight_FeatherOff", Float ) = 0
//リムライト追加プロパティ.
        [MaterialToggle] _LightDirection_MaskOn ("LightDirection_MaskOn", Float ) = 0
        _Tweak_LightDirection_MaskLevel ("Tweak_LightDirection_MaskLevel", Range(0, 0.5)) = 0
        [MaterialToggle] _Add_Antipodean_RimLight ("Add_Antipodean_RimLight", Float ) = 0
        _Ap_RimLightColor ("Ap_RimLightColor", Color) = (1,1,1,1)
        [MaterialToggle] _Is_LightColor_Ap_RimLight ("Is_LightColor_Ap_RimLight", Float ) = 1
        _Ap_RimLight_Power ("Ap_RimLight_Power", Range(0, 1)) = 0.1
        [MaterialToggle] _Ap_RimLight_FeatherOff ("Ap_RimLight_FeatherOff", Float ) = 0
//リムライトマスク.
        _Set_RimLightMask ("Set_RimLightMask", 2D) = "white" {}
        _Tweak_RimLightMaskLevel ("Tweak_RimLightMaskLevel", Range(-1, 1)) = 0
//ここまで.
        [MaterialToggle] _MatCap ("MatCap", Float ) = 0
        _MatCap_Sampler ("MatCap_Sampler", 2D) = "black" {}
        _MatCapColor ("MatCapColor", Color) = (1,1,1,1)
        [MaterialToggle] _Is_LightColor_MatCap ("Is_LightColor_MatCap", Float ) = 1
        [MaterialToggle] _Is_BlendAddToMatCap ("Is_BlendAddToMatCap", Float ) = 1
        _Tweak_MatCapUV ("Tweak_MatCapUV", Range(-0.5, 0.5)) = 0
//        _Rotate_MatCapUV ("Rotate_MatCapUV", Range(-1, 1)) = 0
//        [MaterialToggle] _Is_NormalMapForMatCap ("Is_NormalMapForMatCap", Float ) = 0
//        _NormalMapForMatCap ("NormalMapForMatCap", 2D) = "bump" {}
//        _Rotate_NormalMapForMatCapUV ("Rotate_NormalMapForMatCapUV", Range(-1, 1)) = 0
        [MaterialToggle] _Is_UseTweakMatCapOnShadow ("Is_UseTweakMatCapOnShadow", Float ) = 0
        _TweakMatCapOnShadow ("TweakMatCapOnShadow", Range(0, 1)) = 0
        _Outline_Width ("Outline_Width", Float ) = 1
        _Farthest_Distance ("Farthest_Distance", Float ) = 10
        _Nearest_Distance ("Nearest_Distance", Float ) = 0.5
        _Outline_Sampler ("Outline_Sampler", 2D) = "white" {}
        _Outline_Color ("Outline_Color", Color) = (0.5,0.5,0.5,1)
        [MaterialToggle] _Is_BrendBaseColor ("Is_BrendBaseColor", Float ) = 0
        //Offset parameter
        _Offset_Z ("Offset_Camera_Z", Float) = 0
        _GI_Intensity ("GI_Intensity", Range(0, 1)) = 0
    }
    SubShader {
        Tags {
            "RenderType"="Opaque"
        }
        Pass {
            Name "Outline"
            Tags {
            }
            Cull Front
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #pragma fragmentoption ARB_precision_hint_fastest
            //#pragma multi_compile_shadowcaster
            //#pragma multi_compile_fog
            #pragma only_renderers d3d9 d3d11 glcore gles gles3 metal xboxone ps4 switch
            #pragma target 3.0
            //アウトライン処理は以下のcgincへ.
            #include "UCTS_Outline.cginc"
            ENDCG
        }
//ToonCoreStart
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            Cull[_CullMode]
            
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma multi_compile_fog
            #pragma only_renderers d3d9 d3d11 glcore gles gles3 metal xboxone ps4 switch
            #pragma target 3.0
            uniform sampler2D _BaseMap; uniform float4 _BaseMap_ST;
            uniform sampler2D _1st_ShadeMap; uniform float4 _1st_ShadeMap_ST;
            uniform sampler2D _NormalMap; uniform float4 _NormalMap_ST;
            uniform sampler2D _ShadingGradeMap; uniform float4 _ShadingGradeMap_ST;
            uniform float4 _BaseColor;
            uniform float4 _1st_ShadeColor;
            uniform float _1st_ShadeColor_Step;
            uniform fixed _Is_NormalMap;
            uniform fixed _Set_SystemShadowsToBase;
            uniform float4 _2nd_ShadeColor;
            uniform float _2nd_ShadeColor_Step;
            uniform sampler2D _2nd_ShadeMap; uniform float4 _2nd_ShadeMap_ST;
            uniform float _1st_ShadeColor_Feather;
            uniform float _2nd_ShadeColor_Feather;
            uniform fixed _Is_1st_ShadeColorOnly;
            uniform fixed _Is_NormalMapToHighColor;
            uniform float _HighColor_Power;
            uniform fixed _Is_SpecularToHighColor;
            uniform sampler2D _Set_HighColorMask; uniform float4 _Set_HighColorMask_ST;
            uniform float _TweakHighColorOnShadow;
            uniform float4 _HighColor;
            uniform fixed _Is_UseTweakHighColorOnShadow;
            uniform fixed _Is_BlendAddToHiColor;
            uniform fixed _Is_NormalMapToRimLight;
            uniform float _RimLight_Power;
            uniform float4 _RimLightColor;
            uniform fixed _RimLight;
            uniform float _Tweak_MatCapUV;
            uniform sampler2D _MatCap_Sampler; uniform float4 _MatCap_Sampler_ST;
            uniform float4 _MatCapColor;
            uniform float _TweakMatCapOnShadow;
            uniform fixed _Is_UseTweakMatCapOnShadow;
            uniform fixed _Is_BlendAddToMatCap;
            uniform fixed _MatCap;
            fixed3 DecodeLightProbe( fixed3 N ){
            return ShadeSH9(float4(N,1));
            }
            
            uniform float _GI_Intensity;
            uniform fixed _Is_LightColor_Base;
            uniform fixed _Is_LightColor_1st_Shade;
            uniform fixed _Is_LightColor_2nd_Shade;
            uniform fixed _Is_LightColor_HighColor;
            uniform fixed _Is_LightColor_RimLight;
            uniform fixed _Is_LightColor_MatCap;
            uniform float _Tweak_SystemShadowsLevel;
            uniform float _RimLight_InsideMask;
            uniform fixed _RimLight_FeatherOff;
            uniform fixed _LightDirection_MaskOn;
            uniform float _Tweak_LightDirection_MaskLevel;
            uniform fixed _Add_Antipodean_RimLight;
            uniform float4 _Ap_RimLightColor;
            uniform fixed _Is_LightColor_Ap_RimLight;
            uniform float _Ap_RimLight_Power;
            uniform fixed _Ap_RimLight_FeatherOff;
            uniform sampler2D _Set_RimLightMask; uniform float4 _Set_RimLightMask_ST;
            uniform float _Tweak_HighColorMaskLevel;
            uniform float _Tweak_RimLightMaskLevel;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                float3 tangentDir : TEXCOORD3;
                float3 bitangentDir : TEXCOORD4;
                LIGHTING_COORDS(5,6)
                UNITY_FOG_COORDS(7)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                o.tangentDir = normalize( mul( unity_ObjectToWorld, float4( v.tangent.xyz, 0.0 ) ).xyz );
                o.bitangentDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                float3 lightColor = _LightColor0.rgb;
                o.pos = UnityObjectToClipPos(v.vertex );
                UNITY_TRANSFER_FOG(o,o.pos);
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            float4 frag(VertexOutput i, float facing : VFACE) : COLOR {
                float isFrontFace = ( facing >= 0 ? 1 : 0 );
                float faceSign = ( facing >= 0 ? 1 : -1 );
                i.normalDir = normalize(i.normalDir);
                i.normalDir *= faceSign;
                float3x3 tangentTransform = float3x3( i.tangentDir, i.bitangentDir, i.normalDir);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float2 Set_UV0 = i.uv0;
                float2 node_8690 = Set_UV0;
                float3 _NormalMap_var = UnpackNormal(tex2D(_NormalMap,TRANSFORM_TEX(node_8690, _NormalMap)));
                float3 normalLocal = _NormalMap_var.rgb;
                float3 normalDirection = normalize(mul( normalLocal, tangentTransform )); // Perturbed normals
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                float3 lightColor = _LightColor0.rgb;
                float3 halfDirection = normalize(viewDirection+lightDirection);
////// Lighting:
                float attenuation = LIGHT_ATTENUATION(i);
                float2 node_4128 = Set_UV0;
                float4 _BaseMap_var = tex2D(_BaseMap,TRANSFORM_TEX(node_4128, _BaseMap));
                float3 node_482 = (_BaseMap_var.rgb*_BaseColor.rgb);
                float3 Set_LightColor = _LightColor0.rgb;
                float3 node_347 = Set_LightColor;
                float3 Set_BaseColor = lerp( node_482, (node_482*node_347), _Is_LightColor_Base );
                float4 _1st_ShadeMap_var = tex2D(_1st_ShadeMap,TRANSFORM_TEX(node_4128, _1st_ShadeMap));
                float3 node_7531 = (_1st_ShadeMap_var.rgb*_1st_ShadeColor.rgb);
                float3 _Is_LightColor_1st_Shade_var = lerp( node_7531, (node_7531*node_347), _Is_LightColor_1st_Shade );
                float2 node_9116 = Set_UV0;
                float4 _ShadingGradeMap_var = tex2D(_ShadingGradeMap,TRANSFORM_TEX(node_9116, _ShadingGradeMap));
                float node_6928 = 0.5*dot(lerp( i.normalDir, normalDirection, _Is_NormalMap ),lightDirection)+0.5; // Half Lambert
                float node_4711 = 0.5;
                float Set_ShadingGrade = (_ShadingGradeMap_var.r*lerp( node_6928, (node_6928*saturate(((attenuation*node_4711)+node_4711+_Tweak_SystemShadowsLevel))), _Set_SystemShadowsToBase ));
                float node_5830 = (_1st_ShadeColor_Step-_1st_ShadeColor_Feather);
                float node_9557 = 1.0;
                float Set_FinalShadowMask = saturate((node_9557 + ( (Set_ShadingGrade - node_5830) * (0.0 - node_9557) ) / (_1st_ShadeColor_Step - node_5830))); // Base and 1st Shade Mask
                float3 node_4770 = lerp(Set_BaseColor,_Is_LightColor_1st_Shade_var,Set_FinalShadowMask);
                float4 _2nd_ShadeMap_var = tex2D(_2nd_ShadeMap,TRANSFORM_TEX(node_4128, _2nd_ShadeMap));
                float3 node_5308 = (_2nd_ShadeMap_var.rgb*_2nd_ShadeColor.rgb);
                float node_7994 = (_2nd_ShadeColor_Step-_2nd_ShadeColor_Feather);
                float node_4856 = 1.0;
                float Set_ShadeShadowMask = saturate((node_4856 + ( (Set_ShadingGrade - node_7994) * (0.0 - node_4856) ) / (_2nd_ShadeColor_Step - node_7994))); // 1st and 2nd Shades Mask
                float3 Set_FinalBaseColor = lerp( lerp(node_4770,lerp(_Is_LightColor_1st_Shade_var,lerp( node_5308, (node_5308*node_347), _Is_LightColor_2nd_Shade ),Set_ShadeShadowMask),Set_FinalShadowMask), node_4770, _Is_1st_ShadeColorOnly );
                float3 node_3441 = Set_FinalBaseColor;
                float2 node_7302 = Set_UV0;
                float4 _Set_HighColorMask_var = tex2D(_Set_HighColorMask,TRANSFORM_TEX(node_7302, _Set_HighColorMask));
                float node_6430 = 0.5*dot(halfDirection,lerp( i.normalDir, normalDirection, _Is_NormalMapToHighColor ))+0.5; // Specular
                float node_5827 = (saturate((_Set_HighColorMask_var.g+_Tweak_HighColorMaskLevel))*lerp( (1.0 - step(node_6430,(1.0 - _HighColor_Power))), pow(node_6430,exp2(lerp(11,1,_HighColor_Power))), _Is_SpecularToHighColor ));
                float3 node_2397 = (lerp( _HighColor.rgb, (_HighColor.rgb*Set_LightColor), _Is_LightColor_HighColor )*node_5827);
                float node_7081 = Set_FinalShadowMask;
                float3 Set_HighColor = (lerp( saturate((node_3441-node_5827)), node_3441, _Is_BlendAddToHiColor )+lerp( node_2397, (node_2397*((1.0 - node_7081)+(node_7081*_TweakHighColorOnShadow))), _Is_UseTweakHighColorOnShadow ));
                float3 node_3159 = Set_HighColor;
                float2 node_6650 = Set_UV0;
                float4 _Set_RimLightMask_var = tex2D(_Set_RimLightMask,TRANSFORM_TEX(node_6650, _Set_RimLightMask));
                float3 _Is_LightColor_RimLight_var = lerp( _RimLightColor.rgb, (_RimLightColor.rgb*Set_LightColor), _Is_LightColor_RimLight );
                float node_4927 = (1.0 - dot(lerp( i.normalDir, normalDirection, _Is_NormalMapToRimLight ),viewDirection));
                float node_3992 = pow(node_4927,exp2(lerp(3,0,_RimLight_Power)));
                float node_5985 = 1.0;
                float node_3793 = 0.0;
                float node_9822 = saturate(lerp( (node_3793 + ( (node_3992 - _RimLight_InsideMask) * (node_5985 - node_3793) ) / (node_5985 - _RimLight_InsideMask)), step(_RimLight_InsideMask,node_3992), _RimLight_FeatherOff ));
                float node_9044 = 0.5*dot(i.normalDir,lightDirection)+0.5;
                float3 _LightDirection_MaskOn_var = lerp( (_Is_LightColor_RimLight_var*node_9822), (_Is_LightColor_RimLight_var*saturate((node_9822-((1.0 - node_9044)+_Tweak_LightDirection_MaskLevel)))), _LightDirection_MaskOn );
                float node_4691 = pow(node_4927,exp2(lerp(3,0,_Ap_RimLight_Power)));
                float3 Set_RimLight = (saturate((_Set_RimLightMask_var.g+_Tweak_RimLightMaskLevel))*lerp( _LightDirection_MaskOn_var, (_LightDirection_MaskOn_var+(lerp( _Ap_RimLightColor.rgb, (_Ap_RimLightColor.rgb*Set_LightColor), _Is_LightColor_Ap_RimLight )*saturate((lerp( (node_3793 + ( (node_4691 - _RimLight_InsideMask) * (node_5985 - node_3793) ) / (node_5985 - _RimLight_InsideMask)), step(_RimLight_InsideMask,node_4691), _Ap_RimLight_FeatherOff )-(saturate(node_9044)+_Tweak_LightDirection_MaskLevel))))), _Add_Antipodean_RimLight ));
                float3 _RimLight_var = lerp( node_3159, (node_3159+Set_RimLight), _RimLight );
                float node_4194 = 0.0;
                float node_1400 = (node_4194+_Tweak_MatCapUV);
                float node_885 = 1.0;
                float2 node_1922 = (node_4194 + ( ((mul( UNITY_MATRIX_V, float4(i.normalDir,0) ).xyz.rgb.rg*0.5+0.5) - node_1400) * (node_885 - node_4194) ) / ((node_885-_Tweak_MatCapUV) - node_1400));
                float4 _MatCap_Sampler_var = tex2D(_MatCap_Sampler,TRANSFORM_TEX(node_1922, _MatCap_Sampler));
                float3 node_4310 = (_MatCap_Sampler_var.rgb*_MatCapColor.rgb);
                float3 _Is_LightColor_MatCap_var = lerp( node_4310, (node_4310*Set_LightColor), _Is_LightColor_MatCap );
                float node_6614 = Set_FinalShadowMask;
                float3 Set_MatCap = lerp( _Is_LightColor_MatCap_var, (_Is_LightColor_MatCap_var*((1.0 - node_6614)+(node_6614*_TweakMatCapOnShadow))), _Is_UseTweakMatCapOnShadow );
                float3 node_3547 = Set_MatCap;
                float3 Set_FinalCompOut = saturate((1.0-(1.0-saturate(lerp( _RimLight_var, lerp( (_RimLight_var*node_3547), (_RimLight_var+node_3547), _Is_BlendAddToMatCap ), _MatCap )))*(1.0-(DecodeLightProbe( normalDirection )*_GI_Intensity)))); // Final Composition
                float3 finalColor = Set_FinalCompOut;
                fixed4 finalRGBA = fixed4(finalColor,1);
                UNITY_APPLY_FOG(i.fogCoord, finalRGBA);
                return finalRGBA;
            }
            ENDCG
        }
        Pass {
            Name "ShadowCaster"
            Tags {
                "LightMode"="ShadowCaster"
            }
            Offset 1, 1
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_SHADOWCASTER
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_fog
            #pragma only_renderers d3d9 d3d11 glcore gles gles3 metal xboxone ps4 switch
            #pragma target 3.0
            struct VertexInput {
                float4 vertex : POSITION;
            };
            struct VertexOutput {
                V2F_SHADOW_CASTER;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.pos = UnityObjectToClipPos(v.vertex );
                TRANSFER_SHADOW_CASTER(o)
                return o;
            }
            float4 frag(VertexOutput i, float facing : VFACE) : COLOR {
                float isFrontFace = ( facing >= 0 ? 1 : 0 );
                float faceSign = ( facing >= 0 ? 1 : -1 );
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
//ToonCoreEnd
    }
    FallBack "Legacy Shaders/VertexLit"
}
