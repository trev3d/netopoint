using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering;

public class PointCloudRenderer : MonoBehaviour
{
	private Texture2D points;
	private ComputeBuffer intBuffer;
	private RenderTexture display;
	[SerializeField] private ComputeShader shader;
	private (int x, int y, int z) shaderGroups;
	private int pointsID      = Shader.PropertyToID("points");
	private int intBufferID   = Shader.PropertyToID("intBuffer");
	private int displayID     = Shader.PropertyToID("display");
	private int displaySizeID = Shader.PropertyToID("displaySize");
	private int cameraID      = Shader.PropertyToID("camera");

	public UnityEvent<Texture> OnInitDisplayTexture = new();

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
		intBuffer = new ComputeBuffer(camera.pixelWidth * camera.pixelHeight, sizeof(UInt64), ComputeBufferType.Default);
		display = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 0, GraphicsFormat.R8G8B8A8_UNorm);
		display.enableRandomWrite = true;

		shader.SetBuffer(0, intBufferID, intBuffer);
		shader.SetBuffer(1, intBufferID, intBuffer);
		shader.SetTexture(0, displayID, display);
		shader.SetTexture(1, displayID, display);
		shader.SetInts(displaySizeID, display.width, display.height);
		shader.SetInts(displaySizeID, display.width, display.height);

		OnInitDisplayTexture.Invoke(display);
	}

	public void SetPoints(Texture2D points)
	{
		if (points.graphicsFormat != GraphicsFormat.R32G32B32A32_SFloat)
			throw new Exception($"Texture format must be {nameof(GraphicsFormat.R32G32B32A32_SFloat)}");

		this.points = points;
		shader.SetTexture(0, pointsID, points);

		var p = points;
		uint x, y, z;
		shader.GetKernelThreadGroupSizes(0, out x, out y, out z);
		shaderGroups = ((int)x, (int)y, 1);
	}

	private void Update()
	{
		if (points == null)
			return;

		Matrix4x4 cameraMat = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false) * camera.worldToCameraMatrix;
		shader.SetMatrix(cameraID, cameraMat);

		var s = shaderGroups;

		shader.Dispatch(0, Mathf.CeilToInt(points.width / s.x), Mathf.CeilToInt(points.height / s.y), 1);
		shader.Dispatch(1, Mathf.CeilToInt(display.width / s.x), Mathf.CeilToInt(display.height / s.y), 1);
	}

	private void OnDestroy()
	{
		intBuffer.Release();
	}
}
