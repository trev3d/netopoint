Shader "Custom/Display"
{
	Properties
	{
		[MainTexture] _MainTex("MainTex", 2DArray) = "white" {}
		_DepthTex("DepthTex", 2DArray) = "white" {}
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


			TEXTURE2D_ARRAY(_MainTex);
			TEXTURE2D_ARRAY(_DepthTex);
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

				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				half2 uv = IN.positionSS.xy / IN.positionSS.w;
				uv.y = 1 - uv.y;
				OUT.color = SAMPLE_TEXTURE2D_ARRAY(_MainTex , sampler_MainTex, uv, unity_StereoEyeIndex);
				OUT.depth = SAMPLE_TEXTURE2D_ARRAY(_DepthTex, sampler_MainTex, uv, unity_StereoEyeIndex);

				return OUT;
			}
			ENDHLSL
		}
	}
}