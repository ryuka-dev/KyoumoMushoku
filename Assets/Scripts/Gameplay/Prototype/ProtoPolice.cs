using System.Collections;
using KyoumoMushoku.Core.Police;
using KyoumoMushoku.Core.Randomness;
using KyoumoMushoku.Core.Survival;
using KyoumoMushoku.Gameplay.Items;
using KyoumoMushoku.Gameplay.UI;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 一人称の切片の警官。巡回し、目と耳でプレイヤーに気づき、段階的に追い出す（第五節）。
    /// 段階と注目度の規則は本編と同じ Core の <see cref="PoliceEscalation"/>、捕まったときの没収は <see cref="Confiscation"/>、
    /// 台詞は本編の <see cref="PoliceText"/> に一人称の分（<see cref="ProtoText"/>）を足したもの。台詞は頭の上に出る（<see cref="ProtoPoliceOverlay"/>）。
    /// 捕まると HP が減り、腐った物を取り上げられ、受け持ちの外れへ連れて行かれる（本編と同じ）。連れて行かれる過程は見せ、
    /// 歩いた道のりの分だけ時計が進む（<see cref="CaptureSequence"/>）。追跡や警告が終わるときも、終わったと言う。
    ///
    /// 目：視界の円錐の中で、建物に遮られずに見えているときだけ。路地や建物の陰に入れば見えない。
    /// 耳：ゴミ箱を叩く音（<see cref="ProtoNoise"/>）は、見えていなくても届く。うるさいほど遠くまで届き、注目度が上がる。
    /// 聞きつけた警官は音のした所へ見に来る。叩くたびに、手早く済ませるか、見つかる危険を取るかが問われる。
    /// 警告されたら、見られている間はゴミ箱を叩けない（<see cref="AnyStopsHands"/>。本編の「警告で漁りが中断される」）。
    ///
    /// 追跡の速さはプレイヤーの走りより遅い。視線を切って逃げ切れる（本編の地下通路の代わり）。
    /// 街区は一つなので、ゾーンの警戒度（ZoneAlertLevels）は入れていない。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class ProtoPolice : MonoBehaviour
    {
        [SerializeField] ProtoHands _player;
        [SerializeField] ItemDatabaseAsset _catalog;
        [SerializeField] Transform _escortPoint;
        [SerializeField] Vector3[] _patrol = new Vector3[0];

        [Header("動き")]
        [SerializeField, Min(0f)] float _patrolSpeed = 1.6f;
        [SerializeField, Min(0f)] float _chaseSpeed = 4.8f;
        [SerializeField, Min(0f)] float _warnDistance = 2.5f;
        [SerializeField, Min(0f)] float _captureDistance = 1.3f;
        [SerializeField, Min(0f)] float _lookAroundSeconds = 4f;

        [Header("目と耳")]
        [SerializeField, Min(0f)] float _sightRadius = 10f;
        [SerializeField, Range(10f, 180f)] float _sightAngle = 110f;
        [Tooltip("最もうるさい音（1）が届く距離。小さい音ほど近くまでしか届かない。")]
        [SerializeField, Min(0f)] float _hearingRadius = 20f;
        [Tooltip("聞こえた一打で上がる注目度（大きさ 1 のとき）。")]
        [SerializeField, Min(0f)] float _heardGain = 15f;
        [Tooltip("叩くところを見られたときに、さらに上がる注目度。")]
        [SerializeField, Min(0f)] float _seenHitGain = 20f;

        [Header("捕まったとき")]
        [SerializeField, Min(0f)] float _captureHpLoss = 12f;

        /// <summary>連れて行く先の呼び名（暗転の一行で言う）。字はテキストモジュールにある。</summary>
        public enum EscortPlace
        {
            ParkEdge,
            ArcadeOutside,
        }

        [Tooltip("この警官が連れて行く先（受け持ちの外れ）。暗転の一行で、どこへ連れて行かれたかを言う。")]
        [SerializeField] EscortPlace _escortPlace;

        [Tooltip("捕まえて連れて行く間、遊びを止める（店と同じ止め方）。")]
        [SerializeField] ProtoSession _session;

        [Tooltip("暗転と、暗い間の一行を描く。")]
        [SerializeField] ProtoHud _hud;

        [Tooltip("連れて行かれた道のりの分だけ、時計を進める。")]
        [SerializeField] ProtoDayNight _dayNight;

        [SerializeField] SliceTuningAsset _slice;

        const float EyeHeight = 1.6f;

        CharacterController _body;
        IRng _rng;
        float _suspicion;
        int _patrolIndex;
        bool _investigating;
        Vector3 _investigateAt;
        float _lookAroundLeft;
        Vector3 _lastSeen;

        public PoliceStage Stage { get; private set; }
        public float Suspicion => _suspicion;

        /// <summary>見張っている相手（体の向き・頭の向きを決める側が読む）。</summary>
        public ProtoHands Player => _player;

        /// <summary>切片の数値（警官の音を鳴らす側も読む）。</summary>
        public SliceTuningAsset Slice => _slice;

        /// <summary>頭の上の位置。台詞と目の印はここに出る（<see cref="ProtoPoliceOverlay"/>）。</summary>
        public Vector3 HeadPosition => transform.position + Vector3.up * HeadHeight;

        /// <summary>
        /// いま口にしている台詞（無ければ null）。台詞は誰が言ったか分かるよう、その警官の頭の上に出す
        /// （画面の外なら、画面の端にその方向を示して出す）。手の一言（画面下）とは別の場所なので、互いに消し合わない。
        /// </summary>
        public string Speech => Time.unscaledTime - _speechAt < SpeechSeconds ? _speech : null;

        /// <summary>台詞を口にしてからの秒数（実時間。止まった時間の中の台詞も読めるように）。</summary>
        public float SpeechAge => Time.unscaledTime - _speechAt;

        /// <summary>口を開いた瞬間。声の音（第三層）が聞く。</summary>
        public event System.Action Spoke;

        public const float SpeechSeconds = 3.5f;
        const float HeadHeight = 2.25f;
        const float ScoldCooldownSeconds = 4f;

        string _speech;
        float _speechAt = -100f;
        float _scoldedAt = -100f;

        static readonly System.Collections.Generic.List<ProtoPolice> Active = new System.Collections.Generic.List<ProtoPolice>();

        /// <summary>警告か追い出しの段で、いまプレイヤーを見ているか。</summary>
        bool _stopsHands;

        /// <summary>
        /// 警告か追い出しの段の警官が、いまプレイヤーを見ているか。見られている間は手が止まる
        /// （本編の「警告で漁りが中断される」。コツ `手を止めない` で止まらなくなる・<see cref="ProtoHands.HandsStopped"/>）。
        /// </summary>
        public static bool AnyStopsHands
        {
            get
            {
                foreach (var police in Active)
                {
                    if (police._stopsHands)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// 見られていて手が止まったとき、見ている警官が叱る（手が止まった理由を、具体的な人に結ぶ）。
        /// 叩くたびに叱ると煩いので、間を空ける。
        /// </summary>
        public static void ScoldStoppedHands()
        {
            foreach (var police in Active)
            {
                if (police._stopsHands)
                {
                    if (Time.unscaledTime - police._scoldedAt >= ScoldCooldownSeconds)
                    {
                        police._scoldedAt = Time.unscaledTime;
                        police.Say(ProtoText.PoliceScold);
                    }

                    return;
                }
            }
        }

        /// <summary>この場面のすべての警官（画面の上の台詞と目の印を描く側が読む）。</summary>
        public static System.Collections.Generic.IReadOnlyList<ProtoPolice> All => Active;

        /// <summary>プレイヤーが運ばれて街から消えた。どの警官も追うのをやめ、遭遇を終える。</summary>
        public static void ForgetPlayer()
        {
            foreach (var police in Active)
            {
                police.ResetEncounter();
            }
        }

        public void Configure(ProtoHands player, ItemDatabaseAsset catalog, Transform escortPoint, Vector3[] patrol)
        {
            _player = player;
            _catalog = catalog;
            _escortPoint = escortPoint;
            _patrol = patrol ?? new Vector3[0];
        }

        void Awake()
        {
            _body = GetComponent<CharacterController>();
            _rng = new SystemRng();
        }

        void OnEnable()
        {
            ProtoNoise.Made += OnNoise;
            Active.Add(this);
        }

        void OnDisable()
        {
            ProtoNoise.Made -= OnNoise;
            Active.Remove(this);
            _stopsHands = false;
        }

        void Update()
        {
            var vitals = _player != null ? _player.Vitals : null;
            if (vitals == null || vitals.Vitals == null)
            {
                return;
            }

            var delta = Time.deltaTime;
            if (delta <= 0f)
            {
                return;
            }

            var sees = vitals.Vitals.IsAlive && CanSeePlayer();
            if (sees)
            {
                _lastSeen = _player.transform.position;
            }

            _suspicion = sees
                ? PoliceEscalation.Advance(_suspicion, PoliceEscalation.LoiterGainPerSecond, _player != null ? _player.NoticeMultiplier : 1f, delta)
                : PoliceEscalation.Relax(_suspicion, delta);

            var next = PoliceEscalation.NextStage(Stage, _suspicion);
            if (next != Stage)
            {
                Enter(next);
            }

            _stopsHands = sees && Stage >= PoliceStage.Warning;
            Act(sees, delta);
        }

        void OnNoise(Vector3 position, float loudness)
        {
            if (_player == null || !isActiveAndEnabled)
            {
                return;
            }

            var reach = _hearingRadius * Mathf.Lerp(0.4f, 1f, loudness);
            if ((position - transform.position).sqrMagnitude > reach * reach)
            {
                return;
            }

            var gain = _heardGain * loudness;
            if (CanSeePlayer())
            {
                gain += _seenHitGain;
            }

            _suspicion = PoliceEscalation.Clamp(_suspicion + gain);

            // まだ追い払いに来ていないなら、音のした所へ見に来る。
            if (Stage < PoliceStage.Warning)
            {
                // 見えない所の音に初めて気づいたら、声に出す。音のした方へ来ることの予告になる（第十四節）。
                if (!_investigating && Stage == PoliceStage.Unaware && !CanSeePlayer())
                {
                    Say(ProtoText.PoliceHeard);
                }

                _investigating = true;
                _investigateAt = position;
                _lookAroundLeft = _lookAroundSeconds;
            }
        }

        void Enter(PoliceStage stage)
        {
            var previous = Stage;
            Stage = stage;
            switch (stage)
            {
                case PoliceStage.Noticing when previous == PoliceStage.Unaware:
                    Say(PoliceText.Notice);
                    break;
                case PoliceStage.Warning when previous < PoliceStage.Warning:
                    Say(PoliceText.Warning);
                    // 漁っている最中なら、手を止めない に向けて数える（判断は手が持つ）。
                    _player.NotifyWarned();
                    break;
                case PoliceStage.Pursuing:
                    _investigating = false;
                    Say(PoliceText.Pursue);
                    break;

                // 終わりも言う。言わなければ、もう安全なのかが分からない（第十四節：事後の説明）。
                case PoliceStage.Unaware when previous == PoliceStage.Pursuing:
                    Say(ProtoText.PoliceGaveUp);
                    break;
                case PoliceStage.Noticing when previous == PoliceStage.Warning:
                case PoliceStage.Unaware when previous == PoliceStage.Warning:
                    Say(ProtoText.PoliceLetGo);
                    break;
            }
        }

        void Act(bool sees, float delta)
        {
            var player = _player.transform.position;
            switch (Stage)
            {
                case PoliceStage.Pursuing:
                    MoveTowards(_lastSeen, _chaseSpeed, sees ? _captureDistance * 0.8f : 0.3f);
                    if (sees && Flat(player - transform.position).magnitude <= _captureDistance)
                    {
                        Capture();
                    }

                    break;

                case PoliceStage.Warning:
                    if (sees)
                    {
                        MoveTowards(player, _patrolSpeed * 1.4f, _warnDistance);
                    }
                    else
                    {
                        MoveTowards(_lastSeen, _patrolSpeed * 1.4f, 0.5f);
                    }

                    break;

                default:
                    if (sees && Stage == PoliceStage.Noticing)
                    {
                        Face(player, delta);
                    }
                    else if (_investigating)
                    {
                        Investigate(delta);
                    }
                    else
                    {
                        Patrol();
                    }

                    break;
            }
        }

        void Investigate(float delta)
        {
            if (MoveTowards(_investigateAt, _patrolSpeed * 1.3f, 1.5f))
            {
                // 着いたら、しばらく辺りを見回す。
                transform.Rotate(0f, 70f * delta, 0f);
                _lookAroundLeft -= delta;
                if (_lookAroundLeft <= 0f)
                {
                    _investigating = false;
                }
            }
        }

        void Patrol()
        {
            if (_patrol.Length == 0)
            {
                return;
            }

            if (MoveTowards(_patrol[_patrolIndex], _patrolSpeed, 0.4f))
            {
                _patrolIndex = (_patrolIndex + 1) % _patrol.Length;
            }
        }

        /// <summary>目標へ歩く。着いていれば true。</summary>
        bool MoveTowards(Vector3 target, float speed, float stopDistance)
        {
            var offset = Flat(target - transform.position);
            if (offset.magnitude <= stopDistance)
            {
                _body.SimpleMove(Vector3.zero);
                return true;
            }

            var direction = offset.normalized;
            _body.SimpleMove(direction * speed);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction),
                1f - Mathf.Exp(-8f * Time.deltaTime));
            return false;
        }

        void Face(Vector3 target, float delta)
        {
            _body.SimpleMove(Vector3.zero);
            var offset = Flat(target - transform.position);
            if (offset.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(offset),
                    1f - Mathf.Exp(-6f * delta));
            }
        }

        /// <summary>
        /// 見えているか。視界の円錐の中で、間に遮る物が無いこと。警告・追跡中はこちらを向いているので円錐を問わない。
        /// </summary>
        bool CanSeePlayer()
        {
            var eye = transform.position + Vector3.up * EyeHeight;
            var target = _player.transform.position + Vector3.up * 1.5f;
            var offset = target - eye;
            if (offset.sqrMagnitude > _sightRadius * _sightRadius)
            {
                return false;
            }

            if (Stage < PoliceStage.Warning && Vector3.Angle(transform.forward, Flat(offset)) > _sightAngle * 0.5f)
            {
                return false;
            }

            if (Physics.Raycast(eye, offset.normalized, out var hit, offset.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.collider.GetComponentInParent<ProtoHands>() != null;
            }

            return true;
        }

        bool _capturing;

        void Capture()
        {
            if (_capturing)
            {
                return;
            }

            var vitals = _player.Vitals.Vitals;
            vitals.Apply(new VitalsDelta { Hp = -_captureHpLoss });

            // 突き飛ばされて倒れたなら、そこからは倒れたときの流れ（ProtoSession）が引き取る。
            if (!vitals.IsAlive)
            {
                ResetEncounter();
                return;
            }

            // 遊びを止められないとき（店の中など）は、演出を省いてその場で連れて行く。
            if (_session == null || _hud == null || !_session.TryOpenModal())
            {
                var seized = Confiscate(out _);
                EscortNow(out _);
                Say(seized > 0 ? PoliceText.CaptureWithSeizure : PoliceText.Capture);
                ResetEncounter();
                return;
            }

            StartCoroutine(CaptureSequence());
        }

        /// <summary>
        /// 捕まって連れて行かれる（約 3.5 秒・実時間。遊びは止まっている）。瞬間移動で何が起きたか分からなくならないよう、過程を見せる。
        /// 1. 掴まれる：鈍い音、画面が揺れ、視点が警官の顔へ引き寄せられる。警官が頭の上で言う（取り上げるなら「腐ったものは預かる」）。
        /// 2. 暗転し、暗い間に「〇〇まで連れて行かれた。」と取り上げられた物の名を言う。歩いた道のりの分だけ時計が進む。
        /// 3. 明るくなると、送り先で警官が向かいに立っていて「二度とここで漁るな。」と言い、持ち場へ歩いて帰っていく。
        /// </summary>
        IEnumerator CaptureSequence()
        {
            _capturing = true;
            var view = _player.GetComponent<ProtoFirstPersonController>();
            var camera = _player.ViewCamera != null ? _player.ViewCamera.transform : null;
            var rest = camera != null ? camera.localPosition : Vector3.zero;

            // 掴まれた一打。素材集の鈍い音（SliceTuning）、無ければ合成の音。耳元で鳴るよう、目の位置で鳴らす。
            var hit = _slice != null && _slice.CaptureHit != null ? _slice.CaptureHit : ProtoSounds.Thud;
            var hitVolume = _slice != null ? _slice.CaptureHitVolume : 0.8f;
            AudioSource.PlayClipAtPoint(hit, camera != null ? camera.position : _player.transform.position, hitVolume);
            var seized = Confiscate(out var seizedNames);
            Say(seized > 0 ? ProtoText.PoliceSeize : ProtoText.PoliceTakeAway);

            // 1. 視点を警官の顔へ。最初は強く揺れ、収まっていく。
            var fromYaw = _player.transform.eulerAngles.y;
            var fromPitch = view != null ? view.Pitch : 0f;
            var toFace = camera != null ? HeadPosition - Vector3.up * 0.4f - camera.position : transform.position - _player.transform.position;
            var toYaw = Mathf.Atan2(toFace.x, toFace.z) * Mathf.Rad2Deg;
            var toPitch = -Mathf.Asin(Mathf.Clamp(toFace.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
            for (var t = 0f; t < GrabTurnSeconds; t += Time.unscaledDeltaTime)
            {
                var k = Mathf.SmoothStep(0f, 1f, t / GrabTurnSeconds);
                if (view != null)
                {
                    view.SetView(Mathf.LerpAngle(fromYaw, toYaw, k), Mathf.Lerp(fromPitch, toPitch, k));
                }

                if (camera != null)
                {
                    camera.localPosition = rest + Random.insideUnitSphere * (GrabShake * (1f - k));
                }

                yield return null;
            }

            // こまが重くて途中が飛んでも、最後は必ず警官の顔を向いている。
            if (view != null)
            {
                view.SetView(toYaw, toPitch);
            }

            if (camera != null)
            {
                camera.localPosition = rest;
            }

            yield return new WaitForSecondsRealtime(GrabHoldSeconds);

            // 2. 暗転。暗い間に連れて行く。
            yield return Fade(0f, 1f, null);
            var walked = EscortNow(out var place);
            var seconds = _slice != null ? _slice.EscortSeconds(walked) : 0f;
            if (seconds > 0f)
            {
                _dayNight?.Skip(seconds);
                _player.Vitals.Vitals.DrainTime(seconds);
            }

            var caption = ProtoText.EscortedTo(place, seizedNames);
            _hud.SetCurtain(1f, caption);
            yield return new WaitForSecondsRealtime(CaptionSeconds);

            // 3. 明るくなると、警官が向かいにいる。
            yield return Fade(1f, 0f, caption);
            _hud.SetCurtain(0f, null);
            ResetEncounter();
            Say(PoliceText.Capture);
            _capturing = false;
            _session.CloseModal();
        }

        IEnumerator Fade(float from, float to, string caption)
        {
            for (var t = 0f; t < FadeSeconds; t += Time.unscaledDeltaTime)
            {
                _hud.SetCurtain(Mathf.Lerp(from, to, t / FadeSeconds), caption);
                yield return null;
            }

            _hud.SetCurtain(to, caption);
        }

        const float GrabTurnSeconds = 0.35f;
        const float GrabShake = 0.08f;
        const float GrabHoldSeconds = 1.1f;
        const float FadeSeconds = 0.45f;
        const float CaptionSeconds = 1.6f;

        /// <summary>取り上げた品の数。<paramref name="names"/> は取り上げた品の名前（暗転の一行で言う）。</summary>
        int Confiscate(out string names)
        {
            names = null;
            var bag = _player.Bag;
            if (bag == null || _catalog == null)
            {
                return 0;
            }

            var seized = Confiscation.SelectSeized(bag.Items, _catalog, _rng);
            var list = new System.Collections.Generic.List<string>();
            // 索引は降順なので、この順に抜けばずれない。
            foreach (var index in seized)
            {
                if (bag.TryRemoveAt(index, out var item))
                {
                    list.Add(_player.NameOf(item));
                }
            }

            names = ProtoText.NameList(list);
            return seized.Count;
        }

        /// <summary>
        /// 受け持ちの外れまで連れて行く。プレイヤーは送り先に、警官はその向かいに立つ（警官はそこから持ち場へ歩いて帰る）。
        /// 返すのは連れて行った道のり（m）。<paramref name="place"/> は送り先の呼び名。
        /// </summary>
        float EscortNow(out string place)
        {
            place = _escortPlace == EscortPlace.ArcadeOutside ? ProtoText.PlaceArcadeOutside : ProtoText.PlaceParkEdge;
            if (_escortPoint == null)
            {
                return 0f;
            }

            var from = _player.transform.position;
            var to = _escortPoint.position;
            _player.TeleportTo(to, _escortPoint.rotation);
            var view = _player.GetComponent<ProtoFirstPersonController>();
            if (view != null)
            {
                view.SetView(_escortPoint.eulerAngles.y, 0f);
            }

            var forward = Flat(_escortPoint.forward).normalized;
            _body.enabled = false;
            transform.SetPositionAndRotation(to + forward * EscortFacingDistance, Quaternion.LookRotation(-forward));
            _body.enabled = true;
            return Flat(to - from).magnitude;
        }

        const float EscortFacingDistance = 1.8f;

        void ResetEncounter()
        {
            _suspicion = 0f;
            Stage = PoliceStage.Unaware;
            _investigating = false;
            _stopsHands = false;
        }

        void Say(string line)
        {
            _speech = line;
            _speechAt = Time.unscaledTime;
            Spoke?.Invoke();
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
