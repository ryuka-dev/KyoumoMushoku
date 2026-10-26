using System;
using System.Collections.Generic;
using KyoumoMushoku.Core.Items;
using TMPro;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>試作での珍しさ。産出テーブルの重みから導く（<see cref="Foraging.TrashCanLootAsset.ShareOf"/>）。</summary>
    public enum ProtoRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
    }

    /// <summary>
    /// ゴミ箱から飛び出した1個体（試作）。宙を舞い、落ちてしばらくすると消える。
    /// 消えるまでの間に掴めば鞄へ入る。何が出たかは叩く前に確定していた中身そのものである。
    /// </summary>
    public sealed class ProtoLoot : MonoBehaviour
    {
        static readonly List<ProtoLoot> _active = new List<ProtoLoot>();

        /// <summary>いま掴める個体の一覧。</summary>
        public static IReadOnlyList<ProtoLoot> Active => _active;

        Rigidbody _body;
        Renderer _renderer;
        Light _glow;
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
        public ProtoRarity Rarity { get; private set; }
        public bool IsBeingPulled => _pullTarget != null;

        public static ProtoLoot Spawn(ItemInstance item, string displayName, int slots, ProtoRarity rarity,
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
            loot.Rarity = rarity;
            loot._lifetime = lifetime;
            loot._baseScale = size;

            loot._renderer = go.GetComponent<Renderer>();
            var material = new Material(baseMaterial);
            material.SetColor("_BaseColor", color);
            if (rarity != ProtoRarity.Common)
            {
                var emission = rarity == ProtoRarity.Rare ? new Color(1f, 0.8f, 0.2f) * 3f : Color.white * 0.8f;
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }

            loot._renderer.sharedMaterial = material;

            if (rarity == ProtoRarity.Rare)
            {
                var lightGo = new GameObject("Glow");
                lightGo.transform.SetParent(go.transform, false);
                loot._glow = lightGo.AddComponent<Light>();
                loot._glow.type = LightType.Point;
                loot._glow.color = new Color(1f, 0.8f, 0.3f);
                loot._glow.range = 3.5f;
                loot._glow.intensity = 4f;
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
                loot._label.color = rarity == ProtoRarity.Rare ? new Color(1f, 0.85f, 0.3f) : Color.white;
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

        void Update()
        {
            if (_label != null && Camera.main != null)
            {
                _label.transform.position = transform.position + Vector3.up * (_baseScale + 0.15f);
                _label.transform.rotation = Camera.main.transform.rotation;
            }

            if (Rarity == ProtoRarity.Rare && _glow != null)
            {
                _glow.intensity = 3f + Mathf.Sin(Time.time * 10f) * 1.5f;
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
