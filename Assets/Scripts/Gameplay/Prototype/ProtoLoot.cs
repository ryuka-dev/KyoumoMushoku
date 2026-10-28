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
    /// 見た目は品の段（<see cref="LootTier"/>・`Docs/切片の品目.md` 第三節）で変わる。宙の物は目の前に見えている事実なので、
    /// SAN が低くても段の見た目は消さない（SAN で消えるのは、開ける前の手応えだけ）。
    /// </summary>
    public sealed class ProtoLoot : MonoBehaviour
    {
        static readonly List<ProtoLoot> _active = new List<ProtoLoot>();

        /// <summary>いま掴める個体の一覧。</summary>
        public static IReadOnlyList<ProtoLoot> Active => _active;

        Rigidbody _body;
        Renderer _renderer;
        Light _glow;
        Transform _beam;
        TextMeshPro _label;
        Transform _pullTarget;
        Action<ProtoLoot> _onArrived;
        Vector3 _pullFrom;
        float _pullT;
        float _age;
        float _lifetime;
        float _baseScale;

        public ItemInstance Item { get; private set; }
        public string DisplayName { get; private set; }
        public LootTier Tier { get; private set; }
        public bool IsBeingPulled => _pullTarget != null;

        public static ProtoLoot Spawn(ItemInstance item, string displayName, int slots, LootTier tier,
            Color color, Material baseMaterial, TMP_FontAsset font, Vector3 position, Vector3 velocity, float lifetime)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Loot_" + item.Id;
            // 道具は実寸の 1.35 倍で作る約束（Tools/Models/kmlowpoly.py の PROP_SCALE）に合わせる。
            var size = 0.24f + 0.09f * Mathf.Clamp(slots, 1, 4);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * size;

            var loot = go.AddComponent<ProtoLoot>();
            loot.Item = item;
            loot.DisplayName = displayName;
            loot.Tier = tier;
            loot._lifetime = lifetime;
            loot._baseScale = size;

            loot._renderer = go.GetComponent<Renderer>();
            var material = new Material(baseMaterial);
            material.SetColor("_BaseColor", color);
            if (tier != LootTier.Common)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", EmissionOf(tier));
            }

            loot._renderer.sharedMaterial = material;

            if (tier >= LootTier.Rare)
            {
                var lightGo = new GameObject("Glow");
                lightGo.transform.SetParent(go.transform, false);
                loot._glow = lightGo.AddComponent<Light>();
                loot._glow.type = LightType.Point;
                loot._glow.color = tier == LootTier.Jackpot ? JackpotColor : RareColor;
                loot._glow.range = tier == LootTier.Jackpot ? 6f : 3.5f;
                loot._glow.intensity = 4f;
            }

            if (tier == LootTier.Jackpot)
            {
                loot._beam = BuildBeam(baseMaterial);
            }

            if (font != null)
            {
                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(go.transform, false);
                loot._label = labelGo.AddComponent<TextMeshPro>();
                loot._label.font = font;
                loot._label.text = displayName;
                loot._label.fontSize = 2.2f;
                loot._label.alignment = TextAlignmentOptions.Center;
                loot._label.color = tier == LootTier.Jackpot ? JackpotColor : tier == LootTier.Rare ? new Color(1f, 0.85f, 0.3f) : Color.white;
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
            if (_beam != null)
            {
                Destroy(_beam.gameObject);
            }
        }

        static readonly Color RareColor = new Color(1f, 0.8f, 0.3f);
        static readonly Color JackpotColor = new Color(0.75f, 0.92f, 1f);
        const float BeamHeight = 6f;

        static Color EmissionOf(LootTier tier) => tier switch
        {
            LootTier.Jackpot => JackpotColor * 5f,
            LootTier.Rare => new Color(1f, 0.8f, 0.2f) * 3f,
            _ => Color.white * 0.8f,
        };

        /// <summary>大当たりだけに立つ光の柱。遠くからでも「何か出た」と分かる。当たり判定は持たない。</summary>
        static Transform BuildBeam(Material baseMaterial)
        {
            var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name = "JackpotBeam";
            Destroy(beam.GetComponent<Collider>());
            beam.transform.localScale = new Vector3(0.08f, BeamHeight * 0.5f, 0.08f);
            var material = new Material(baseMaterial);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_BaseColor", JackpotColor);
            material.SetColor("_EmissionColor", JackpotColor * 6f);
            var renderer = beam.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return beam.transform;
        }

        void Update()
        {
            if (_label != null && Camera.main != null)
            {
                _label.transform.position = transform.position + Vector3.up * (_baseScale + 0.15f);
                _label.transform.rotation = Camera.main.transform.rotation;
            }

            if (_glow != null)
            {
                _glow.intensity = Tier == LootTier.Jackpot
                    ? 6f + Mathf.Sin(Time.time * 14f) * 2.5f
                    : 3f + Mathf.Sin(Time.time * 10f) * 1.5f;
            }

            if (_beam != null)
            {
                // 光の柱は品の真上に立ち、傾かない。掴んで引き寄せる間に消す。
                _beam.gameObject.SetActive(_pullTarget == null && _renderer.enabled);
                _beam.position = transform.position + Vector3.up * (BeamHeight * 0.5f);
                _beam.rotation = Quaternion.identity;
            }

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
                _renderer.enabled = Mathf.Repeat(_age * 8f, 1f) > 0.3f;
            }
        }
    }
}
