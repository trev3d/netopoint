Shader "Custom/Display"
{
	Properties
	{
		[MainTexture] _MainTex("MainTex", 2D) = "white" {}
		_DepthTex("DepthTex", 2D) = "white" {}
	}

	// Universal Render Pipeline subshader. If URP is installed this will be used.
	SubShader
	{
		Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalRenderPipeline"}

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
				float depth : SV_Depth;
			};


			TEXTURE2D(_MainTex);
			TEXTURE2D(_DepthTex);
			SAMPLER(sampler_MainTex);

			Varyings vert(Attributes IN)
			{
				Varyings OUT;

				UNITY_SETUP_INSTANCE_ID(IN);
				// UNITY_INITIALIZE_OUTPUT(IN, OUT);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

				OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
				OUT.positionSS  = ComputeScreenPos(OUT.positionHCS);
				return OUT;
			}

			Output frag(Varyings IN)
			{
				Output OUT;

				half2 uv = IN.positionSS.xy / IN.positionSS.w;
				OUT.color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
				OUT.depth = SAMPLE_TEXTURE2D(_DepthTex, sampler_MainTex, uv);

				return OUT;
			}
			ENDHLSL
		}
	}
}