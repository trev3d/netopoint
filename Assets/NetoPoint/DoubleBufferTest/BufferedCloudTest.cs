using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Anaglyph.NetoPoint
{
	public class BufferedCloudTest : MonoBehaviour
	{
		public Vector3 scale = Vector3.one;

		public Texture2D points;
		private RenderTexture prevFrame;
		private RenderTexture nextFrame;
		[SerializeField] private ComputeShader shader;
		private (int x, int y, int z) shaderGroups;
		private int pointsID = Shader.PropertyToID("points");
		private int prevFrameID = Shader.PropertyToID("prevFrame");
		private int nextFrameID = Shader.PropertyToID("nextFrame");
		private int displaySizeID = Shader.PropertyToID("displaySize");
		private int modelID = Shader.PropertyToID("model");
		private int viewProjID = Shader.PropertyToID("viewProj");
		private bool setup = false;

		[SerializeField] private Material material;

		private new Camera camera;

		private void Awake()
		{
			camera = Camera.main;
		}

		private void Start()
		{
			InitDisplayTex();
		}

		private void InitDisplayTex()
		{
			Vector2Int displaySize = new(camera.pixelWidth, camera.pixelHeight);

			RenderTextureDescriptor desc = new()
			{
				width = displaySize.x,
				height = displaySize.y,
				volumeDepth = 1,
				graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat,
				enableRandomWrite = true,
				dimension = TextureDimension.Tex2D,
				msaaSamples = 1,
			};

			prevFrame = new RenderTexture(desc);
			nextFrame = new RenderTexture(desc);

			shader.SetInts(displaySizeID, displaySize.x, displaySize.y);

			setup = true;
		}

		private void OnEnable()
		{
			RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
		}

		private void OnDisable()
		{
			RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
		}

		private static Matrix4x4 ViewProjMat(Matrix4x4 proj, Matrix4x4 view)
		{
			return GL.GetGPUProjectionMatrix(proj, false) * view;
		}

		private bool firstFrame = true;

		private void OnBeginCameraRendering(ScriptableRenderContext arg1, Camera arg2)
		{
			Matrix4x4 viewProj = ViewProjMat(camera.projectionMatrix, camera.worldToCameraMatrix);

			var s = shaderGroups;
			shader.SetMatrix(modelID, transform.localToWorldMatrix * Matrix4x4.Scale(scale));
			shader.SetMatrix(viewProjID, viewProj);

			RenderTexture next = prevFrame;
			prevFrame = nextFrame;
			nextFrame = next;

			if(firstFrame)
			{
				shader.SetTexture(0, pointsID, points);
				shader.SetTexture(0, prevFrameID, prevFrame);
				shader.Dispatch(0,
					Mathf.CeilToInt(points.width / 8),
					Mathf.CeilToInt(points.height / 8),
					1);

				firstFrame = false;
			}
			
			shader.SetTexture(1, nextFrameID, nextFrame);
			shader.SetTexture(1, prevFrameID, prevFrame);

			material.mainTexture = nextFrame;

			shader.Dispatch(1, 
				Mathf.CeilToInt(prevFrame.width / 8), 
				Mathf.CeilToInt(prevFrame.height / 8), 
				1);
		}
	}
}