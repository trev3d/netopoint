Shader "Custom/DisplayWorking"
{
	Properties
	{
		_MainTex("MainTex", 2DArray) = "white" {}
		_DepthTex("DepthTex", 2DArray) = "white" {}
	}

	// Universal Render Pipeline subshader. If URP is installed this will be used.
	SubShader
	{
		Tags { "RenderType"="Opaque" }

		Pass
		{
			ZWrite On
			Cull Off
			Tags { "LightMode"="UniversalForward" }

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct Attributes
			{
				float4 positionOS   : POSITION;
				float2 uv           : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID 
			};

			struct Varyings
			{
				float4 positionHCS  : SV_POSITION;
				float4 positionSS   : TEXCOORD0;

				UNITY_VERTEX_OUTPUT_STEREO
			};

			struct Output {
				float4 color : SV_Target;
				float depth : SV_Depth;
			};

			UNITY_DECLARE_TEX2DARRAY(_MainTex);
			UNITY_DECLARE_TEX2DARRAY(_DepthTex);

			Varyings vert(Attributes IN)
			{
				Varyings OUT;

				UNITY_SETUP_INSTANCE_ID(IN);
				// UNITY_INITIALIZE_OUTPUT(IN, OUT);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

				OUT.positionHCS = mul(UNITY_MATRIX_MVP, IN.positionOS);//(IN.positionOS.xyz);
				OUT.positionSS  = OUT.positionHCS;
				return OUT;
			}

			Output frag(Varyings IN)
			{
				Output OUT;

				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				half2 uv = (IN.positionSS.xy / IN.positionSS.w) * 0.5 + 0.5;
				uv.y = 1 - uv.y;
				OUT.color = UNITY_SAMPLE_TEX2DARRAY(_MainTex,  half3(uv, unity_StereoEyeIndex));
				OUT.depth = UNITY_SAMPLE_TEX2DARRAY(_DepthTex, half3(uv, unity_StereoEyeIndex));

				return OUT;
			}
			ENDHLSL
		}
	}
}