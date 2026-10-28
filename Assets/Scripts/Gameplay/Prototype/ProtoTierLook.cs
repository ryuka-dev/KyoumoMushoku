using KyoumoMushoku.Core.Foraging;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 品の段（珍しさ）の見た目の辞書（`Docs/GameDesign.md` 第十七節「色の言葉」）。宙の物（<see cref="ProtoLoot"/>）と、
    /// 叩いたときに漏れる光（<see cref="ProtoTrashCan"/>）が同じ言葉で話す。
    ///
    /// 色は三つの層に分け、層をまたいで同じ色を使わない：
    /// - 何ができるか（<see cref="ProtoInteractColors"/>）：探る＝琥珀、使う＝水色。縁と照準の輪だけ。
    /// - どれだけの値打ちか（ここ）：やや珍しい＝緑・珍しい＝紫・大当たり＝虹（パチンコの「虹」）。
    /// - 読めない（ここ）：白。SAN が足りず段が分からない「何か良い物」。
    ///
    /// 色は SAN の退色を受ける（灰色の街では虹も灰になる）。だから段は**形**でも語る：
    /// やや珍しい＝静かに光るだけ・珍しい＝まわりで小さな菱形が瞬く・大当たり＝五つの角の星（開いた瞬間は星が弾け、
    /// 足もとに虹の輪が広がる。名前は一字ずつ虹）。星は大当たりだけの形で、叩いたときに漏れる光でも同じ。
    /// </summary>
    public static class ProtoTierLook
    {
        public static readonly Color Uncommon = new Color(0.35f, 0.95f, 0.45f);
        public static readonly Color Rare = new Color(0.72f, 0.42f, 1f);
        public static readonly Color Unknown = new Color(0.95f, 0.95f, 0.95f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.3f);

        /// <summary>虹の色相が一周する秒数。速すぎると下品になる。</summary>
        const float RainbowPeriod = 2.4f;

        /// <summary>虹の彩度。1 にすると原色が眩しすぎる（やりすぎない）。</summary>
        const float RainbowSaturation = 0.7f;

        /// <summary>大当たりの虹。<paramref name="offset"/>（0〜1）でずらすと、並んだ火花が別々の色になる。</summary>
        public static Color Rainbow(float time, float offset = 0f) =>
            Color.HSVToRGB(Mathf.Repeat(time / RainbowPeriod + offset, 1f), RainbowSaturation, 1f);

        /// <summary>段の色（大当たりは時刻で変わる）。並は色を持たない（白を返すが、光らせない）。</summary>
        public static Color Of(LootTier tier, float time) => tier switch
        {
            LootTier.Jackpot => Rainbow(time),
            LootTier.Rare => Rare,
            LootTier.Uncommon => Uncommon,
            _ => Color.white,
        };

        /// <summary>段の縁取りの太さ（画面のピクセル）。品そのものは塗らず、元の材質を見せる。並は縁を持たない。</summary>
        public static float OutlineWidth(LootTier tier) => tier switch
        {
            LootTier.Jackpot => 3.5f,
            LootTier.Rare => 3f,
            LootTier.Uncommon => 2.5f,
            _ => 0f,
        };

        static readonly int OutlineColorId = Shader.PropertyToID("_Color");
        static readonly int OutlineWidthId = Shader.PropertyToID("_Width");
        static readonly System.Collections.Generic.Dictionary<Mesh, Mesh> _shells = new System.Collections.Generic.Dictionary<Mesh, Mesh>();
        static Material _outlineMaterial;

        /// <summary>
        /// 品の縁取り：模型の各部品に裏返しの殻を重ね、段の色で縁を描く（触れる物の縁と同じシェーダー `ProtoOutline`）。
        /// 元は `Assets/Prototype/Resources/ProtoTierOutline.mat`。殻の網は模型ごとに一度だけ作って使い回す（模型の FBX は読み取り可にしてある）。
        /// 返すのは殻の描画（大当たりの虹は毎こま <see cref="TintOutline"/> で塗り直す）。
        /// </summary>
        public static Renderer[] AddOutline(Transform root, LootTier tier)
        {
            if (tier == LootTier.Common)
            {
                return System.Array.Empty<Renderer>();
            }

            _outlineMaterial ??= Resources.Load<Material>("ProtoTierOutline");
            if (_outlineMaterial == null)
            {
                return System.Array.Empty<Renderer>();
            }

            var shells = new System.Collections.Generic.List<Renderer>();
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;
                if (mesh == null || !mesh.isReadable)
                {
                    continue;
                }

                var go = new GameObject("TierOutline");
                go.transform.SetParent(filter.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = ShellOf(mesh);
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _outlineMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                TintOutline(renderer, Of(tier, 0f), OutlineWidth(tier));
                shells.Add(renderer);
            }

            return shells.ToArray();
        }

        /// <summary>縁の色と太さを、共有の材質を書き換えずに塗る。</summary>
        public static void TintOutline(Renderer renderer, Color color, float width)
        {
            _block ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_block);
            _block.SetColor(OutlineColorId, color);
            _block.SetFloat(OutlineWidthId, width);
            renderer.SetPropertyBlock(_block);
        }

        /// <summary>
        /// 殻の網：同じ位置にある頂点の法線を平均する（角で殻が割れない）。編集の道具 SliceOutlineBaker の殻と同じ作り方。
        /// </summary>
        static Mesh ShellOf(Mesh source)
        {
            if (_shells.TryGetValue(source, out var cached))
            {
                return cached;
            }

            var vertices = source.vertices;
            var normals = source.normals;
            var sums = new System.Collections.Generic.Dictionary<Vector3Int, System.Collections.Generic.List<Vector3>>();
            Vector3Int Key(Vector3 v) => new Vector3Int(Mathf.RoundToInt(v.x * 10000f), Mathf.RoundToInt(v.y * 10000f), Mathf.RoundToInt(v.z * 10000f));
            for (var i = 0; i < vertices.Length; i++)
            {
                var n = normals.Length == vertices.Length ? normals[i] : Vector3.up;
                if (!sums.TryGetValue(Key(vertices[i]), out var list))
                {
                    list = new System.Collections.Generic.List<Vector3>();
                    sums.Add(Key(vertices[i]), list);
                }

                // 向きの同じ法線は一度だけ数える（同じ面の頂点が多い所に引っ張られない）。
                var known = false;
                foreach (var existing in list)
                {
                    known |= Vector3.Dot(existing, n) > 0.999f;
                }

                if (!known)
                {
                    list.Add(n);
                }
            }

            var smooth = new Vector3[vertices.Length];
            for (var i = 0; i < vertices.Length; i++)
            {
                var sum = Vector3.zero;
                foreach (var n in sums[Key(vertices[i])])
                {
                    sum += n;
                }

                smooth[i] = sum.sqrMagnitude > 1e-8f ? sum.normalized : Vector3.up;
            }

            var triangles = new System.Collections.Generic.List<int>();
            for (var s = 0; s < source.subMeshCount; s++)
            {
                triangles.AddRange(source.GetTriangles(s));
            }

            var shell = new Mesh { name = source.name + "_TierOutline", vertices = vertices, normals = smooth };
            shell.SetTriangles(triangles, 0);
            shell.RecalculateBounds();
            _shells[source] = shell;
            return shell;
        }

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static MaterialPropertyBlock _block;
        static Material _sparkMaterial;

        /// <summary>火花・漏れる光に使う共有の材質（色は個体ごとに <see cref="Tint"/> で塗る）。</summary>
        public static Material SparkMaterial(Material baseMaterial)
        {
            if (_sparkMaterial == null && baseMaterial != null)
            {
                _sparkMaterial = new Material(baseMaterial) { name = "ProtoSpark" };
                _sparkMaterial.EnableKeyword("_EMISSION");
            }

            return _sparkMaterial;
        }

        /// <summary>共有の材質を書き換えずに、その描画だけの色と発光を塗る。</summary>
        public static void Tint(Renderer renderer, Color color, float emission)
        {
            _block ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            _block.SetColor(EmissionColorId, color * emission);
            renderer.SetPropertyBlock(_block);
        }

        /// <summary>演出（後光・虹の星・輪・粒）の描画順。品の名前より先に描き、字を光で隠さない。</summary>
        public const int EffectSortingOrder = -10;

        static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");
        static Texture2D _rainbowTexture;
        static Material _rainbowMaterial;

        /// <summary>
        /// 虹の模様の材質（大当たりの輪と星）。色相を一周する細長い模様を、地の色と発光の両方に貼る。
        /// 模様の位置を <see cref="ScrollRainbow"/> でずらすと、虹が流れる。
        /// </summary>
        public static Material RainbowMaterial(Material baseMaterial)
        {
            if (_rainbowMaterial != null || baseMaterial == null)
            {
                return _rainbowMaterial;
            }

            _rainbowMaterial = new Material(baseMaterial) { name = "ProtoRainbow" };
            _rainbowMaterial.SetTexture("_BaseMap", RainbowTexture());
            _rainbowMaterial.SetColor("_BaseColor", Color.white);
            _rainbowMaterial.EnableKeyword("_EMISSION");
            _rainbowMaterial.SetTexture("_EmissionMap", RainbowTexture());
            _rainbowMaterial.SetColor("_EmissionColor", Color.white * 1.4f);
            return _rainbowMaterial;
        }

        static Material _rainbowGlow;

        /// <summary>色相を一周する細長い模様（繰り返し）。</summary>
        static Texture2D RainbowTexture()
        {
            if (_rainbowTexture != null)
            {
                return _rainbowTexture;
            }

            _rainbowTexture = new Texture2D(64, 1, TextureFormat.RGBA32, false)
            {
                name = "ProtoRainbow",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
            };
            for (var x = 0; x < 64; x++)
            {
                _rainbowTexture.SetPixel(x, 0, Color.HSVToRGB(x / 64f, RainbowSaturation, 1f));
            }

            _rainbowTexture.Apply();
            return _rainbowTexture;
        }

        /// <summary>
        /// 加算で光る虹の材質（大当たりの後光）。透ける光なので、頂点色の明るさで筋の先を淡く消せる。
        /// 元の材質は `Assets/Prototype/Resources/ProtoRainbowGlow.mat`（URP の Particles/Unlit・加算。ビルドに含めるため Resources に置く）。
        /// </summary>
        public static Material RainbowGlowMaterial()
        {
            if (_rainbowGlow != null)
            {
                return _rainbowGlow;
            }

            var source = Resources.Load<Material>("ProtoRainbowGlow");
            if (source == null)
            {
                return null;
            }

            _rainbowGlow = new Material(source) { name = "ProtoRainbowGlow" };
            _rainbowGlow.SetTexture("_BaseMap", RainbowTexture());
            return _rainbowGlow;
        }

        /// <summary>虹の模様を <paramref name="offset"/> だけずらす（共有の材質は書き換えない）。</summary>
        public static void ScrollRainbow(Renderer renderer, float offset)
        {
            _block ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_block);
            _block.SetVector(BaseMapStId, new Vector4(1f, 1f, offset, 0f));
            renderer.SetPropertyBlock(_block);
        }

        /// <summary>
        /// 名前の文字を一字ずつ虹に塗る（パチンコの「大当り」の字）。文字の網を作り直さず、色だけを書き換える。
        /// </summary>
        public static void PaintRainbowText(TMPro.TextMeshPro label, float time)
        {
            var info = label.textInfo;
            if (info == null || info.characterCount == 0)
            {
                return;
            }

            for (var i = 0; i < info.characterCount; i++)
            {
                var character = info.characterInfo[i];
                if (!character.isVisible)
                {
                    continue;
                }

                var colors = info.meshInfo[character.materialReferenceIndex].colors32;
                // 字は細いので、品や輪より鮮やかにする（淡いと金の縁と光のにじみで白く見える）。
                var color = (Color32)Color.HSVToRGB(Mathf.Repeat(time / RainbowPeriod - i * 0.06f, 1f), 0.95f, 1f);
                for (var k = 0; k < 4; k++)
                {
                    colors[character.vertexIndex + k] = color;
                }
            }

            label.UpdateVertexData(TMPro.TMP_VertexDataUpdateFlags.Colors32);
        }

        /// <summary>当たり判定と影を持たない小さな光の粒。</summary>
        public static GameObject Spark(Material baseMaterial, Vector3 position, float size, Color color, float emission)
        {
            var spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spark.name = "Spark";
            Object.Destroy(spark.GetComponent<Collider>());
            spark.transform.position = position;
            spark.transform.localScale = Vector3.one * size;
            spark.transform.rotation = Quaternion.Euler(45f, 45f, 0f);
            var renderer = spark.GetComponent<Renderer>();
            renderer.sharedMaterial = SparkMaterial(baseMaterial);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.sortingOrder = EffectSortingOrder;
            Tint(renderer, color, emission);
            return spark;
        }
    }
}
