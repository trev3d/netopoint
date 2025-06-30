using UnityEngine;

namespace Anaglyph.NetoPoint
{
	public class PointCloudLoader : MonoBehaviour
	{
		[SerializeField] private Texture2D points;
		[SerializeField] private Texture2D colors;

		private void Start()
		{
			var renderer = GetComponent<PointCloudRenderer>();

			renderer.SetPoints(points, colors);
		}
	}
}