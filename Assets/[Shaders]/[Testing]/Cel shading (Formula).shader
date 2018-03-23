
//
//Use a formula to create a cel shading.
//
//REFERENCE: https://www.gamasutra.com/blogs/DavidLeon/20150702/247602/NextGen_Cel_Shading_in_Unity_5.php

//Session19/Lighting/CelShader

Shader "Cartoon-Shader-Library/Test/Lighting CelShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Treshold ("Cel Treshold" , Range(0.001, 20.)) = 5.
		_Ambient ("Ambient intensity", Range(0.,0.5)) = 0.1
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" "LightMode" = "ForwardBase"}

		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			//-----------------------
			// 顶点输出，片元输入
			//-----------------------
			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 worldNormal : NORMAL;
			};


			float _Treshold;

			float LightingToonShading(float3 normal , float3 lightDir)
			{
				float NdotL = max(0.0, dot(normalize(normal), normalize(lightDir)));
				return floor(NdotL * _Treshold ) / (_Treshold - 0.5);

			}

			float LightingToonShading2(float3 normal , float3 lightDir)
			{
				float NdotL = max(0.0, dot(normalize(normal), normalize(lightDir)));
				return smoothstep(0, _Treshold, NdotL);

			}

			float LightingToonShading3(float3 normal , float3 lightDir)
			{
				float NdotL = max(0.0, dot(normalize(normal), normalize(lightDir)));
				 return 1+ clamp(floor(NdotL), -1* _Treshold , 0);
			}


			//-----------------------
			// 内置结构-appdata_full的定义
			//-----------------------
			/*
			struct appdata_full 
			{
				float4 vertex : POSITION;
				float4 tangent : TANGENT;
				float3 normal : NORMAL;
				float4 texcoord : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
				float4 texcoord3 : TEXCOORD3;
				fixed4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			*/
						
			sampler2D _MainTex;
			float4 _MainTex_ST;

			v2f vert (appdata_full v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.worldNormal = mul(v.normal.xyz ,(float3x3) unity_WorldToObject);
				return o;
			}
			
			fixed4 _LightColor0;
			half _Ambient;

			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);

				col.rgb *= saturate(LightingToonShading(i.worldNormal , _WorldSpaceLightPos0.xyz) + _Ambient) * _LightColor0.rgb;

				return col;
			}
			ENDCG
		}
	}
}
