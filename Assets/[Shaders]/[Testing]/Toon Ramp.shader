//https://docs.unity3d.com/Manual/SL-SurfaceShaderLightingExamples.html

//The following example shows a “Ramp” lighting model that uses a Texture ramp to define how surfaces respond to the angles between the light and the normal. 
//This can be used for a variety of effects, and is especially effective when used with Toon lighting.

//Session4/03-Toon Ramp
Shader "Cartoon-Shader-Library/Test/Toon Ramp"
{
	Properties 
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Ramp ("Ramp", 2D) = "white" {}
	}

	SubShader 
	{
		Tags { "RenderType" = "Opaque" }

		CGPROGRAM
		#pragma surface surf Ramp

		sampler2D _Ramp;

		half4 LightingRamp (SurfaceOutput s, half3 lightDir, half atten) 
		{
			half NdotL = dot (s.Normal, lightDir);
			half diff = NdotL * 0.5 + 0.5;
			half3 ramp = tex2D (_Ramp, float2(diff,diff)).rgb;
			half4 c;
			c.rgb = s.Albedo * _LightColor0.rgb * ramp * atten;
			c.a = s.Alpha;
			return c;
		}

		struct Input 
		{
			float2 uv_MainTex;
		};

		sampler2D _MainTex;

		void surf (Input IN, inout SurfaceOutput o) 
		{
			o.Albedo = tex2D (_MainTex, IN.uv_MainTex).rgb;
		}

		ENDCG
	}
	Fallback "Diffuse"
}
