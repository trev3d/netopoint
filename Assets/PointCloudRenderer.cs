using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.XR;

public class PointCloudRenderer : MonoBehaviour
{
	private Texture2D points;
	private Texture2D colors;
	private ComputeBuffer intBuffer;
	private RenderTexture display;
	private RenderTexture depth;
	[SerializeField] private ComputeShader shader;
	private (int x, int y, int z) shaderGroups;
	private int pointsID      = Shader.PropertyToID("points");
	private int colorsID      = Shader.PropertyToID("colors");
	private int intBufferID   = Shader.PropertyToID("intBuffer");
	private int displayID     = Shader.PropertyToID("display");
	private int depthID       = Shader.PropertyToID("depth");
	private int displaySizeID = Shader.PropertyToID("displaySize");
	private int modelID       = Shader.PropertyToID("model");
	private int viewProjID    = Shader.PropertyToID("viewProj");
	private int drawRegionID  = Shader.PropertyToID("drawRegion");
	private bool setup = false;

	[SerializeField] private Material material;

	private new Camera camera;
	private bool stereoRendering = false;

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
		Vector2Int s = new(camera.pixelWidth, camera.pixelHeight);
		int bufferSize = s.x * s.y;
		if (bufferSize == 0)
			return;

		intBuffer = new ComputeBuffer(bufferSize, sizeof(UInt64), ComputeBufferType.Default);
		display = new RenderTexture(s.x, s.y, 0, GraphicsFormat.R8G8B8A8_UNorm);
		display.enableRandomWrite = true;
		depth = new RenderTexture(s.x, s.y, 0, GraphicsFormat.R16_UNorm);
		depth.enableRandomWrite = true;

		shader.SetBuffer(0, intBufferID, intBuffer);
		shader.SetBuffer(1, intBufferID, intBuffer);
		shader.SetTexture(1, depthID, depth);
		shader.SetTexture(1, displayID, display);
		shader.SetInts(displaySizeID, display.width, display.height);
		shader.SetInts(displaySizeID, display.width, display.height);

		material.SetTexture("_MainTex", display);
		material.SetTexture("_DepthTex", depth);

		stereoRendering = XRSettings.isDeviceActive;
		setup = true;
	}

	public void SetPoints(Texture2D points, Texture2D colors)
	{
		//if (points.graphicsFormat != GraphicsFormat.R32G32B32A32_SFloat)
		//	throw new Exception($"Texture format must be {nameof(GraphicsFormat.R32G32B32A32_SFloat)}");

		if (points.width != colors.width || points.height != colors.height)
		{
			throw new Exception("point and color textures must be same size!");
		}

		this.points = points;
		this.colors = colors;
		shader.SetTexture(0, pointsID, points);
		shader.SetTexture(0, colorsID, colors);

		var p = points;
		uint x, y, z;
		shader.GetKernelThreadGroupSizes(0, out x, out y, out z);
		shaderGroups = ((int)x, (int)y, 1);
	}

	private void Update()
	{
		if (points == null)
			return;

		if (!setup ||
			stereoRendering != XRSettings.isDeviceActive ||
			display.width < camera.pixelWidth || display.height < camera.pixelHeight)
		{
			InitDisplayTex();

			if (!setup)
				return;
		}

		Matrix4x4[] viewProj = new Matrix4x4[2];
		Matrix4x4 l = camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
		Matrix4x4 r = camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
		viewProj[0] = GL.GetGPUProjectionMatrix(l, false) * camera.worldToCameraMatrix;
		viewProj[1] = GL.GetGPUProjectionMatrix(r, false) * camera.worldToCameraMatrix;
		shader.SetMatrixArray(viewProjID, viewProj);
		shader.SetInt(drawRegionID, stereoRendering ? 2 : 1);

		var s = shaderGroups;

		shader.Dispatch(0, Mathf.CeilToInt(points.width / s.x), Mathf.CeilToInt(points.height / s.y), 1);
		shader.Dispatch(1, Mathf.CeilToInt(display.width / s.x), Mathf.CeilToInt(display.height / s.y), 1);
	}

	private void OnDestroy()
	{
		intBuffer.Release();
	}
}
