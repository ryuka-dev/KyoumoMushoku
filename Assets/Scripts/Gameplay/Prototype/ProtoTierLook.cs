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

        /// <summary>品そのものの発光の強さ。白飛びして形が読めなくならない程度に抑える。</summary>
        public static float BodyEmission(LootTier tier) => tier switch
        {
            LootTier.Jackpot => 0.8f,
            LootTier.Rare => 0.7f,
            LootTier.Uncommon => 0.45f,
            _ => 0f,
        };

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
            _rainbowMaterial = new Material(baseMaterial) { name = "ProtoRainbow" };
            _rainbowMaterial.SetTexture("_BaseMap", _rainbowTexture);
            _rainbowMaterial.SetColor("_BaseColor", Color.white);
            _rainbowMaterial.EnableKeyword("_EMISSION");
            _rainbowMaterial.SetTexture("_EmissionMap", _rainbowTexture);
            _rainbowMaterial.SetColor("_EmissionColor", Color.white * 1.4f);
            return _rainbowMaterial;
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
                var color = (Color32)Rainbow(time, -i * 0.06f);
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
            Tint(renderer, color, emission);
            return spark;
        }
    }
}
