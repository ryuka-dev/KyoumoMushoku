using System.Collections;
using KyoumoMushoku.Core.Economy;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Knacks;
using KyoumoMushoku.Gameplay.Items;
using KyoumoMushoku.Gameplay.Knacks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 一人称の手（試作）。右手だけを見せる（作者 2026-10-26：両手は要らない）。操作は左クリック一つ。
    ///
    /// 左クリックの解釈：
    /// 1. 照準がまだ開いていないゴミ箱に当たっていれば、拳で叩く。
    /// 2. そうでなく、照準が使える物（<see cref="IProtoUsable"/>：店の台・水道・寝床）に当たっていれば、それを使う。
    /// 3. そうでなく、照準の近くに宙の物があれば、手を開いて伸ばし、握って引き戻す（伸びる腕）。
    /// 4. どれでもなければ、空を打つ（開いた箱なら「もう何も出ない」）。
    ///
    /// 手は指を回さない。握り（拳）と開きの 2 つの模型を差し替える（前の版の「指の向きが逆」を繰り返さない）。
    /// 模型は第三者素材 FPS_Psx_Hands（CC0）から <c>Tools/Models/gen/hand_psx.py</c> が作る。原点は肘、前は +Z。
    /// 鞄は本編と同じ Core の <see cref="Inventory"/>（容量制）。入らない物は掴めない。
    /// 試作ではプレイヤーの持ち物（鞄と所持金）をここが持つ。
    /// コツ（第六節）は本編と同じ <see cref="PlayerKnacks"/> が持ち、ここは漁った・警告されたを申告し、覚えた瞬間を独り言にする。
    /// </summary>
    public sealed class ProtoHands : MonoBehaviour
    {
        enum GrabPhase
        {
            None,
            Reach,
            Retract,
        }

        [SerializeField] Camera _camera;
        [SerializeField] ItemDatabaseAsset _catalog;
        [SerializeField] GameObject _fistModel;
        [SerializeField] GameObject _openModel;
        [SerializeField] Material _armMaterial;
        [SerializeField, Min(1)] int _bagCapacity = Inventory.DefaultCapacity;
        [Tooltip("始めの所持金（円）。本編の PlayerWallet の既定と同じ。")]
        [SerializeField, Min(0)] int _startingYen = 500;

        [Tooltip("掴むと鞄に入れず財布へ入る品（小銭入れ）の出典。")]
        [SerializeField] SliceTuningAsset _slice;

        [Header("手の構え（カメラ基準・原点は肘）")]
        [SerializeField] Vector3 _restPosition = new Vector3(0.20f, -0.26f, -0.10f);
        [SerializeField] Vector3 _restEuler = new Vector3(-8f, -8f, -80f);
        [Tooltip("肘から掌の中心まで（模型の寸法）。掴んだ物はここへ寄せる。")]
        [SerializeField, Min(0.05f)] float _palmDistance = 0.38f;

        [Header("叩く")]
        [SerializeField, Min(0.5f)] float _punchReach = 2.4f;
        [SerializeField, Min(0f)] float _punchDistance = 0.30f;
        [SerializeField, Min(0f)] float _hitStopMax = 0.09f;
        [SerializeField, Min(0f)] float _shakeMax = 0.06f;
        [Tooltip("これより短い間隔で叩き続けると、音は最も大きい（連打）。")]
        [SerializeField, Min(0.01f)] float _mashInterval = 0.25f;
        [Tooltip("これより間を空けて叩けば、音は最も小さい。")]
        [SerializeField, Min(0.02f)] float _calmInterval = 0.9f;
        [Tooltip("間を空けて叩いたときの音の大きさ（連打を 1 として）。")]
        [SerializeField, Range(0f, 1f)] float _calmLoudness = 0.45f;

        [Header("案内")]
        [Tooltip("この距離までの叩ける物・使える物は、届かなくても照準が薄く変わる（何かがあると分かる）。")]
        [SerializeField, Min(0.5f)] float _hintReach = 6f;

        [Header("掴む")]
        [SerializeField, Min(1f)] float _grabReach = 14f;
        [SerializeField, Range(1f, 30f)] float _grabConeDegrees = 10f;
        [SerializeField, Min(0.02f)] float _reachSeconds = 0.12f;
        [SerializeField, Min(0.02f)] float _retractSeconds = 0.2f;

        InputAction _use;
        InputAction _unload;
        bool _saidFirstButt;

        /// <summary>この局で開け終えた箱と袋の数（最初の大当たりを数える）。</summary>
        int _opensCompleted;

        /// <summary>最初の大当たりをもう入れたか（一局に一度）。</summary>
        bool _jackpotPlanted;
        Transform _hand;
        GameObject _fist;
        GameObject _open;
        Transform _palm;
        LineRenderer _arm;
        AudioSource _audio;
        Vector3 _cameraRest;
        float _punchT = 1f;
        float _shake;
        float _hitStopUntil;
        float _lastPunchAt = -10f;
        bool _hitStopRunning;

        ProtoLoot _grabbing;
        GrabPhase _phase;
        float _grabT;
        Vector3 _reachFrom;
        Quaternion _reachFromRotation;

        public Inventory Bag { get; private set; }
        public ProtoVitals Vitals { get; private set; }

        /// <summary>背負い枠（段ボール）。鞄の枠は使わない。</summary>
        public CarrySlot Back { get; private set; }

        /// <summary>歩きの速さの倍率。段ボールを背負っていると遅い。</summary>
        public float MoveSpeedMultiplier => Back != null && Back.IsOccupied && _slice != null ? _slice.CarrySpeedMultiplier : 1f;

        /// <summary>走れるか。段ボールを背負っていると走れない。</summary>
        public bool CanRun => Back == null || !Back.IsOccupied;

        /// <summary>警官の目に気づかれる速さの倍率。段ボールを背負っていると目立ち、キャップを持っていると目立たない。</summary>
        public float NoticeMultiplier
        {
            get
            {
                if (_slice == null)
                {
                    return 1f;
                }

                var multiplier = Back != null && Back.IsOccupied ? _slice.CarryNoticeMultiplier : 1f;
                return HasInBag(_slice.IsCap) ? multiplier * _slice.CapNoticeMultiplier : multiplier;
            }
        }

        /// <summary>軍手を持っているか。ふたのあるゴミ箱が一打早く開く。</summary>
        public bool HasGloves => _slice != null && HasInBag(_slice.IsGloves);

        /// <summary>宙の物を掴んで鞄に入れた。拾った物に反応する世界の側（独り言など）が聞く。</summary>
        public event System.Action<ItemInstance> Grabbed;
        public Wallet Wallet { get; private set; }

        /// <summary>品のデータ（没収の規則が品の種類を読む）。</summary>
        public ItemDatabaseAsset Catalog => _catalog;

        /// <summary>目（視点のカメラ）。画面の上に世界の物の位置を描く側が使う。</summary>
        public Camera ViewCamera => _camera;

        /// <summary>耳（自分の身に起きたことの音と、耳がこもる後処理）。</summary>
        public ProtoEars Ears { get; private set; }

        /// <summary>体ごと別の場所へ移す（運ばれる・連れて行かれる）。歩きの当たり判定を一度切ってから置く。</summary>
        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            var body = GetComponent<CharacterController>();
            if (body != null)
            {
                body.enabled = false;
            }

            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, rotation.eulerAngles.y, 0f));
            if (body != null)
            {
                body.enabled = true;
            }
        }

        /// <summary>コツ（生活の知恵・第六節）。触発の申告とルール変化の問い合わせは、ここを通す。</summary>
        public PlayerKnacks Knacks { get; private set; }

        /// <summary>
        /// 警官に見られていて、ゴミ箱を叩けないか（警告か追い出しの段の警官がこちらを見ている）。
        /// コツ `手を止めない` を覚えると、見られていても叩ける。
        /// </summary>
        public bool HandsStopped => ProtoPolice.AnyStopsHands && (Knacks == null || !Knacks.Has(KnackId.SteadyHands));

        /// <summary>叩いてからこの秒数のうちに警告されたら「漁っている最中に警告された」と数える（手を止めない）。</summary>
        const float ForagingWindowSeconds = 3f;

        /// <summary>覚えた瞬間の独り言を出しておく秒数。</summary>
        const float KnackToastSeconds = 6f;

        float _lastForageAt = -10f;

        public enum AimState
        {
            None,
            /// <summary>叩ける物・使える物を見ているが、まだ手が届かない。</summary>
            Distant,
            /// <summary>左クリックで何かが起きる。</summary>
            InReach,
        }

        /// <summary>照準の先。HUD の照準の形を決める。</summary>
        public AimState Aim { get; private set; }

        /// <summary>左クリックで起きることの案内（押す操作は含まない）。<see cref="AimState.InReach"/> のときだけ。無ければ null。</summary>
        public string AimPrompt { get; private set; }

        /// <summary>手の届くところで照準が合っているゴミ箱（袋を含む）。無ければ null。</summary>
        public ProtoTrashCan AimedTrashCan { get; private set; }

        /// <summary>左クリックで使われる物（開いていないゴミ箱か使える物）。無ければ null。縁取りが「狙われている」を知るのに使う。</summary>
        public Component AimedObject { get; private set; }

        /// <summary>前に <see cref="TakeCaughtCount"/> してから掴んで鞄に入れた数。</summary>
        int _caught;
        public string Toast { get; private set; }
        public float ToastAge { get; private set; }

        public void Configure(Camera camera, ItemDatabaseAsset catalog, GameObject fistModel, GameObject openModel,
            Material armMaterial)
        {
            _camera = camera;
            _catalog = catalog;
            _fistModel = fistModel;
            _openModel = openModel;
            _armMaterial = armMaterial;
        }

        void Awake()
        {
            Bag = new Inventory(_catalog, _bagCapacity);
            Back = new CarrySlot(_catalog);
            Wallet = new Wallet(_startingYen);
            Vitals = GetComponent<ProtoVitals>();
            Knacks = GetComponent<PlayerKnacks>();
            if (Knacks == null)
            {
                Knacks = gameObject.AddComponent<PlayerKnacks>();
            }

            // 手が止まっている間（朝の区切り・店の中）に覚えることもあるので、有効かどうかに関わらず聞く。
            Knacks.Acquired += OnKnackAcquired;
            Ears = GetComponent<ProtoEars>();
            if (Ears == null)
            {
                Ears = gameObject.AddComponent<ProtoEars>();
                Ears.Configure(_slice);
            }

            // BGM も耳と同じくプレイヤーに付ける。時間帯は街に一つの ProtoDayNight から読む。
            if (GetComponent<ProtoMusic>() == null)
            {
                gameObject.AddComponent<ProtoMusic>().Configure(_slice, FindFirstObjectByType<ProtoDayNight>());
            }
            _use = new InputAction("Use", InputActionType.Button, "<Mouse>/leftButton");
            _unload = new InputAction("Unload", InputActionType.Button, "<Keyboard>/g");
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _cameraRest = _camera.transform.localPosition;
            BuildHand();
        }

        void OnEnable()
        {
            _use.Enable();
            _unload.Enable();
        }

        void OnDisable()
        {
            _use.Disable();
            _unload.Disable();
            Time.timeScale = 1f;
        }

        void OnDestroy()
        {
            if (Knacks != null)
            {
                Knacks.Acquired -= OnKnackAcquired;
            }

            _use?.Dispose();
            _unload?.Dispose();
        }

        void Update()
        {
            ToastAge += Time.unscaledDeltaTime;
            UpdateAim();

            if (_use.WasPressedThisFrame())
            {
                Use();
            }

            if (_unload.WasPressedThisFrame() && Back.TryTakeOut(out var unloaded))
            {
                Say(ProtoText.Unloaded(NameOf(unloaded)));
            }
        }

        void LateUpdate()
        {
            // 視点（ProtoFirstPersonController.Update）が動いた後に手を置く。
            AnimateHand();
            AnimateShake();
        }

        void Use()
        {
            if (_phase != GrabPhase.None)
            {
                return;
            }

            var ray = new Ray(_camera.transform.position, _camera.transform.forward);
            ProtoTrashCan can = null;
            if (Physics.Raycast(ray, out var hit, _punchReach, ~0, QueryTriggerInteraction.Ignore))
            {
                can = hit.collider.GetComponentInParent<ProtoTrashCan>();
            }

            if (can != null && !can.IsOpened)
            {
                Punch(can);
                return;
            }

            var usable = hit.collider != null ? UsableAt(hit.collider) : null;
            if (usable != null)
            {
                _punchT = 0f;
                Say(usable.Use(this));
                _audio.pitch = 1.2f;
                _audio.PlayOneShot(ProtoSounds.Grab, 0.8f);
                return;
            }

            var target = FindGrabTarget();
            if (target != null)
            {
                Grab(target);
                return;
            }

            Punch(can);
        }

        void Punch(ProtoTrashCan can)
        {
            _punchT = 0f;
            if (can == null)
            {
                return;
            }

            // 警官に見られている間は手が止まる（本編の「警告で漁りが中断される」の一人称版）。叩かないので音も出ない。
            if (!can.IsOpened && HandsStopped)
            {
                Say(ProtoText.HandsStopped);
                ProtoPolice.ScoldStoppedHands();
                return;
            }

            if (!can.IsOpened)
            {
                _lastForageAt = Time.unscaledTime;
            }

            // 連打するほどうるさい。叩く回数は同じなので、連打すれば早く開くが、警官に聞こえやすい。
            var interval = Time.unscaledTime - _lastPunchAt;
            _lastPunchAt = Time.unscaledTime;
            var mash = 1f - Mathf.Clamp01((interval - _mashInterval) / Mathf.Max(0.01f, _calmInterval - _mashInterval));
            // 最初の大当たり：決めた数だけ開け終えた後、次に初めて叩く箱に入れる（SliceTuning）。
            var plant = default(ItemId);
            if (!_jackpotPlanted && _slice != null && _slice.GuaranteedJackpotAfterOpens > 0
                && _opensCompleted >= _slice.GuaranteedJackpotAfterOpens)
            {
                plant = _slice.GuaranteedJackpotItem(UnityEngine.Random.value * 0.9999f);
            }

            var wasOpened = can.IsOpened;
            var strength = can.Hit(_camera.transform.forward, Mathf.Lerp(_calmLoudness, 1f, mash), Vitals != null ? Vitals.Vitals : null,
                Knacks.Has(KnackId.SpotDuds), HasGloves, plant, out var plantedNow, out var toast);
            _jackpotPlanted |= plantedNow;
            if (toast != null)
            {
                Say(toast);
            }

            if (!wasOpened && can.IsOpened)
            {
                _opensCompleted++;
                // 開け終えた箱と袋を数える。10 個で あたりの見分け方 を覚える（本編の「ゴミ箱を10個漁る」）。
                // 覚えた独り言が開いた一言に代わるよう、一言の後に申告する。
                Knacks.RecordRummage();
            }

            if (strength > 0f)
            {
                _shake = Mathf.Max(_shake, strength);
                HitStop(_hitStopMax * strength);
            }
        }

        void Grab(ProtoLoot target)
        {
            if (!CanTake(target.Item))
            {
                Say(IsCarriedOnBack(target.Item) ? ProtoText.AlreadyCarrying(target.DisplayName) : ProtoText.NoRoom(target.DisplayName));
                _audio.pitch = 0.6f;
                _audio.PlayOneShot(ProtoSounds.Clang, 0.4f);
                return;
            }

            _grabbing = target;
            _phase = GrabPhase.Reach;
            _grabT = 0f;
            _reachFrom = _hand.position;
            _reachFromRotation = _hand.rotation;
            SetOpen(true);
        }

        void OnArrived(ProtoLoot loot)
        {
            if (IsPurse(loot.Item))
            {
                // 小銭入れは鞄に入れない。中身は掴んだ瞬間に決まり、そのまま財布へ。
                var yen = _slice.PurseYen(UnityEngine.Random.value * 0.9999f);
                Wallet.Add(yen);
                _caught++;
                Say(ProtoText.PurseOpened(yen));
                _audio.pitch = 1.4f;
                _audio.PlayOneShot(ProtoSounds.Grab, 1f);
                Grabbed?.Invoke(loot.Item);
            }
            else if (IsCarriedOnBack(loot.Item))
            {
                if (Back.TryCarry(loot.Item))
                {
                    _caught++;
                    Say(ProtoText.Shouldered(NameOf(loot.Item)));
                    _audio.pitch = 0.8f;
                    _audio.PlayOneShot(ProtoSounds.Grab, 1f);
                    Grabbed?.Invoke(loot.Item);
                }
                else
                {
                    Say(ProtoText.AlreadyCarrying(loot.DisplayName));
                }
            }
            else if (Bag.TryAdd(loot.Item))
            {
                _caught++;
                Say(ProtoText.Caught(loot.DisplayName));
                if (!_saidFirstButt && _slice != null && _slice.IsButt(loot.Item.ItemId))
                {
                    // 保底が世界の中で見える最初の瞬間（第十四節：見えない状態は誰かが説明する）。
                    _saidFirstButt = true;
                    Say(ProtoText.FirstButt);
                }
                else if (_slice != null && _slice.IsGloves(loot.Item.ItemId))
                {
                    // 道具の効き目は、拾った瞬間の独り言で伝える。
                    Say(ProtoText.GlovesFound);
                }
                else if (_slice != null && _slice.IsCap(loot.Item.ItemId))
                {
                    Say(ProtoText.CapFound);
                }

                Grabbed?.Invoke(loot.Item);
                _audio.pitch = loot.Tier >= KyoumoMushoku.Core.Foraging.LootTier.Rare ? 1.4f : 1f;
                _audio.PlayOneShot(ProtoSounds.Grab, 1f);
            }
            else
            {
                Say(ProtoText.NoRoom(loot.DisplayName));
            }
        }

        /// <summary>
        /// 照準の案内を決める。順番は <see cref="Use"/> の解釈と同じ（案内と、押して起きることを食い違わせない）：
        /// 開いていないゴミ箱 → 使える物 → 宙の物。開いたゴミ箱は案内しない（叩いても何も起きない）。
        /// 届かない距離の物は照準の形だけで知らせる。
        /// </summary>
        void UpdateAim()
        {
            Aim = AimState.None;
            AimPrompt = null;
            AimedTrashCan = null;
            AimedObject = null;

            var ray = new Ray(_camera.transform.position, _camera.transform.forward);
            ProtoTrashCan can = null;
            IProtoUsable usable = null;
            var inReach = false;
            if (Physics.Raycast(ray, out var hit, _hintReach, ~0, QueryTriggerInteraction.Ignore))
            {
                can = hit.collider.GetComponentInParent<ProtoTrashCan>();
                usable = UsableAt(hit.collider);
                inReach = hit.distance <= _punchReach;
            }

            if (inReach && can != null)
            {
                AimedTrashCan = can;
            }

            if (inReach && can != null && !can.IsOpened)
            {
                // 案内と押して起きることを食い違わせない（第十七節）。見られている間は叩けないと言う。
                SetAim(HandsStopped ? ProtoText.WatchedPrompt : can.Prompt);
                AimedObject = can;
                return;
            }

            if (inReach && usable != null)
            {
                SetAim(usable.Prompt(this));
                AimedObject = usable as Component;
                return;
            }

            var loot = _phase == GrabPhase.None ? FindGrabTarget() : null;
            if (loot != null)
            {
                SetAim(ProtoText.GrabPrompt(loot.DisplayName));
                return;
            }

            // 開いた箱はもう触れる物ではないので、近くても遠くても照準は元の「+」に戻す。
            if ((can != null && !can.IsOpened) || usable != null)
            {
                Aim = AimState.Distant;
            }
        }

        bool IsPurse(ItemInstance item) => _slice != null && _slice.IsPurse(item.ItemId);

        bool HasInBag(System.Func<ItemId, bool> match)
        {
            if (Bag == null)
            {
                return false;
            }

            for (var i = 0; i < Bag.Count; i++)
            {
                if (match(Bag[i].ItemId))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>担いで運ぶ物（段ボール）。鞄でなく背負い枠に入る。</summary>
        bool IsCarriedOnBack(ItemInstance item) =>
            _catalog != null && _catalog.TryGet(item.ItemId, out var definition) && definition.CarriedOnBack;

        /// <summary>掴めるか。小銭入れは財布へ、段ボールは背負い枠へ、それ以外は鞄へ入るかで決まる。</summary>
        bool CanTake(ItemInstance item) =>
            IsPurse(item) || (IsCarriedOnBack(item) ? Back.CanCarry(item) : Bag.CanAdd(item));

        /// <summary>品の表示名（品のデータに無ければ id）。</summary>
        public string NameOf(ItemInstance item) =>
            _catalog != null && _catalog.TryGet(item.ItemId, out var definition) ? definition.DisplayName : item.Id;

        /// <summary>照準の先の使う物。閉まっている店は触れる物ではないので、無いのと同じに扱う（第十七節 規則 1）。</summary>
        static IProtoUsable UsableAt(Collider collider)
        {
            var usable = collider.GetComponentInParent<IProtoUsable>();
            return usable is IProtoClosable closable && !closable.IsOpenNow ? null : usable;
        }

        void SetAim(string prompt)
        {
            Aim = AimState.InReach;
            AimPrompt = prompt;
        }

        /// <summary>掴んだ数を受け取り、数え直す。<see cref="ProtoSession"/> が一日の区切りで呼ぶ。</summary>
        public int TakeCaughtCount()
        {
            var count = _caught;
            _caught = 0;
            return count;
        }

        ProtoLoot FindGrabTarget()
        {
            ProtoLoot best = null;
            var bestAngle = _grabConeDegrees;
            var origin = _camera.transform.position;
            foreach (var loot in ProtoLoot.Active)
            {
                if (loot.IsBeingPulled)
                {
                    continue;
                }

                var toLoot = loot.transform.position - origin;
                if (toLoot.magnitude > _grabReach)
                {
                    continue;
                }

                var angle = Vector3.Angle(_camera.transform.forward, toLoot);
                if (angle < bestAngle)
                {
                    bestAngle = angle;
                    best = loot;
                }
            }

            return best;
        }

        void HitStop(float seconds)
        {
            if (seconds <= 0f)
            {
                return;
            }

            _hitStopUntil = Mathf.Max(_hitStopUntil, Time.unscaledTime + seconds);
            if (!_hitStopRunning)
            {
                StartCoroutine(HitStopRoutine());
            }
        }

        IEnumerator HitStopRoutine()
        {
            _hitStopRunning = true;
            Time.timeScale = 0f;
            while (Time.unscaledTime < _hitStopUntil)
            {
                yield return null;
            }

            // 手が止められていたら（ポーズ・店・区切り）、時間は止めた側が持つ。ここで 1 に戻すと、止まった画面の裏で時間が流れる。
            if (enabled)
            {
                Time.timeScale = 1f;
            }

            _hitStopRunning = false;
        }

        /// <summary>一言を出す既定の秒数。</summary>
        public const float DefaultToastSeconds = 2.2f;

        /// <summary>いまの一言を出しておく秒数。</summary>
        public float ToastSeconds { get; private set; } = DefaultToastSeconds;

        /// <summary>画面の一言。手の動作の結果のほか、飲食などほかの部品も使う。読ませたい独り言は長めに出す。</summary>
        public void Say(string text, float seconds = DefaultToastSeconds)
        {
            Toast = text;
            ToastAge = 0f;
            ToastSeconds = seconds;
        }

        /// <summary>警官が警告に入った（<see cref="ProtoPolice"/>）。叩いてすぐなら「漁っている最中に警告された」と数える。</summary>
        public void NotifyWarned()
        {
            if (Time.unscaledTime - _lastForageAt <= ForagingWindowSeconds)
            {
                Knacks.RecordForageWarned();
            }
        }

        /// <summary>コツを覚えた瞬間は、はっきり知らせる（第六節）。主人公の独り言で、何が変わったかを言う。</summary>
        void OnKnackAcquired(KnackId id)
        {
            Say(ProtoText.KnackLearned(id), KnackToastSeconds);
            _audio.pitch = 1f;
            _audio.PlayOneShot(ProtoSounds.Chime, 0.7f);
        }

        void SetOpen(bool open)
        {
            _open.SetActive(open);
            _fist.SetActive(!open);
        }

        void AnimateHand()
        {
            var camera = _camera.transform;

            // 構え＋拳の突き（前へ速く、ゆっくり戻る）。
            _punchT = Mathf.Min(1f, _punchT + Time.unscaledDeltaTime / 0.16f);
            var jab = _punchT < 0.3f ? _punchT / 0.3f : 1f - (_punchT - 0.3f) / 0.7f;
            jab = Mathf.Clamp01(jab);
            var restPosition = camera.TransformPoint(_restPosition + new Vector3(-0.06f, 0.07f, _punchDistance) * jab);
            var restRotation = camera.rotation * Quaternion.Euler(_restEuler) * Quaternion.Euler(-4f * jab, -6f * jab, 0f);

            switch (_phase)
            {
                case GrabPhase.Reach:
                {
                    if (_grabbing == null)
                    {
                        BeginRetract();
                        break;
                    }

                    _grabT += Time.unscaledDeltaTime / _reachSeconds;
                    var t = Mathf.Clamp01(_grabT);
                    var eased = 1f - (1f - t) * (1f - t);
                    var target = _grabbing.transform.position;
                    var toTarget = target - restPosition;
                    var direction = toTarget.sqrMagnitude > 1e-4f ? toTarget.normalized : camera.forward;
                    // 肘を動かして、掌が目標に届くようにする。
                    var elbow = target - direction * _palmDistance;
                    _hand.position = Vector3.Lerp(_reachFrom, elbow, eased);
                    _hand.rotation = Quaternion.Slerp(_reachFromRotation,
                        Quaternion.LookRotation(direction, camera.up) * Quaternion.Euler(0f, 0f, _restEuler.z), eased);
                    if (_grabT >= 1f)
                    {
                        SetOpen(false);
                        _grabbing.PullTo(_palm, OnArrived);
                        _grabbing = null;
                        BeginRetract();
                    }

                    break;
                }

                case GrabPhase.Retract:
                {
                    _grabT += Time.unscaledDeltaTime / _retractSeconds;
                    var t = Mathf.Clamp01(_grabT);
                    var eased = t * t;
                    _hand.position = Vector3.Lerp(_reachFrom, restPosition, eased);
                    _hand.rotation = Quaternion.Slerp(_reachFromRotation, restRotation, eased);
                    if (_grabT >= 1f)
                    {
                        _phase = GrabPhase.None;
                    }

                    break;
                }

                default:
                    _hand.position = restPosition;
                    _hand.rotation = restRotation;
                    break;
            }

            // 伸びる腕：構えの位置から、いまの肘まで袖色の紐を張る。
            var stretched = Vector3.Distance(restPosition, _hand.position) > 0.08f;
            _arm.enabled = stretched;
            if (stretched)
            {
                _arm.SetPosition(0, restPosition);
                _arm.SetPosition(1, _hand.position);
            }
        }

        void BeginRetract()
        {
            _phase = GrabPhase.Retract;
            _grabT = 0f;
            _reachFrom = _hand.position;
            _reachFromRotation = _hand.rotation;
        }

        void AnimateShake()
        {
            _shake = Mathf.MoveTowards(_shake, 0f, Time.unscaledDeltaTime * 5f);
            _camera.transform.localPosition = _cameraRest + Random.insideUnitSphere * (_shakeMax * _shake * _shake);
        }

        void BuildHand()
        {
            // 模型の根は FBX の軸変換の回転を持つので、空の容器の下に置き、容器を動かす。
            var hand = new GameObject("RightHand");
            _hand = hand.transform;
            _fist = Instantiate(_fistModel, _hand, false);
            _open = Instantiate(_openModel, _hand, false);
            foreach (var r in hand.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            _palm = new GameObject("Palm").transform;
            _palm.SetParent(_hand, false);
            _palm.localPosition = new Vector3(0f, 0f, _palmDistance);
            SetOpen(false);

            var armGo = new GameObject("RubberArm");
            armGo.transform.SetParent(transform, false);
            _arm = armGo.AddComponent<LineRenderer>();
            _arm.positionCount = 2;
            _arm.startWidth = 0.07f;
            _arm.endWidth = 0.06f;
            _arm.numCapVertices = 2;
            _arm.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _arm.sharedMaterial = _armMaterial;
            _arm.enabled = false;
        }
    }
}
