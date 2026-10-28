using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 大当たりの虹（<see cref="ProtoTierLook"/>）。パチンコの大当たりの演出から借りた二つの形：
    /// 足もとに広がる虹の波紋と、弾けて回る虹色の星。どちらも自分で動いて消える使い捨ての演出で、当たり判定も影も持たない。
    /// 色は街と一緒に退色するが、輪が広がる・星が弾けるという形は残る（灰色の街でも大当たりと分かる）。
    /// </summary>
    public sealed class ProtoRainbowFx : MonoBehaviour
    {
        enum Kind
        {
            Ring,
            Star,
        }

        const int RingSegments = 40;

        static Mesh _starMesh;

        Kind _kind;
        Renderer _renderer;
        Mesh _ringMesh;
        Vector3 _velocity;
        float _delay;
        float _age;
        float _life;
        float _size;
        float _spin;
        float _scroll;

        /// <summary>開いた瞬間：足もとから虹の輪が三重に広がる。</summary>
        public static void Ripple(Material baseMaterial, Vector3 ground)
        {
            for (var i = 0; i < 3; i++)
            {
                var fx = Create(baseMaterial, "RainbowRing", ground + Vector3.up * (0.02f + 0.004f * i), Kind.Ring);
                fx._delay = 0.12f * i;
                fx._life = 0.7f;
                fx._size = 2.2f - 0.4f * i;
                fx._ringMesh = new Mesh { name = "RainbowRing" };
                fx.GetComponent<MeshFilter>().sharedMesh = fx._ringMesh;
                fx.UpdateRing(0f);
            }
        }

        /// <summary>開いた瞬間：虹色の星がまわりへ弾ける。</summary>
        public static void Burst(Material baseMaterial, Vector3 center, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var side = Quaternion.Euler(0f, 360f * i / count + Random.Range(-20f, 20f), 0f) * Vector3.forward;
                Star(baseMaterial, center, side * Random.Range(1.2f, 2f) + Vector3.up * Random.Range(1.8f, 2.6f),
                    Random.Range(0.18f, 0.26f), 0.9f);
            }
        }

        /// <summary>虹色の星を一つ。<paramref name="velocity"/> で飛び、少し落ちながら回って消える。</summary>
        public static void Star(Material baseMaterial, Vector3 position, Vector3 velocity, float size, float life)
        {
            var fx = Create(baseMaterial, "RainbowStar", position, Kind.Star);
            fx.GetComponent<MeshFilter>().sharedMesh = StarMesh();
            fx._velocity = velocity;
            fx._size = size;
            fx._life = life;
            fx._spin = Random.Range(-240f, 240f);
            fx._scroll = Random.value;
            fx.transform.localScale = Vector3.zero;
        }

        static ProtoRainbowFx Create(Material baseMaterial, string name, Vector3 position, Kind kind)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = ProtoTierLook.RainbowMaterial(baseMaterial);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var fx = go.AddComponent<ProtoRainbowFx>();
            fx._kind = kind;
            fx._renderer = renderer;
            return fx;
        }

        void Update()
        {
            if (_delay > 0f)
            {
                _delay -= Time.deltaTime;
                return;
            }

            _age += Time.deltaTime;
            var t = Mathf.Clamp01(_age / _life);
            if (_kind == Kind.Ring)
            {
                UpdateRing(t);
            }
            else
            {
                _velocity += Vector3.down * (4f * Time.deltaTime);
                _velocity *= 1f - 2f * Time.deltaTime;
                transform.position += _velocity * Time.deltaTime;
                // ぽんと膨らみ、後半で縮んで消える。
                var scale = t < 0.15f ? t / 0.15f : t > 0.6f ? 1f - (t - 0.6f) / 0.4f : 1f;
                transform.localScale = Vector3.one * (_size * scale);
                var camera = Camera.main;
                if (camera != null)
                {
                    transform.rotation = camera.transform.rotation * Quaternion.Euler(0f, 0f, _spin * _age);
                }
            }

            // 虹を流す（模様の色相がずれていく）。
            ProtoTierLook.ScrollRainbow(_renderer, _scroll + Time.time * 0.8f);

            if (_age >= _life)
            {
                Destroy(gameObject);
            }
        }

        void OnDestroy()
        {
            if (_ringMesh != null)
            {
                Destroy(_ringMesh);
            }
        }

        /// <summary>輪は広がりながら細くなって消える（透明にせず、幅で消す）。</summary>
        void UpdateRing(float t)
        {
            var eased = 1f - (1f - t) * (1f - t);
            var outer = Mathf.Lerp(0.15f, _size * 0.5f, eased);
            var inner = outer * Mathf.Lerp(0.7f, 1f, t);
            var vertices = new Vector3[RingSegments * 2 + 2];
            var uvs = new Vector2[vertices.Length];
            for (var i = 0; i <= RingSegments; i++)
            {
                var a = 2f * Mathf.PI * i / RingSegments;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                vertices[i * 2] = dir * inner;
                vertices[i * 2 + 1] = dir * outer;
                // 輪を一周すると虹が二回めぐる。
                var u = 2f * i / RingSegments;
                uvs[i * 2] = new Vector2(u, 0.5f);
                uvs[i * 2 + 1] = new Vector2(u, 0.5f);
            }

            if (_ringMesh.vertexCount != vertices.Length)
            {
                _ringMesh.Clear();
                _ringMesh.vertices = vertices;
                _ringMesh.uv = uvs;
                var triangles = new int[RingSegments * 12];
                for (var i = 0; i < RingSegments; i++)
                {
                    int a0 = i * 2, a1 = i * 2 + 1, b0 = i * 2 + 2, b1 = i * 2 + 3, k = i * 12;
                    // 上からも下からも見えるよう両面に張る。
                    triangles[k] = a0; triangles[k + 1] = b0; triangles[k + 2] = a1;
                    triangles[k + 3] = a1; triangles[k + 4] = b0; triangles[k + 5] = b1;
                    triangles[k + 6] = a0; triangles[k + 7] = a1; triangles[k + 8] = b0;
                    triangles[k + 9] = a1; triangles[k + 10] = b1; triangles[k + 11] = b0;
                }

                _ringMesh.triangles = triangles;
                // 両面を同じ頂点で張るので、法線は自動で求めない（表と裏が打ち消して 0 になる）。光るのは発光なので上向きで足りる。
                var normals = new Vector3[vertices.Length];
                for (var i = 0; i < normals.Length; i++)
                {
                    normals[i] = Vector3.up;
                }

                _ringMesh.normals = normals;
            }
            else
            {
                _ringMesh.vertices = vertices;
            }

            _ringMesh.RecalculateBounds();
        }

        /// <summary>五つの角の星（両面）。模様は斜めに走る虹（参考：虹色のグラデーションの星）。</summary>
        static Mesh StarMesh()
        {
            if (_starMesh != null)
            {
                return _starMesh;
            }

            var vertices = new Vector3[11];
            var uvs = new Vector2[11];
            vertices[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);
            for (var i = 0; i < 10; i++)
            {
                var a = Mathf.PI / 2f + Mathf.PI * i / 5f;
                var r = i % 2 == 0 ? 0.5f : 0.21f;
                var p = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
                vertices[i + 1] = p;
                uvs[i + 1] = new Vector2(0.5f + (p.x + p.y) * 0.9f, 0.5f);
            }

            var triangles = new int[10 * 6];
            for (var i = 0; i < 10; i++)
            {
                var a = i + 1;
                var b = (i + 1) % 10 + 1;
                triangles[i * 6] = 0; triangles[i * 6 + 1] = a; triangles[i * 6 + 2] = b;
                triangles[i * 6 + 3] = 0; triangles[i * 6 + 4] = b; triangles[i * 6 + 5] = a;
            }

            var normals = new Vector3[vertices.Length];
            for (var i = 0; i < normals.Length; i++)
            {
                normals[i] = Vector3.back;
            }

            _starMesh = new Mesh { name = "RainbowStar", vertices = vertices, uv = uvs, normals = normals, triangles = triangles };
            _starMesh.RecalculateBounds();
            return _starMesh;
        }
    }
}
