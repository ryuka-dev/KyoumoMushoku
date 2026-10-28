using System;
using System.Collections.Generic;
using KyoumoMushoku.Core.Foraging;
using KyoumoMushoku.Core.Items;
using TMPro;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// ゴミ箱から飛び出した1個体（試作）。宙を舞い、落ちてしばらくすると消える。
    /// 消えるまでの間に掴めば鞄へ入る。何が出たかは叩く前に確定していた中身そのものである。
    ///
    /// 見た目は品の段（<see cref="LootTier"/>）で変わる。色と形の辞書は <see cref="ProtoTierLook"/>：
    /// やや珍しい＝緑に静かに光る・珍しい＝紫で、まわりに小さな菱形が瞬く・大当たり＝虹で、ときどき虹の星が弾け、名前が一字ずつ虹に流れる。
    /// （開いた瞬間の虹の輪と星の弾けは <see cref="ProtoTrashCan"/> が出す。）
    /// 宙の物は目の前に見えている事実なので、段の演出そのものは SAN で消さない。色は街と一緒に退色するが、形は残る。
    /// </summary>
    public sealed class ProtoLoot : MonoBehaviour
    {
        static readonly List<ProtoLoot> _active = new List<ProtoLoot>();

        /// <summary>いま掴める個体の一覧。</summary>
        public static IReadOnlyList<ProtoLoot> Active => _active;

        Rigidbody _body;
        Renderer[] _renderers;
        Material[] _bodyMaterials = Array.Empty<Material>();
        Material _sparkBase;
        Vector3 _extents;
        float _sparkTimer;
        readonly List<Spark> _sparks = new List<Spark>();

        struct Spark
        {
            public Transform Transform;
            public Vector3 Velocity;
            public float Age;
            public float Life;
            public float Size;
            public bool Twinkle;
        }
        TextMeshPro _label;
        Transform _pullTarget;
        Action<ProtoLoot> _onArrived;
        Vector3 _pullFrom;
        float _pullT;
        float _age;
        float _lifetime;
        float _baseScale;
        float _labelHeight;

        public ItemInstance Item { get; private set; }
        public string DisplayName { get; private set; }
        public LootTier Tier { get; private set; }
        public bool IsBeingPulled => _pullTarget != null;

        /// <summary>
        /// 宙に飛ばす。<paramref name="model"/>（`Assets/Art/Models/Props/Items/Item_*.fbx`）があればそれを容器の子に置き、
        /// 当たり判定は模型の外形の箱にする（FBX の根は軸変換の回転を持つので、直接回さない）。無ければ色付きの立方体。
        /// </summary>
        public static ProtoLoot Spawn(ItemInstance item, string displayName, int slots, LootTier tier,
            Color color, Material baseMaterial, TMP_FontAsset font, Vector3 position, Vector3 velocity, float lifetime,
            GameObject model = null)
        {
            GameObject go;
            float labelHeight;
            float fontScale;
            Vector3 extents;
            if (model != null)
            {
                go = new GameObject("Loot_" + item.Id);
                go.transform.position = position;
                var shape = Instantiate(model, go.transform, false);
                var bounds = LocalBounds(go.transform, shape);
                // 容器の原点を模型の中心に合わせる（回転が中心で起きる）。
                shape.transform.localPosition -= bounds.center;
                var box = go.AddComponent<BoxCollider>();
                box.size = Vector3.Max(bounds.size, Vector3.one * 0.05f);
                labelHeight = bounds.extents.y + 0.18f;
                fontScale = 0.4f;
                extents = bounds.extents;
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Loot_" + item.Id;
                // 道具は実寸の 1.35 倍で作る約束（Tools/Models/kmlowpoly.py の PROP_SCALE）に合わせる。
                var size = 0.24f + 0.09f * Mathf.Clamp(slots, 1, 4);
                go.transform.position = position;
                go.transform.localScale = Vector3.one * size;
                labelHeight = size + 0.15f;
                fontScale = 1f;
                extents = Vector3.one * (size * 0.5f);
                var material = new Material(baseMaterial);
                material.SetColor("_BaseColor", color);
                go.GetComponent<Renderer>().sharedMaterial = material;
            }

            var loot = go.AddComponent<ProtoLoot>();
            loot.Item = item;
            loot.DisplayName = displayName;
            loot.Tier = tier;
            loot._lifetime = lifetime;
            loot._baseScale = go.transform.localScale.x;
            loot._labelHeight = labelHeight;
            loot._renderers = go.GetComponentsInChildren<Renderer>();
            loot._extents = extents;
            loot._sparkBase = baseMaterial;

            // 段の色で品そのものを光らせる：材質を個体ごとに複製する（共有の調色板材質は書き換えない）。
            if (tier != LootTier.Common)
            {
                loot._bodyMaterials = new Material[loot._renderers.Length];
                for (var i = 0; i < loot._renderers.Length; i++)
                {
                    var glowing = new Material(loot._renderers[i].sharedMaterial);
                    glowing.EnableKeyword("_EMISSION");
                    glowing.SetColor("_EmissionColor", ProtoTierLook.Of(tier, 0f) * ProtoTierLook.BodyEmission(tier));
                    loot._renderers[i].sharedMaterial = glowing;
                    loot._bodyMaterials[i] = glowing;
                }
            }

            // 点光源は持たない：夜の暗い地面では弱い光でも色の水たまりになり、段が「光の量」の話になる。
            // 段は品の発光の色と、星・柱・火花の形で語る。

            if (font != null)
            {
                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(go.transform, false);
                loot._label = labelGo.AddComponent<TextMeshPro>();
                loot._label.font = font;
                loot._label.text = displayName;
                loot._label.fontSize = 2.2f * fontScale;
                loot._label.alignment = TextAlignmentOptions.Center;
                loot._label.color = ProtoTierLook.Of(tier, 0f);
                if (tier == LootTier.Jackpot)
                {
                    // パチンコの「大当り」の字：金の縁取り（文字の色は一字ずつ虹に塗る）。
                    loot._label.color = Color.white;
                    loot._label.outlineColor = (Color32)ProtoTierLook.Gold;
                    loot._label.outlineWidth = 0.12f;
                    loot._label.fontStyle = FontStyles.Bold;
                    loot._label.ForceMeshUpdate();
                    ProtoTierLook.PaintRainbowText(loot._label, Time.time);
                }
                loot._label.rectTransform.sizeDelta = new Vector2(4f, 1f);
            }

            loot._body = go.AddComponent<Rigidbody>();
            loot._body.mass = 0.3f * slots;
            loot._body.linearVelocity = velocity;
            loot._body.angularVelocity = UnityEngine.Random.insideUnitSphere * 12f;
            loot._body.interpolation = RigidbodyInterpolation.Interpolate;
            return loot;
        }

        /// <summary>手元へ引き寄せる。着いたら <paramref name="onArrived"/> を呼ぶ。</summary>
        public void PullTo(Transform target, Action<ProtoLoot> onArrived)
        {
            _pullTarget = target;
            _onArrived = onArrived;
            _pullFrom = transform.position;
            _pullT = 0f;
            _body.isKinematic = true;
        }

        void OnEnable() => _active.Add(this);

        void OnDisable() => _active.Remove(this);

        void OnDestroy()
        {
            foreach (var spark in _sparks)
            {
                if (spark.Transform != null)
                {
                    Destroy(spark.Transform.gameObject);
                }
            }

            foreach (var material in _bodyMaterials)
            {
                Destroy(material);
            }
        }

        /// <summary>模型の外形（容器の局所座標）。</summary>
        static Bounds LocalBounds(Transform container, GameObject shape)
        {
            var renderers = shape.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.one * 0.2f);
            }

            var bounds = new Bounds(container.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);
            foreach (var renderer in renderers)
            {
                var b = renderer.bounds;
                for (var i = 0; i < 8; i++)
                {
                    var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    bounds.Encapsulate(container.InverseTransformPoint(corner));
                }
            }

            return bounds;
        }

        /// <summary>
        /// 段の形：珍しいはまわりで星が瞬き（その場で膨らんで消える）、大当たりは足もとから火花が昇る（金と虹が交互）。
        /// 掴んで引き寄せている間と、消える前の明滅の間は出さない。
        /// </summary>
        void UpdateSparks(bool emit)
        {
            if (emit && Tier >= LootTier.Rare)
            {
                _sparkTimer -= Time.deltaTime;
                if (_sparkTimer <= 0f)
                {
                    if (Tier == LootTier.Jackpot)
                    {
                        // 地面にある間は静かに：ときどき一つだけ虹の星が弾ける。
                        _sparkTimer = 0.4f;
                        var side = UnityEngine.Random.insideUnitCircle.normalized * 0.7f;
                        ProtoRainbowFx.Star(_sparkBase, transform.position + Vector3.up * _extents.y,
                            new Vector3(side.x, 1.9f, side.y), UnityEngine.Random.Range(0.12f, 0.18f), 0.9f);
                    }
                    else
                    {
                        _sparkTimer = 0.17f;
                        var offset = Vector3.Scale(UnityEngine.Random.onUnitSphere, _extents + Vector3.one * 0.06f);
                        AddSpark(transform.position + offset, Vector3.zero, 0.45f, 0.07f, true, ProtoTierLook.Rare, 2.5f);
                    }
                }
            }

            for (var i = _sparks.Count - 1; i >= 0; i--)
            {
                var spark = _sparks[i];
                spark.Age += Time.deltaTime;
                if (spark.Transform == null || spark.Age >= spark.Life)
                {
                    if (spark.Transform != null)
                    {
                        Destroy(spark.Transform.gameObject);
                    }

                    _sparks.RemoveAt(i);
                    continue;
                }

                var t = spark.Age / spark.Life;
                spark.Transform.position += spark.Velocity * Time.deltaTime;
                spark.Transform.Rotate(0f, 360f * Time.deltaTime, 0f, Space.World);
                // 瞬く星は膨らんで縮む。昇る火花は小さくなって消える。
                var scale = spark.Twinkle ? Mathf.Sin(t * Mathf.PI) : 1f - t;
                spark.Transform.localScale = Vector3.one * (spark.Size * scale);
                _sparks[i] = spark;
            }
        }

        void AddSpark(Vector3 position, Vector3 velocity, float life, float size, bool twinkle, Color color, float emission)
        {
            var go = ProtoTierLook.Spark(_sparkBase, position, twinkle ? 0f : size, color, emission);
            _sparks.Add(new Spark { Transform = go.transform, Velocity = velocity, Life = life, Size = size, Twinkle = twinkle });
        }

        /// <summary>大当たりの虹：品・光・柱・名前の色をそろえて回す。</summary>
        void UpdateRainbow()
        {
            var color = ProtoTierLook.Rainbow(Time.time);
            foreach (var material in _bodyMaterials)
            {
                material.SetColor("_EmissionColor", color * ProtoTierLook.BodyEmission(LootTier.Jackpot));
            }

            if (_label != null)
            {
                ProtoTierLook.PaintRainbowText(_label, Time.time);
            }
        }

        void Update()
        {
            if (_label != null && Camera.main != null)
            {
                var bounce = Tier == LootTier.Jackpot ? Mathf.Abs(Mathf.Sin(Time.time * 5f)) * 0.06f : 0f;
                _label.transform.position = transform.position + Vector3.up * (_labelHeight + bounce);
                _label.transform.rotation = Camera.main.transform.rotation;
            }

            if (Tier == LootTier.Jackpot)
            {
                UpdateRainbow();
            }

            var blinking = _age > _lifetime - 1f;
            UpdateSparks(_pullTarget == null && !blinking);

            if (_pullTarget != null)
            {
                // 伸びた腕が掴んで縮む。最後に加速させると「掴んだ」感じが出る。
                _pullT += Time.deltaTime / 0.18f;
                var eased = _pullT * _pullT;
                transform.position = Vector3.Lerp(_pullFrom, _pullTarget.position, eased);
                transform.localScale = Vector3.one * _baseScale * Mathf.Lerp(1f, 0.4f, eased);
                if (_pullT >= 1f)
                {
                    _onArrived?.Invoke(this);
                    Destroy(gameObject);
                }

                return;
            }

            _age += Time.deltaTime;
            if (_age > _lifetime)
            {
                Destroy(gameObject);
            }
            else if (_age > _lifetime - 1f)
            {
                // 消える直前は明滅させて「もうすぐ消える」を見せる。
                var visible = Mathf.Repeat(_age * 8f, 1f) > 0.3f;
                foreach (var renderer in _renderers)
                {
                    renderer.enabled = visible;
                }
            }
        }
    }
}
