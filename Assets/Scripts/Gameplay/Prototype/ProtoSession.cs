using System.Collections.Generic;
using KyoumoMushoku.Core.Knacks;
using KyoumoMushoku.Core.Survival;
using KyoumoMushoku.Gameplay.World;
using UnityEngine;
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
    /// 倒れたら局を終える。本編の病院（搬送・医療費・SAN の代償）は切片には入れていない。
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

        public SessionState State { get; private set; }

        bool _modalOpen;

        /// <summary>眠る前の一日に掴んだ物の数（朝の区切りで見せる）。</summary>
        public int CaughtYesterday { get; private set; }

        /// <summary>この局でこれまでに掴んだ物の数（結算で見せる）。</summary>
        public int CaughtTotal { get; private set; }

        /// <summary>切片が終わる日。</summary>
        public int FinalDay => _slice != null ? _slice.FinalDay : 3;

        public void Configure(ProtoVitals vitals, ProtoDayNight dayNight, ProtoStore store, ProtoHands hands,
            ProtoFirstPersonController body, SliceTuningAsset slice)
        {
            _slice = slice;
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
        }

        void OnKnackAcquired(KnackId id) => _knacksSinceMorning.Add(id);

        public void Restart()
        {
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
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void OnCollapsed() => Enter(SessionState.Collapsed);

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
