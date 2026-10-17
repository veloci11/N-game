using UnityEngine;
using UnityEngine.Rendering;

namespace NGame.Presentation
{
    /// <summary>
    /// A dense alpha-textured mesh with independent ear, neck and chest deformation.
    /// This is an animated decorative wolf, with no connection to a player's private role.
    /// </summary>
    public sealed class WolfPaperPuppet : MonoBehaviour
    {
        public bool IsReady => mesh != null && material != null;
        public int VertexCount => mesh != null ? mesh.vertexCount : 0;
        public float HeadAngleDegrees { get; private set; }
        public float LeftEarAngleDegrees { get; private set; }
        public float RightEarAngleDegrees { get; private set; }
        public float ChestExpansion { get; private set; }
        public float EyeGlow { get; private set; }
        private Mesh mesh;
        private Material material;
        private MeshRenderer visiblePaper;

        public bool Initialize(SpriteRenderer illustration)
        {
            if (illustration == null || illustration.sprite == null) return false;
            var shader = Resources.Load<Shader>("Werewolf/Shader/WolfPaper");
            if (shader == null) return false;
            var sprite = illustration.sprite;
            material = new Material(shader) { name = "Independent folded wolf anatomy" };
            material.SetTexture("_MainTex", sprite.texture);
            var textureRect = sprite.rect;
            material.SetVector("_SourceRect", new Vector4(textureRect.x / sprite.texture.width,
                textureRect.y / sprite.texture.height, textureRect.width / sprite.texture.width,
                textureRect.height / sprite.texture.height));
            material.SetVector("_ArtSize", new Vector4(sprite.bounds.size.x, sprite.bounds.size.y, 0, 0));
            mesh = CreateGrid(sprite.bounds, 48, 56);
            var child = new GameObject("Deformable folded wolf surface", typeof(MeshFilter), typeof(MeshRenderer));
            child.transform.SetParent(transform, false);
            child.GetComponent<MeshFilter>().sharedMesh = mesh;
            visiblePaper = child.GetComponent<MeshRenderer>();
            visiblePaper.sharedMaterial = material;
            visiblePaper.sortingLayerID = illustration.sortingLayerID;
            visiblePaper.sortingOrder = illustration.sortingOrder;
            visiblePaper.shadowCastingMode = ShadowCastingMode.Off;
            visiblePaper.receiveShadows = false;
            illustration.enabled = false;
            Evaluate(0, false, illustration.color);
            return true;
        }

        public void Evaluate(float seconds, bool reducedMotion, Color opacity)
        {
            if (!IsReady) return;
            HeadAngleDegrees = reducedMotion ? 0 : Mathf.Sin(seconds * .39f) * 3.4f + Mathf.Sin(seconds * .17f) * .85f;
            LeftEarAngleDegrees = reducedMotion ? 0 : Mathf.Sin(seconds * .97f + .3f) * 3.6f + EarTwitch(seconds, 8.2f, 6.1f) * 5.8f;
            RightEarAngleDegrees = reducedMotion ? 0 : Mathf.Sin(seconds * .79f + 2) * 3.1f - EarTwitch(seconds, 9.7f, 5.8f) * 5.2f;
            ChestExpansion = reducedMotion ? 0 : Mathf.Sin(seconds * 1.31f);
            EyeGlow = reducedMotion ? .58f : .62f + Mathf.Sin(seconds * 1.31f + .5f) * .35f;
            material.SetFloat("_HeadAngle", HeadAngleDegrees * Mathf.Deg2Rad);
            material.SetFloat("_LeftEarAngle", LeftEarAngleDegrees * Mathf.Deg2Rad);
            material.SetFloat("_RightEarAngle", RightEarAngleDegrees * Mathf.Deg2Rad);
            material.SetFloat("_Breath", ChestExpansion);
            material.SetFloat("_EyeLight", EyeGlow);
            material.SetColor("_Color", opacity);
        }

        private static float EarTwitch(float seconds, float period, float offset)
        {
            var phase = (seconds + offset) % period;
            var outwards = (phase - .18f) / .11f;
            var returnFold = (phase - .43f) / .16f;
            return Mathf.Exp(-outwards * outwards) - .35f * Mathf.Exp(-returnFold * returnFold);
        }

        private static Mesh CreateGrid(Bounds art, int columns, int rows)
        {
            var vertices = new Vector3[(columns + 1) * (rows + 1)];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[columns * rows * 6];
            for (var y = 0; y <= rows; y++)
                for (var x = 0; x <= columns; x++)
                {
                    var index = y * (columns + 1) + x;
                    var uv = new Vector2(x / (float)columns, y / (float)rows);
                    vertices[index] = new Vector3(art.min.x + art.size.x * uv.x, art.min.y + art.size.y * uv.y, 0);
                    uvs[index] = uv;
                }
            var next = 0;
            for (var y = 0; y < rows; y++)
                for (var x = 0; x < columns; x++)
                {
                    var a = y * (columns + 1) + x;
                    triangles[next++] = a;
                    triangles[next++] = a + columns + 1;
                    triangles[next++] = a + 1;
                    triangles[next++] = a + 1;
                    triangles[next++] = a + columns + 1;
                    triangles[next++] = a + columns + 2;
                }
            var result = new Mesh { name = "Folded wolf - 48 by 56 deformation grid" };
            result.vertices = vertices;
            result.uv = uvs;
            result.triangles = triangles;
            result.bounds = new Bounds(art.center, art.size * 1.3f);
            result.UploadMeshData(false);
            return result;
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            if (material != null) Destroy(material);
        }
    }
}
