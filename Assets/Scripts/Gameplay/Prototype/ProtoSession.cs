using System.Collections.Generic;
using KyoumoMushoku.Core.Knacks;
using KyoumoMushoku.Core.Police;
using KyoumoMushoku.Core.Randomness;
using KyoumoMushoku.Core.Survival;
using KyoumoMushoku.Gameplay.App;
using KyoumoMushoku.Gameplay.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 一人称の切片の一局の流れ。寝て一晩を越すこと、朝の区切り、倒れたときの終わりを持つ。
    ///
    /// 毎朝、区切りで前の日の結果を見せ、続けるか終えるかを選ばせる（GameDesign 第十六節：一日ごとにやめられる）。
    /// 切片は三日（<see cref="SliceTuningAsset.FinalDay"/>）。その夜に寝たら、翌朝に三日間の結算を見せる。
    /// 結算の後も続けて遊べる（2026-10-28 決定）。四日目からの昼夜の長さは三日目のまま（DaySchedule は最後の日を使い続ける）。
    /// 日付の変わり目は就寝の瞬間だけに起こる（第二節）。時計・店の帳簿・ゴミ箱の中身は、ここから一度に新しい一日へ進める。
    ///
    /// 倒れても局は終わらない（2026-10-28 決定・本編の病院と同じ規則）。西の外れの診療所（<see cref="ProtoClinic"/>）へ運ばれ、
    /// HP だけが全回復し、渇きと空腹は倒れる前のまま。医療費を取られ（足りなければあるだけ・借金はしない）、
    /// 倒れる前の SAN が高いほど気分が沈み（Core の <see cref="DeathPenalty"/>）、腐った物は警官と同じ規則で取り上げられる。
    /// 時計は進まず、その日がそのまま続く。何が起きたかは区切りの画面で、世界の言葉で言う（第十四節）。倒れることはセーブ地点ではない。
    /// </summary>
    public sealed class ProtoSession : MonoBehaviour
    {
        public enum SessionState
        {
            Playing,
            Morning,
            Final,
            Collapsed,
        }

        [SerializeField] ProtoVitals _vitals;
        [SerializeField] ProtoDayNight _dayNight;
        [SerializeField] ProtoStore _store;
        [SerializeField] ProtoHands _hands;
        [SerializeField] ProtoFirstPersonController _body;
        [SerializeField] SliceTuningAsset _slice;
        [SerializeField] ProtoClinic _clinic;

        public SessionState State { get; private set; }

        /// <summary>この局で診療所に運ばれた回数（結算で見せる）。</summary>
        public int ClinicVisits { get; private set; }

        /// <summary>直前に運ばれたときの医療費の額と、実際に払えた額。倒れた区切りで見せる。</summary>
        public int LastClinicFeeYen { get; private set; }
        public int LastClinicPaidYen { get; private set; }

        /// <summary>直前に運ばれたとき、腐った物を取り上げられたか。</summary>
        public bool LastClinicSeizedRotten { get; private set; }

        readonly IRng _rng = new SystemRng();

        bool _modalOpen;

        /// <summary>店などの場面を閉じたこま。同じ Esc で店を出て、そのままポーズが開かないようにする。</summary>
        int _modalClosedFrame = -1;

        InputAction _pauseKey;

        /// <summary>遊んでいる最中に Esc で開くポーズ画面（続ける・最初から・やめる・音量・マウスの感度）。</summary>
        public bool IsPaused { get; private set; }

        /// <summary>眠る前の一日に掴んだ物の数（朝の区切りで見せる）。</summary>
        public int CaughtYesterday { get; private set; }

        /// <summary>この局でこれまでに掴んだ物の数（結算で見せる）。</summary>
        public int CaughtTotal { get; private set; }

        /// <summary>切片が終わる日。</summary>
        public int FinalDay => _slice != null ? _slice.FinalDay : 3;

        public void Configure(ProtoVitals vitals, ProtoDayNight dayNight, ProtoStore store, ProtoHands hands,
            ProtoFirstPersonController body, SliceTuningAsset slice, ProtoClinic clinic = null)
        {
            _slice = slice;
            _clinic = clinic;
            _vitals = vitals;
            _dayNight = dayNight;
            _store = store;
            _hands = hands;
            _body = body;
        }

        /// <summary>前の朝の区切りから覚えたコツ（朝の区切りで見せる）。</summary>
        public IReadOnlyList<KnackId> KnacksLearnedSinceMorning => _knacksSinceMorning;

        /// <summary>この局で覚えたコツのすべて（結算で見せる）。</summary>
        public IReadOnlyList<KnackId> KnacksLearned =>
            _hands != null && _hands.Knacks != null ? _hands.Knacks.CaptureState().Acquired : new List<KnackId>();

        readonly List<KnackId> _knacksSinceMorning = new List<KnackId>();

        void Awake()
        {
            _pauseKey = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");
        }

        void OnEnable() => _pauseKey.Enable();

        void OnDisable() => _pauseKey.Disable();

        void Start()
        {
            if (_vitals != null)
            {
                _vitals.Died += OnCollapsed;
            }

            if (_hands != null && _hands.Knacks != null)
            {
                _hands.Knacks.Acquired += OnKnackAcquired;
            }
        }

        void OnDestroy()
        {
            if (_vitals != null)
            {
                _vitals.Died -= OnCollapsed;
            }

            if (_hands != null && _hands.Knacks != null)
            {
                _hands.Knacks.Acquired -= OnKnackAcquired;
            }

            _pauseKey?.Dispose();
            Time.timeScale = 1f;
        }

        /// <summary>この局で安宿に泊まった晩の数（結算で見せる。2D 版の段階目標「初めての安宿」）。</summary>
        public int InnNights { get; private set; }

        /// <summary>寝床（ベンチ）で眠る。返すのは目覚めの一言。</summary>
        public string Sleep(ProtoSleepSpot spot) => spot != null ? Sleep(spot.Recovery, atInn: false) : null;

        /// <summary>
        /// 眠る。一晩ぶんの消耗と回復を当て、日付を進める。返すのは目覚めの一言。
        /// 安宿（<paramref name="atInn"/>）は 2D 版と同じくすべてを満たし、一晩の消耗も免除される。段ボールは敷かない（背負ったまま朝を迎える）。
        /// </summary>
        public string Sleep(SleepTuningAsset.Recovery recovery, bool atInn)
        {
            if (State != SessionState.Playing || _vitals == null || _vitals.Vitals == null)
            {
                return null;
            }

            // 本編の GameSession.ApplySleepRecovery と同じ規則：一晩ぶん渇きと空腹が減り、寝床の回復が乗る。
            // SAN の回復だけは崩壊時に下がるが、下限を割らない（第三節）。
            var vitals = _vitals.Vitals;

            // 段ボールを背負ったままベンチで寝ると、下に敷く。回復が上がり、段ボールは朝には湿って使えない（切片の品目 第五節）。
            var bedding = !atInn && _slice != null && _hands != null && _hands.Back != null && _hands.Back.IsOccupied
                          && _hands.Back.TryTakeOut(out _);
            if (bedding)
            {
                recovery = _slice.CardboardBedding;
            }

            if (atInn || recovery.fullRestore)
            {
                // 完全回復（本編の GameSession.ApplySleepRecovery と同じ）。SAN も含めて満たす。
                InnNights++;
                vitals.Apply(new VitalsDelta
                {
                    Hp = vitals.Tuning.MaxHp,
                    Thirst = vitals.Tuning.MaxThirst,
                    Hunger = vitals.Tuning.MaxHunger,
                    Sanity = vitals.Tuning.MaxSanity,
                });
            }
            else
            {
                // 路上の寝方（第六節）：野外の寝床では回復が上がる。判定は覚える前の状態で行う＝覚えた次の晩から効く（本編と同じ）。
                var knacks = _hands != null ? _hands.Knacks : null;
                var outdoorBonus = knacks != null && knacks.Has(KnackId.StreetSleeper) ? KnackTuning.StreetSleeperRecoveryBonus : 0f;
                vitals.Apply(new VitalsDelta
                {
                    Hp = recovery.hp + outdoorBonus,
                    Thirst = recovery.thirst - vitals.Tuning.OvernightThirstDrain,
                    Hunger = recovery.hunger - vitals.Tuning.OvernightHungerDrain,
                    Sanity = SanityScale.SleepRecovery(recovery.sanity + outdoorBonus, vitals.Sanity),
                });

                // 野外で寝た回数を数える。2 回で 路上の寝方 を覚える。
                if (knacks != null)
                {
                    knacks.RecordOutdoorSleep();
                }
            }

            _dayNight.BeginNextDay();
            _store?.BeginNextDay();
            // ゴミ箱の補充は各ゴミ箱が日付の変わり目を読んで自分で行う（ProtoTrashCan）。
            if (_hands != null)
            {
                CaughtYesterday = _hands.TakeCaughtCount();
                CaughtTotal += CaughtYesterday;
            }

            // 三日の結算は一度だけ。続けて遊んだ後の朝は、ふつうの朝の区切りになる。
            var final = !FinalShown && _dayNight.Clock.Day > FinalDay;
            FinalShown |= final;
            Enter(final ? SessionState.Final : SessionState.Morning);
            return atInn ? ProtoText.WokeUpAtInn : bedding ? ProtoText.WokeUpOnCardboard : ProtoText.WokeUp;
        }

        /// <summary>
        /// 店の中のような、遊びを止めて画面の上で選ぶ場面を開く。遊んでいる最中でなければ開かない。
        /// 開いた側が <see cref="CloseModal"/> で閉じる。
        /// </summary>
        public bool TryOpenModal()
        {
            if (State != SessionState.Playing || _modalOpen)
            {
                return false;
            }

            _modalOpen = true;
            SetPlayerActive(false);
            return true;
        }

        public void CloseModal()
        {
            if (!_modalOpen)
            {
                return;
            }

            _modalOpen = false;
            _modalClosedFrame = Time.frameCount;
            if (State == SessionState.Playing)
            {
                SetPlayerActive(true);
            }
        }

        /// <summary>三日の結算をもう見せたか。見せた後は、続けて遊んでも二度は出ない。</summary>
        public bool FinalShown { get; private set; }

        /// <summary>朝の区切りか三日の結算から、そのまま次の一日を遊ぶ。</summary>
        public void Continue()
        {
            if (State == SessionState.Morning || State == SessionState.Final)
            {
                _knacksSinceMorning.Clear();
                Enter(SessionState.Playing);
            }
            else if (State == SessionState.Collapsed && !TransportPending)
            {
                // 運ばれた後は、同じ日のまま診療所の前から続ける。
                Enter(SessionState.Playing);
                if (_hands != null)
                {
                    _hands.Say(ProtoText.WokeUpAtClinic);
                }
            }
        }

        void OnKnackAcquired(KnackId id) => _knacksSinceMorning.Add(id);

        public void Restart()
        {
            PlayerOptions.Save();
            Time.timeScale = 1f;
            var scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
            // エディタではシーンが Build Settings に無くても読み直せるようにする。
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters());
#else
            SceneManager.LoadScene(scene.buildIndex);
#endif
        }

        public void Quit()
        {
            PlayerOptions.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>倒れて、まだ運び終えていない。区切りの画面はこの間、本文を出さない。</summary>
        public bool TransportPending { get; private set; }

        float _sanityAtCollapse;

        /// <summary>
        /// 倒れた瞬間。手と足を止め、運ぶのは次の Update に回す。倒れたのは飲み食いや捕まった最中の HP の変更の内側なので、
        /// ここで鞄を書き換えると、呼んだ側がまだ使っている索引がずれる（腐った物を食べて倒れると、別の品が消える）。
        /// </summary>
        void OnCollapsed()
        {
            _sanityAtCollapse = _vitals.Vitals.Sanity;
            TransportPending = true;
            Enter(SessionState.Collapsed);
        }

        void Update()
        {
            if (TransportPending)
            {
                TransportPending = false;
                TransportToClinic();
            }

            if (_pauseKey.WasPressedThisFrame())
            {
                if (IsPaused)
                {
                    Resume();
                }
                else if (Time.frameCount != _modalClosedFrame && TryOpenModal())
                {
                    IsPaused = true;
                }
            }
        }

        /// <summary>ポーズ画面を閉じて遊びに戻る。設定はここで一度だけ書き出す。</summary>
        public void Resume()
        {
            if (!IsPaused)
            {
                return;
            }

            IsPaused = false;
            PlayerOptions.Save();
            CloseModal();
        }

        /// <summary>診療所へ運ぶ。規則は本編の GameSession.OnPlayerDied と同じ。</summary>
        void TransportToClinic()
        {
            var vitals = _vitals.Vitals;
            var fee = _clinic != null ? _clinic.FeeYen : 0;
            LastClinicFeeYen = fee;
            LastClinicPaidYen = _hands != null && _hands.Wallet != null ? _hands.Wallet.SeizeUpTo(fee) : 0;

            // SAN の代償は倒れる直前の値で決まる。渇き・空腹は倒れる前のまま、HP だけが戻る。
            vitals.Apply(new VitalsDelta { Sanity = -DeathPenalty.SanityLoss(_sanityAtCollapse) });
            vitals.Revive();

            // 腐った物の没収は警官と同じ規則（取り上げられるという同じ出来事に、二つの規則を持たせない）。
            LastClinicSeizedRotten = Confiscate() > 0;
            ClinicVisits++;

            ProtoPolice.ForgetPlayer();
            if (_hands != null && _clinic != null)
            {
                _hands.TeleportTo(_clinic.WakePosition, _clinic.WakeRotation);
            }
        }

        int Confiscate()
        {
            var bag = _hands != null ? _hands.Bag : null;
            if (bag == null || _hands.Catalog == null)
            {
                return 0;
            }

            var seized = Confiscation.SelectSeized(bag.Items, _hands.Catalog, _rng);
            // 索引は降順なので、この順に抜けばずれない。
            foreach (var index in seized)
            {
                bag.TryRemoveAt(index, out _);
            }

            return seized.Count;
        }

        void Enter(SessionState state)
        {
            State = state;
            SetPlayerActive(state == SessionState.Playing && !_modalOpen);
        }

        /// <summary>
        /// 区切りや店の間は手と足を離してカーソルを返し、時間を止める。
        /// 手を止めると時間の流れを戻す（ヒットストップの後始末）ので、時間はその後に決める。
        /// </summary>
        void SetPlayerActive(bool active)
        {
            if (_body != null)
            {
                _body.enabled = active;
            }

            if (_hands != null)
            {
                _hands.enabled = active;
            }

            Time.timeScale = active ? 1f : 0f;
        }
    }
}
