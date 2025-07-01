Shader "Custom/BufferedCloudDisplay"
{
	Properties
	{
		[MainTexture] _MainTex("MainTex", 2D) = "white" {}
	}

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
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct Attributes
			{
				float4 positionOS   : POSITION;

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
			};

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			Varyings vert(Attributes IN)
			{
				Varyings OUT;

				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

				OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
				OUT.positionSS  = ComputeScreenPos(OUT.positionHCS);
				return OUT;
			}

			Output frag(Varyings IN)
			{
				Output OUT;

				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float2 uv = IN.positionSS.xy / IN.positionSS.w;
				OUT.color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

				return OUT;
			}
			ENDHLSL
		}
	}
}