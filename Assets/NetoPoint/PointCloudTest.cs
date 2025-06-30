using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Anaglyph.NetoPoint
{
    public class CloudTest : MonoBehaviour
    {
        [SerializeField] private Vector2Int pointsSize = new(1024, 1024);

        private void Start()
        {
            Texture2D points = new Texture2D(pointsSize.x, pointsSize.y, GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None);

            for (int x = 0; x < points.width; x++)
                for (int y = 0; y < points.height; y++)
                {

                    //float posX = x / (float)pointsSize.x;
                    //float posY = y / (float)pointsSize.y;

                    points.SetPixel(x, y, new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)));

                }

            points.Apply();

            GetComponent<PointCloudRenderer>().SetPoints(points, null);
        }
    }
}