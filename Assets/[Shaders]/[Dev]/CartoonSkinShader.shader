///
/// Cartoon Skin Shader
/// Reference : Unitychan_chara_hada.shader 
///

Shader "Cartoon-Shader-Library/CartoonSkinShader" 
{
	Properties
	{
		_Color ("Main Color", Color) = (1, 1, 1, 1)
		_ShadowColor ("Shadow Color", Color) = (0.8, 0.8, 1, 1)
		_EdgeThickness ("Outline Thickness", Float) = 1
				
		_MainTex ("Diffuse", 2D) = "white" {}
		_FalloffSampler ("Falloff Control", 2D) = "white" {}
		_RimLightSampler ("RimLight Control", 2D) = "white" {}
	}

	SubShader
	{
		Tags
		{
			"RenderType"="Opaque"
			"Queue"="Geometry"
			"LightMode"="ForwardBase"
		}		

		//------------------------------------------
		// 主Pass
		//------------------------------------------
		Pass
		{
			Cull Off
			ZTest LEqual


			CGPROGRAM

			#pragma multi_compile_fwdbase
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"

			// Character skin shader
			// Includes falloff shadow

			#define ENABLE_CAST_SHADOWS

			// Material parameters
			float4 _Color;
			float4 _ShadowColor;
			float4 _LightColor0;
			float4 _MainTex_ST;

			// Textures
			sampler2D _MainTex;
			sampler2D _FalloffSampler;
			sampler2D _RimLightSampler;

			// Constants
			#define FALLOFF_POWER 1.0

			#ifdef ENABLE_CAST_SHADOWS

			// Structure from vertex shader to fragment shader
			struct v2f
			{
				float4 pos    : SV_POSITION;
				LIGHTING_COORDS( 0, 1 )
				float3 normal : TEXCOORD2;
				float2 uv     : TEXCOORD3;
				float3 eyeDir : TEXCOORD4;
				float3 lightDir : TEXCOORD5;
			};

			#else
				
			// Structure from vertex shader to fragment shader
			struct v2f
			{
				float4 pos    : SV_POSITION;
				float3 normal : TEXCOORD0;
				float2 uv     : TEXCOORD1;
				float3 eyeDir : TEXCOORD2;
				float3 lightDir : TEXCOORD3;
			};
				
			#endif

			// Float types
			#define float_t  half
			#define float2_t half2
			#define float3_t half3
			#define float4_t half4

			// Vertex shader
			v2f vert( appdata_base v )
			{
				v2f o;
				o.pos = UnityObjectToClipPos( v.vertex );
				o.uv = TRANSFORM_TEX( v.texcoord.xy, _MainTex );
				o.normal = normalize( mul( unity_ObjectToWorld, float4_t( v.normal, 0 ) ).xyz );
				
				// Eye direction vector
				float4_t worldPos =  mul( unity_ObjectToWorld, v.vertex );
				o.eyeDir = normalize( _WorldSpaceCameraPos - worldPos );

				o.lightDir = WorldSpaceLightDir( v.vertex );

			#ifdef ENABLE_CAST_SHADOWS
				TRANSFER_VERTEX_TO_FRAGMENT( o );
			#endif

				return o;
			}

			// Fragment shader
			float4 frag( v2f i ) : COLOR
			{
				float4_t diffSamplerColor = tex2D( _MainTex, i.uv );

				// Falloff. Convert the angle between the normal and the camera direction into a lookup for the gradient
				float_t normalDotEye = dot( i.normal, i.eyeDir );
				float_t falloffU = clamp( 1 - abs( normalDotEye ), 0.02, 0.98 );
				float4_t falloffSamplerColor = FALLOFF_POWER * tex2D( _FalloffSampler, float2( falloffU, 0.25f ) );
				float3_t combinedColor = lerp( diffSamplerColor.rgb, falloffSamplerColor.rgb * diffSamplerColor.rgb, falloffSamplerColor.a );

				// Rimlight
				float_t rimlightDot = saturate( 0.5 * ( dot( i.normal, i.lightDir ) + 1.0 ) );
				falloffU = saturate( rimlightDot * falloffU );
				//falloffU = saturate( ( rimlightDot * falloffU - 0.5 ) * 32.0 );
				falloffU = tex2D( _RimLightSampler, float2( falloffU, 0.25f ) ).r;
				float3_t lightColor = diffSamplerColor.rgb * 0.5; // * 2.0;
				combinedColor += falloffU * lightColor;

			#ifdef ENABLE_CAST_SHADOWS
				// Cast shadows
				float3_t shadowColor = _ShadowColor.rgb * combinedColor;
				float_t attenuation = saturate( 2.0 * LIGHT_ATTENUATION( i ) - 1.0 );
				combinedColor = lerp( shadowColor, combinedColor, attenuation );
			#endif

				return float4_t( combinedColor, diffSamplerColor.a ) * _Color * _LightColor0;
			}

			ENDCG
		}

		//------------------------------------------
		// outline-轮廓Pass
		//------------------------------------------
		Pass
		{
			Cull Front
			ZTest Less


			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			//#include "CharaOutline.cg"


			// Material parameters
			float4 _Color;
			float4 _LightColor0;
			float _EdgeThickness = 1.0;
			float4 _MainTex_ST;

			// Textures
			sampler2D _MainTex;

			// Structure from vertex shader to fragment shader
			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			// Float types
			#define float_t  half
			#define float2_t half2
			#define float3_t half3
			#define float4_t half4

			// Outline thickness multiplier
			#define INV_EDGE_THICKNESS_DIVISOR 0.00285
			// Outline color parameters
			#define SATURATION_FACTOR 0.6
			#define BRIGHTNESS_FACTOR 0.8

			// Vertex shader
			v2f vert( appdata_base v )
			{
				v2f o;
				o.uv = TRANSFORM_TEX( v.texcoord.xy, _MainTex );

				half4 projSpacePos = UnityObjectToClipPos( v.vertex );
				half4 projSpaceNormal = normalize( UnityObjectToClipPos( half4( v.normal, 0 ) ) );
				half4 scaledNormal = _EdgeThickness * INV_EDGE_THICKNESS_DIVISOR * projSpaceNormal; // * projSpacePos.w;

				scaledNormal.z += 0.00001;
				o.pos = projSpacePos + scaledNormal;

				return o;
			}

			// Fragment shader
			float4 frag( v2f i ) : COLOR
			{
				float4_t diffuseMapColor = tex2D( _MainTex, i.uv );

				float_t maxChan = max( max( diffuseMapColor.r, diffuseMapColor.g ), diffuseMapColor.b );
				float4_t newMapColor = diffuseMapColor;

				maxChan -= ( 1.0 / 255.0 );
				float3_t lerpVals = saturate( ( newMapColor.rgb - float3( maxChan, maxChan, maxChan ) ) * 255.0 );
				newMapColor.rgb = lerp( SATURATION_FACTOR * newMapColor.rgb, newMapColor.rgb, lerpVals );
				
				return float4( BRIGHTNESS_FACTOR * newMapColor.rgb * diffuseMapColor.rgb, diffuseMapColor.a ) * _Color * _LightColor0; 
			}

			ENDCG
		}

	}

	FallBack "Transparent/Cutout/Diffuse"
}
