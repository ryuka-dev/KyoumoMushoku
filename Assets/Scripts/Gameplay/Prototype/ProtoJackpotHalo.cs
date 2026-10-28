using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 大当たりの品の後ろに立つ虹の後光（パチンコの当たりの放射光）。品について動き、いつも視点を向き、回り続ける。
    /// 細い筋が根元から先へ淡く消え（加算の光）、二層が逆向きに回り、中心に柔らかな光の玉がある。
    /// 現れるときは一度大きく弾けてから落ち着く。品が掴まれるか消える前の明滅に入ったら隠す。
    /// 色は街と一緒に退色するが、回る放射の形は残る（灰色の街でも大当たりと分かる）。
    /// </summary>
    public sealed class ProtoJackpotHalo : MonoBehaviour
    {
        const float Radius = 0.75f;

        static Mesh _fineRays;
        static Mesh _broadRays;
        static Mesh _core;

        Transform _target;
        Transform _fine;
        Transform _broad;
        Renderer[] _renderers;
        float _age;

        /// <summary><paramref name="target"/>（品の容器）の後ろに後光を立てる。</summary>
        public static ProtoJackpotHalo Attach(Transform target)
        {
            var material = ProtoTierLook.RainbowGlowMaterial();
            if (material == null)
            {
                return null;
            }

            var root = new GameObject("JackpotHalo");
            root.transform.position = target.position;
            var halo = root.AddComponent<ProtoJackpotHalo>();
            halo._target = target;
            halo._broad = Layer(root.transform, "Broad", BroadRays(), material, 0.02f);
            halo._fine = Layer(root.transform, "Fine", FineRays(), material, 0.01f);
            Layer(root.transform, "Core", Core(), material, 0f);
            halo._renderers = root.GetComponentsInChildren<Renderer>();
            root.transform.localScale = Vector3.zero;
            return halo;
        }

        /// <summary>品が掴まれた・明滅に入ったなどで見せない。</summary>
        public void SetVisible(bool visible)
        {
            foreach (var renderer in _renderers)
            {
                renderer.enabled = visible;
            }
        }

        static Transform Layer(Transform parent, string name, Mesh mesh, Material material, float back)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            // 層を奥へ少しずつずらす（加算の光なので前後の順で色が濁らないように）。
            go.transform.localPosition = new Vector3(0f, 0f, back);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = ProtoTierLook.EffectSortingOrder;
            return go.transform;
        }

        void LateUpdate()
        {
            if (_target == null)
            {
                Destroy(gameObject);
                return;
            }

            _age += Time.deltaTime;
            var camera = Camera.main;
            // 品の少し奥（視点から見て後ろ）に置く。品の模型を光で覆わない。
            var toCamera = camera != null ? (camera.transform.position - _target.position).normalized : Vector3.back;
            transform.position = _target.position - toCamera * 0.35f;
            if (camera != null)
            {
                transform.rotation = camera.transform.rotation;
            }

            // 現れるとき：0.25 秒で 1.6 倍まで弾け、0.6 秒かけて落ち着く。その後はゆっくり脈打つ。
            var pop = _age < 0.25f
                ? Mathf.Lerp(0f, 1.6f, _age / 0.25f)
                : _age < 0.85f ? Mathf.Lerp(1.6f, 1f, (_age - 0.25f) / 0.6f) : 1f + 0.05f * Mathf.Sin(_age * 3f);
            transform.localScale = Vector3.one * (Radius * 2f * pop);

            _fine.localRotation = Quaternion.Euler(0f, 0f, -40f * _age);
            _broad.localRotation = Quaternion.Euler(0f, 0f, 22f * _age);
            foreach (var renderer in _renderers)
            {
                ProtoTierLook.ScrollRainbow(renderer, Time.time * 0.35f);
            }
        }

        // ── 網 ──（半径 0.5 の単位円の中。頂点色の明るさが光の強さ：根元が明るく先は 0＝加算で消える）

        /// <summary>細い筋。長短を交互に 32 本。</summary>
        static Mesh FineRays()
        {
            return _fineRays != null ? _fineRays : _fineRays = Rays("HaloFine", 32, 0.07f, 0.5f, 0.31f, 0.009f, 0.005f, 1f);
        }

        /// <summary>太く淡い筋。12 本。</summary>
        static Mesh BroadRays()
        {
            return _broadRays != null ? _broadRays : _broadRays = Rays("HaloBroad", 12, 0.05f, 0.42f, 0.42f, 0.04f, 0.04f, 0.35f);
        }

        static Mesh Rays(string name, int count, float inner, float longLength, float shortLength, float longWidth,
            float shortWidth, float brightness)
        {
            var vertices = new Vector3[count * 3];
            var colors = new Color[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[count * 3];
            for (var i = 0; i < count; i++)
            {
                var a = 2f * Mathf.PI * i / count;
                var isLong = i % 2 == 0;
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                var side = new Vector3(-dir.y, dir.x, 0f) * (isLong ? longWidth : shortWidth);
                vertices[i * 3] = dir * inner + side;
                vertices[i * 3 + 1] = dir * inner - side;
                vertices[i * 3 + 2] = dir * (isLong ? longLength : shortLength);
                var root = new Color(brightness, brightness, brightness, 1f);
                colors[i * 3] = root;
                colors[i * 3 + 1] = root;
                colors[i * 3 + 2] = new Color(0f, 0f, 0f, 1f);
                var u = i / (float)count;
                uvs[i * 3] = uvs[i * 3 + 1] = uvs[i * 3 + 2] = new Vector2(u, 0.5f);
                triangles[i * 3] = i * 3;
                triangles[i * 3 + 1] = i * 3 + 1;
                triangles[i * 3 + 2] = i * 3 + 2;
            }

            var mesh = new Mesh { name = name, vertices = vertices, colors = colors, uv = uvs, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>中心の柔らかな光の玉（中心が明るく縁で 0）。</summary>
        static Mesh Core()
        {
            if (_core != null)
            {
                return _core;
            }

            const int segments = 24;
            var vertices = new Vector3[segments + 1];
            var colors = new Color[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;
            colors[0] = new Color(0.25f, 0.25f, 0.25f, 1f);
            uvs[0] = new Vector2(0f, 0.5f);
            for (var i = 0; i < segments; i++)
            {
                var a = 2f * Mathf.PI * i / segments;
                vertices[i + 1] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 0.2f;
                colors[i + 1] = new Color(0f, 0f, 0f, 1f);
                uvs[i + 1] = new Vector2(i / (float)segments, 0.5f);
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % segments + 1;
            }

            _core = new Mesh { name = "HaloCore", vertices = vertices, colors = colors, uv = uvs, triangles = triangles };
            _core.RecalculateBounds();
            return _core;
        }
    }
}
