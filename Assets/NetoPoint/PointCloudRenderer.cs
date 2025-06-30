using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace Anaglyph.NetoPoint
{
	public class PointCloudRenderer : MonoBehaviour
	{
		public Vector3 scale = Vector3.one;

		private Texture2D points;
		private Texture2D colors;
		private ComputeBuffer intBuffer;
		private RenderTexture display;
		private RenderTexture zDepth;
		[SerializeField] private ComputeShader shader;
		private (int x, int y, int z) shaderGroups;
		private int pointsID = Shader.PropertyToID("points");
		private int colorsID = Shader.PropertyToID("colors");
		private int intBufferID = Shader.PropertyToID("intBuffer");
		private int displayID = Shader.PropertyToID("display");
		private int depthID = Shader.PropertyToID("depth");
		private int displaySizeID = Shader.PropertyToID("displaySize");
		private int modelID = Shader.PropertyToID("model");
		private int viewProjID = Shader.PropertyToID("viewProj");
		private int eyeIndexID = Shader.PropertyToID("eyeIndex");
		private bool setup = false;

		[SerializeField] private Material material;

		private new Camera camera;
		private bool stereoRendering = false;

		private List<XRDisplaySubsystem> xrDisplays = new();

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
			stereoRendering = XRSettings.isDeviceActive;

			Vector2Int displaySize = GetDisplaySize();

			int bufferSize = displaySize.x * displaySize.y;
			if (bufferSize == 0)
				return;

			intBuffer = new ComputeBuffer(bufferSize, sizeof(UInt64), ComputeBufferType.Default);

			RenderTextureDescriptor desc = new()
			{
				width = displaySize.x,
				height = displaySize.y,
				volumeDepth = stereoRendering ? 2 : 1,
				graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm,
				enableRandomWrite = true,
				dimension = TextureDimension.Tex2DArray,
				msaaSamples = 1,
			};

			display = new RenderTexture(desc);
			desc.graphicsFormat = GraphicsFormat.R16_UNorm;
			zDepth = new RenderTexture(desc);

			shader.SetBuffer(0, intBufferID, intBuffer);
			shader.SetBuffer(1, intBufferID, intBuffer);
			shader.SetBuffer(2, intBufferID, intBuffer);

			shader.SetTexture(1, depthID, zDepth);
			shader.SetTexture(1, displayID, display);

			shader.SetInts(displaySizeID, display.width, display.height);
			shader.SetInts(displaySizeID, display.width, display.height);

			material.SetTexture("_MainTex", display);
			material.SetTexture("_DepthTex", zDepth);

			setup = true;
		}

		public void SetPoints(Texture2D points, Texture2D colors)
		{
			if (points.width != colors.width || points.height != colors.height)
				throw new Exception("point and color textures must be same size!");

			this.points = points;
			this.colors = colors;

			shader.SetTexture(0, pointsID, points);
			shader.SetTexture(0, colorsID, colors);

			var p = points;
			uint x, y, z;
			shader.GetKernelThreadGroupSizes(0, out x, out y, out z);
			shaderGroups = ((int)x, (int)y, 1);
		}

		private Vector2Int GetDisplaySize()
		{
			if (XRSettings.isDeviceActive)
			{
				SubsystemManager.GetSubsystems(xrDisplays);
				Vector2Int displaySize = new(camera.pixelWidth, camera.pixelHeight);
				if (xrDisplays.Count > 0 && xrDisplays[0].GetRenderPassCount() > 0)
				{
					xrDisplays[0].GetRenderPass(0, out XRDisplaySubsystem.XRRenderPass renderPass);
					return new Vector2Int(renderPass.renderTargetDesc.width, renderPass.renderTargetDesc.height);
				}
			}

			return new Vector2Int(camera.pixelWidth, camera.pixelHeight);
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

		private void OnBeginCameraRendering(ScriptableRenderContext arg1, Camera arg2)
		{
			if (points == null)
				return;

			Vector2Int displaySize = GetDisplaySize();

			if (!setup ||
				stereoRendering != XRSettings.isDeviceActive ||
				display.width != displaySize.x || display.height != displaySize.y)
			{
				InitDisplayTex();

				if (!setup)
					return;
			}

			Matrix4x4 viewProj = ViewProjMat(camera.projectionMatrix, camera.worldToCameraMatrix);

			for (int i = 0; i < (stereoRendering ? 2 : 1); i++)
			{
				shader.Dispatch(2, Mathf.CeilToInt(intBuffer.count / 64), 1, 1);

				if (stereoRendering)
				{
					var eye = (Camera.StereoscopicEye)i;
					viewProj = ViewProjMat(camera.GetStereoProjectionMatrix(eye), camera.GetStereoViewMatrix(eye));
				}

				var s = shaderGroups;
				shader.SetMatrix(modelID, transform.localToWorldMatrix * Matrix4x4.Scale(scale));
				shader.SetMatrix(viewProjID, viewProj);
				shader.SetInt(eyeIndexID, i);

				shader.Dispatch(0, Mathf.CeilToInt(points.width / s.x), Mathf.CeilToInt(points.height / s.y), 1);
				shader.Dispatch(1, Mathf.CeilToInt(display.width / s.x), Mathf.CeilToInt(display.height / s.y), 1);
			}
		}

		private void OnDestroy()
		{
			intBuffer.Release();
		}
	}
}