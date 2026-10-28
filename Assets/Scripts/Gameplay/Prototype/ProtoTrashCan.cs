using System;
using System.Collections;
using System.Collections.Generic;
using KyoumoMushoku.Core.Foraging;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Randomness;
using KyoumoMushoku.Core.Survival;
using KyoumoMushoku.Gameplay.Foraging;
using KyoumoMushoku.Gameplay.Items;
using TMPro;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 叩くゴミ箱（試作・Docs/一人称試作.md）。
    ///
    /// 中身は叩く前に確定している（本編の <c>TrashCan._nextDraw</c> と同じ考え方）。
    /// 叩いた時の揺れ・音・漏れる光は、その確定済みの中身から作る。再抽選しないので、反応は嘘をつかない。
    /// 抽選は本編と同じ Core の <see cref="LootTable"/>。表は切片用に複製した <c>Assets/Config/TrashCanLoot_Slice.asset</c>（タバコを減らしてある）。
    ///
    /// 手応えをどこまで読めるかは叩いた人の SAN で決まる（<see cref="HitTellSight"/>・`Docs/切片の品目.md` 第三節）。
    /// 上機嫌なら漏れる光の色で珍しいか大当たりかまで、崩壊していればどの箱も同じ手応えになる。読めないときは合図が消えるだけで、
    /// 偽の合図は出さない。街に響く音（警官の耳）は中身どおりのまま（変わるのは本人の受け取り方だけ）。
    ///
    /// ふたの無いゴミ袋にも使う（<c>_hasLid</c> を外し、一打で破れる少量の袋にする）。
    ///
    /// 中身が戻るのは本編と同じ二つの契機：日付が変わったとき（就寝）と、夜になったとき（<see cref="ForageRefillTracker"/>）。
    /// 開けた箱も、開けていない箱も、その時点の時間帯のテーブルで引き直す。
    ///
    /// 開けて漁ると、本編と同じく時計が進み、その間の渇きと空腹が減る（種類ごとの <c>forageClockSeconds</c>）。
    /// 一日に開けられる数が昼の長さで決まり、どの箱を開けるかが判断になる。廃棄の袋は勝手に噴き出すので時計を進めない。
    /// </summary>
    public sealed class ProtoTrashCan : MonoBehaviour
    {
        [SerializeField] TrashCanKind _kind = TrashCanKind.Park;
        [SerializeField] TrashCanLootAsset _loot;
        [SerializeField] ItemDatabaseAsset _catalog;
        [SerializeField] ProtoDayNight _dayNight;
        [SerializeField] Material _itemMaterial;
        [SerializeField] TMP_FontAsset _font;

        [Tooltip("品の段（珍しさ）と、SAN の保底の出典。")]
        [SerializeField] SliceTuningAsset _slice;

        [Tooltip("叩く人の状態。中身を引き直すとき、その SAN で表を作り直す（保底）。")]
        [SerializeField] ProtoVitals _reader;

        [Tooltip("1回ふたが開いたときに引く回数。本編の1日分の湧きに当たる。飛び出す数が鞄より多いと、掴む判断が生まれる。")]
        [SerializeField, Min(1)] int _drawsPerOpen = 3;

        [Tooltip("ふたが開くまでに叩く回数。")]
        [SerializeField, Min(1)] int _hitsToOpen = 4;

        [Tooltip("ふたがあるか。ゴミ袋なら外す（破れて中身が出る）。")]
        [SerializeField] bool _hasLid = true;

        [Tooltip("飛び出した物が消えるまでの秒数。")]
        [SerializeField, Min(1f)] float _lootLifetime = 6f;

        readonly List<LootDraw> _contents = new List<LootDraw>();
        readonly ForageRefillTracker _refill = new ForageRefillTracker();
        IRng _rng;
        AudioSource _audio;
        Transform _lid;
        Vector3 _lidLocalPosition;
        Quaternion _lidLocalRotation;
        Vector3 _lidLocalScale;
        Transform _lidParent;
        Collider _collider;
        Quaternion _restRotation;
        Vector3 _restScale;
        Vector2 _tilt;
        Vector2 _tiltVelocity;
        float _squash;
        int _hits;
        bool _opened;
        bool _drawnForNight;
        bool _disposal;
        float _spewInterval;

        /// <summary>ふたが開いたか、またはもう何も出ない。</summary>
        public bool IsOpened => _opened;

        /// <summary>今の中身になってから叩かれた回数（補充で 0 に戻る）。蠅が驚くのに使う。</summary>
        public int Hits => _hits;

        /// <summary>照準の案内（開く前だけ使う。開いた箱は案内しない）。</summary>
        public string Prompt => _hasLid ? ProtoText.TrashCanPrompt : ProtoText.TrashBagPrompt;

        public void Configure(TrashCanKind kind, TrashCanLootAsset loot, ItemDatabaseAsset catalog,
            ProtoDayNight dayNight, Material itemMaterial, TMP_FontAsset font, SliceTuningAsset slice)
        {
            _slice = slice;
            _kind = kind;
            _loot = loot;
            _catalog = catalog;
            _dayNight = dayNight;
            _itemMaterial = itemMaterial;
            _font = font;
        }

        void Awake()
        {
            _rng = new SystemRng();
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.spatialBlend = 1f;
            _audio.playOnAwake = false;
            _restRotation = transform.localRotation;
            _restScale = transform.localScale;
            _collider = GetComponent<Collider>();
            FindOrBuildLid();
        }

        void OnEnable() => Refill();

        /// <summary>
        /// 叩く。戻り値はヒットストップの強さ（0〜1）。開いた後は 0。
        /// <paramref name="loudness"/> は叩き方の音の大きさ（連打で 1、間を空けると小さい）。箱の中身による響きに掛かる。
        /// <paramref name="vitals"/> は叩いた人の状態。開けて漁った時間ぶん、渇きと空腹が減る。
        /// <paramref name="gloved"/> なら（軍手）、ふたが一打早く開く。
        /// <paramref name="planted"/> が空でなく、まだ一度も叩かれていない箱なら、最初の一打の手応えを作る前に中身の一つをその品に替える
        /// （最初の大当たり。手応えより先に中身を決めるので、漏れる光も揺れも嘘をつかない）。替えたら true を返す。
        /// </summary>
        public float Hit(Vector3 direction, float loudness, Vitals vitals, bool gloved, ItemId planted, out bool plantedNow,
            out string toast)
        {
            plantedNow = false;
            toast = null;
            if (_opened)
            {
                toast = ProtoText.Depleted;
                _audio.pitch = 1.4f;
                _audio.PlayOneShot(ProtoSounds.Clang, 0.3f);
                ProtoNoise.Emit(transform.position, 0.3f * loudness);
                return 0f;
            }

            if (!planted.IsEmpty && _hits == 0 && !_disposal && _contents.Count > 0)
            {
                // 空振りの一つがあればそこを、無ければ最初の一つを替える（他の当たりは残す）。
                var slot = _contents.FindIndex(draw => !draw.HasItem);
                _contents[slot >= 0 ? slot : 0] = LootDraw.Of(planted, FoodState.Fresh);
                plantedNow = true;
            }

            _hits++;
            var weight = ContentWeight();
            var empty = weight <= 0;
            var heaviness = Mathf.Clamp01(weight / 6f);
            var tell = HitTellSight.Read(vitals != null ? vitals.Sanity : SanityScale.Max);

            // 軽い（空の）箱ほど大きく揺れて甲高く鳴る。詰まった箱ほど鈍く、揺れない。
            // 崩壊していると、どの箱も同じ中ほどの手応えに感じる（読めない・第三節）。
            var feelsWeight = HitTellSight.FeelsWeight(tell);
            var feltEmpty = feelsWeight && empty;
            var feltHeaviness = feelsWeight ? heaviness : 0.4f;
            var kick = feltEmpty ? 1f : Mathf.Lerp(0.6f, 0.2f, feltHeaviness);
            var local = transform.InverseTransformDirection(direction);
            _tiltVelocity += new Vector2(local.z, -local.x) * (kick * 260f);
            _squash = 1f;

            _audio.pitch = feltEmpty ? UnityEngine.Random.Range(1.1f, 1.3f) : Mathf.Lerp(1.15f, 0.7f, feltHeaviness);
            _audio.PlayOneShot(feltEmpty ? ProtoSounds.Clang : ProtoSounds.Thud, 0.9f);

            // 叩く音は街に響き、警官の耳に届く。空の箱ほどうるさい（ふたが飛ぶ一打は最もうるさい）。
            // 警官が聞くのは本当の音なので、ここは叩いた人の SAN に関係なく中身どおり。
            var hitsToOpen = _hasLid && gloved ? Mathf.Max(1, _hitsToOpen - 1) : _hitsToOpen;
            var opening = _hits >= hitsToOpen;
            ProtoNoise.Emit(transform.position, loudness * (opening || empty ? 1f : Mathf.Lerp(0.8f, 0.4f, heaviness)));

            // 珍しい物が入っていて、それが読めるときだけ、叩くたびにふたの隙間から光が漏れる（廃棄の袋も同じ）。
            var glint = HitTellSight.Glint(tell, BestTier());
            if (glint != HitGlint.None)
            {
                LeakGlint(glint);
            }

            if (_hits < hitsToOpen)
            {
                return 0.4f;
            }

            Open(direction, out toast);
            if (!_disposal)
            {
                SpendForageTime(vitals);
            }

            return 1f;
        }

        /// <summary>漁った時間ぶん時計を進め、その間の渇きと空腹を減らす（本編の TrashCan と同じ）。</summary>
        void SpendForageTime(Vitals vitals)
        {
            var seconds = _loot != null ? _loot.SpawnFor(_kind).ForageClockSeconds : 0f;
            if (seconds <= 0f)
            {
                return;
            }

            _dayNight?.Clock?.Advance(seconds);
            vitals?.DrainTime(seconds);
        }

        public void Refill()
        {
            StopAllCoroutines();
            _disposal = false;
            _spewInterval = 0f;
            _opened = false;
            _hits = 0;
            ResetLid();
            Draw();
        }

        /// <summary>
        /// 店員が廃棄の袋を入れた（<see cref="ProtoDisposal"/>）。中身を差し替え、ふたを閉じ直す。
        /// 開けると一度に飛び出さず、<paramref name="spewInterval"/> 秒おきに噴き出し続ける（第 5 分の山場）。
        /// </summary>
        public void LoadDisposal(IReadOnlyList<LootDraw> draws, float spewInterval)
        {
            // 夜になった補充が、同じフレームで後から廃棄を上書きしないよう、先に契機を読んで済ませておく。
            ObserveRefill();
            StopAllCoroutines();
            _contents.Clear();
            _contents.AddRange(draws);
            _disposal = true;
            _spewInterval = Mathf.Max(0.05f, spewInterval);
            _opened = false;
            _hits = 0;
            ResetLid();
        }

        /// <summary>補充の契機（日付の変わり目・夜への切り替わり）を読む。契機なら true。</summary>
        bool ObserveRefill() =>
            _dayNight != null && _dayNight.Clock != null && _refill.Observe(_dayNight.Clock.Day, _dayNight.IsNight);

        void Draw()
        {
            _contents.Clear();
            if (_loot == null)
            {
                return;
            }

            _drawnForNight = _dayNight != null && _dayNight.IsNight;
            var table = _loot.TableFor(_kind, _drawnForNight);
            // 保底：SAN が低いほど気を晴らす物が出やすい（切片の品目 第十節）。引くのはここだけなので、叩く前に中身は決まっている。
            if (_slice != null && _reader != null && _reader.Vitals != null)
            {
                table = _slice.AdjustForSanity(table, _reader.Vitals.Sanity);
            }

            for (var i = 0; i < _drawsPerOpen; i++)
            {
                _contents.Add(table.Draw(_rng, _catalog));
            }
        }

        void Open(Vector3 direction, out string toast)
        {
            _opened = true;
            _audio.pitch = 1f;
            _audio.PlayOneShot(ProtoSounds.Pop, 1f);
            ThrowLid(direction);

            var toPlayer = -direction;
            toPlayer.y = 0f;
            if (_disposal)
            {
                StartCoroutine(Spew(toPlayer, new List<LootDraw>(_contents)));
                toast = ProtoText.DisposalBurst;
                return;
            }

            var anyItem = false;
            var best = LootTier.Common;
            foreach (var draw in _contents)
            {
                if (draw.HasItem)
                {
                    anyItem = true;
                    var tier = TierOf(draw.Item);
                    best = tier > best ? tier : best;
                }

                Throw(draw, toPlayer);
            }

            PlayTierSound(best);

            toast = anyItem ? (_hasLid ? ProtoText.Opened : ProtoText.BagRipped) : ProtoText.Nothing;
        }

        /// <summary>廃棄の袋は破れたまま噴き出し続ける。噴くたびに少し鳴る（警官の耳にも届く）。</summary>
        IEnumerator Spew(Vector3 toPlayer, List<LootDraw> draws)
        {
            foreach (var draw in draws)
            {
                Throw(draw, toPlayer);
                if (draw.HasItem)
                {
                    PlayTierSound(TierOf(draw.Item));
                }

                _squash = 1f;
                _audio.pitch = UnityEngine.Random.Range(0.9f, 1.2f);
                _audio.PlayOneShot(ProtoSounds.Pop, 0.6f);
                ProtoNoise.Emit(transform.position, 0.35f);
                yield return new WaitForSeconds(_spewInterval);
            }
        }

        void Throw(LootDraw draw, Vector3 toPlayer)
        {
            var top = TopPoint() + Vector3.up * 0.1f;
            var velocity = Vector3.up * UnityEngine.Random.Range(6.5f, 9f)
                + toPlayer.normalized * UnityEngine.Random.Range(0.5f, 1.5f)
                + Vector3.ProjectOnPlane(UnityEngine.Random.insideUnitSphere, Vector3.up) * 1.6f;
            if (!draw.HasItem)
            {
                SpawnDebris(top, velocity);
                return;
            }

            var tier = TierOf(draw.Item);
            var name = draw.Item.Value;
            var slots = 1;
            var category = ItemCategory.Salvage;
            if (_catalog != null && _catalog.TryGet(draw.Item, out var definition))
            {
                name = definition.DisplayName;
                slots = definition.Slots;
                category = definition.Category;
            }

            var tierName = ProtoText.TierName(tier);
            if (tierName != null)
            {
                name = ProtoText.WithRarity(name, tierName);
            }

            ProtoLoot.Spawn(new ItemInstance(draw.Item, draw.State), name, slots, tier, ColorOf(category),
                _itemMaterial, _font, top, velocity, _slice != null ? _slice.LootLifetimeOf(tier) : _lootLifetime,
                _slice != null ? _slice.ModelOf(draw.Item) : null);
            if (tier == LootTier.Jackpot)
            {
                // 大当たりが飛び出した瞬間：足もとに広がる虹の輪・弾ける虹の星（後光は品についていく。ProtoLoot）。
                var ground = _collider != null ? _collider.bounds.min.y : transform.position.y;
                ProtoRainbowFx.Ripple(_itemMaterial, new Vector3(top.x, ground, top.z));
                ProtoRainbowFx.Burst(_itemMaterial, top, 12);
            }
            else if (tier == LootTier.Rare)
            {
                // 珍しい：紫の菱形が弾ける（星は大当たりだけの形）。
                ProtoRainbowFx.Diamonds(_itemMaterial, top, ProtoTierLook.Rare, 6);
            }
        }

        void Update()
        {
            if (ObserveRefill())
            {
                Refill();
            }

            // 揺れはばねで戻す。ヒットストップ中（timeScale 0）は止まって見えるのが正しい。
            var dt = Time.deltaTime;
            _tiltVelocity += -_tilt * (140f * dt) - _tiltVelocity * (9f * dt);
            _tilt += _tiltVelocity * dt;
            _tilt = Vector2.ClampMagnitude(_tilt, 25f);
            transform.localRotation = _restRotation * Quaternion.Euler(_tilt.x, 0f, _tilt.y);

            _squash = Mathf.MoveTowards(_squash, 0f, dt * 6f);
            var s = 1f + 0.12f * _squash * Mathf.Sin(_squash * Mathf.PI * 3f);
            transform.localScale = new Vector3(_restScale.x * s, _restScale.y / s, _restScale.z * s);
        }

        int ContentWeight()
        {
            var weight = 0;
            foreach (var draw in _contents)
            {
                if (!draw.HasItem)
                {
                    continue;
                }

                weight += _catalog != null && _catalog.TryGet(draw.Item, out var definition) ? definition.Slots : 1;
            }

            return weight;
        }

        LootTier BestTier()
        {
            var best = LootTier.Common;
            foreach (var draw in _contents)
            {
                if (draw.HasItem)
                {
                    var tier = TierOf(draw.Item);
                    if (tier > best)
                    {
                        best = tier;
                    }
                }
            }

            return best;
        }

        LootTier TierOf(ItemId item) => _slice != null ? _slice.TierOf(item) : LootTier.Common;

        /// <summary>珍しい物は鈴、大当たりは「キュイン」。</summary>
        void PlayTierSound(LootTier tier)
        {
            if (tier == LootTier.Jackpot)
            {
                var clip = _slice != null && _slice.JackpotSound != null ? _slice.JackpotSound : ProtoSounds.Jackpot;
                _audio.pitch = 1f;
                _audio.PlayOneShot(clip, 1f);
            }
            else if (tier == LootTier.Rare)
            {
                _audio.PlayOneShot(ProtoSounds.Chime, 0.8f);
            }
        }

        static Color ColorOf(ItemCategory category) => category switch
        {
            ItemCategory.Water => new Color(0.3f, 0.6f, 1f),
            ItemCategory.Food => new Color(1f, 0.55f, 0.25f),
            ItemCategory.Salvage => new Color(0.75f, 0.78f, 0.82f),
            ItemCategory.Equipment => new Color(0.55f, 0.4f, 0.25f),
            ItemCategory.Consumable => new Color(0.9f, 0.3f, 0.5f),
            _ => new Color(0.9f, 0.85f, 0.4f),
        };

        /// <summary>天面の中心（ふたの位置）。当たり判定の箱から求める。</summary>
        Vector3 TopPoint()
        {
            if (_collider != null)
            {
                var b = _collider.bounds;
                return new Vector3(b.center.x, b.max.y, b.center.z);
            }

            return transform.position + Vector3.up * (transform.lossyScale.y * 0.6f);
        }

        /// <summary>模型に「Lid」で始まる子があればそれをふたに使う。無ければ灰箱の板を作る。</summary>
        void FindOrBuildLid()
        {
            if (!_hasLid)
            {
                return;
            }

            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                if (child != transform && child.name.StartsWith("Lid", System.StringComparison.Ordinal))
                {
                    _lid = child;
                    break;
                }
            }

            if (_lid == null)
            {
                var lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lid.name = "Lid";
                Destroy(lid.GetComponent<Collider>());
                lid.transform.SetParent(transform, false);
                lid.transform.localScale = new Vector3(1.08f, 0.08f, 1.08f);
                lid.transform.localPosition = new Vector3(0f, 0.54f, 0f);
                var body = GetComponent<Renderer>();
                if (body != null)
                {
                    lid.GetComponent<Renderer>().sharedMaterial = body.sharedMaterial;
                }

                _lid = lid.transform;
            }

            _lidParent = _lid.parent;
            _lidLocalPosition = _lid.localPosition;
            _lidLocalRotation = _lid.localRotation;
            _lidLocalScale = _lid.localScale;
        }

        void ResetLid()
        {
            if (_lid == null)
            {
                return;
            }

            // 同じフレームでまた開くこともある（補充の直後に叩く）ので、その場で消す。
            // Destroy はフレームの終わりまで残り、次の AddComponent<Rigidbody> が null を返す。
            var rb = _lid.GetComponent<Rigidbody>();
            if (rb != null)
            {
                DestroyImmediate(rb);
            }

            var col = _lid.GetComponent<Collider>();
            if (col != null)
            {
                DestroyImmediate(col);
            }

            _lid.SetParent(_lidParent, false);
            _lid.localPosition = _lidLocalPosition;
            _lid.localRotation = _lidLocalRotation;
            _lid.localScale = _lidLocalScale;
        }

        void ThrowLid(Vector3 direction)
        {
            if (_lid == null)
            {
                return;
            }

            var world = _lid.lossyScale;
            _lid.SetParent(null, true);
            _lid.localScale = world;
            _lid.gameObject.AddComponent<BoxCollider>();
            var rb = _lid.gameObject.AddComponent<Rigidbody>();
            rb.linearVelocity = Vector3.up * 7f + direction * 2f;
            rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 15f;
        }

        /// <summary>
        /// ふたの隙間から漏れる光（<see cref="ProtoTierLook"/> と同じ言葉）。上機嫌なら色が段を語る（紫＝珍しい・虹＝大当たり）。
        /// 「何か良い物」までしか読めないときは白の粒（読めない色）。色は退色するので、形でも分ける：
        /// 珍しいと「何か良い物」は小さな菱形の粒、大当たりは虹の星（星は大当たりだけの形）。
        /// </summary>
        void LeakGlint(HitGlint glint)
        {
            var position = TopPoint() + Vector3.ProjectOnPlane(UnityEngine.Random.insideUnitSphere, Vector3.up) * 0.3f;
            if (glint == HitGlint.Jackpot)
            {
                ProtoRainbowFx.Star(_itemMaterial, position, Vector3.up * 1.8f + UnityEngine.Random.insideUnitSphere * 0.4f, 0.1f, 0.6f);
                return;
            }

            var color = glint == HitGlint.Rare ? ProtoTierLook.Rare : ProtoTierLook.Unknown;
            var spark = ProtoTierLook.Spark(_itemMaterial, position, 0.05f, color, 3f);
            spark.name = "Glint";
            var rb = spark.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearVelocity = Vector3.up * 1.5f + UnityEngine.Random.insideUnitSphere * 0.5f;
            Destroy(spark, 0.6f);
        }

        void SpawnDebris(Vector3 position, Vector3 velocity)
        {
            // 空振りでも、紙くずは飛ぶ。ただし掴めない（鞄に入る物ではない）。
            for (var i = 0; i < 3; i++)
            {
                var scrap = GameObject.CreatePrimitive(PrimitiveType.Cube);
                scrap.name = "Debris";
                scrap.transform.position = position;
                scrap.transform.localScale = new Vector3(0.12f, 0.02f, 0.09f);
                scrap.GetComponent<Renderer>().sharedMaterial = _itemMaterial;
                var rb = scrap.AddComponent<Rigidbody>();
                rb.mass = 0.02f;
                rb.linearDamping = 2.5f;
                rb.linearVelocity = velocity * 0.6f + UnityEngine.Random.insideUnitSphere;
                rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 20f;
                Destroy(scrap, 4f);
            }
        }
    }
}
