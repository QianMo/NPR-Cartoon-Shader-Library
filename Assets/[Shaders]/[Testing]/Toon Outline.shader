// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


//Session9/12-Toon Outline
Shader "Cartoon-Shader-Library/Test/Toon Outline"
{
	Properties 
	{
		_Color ("Lit Color", Color) = (1,1,1,1) 
		_UnlitColor ("Unlit Color", Color) = (0.5,0.5,0.5,1) 
		_DiffuseThreshold ("Lighting Threshold", Range(-1.1,1)) = 0.1 
		_Diffusion ("Diffusion", Range(0,0.99)) = 0.0 
		_SpecColor ("Specular Color", Color) = (1,1,1,1) 
		_Shininess ("Shininess", Range(0.5,1)) = 1 
		_SpecDiffusion ("Specular Diffusion", Range(0,0.99)) = 0.0 
		_OutlineColor ("Outline Color", Color) = (0,0,0,1)
		_OutlineThickness ("Outline Thickness", Range(0,1)) = 0.1
		_OutlineDiffusion ("Outline Diffusion", Range(0,1)) = 0.0 
	}

	SubShader 
	{
		Pass 
		{
			Tags {"LightMode" = "ForwardBase"}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			//user defined variables
	         uniform fixed4 _Color; 
	         uniform fixed4 _UnlitColor;
	         uniform fixed _DiffuseThreshold;
	         uniform fixed _Diffusion;
	         uniform fixed4 _SpecColor; 
	         uniform fixed _Shininess;
	         uniform half _SpecDiffusion;
	         uniform fixed4 _OutlineColor; 
	         uniform fixed _OutlineThickness;
	         uniform fixed _OutlineDiffusion;
			
			//unity defined variables
			uniform half4 _LightColor0;
			
			//base input structs
			struct vertexInput
			{
				half4 vertex : POSITION;
				half3 normal : NORMAL;
			};
			struct vertexOutput
			{
				half4 pos : SV_POSITION;
				fixed3 normalDir : TEXCOORD0;
				fixed4 lightDir : TEXCOORD1;
				fixed3 viewDir : TEXCOORD2;
			};
			
			//vertex Function
			vertexOutput vert(vertexInput v)
			{
				vertexOutput o;
				
				//normalDirection
				o.normalDir = normalize( mul( half4( v.normal, 0.0 ), unity_WorldToObject ).xyz );
				
				//unity transform position
				o.pos = UnityObjectToClipPos(v.vertex);

				//world position
				half4 posWorld = mul(unity_ObjectToWorld, v.vertex);
				//view direction
				o.viewDir = normalize( _WorldSpaceCameraPos.xyz - posWorld.xyz );
				//light direction
				half3 fragmentToLightSource = _WorldSpaceLightPos0.xyz - posWorld.xyz;
				o.lightDir = fixed4(
					normalize( lerp(_WorldSpaceLightPos0.xyz , fragmentToLightSource, _WorldSpaceLightPos0.w) ),
					lerp(1.0 , 1.0/length(fragmentToLightSource), _WorldSpaceLightPos0.w)
				);
				
				return o;
			}
			
			//fragment function
			fixed4 frag(vertexOutput i) : COLOR
			{
				//Lighting
				//dot product
				fixed nDotL = saturate(dot(i.normalDir, i.lightDir.xyz));
				
				fixed diffuseCutoff = saturate( ( max(_DiffuseThreshold, nDotL) - _DiffuseThreshold ) * pow( (2-_Diffusion), 10 ) );
				fixed specularCutoff = saturate((max(_Shininess,dot( reflect( -i.lightDir.xyz, i.normalDir ), i.viewDir ))-_Shininess)*pow((2-_SpecDiffusion),10));
				
				//calculate outlines
				fixed outlineStrength = saturate( (dot( i.normalDir, i.viewDir ) - _OutlineThickness) * pow( (2-_OutlineDiffusion), 10 ) + _OutlineThickness );
				fixed3 outlineOverlay = (_OutlineColor.xyz * (1-outlineStrength)) + outlineStrength;
				
				fixed3 ambientLight = (1-diffuseCutoff) * _UnlitColor.xyz;
				fixed3 diffuseReflection = (1-specularCutoff) * _Color.xyz * diffuseCutoff;
				fixed3 specularReflection = _SpecColor.xyz * specularCutoff;
				
				fixed3 lightFinal = (ambientLight + diffuseReflection) * outlineOverlay + specularReflection;
				
				return fixed4(lightFinal, 1.0);
			}
			
			ENDCG
		}
	}
	//Fallback "Specular"
}
