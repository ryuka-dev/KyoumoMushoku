using System;
using System.Collections.Generic;

namespace KyoumoMushoku.Core.Shift
{
    /// <summary>返事を試みた結果。</summary>
    public enum AnswerOutcome
    {
        /// <summary>返事が成立した。</summary>
        Answered = 0,

        /// <summary>その方向に用件が無い。向く先を間違えている。</summary>
        NothingThere = 1,

        /// <summary>
        /// 言えない。SAN が低すぎて口が動かない（<see cref="ResponseSight"/>）。
        /// 黙る選択肢はこれを受けない。**操作そのものは決して奪わない。**
        /// </summary>
        CannotSpeak = 2,

        /// <summary>その番号の選択肢が無い。</summary>
        NoSuchOption = 3,
    }

    public readonly struct AnswerResult
    {
        public AnswerResult(AnswerOutcome outcome, Matter matter, ResponseOption option, float sanityDelta)
        {
            Outcome = outcome;
            Matter = matter;
            Option = option;
            SanityDelta = sanityDelta;
        }

        public AnswerOutcome Outcome { get; }

        public Matter Matter { get; }

        public ResponseOption Option { get; }

        /// <summary>
        /// 呼び出し側が適用する SAN の増減。**ここでは適用しない。**
        /// プレイヤーの状態の所有者は Gameplay 側の一箇所だけである（CLAUDE.md 第5条）。
        /// </summary>
        public float SanityDelta { get; }

        public bool Succeeded => Outcome == AnswerOutcome.Answered;
    }

    /// <summary>待たされている用件と、その残り時間。</summary>
    public sealed class PendingMatter
    {
        internal PendingMatter(Matter matter)
        {
            Matter = matter;
            RemainingSeconds = matter.PatienceSeconds;
        }

        public Matter Matter { get; }

        public float RemainingSeconds { get; internal set; }

        public bool IsOutOfPatience => RemainingSeconds <= 0f;
    }

    /// <summary>
    /// 一班（シフト）の権威。台本に従って用件を降らせ、待たせ、去らせる。
    /// 純粋ロジックであり、描画も入力も時間の出所も持たない（`Docs/第二章試作.md` 第五節・段1）。
    ///
    /// **SAN は適用しない。** 増減を返し、報せるだけである。プレイヤーの状態を書き換えてよいのは
    /// Gameplay 側の所有者だけであり、ここが二人目の所有者になってはならない（CLAUDE.md 第5条）。
    ///
    /// 用件は方向ごとに、着いた順に並ぶ。向いていない方向でも待ち時間は進む——
    /// **それが「一度に一つしか見られない」ことの代償であり、この章の圧力そのものである。**
    /// </summary>
    public sealed class NightShift
    {
        readonly List<ScheduledMatter> _script = new List<ScheduledMatter>();
        readonly List<PendingMatter> _pending = new List<PendingMatter>();

        int _nextIndex;

        public NightShift(IEnumerable<ScheduledMatter> script)
        {
            if (script != null)
            {
                foreach (var entry in script)
                {
                    if (entry.Matter != null)
                    {
                        _script.Add(entry);
                    }
                }
            }

            _script.Sort((a, b) => a.AtSeconds.CompareTo(b.AtSeconds));
        }

        /// <summary>班が始まってからの秒数。</summary>
        public float Elapsed { get; private set; }

        /// <summary>いま待たされている用件。着いた順。</summary>
        public IReadOnlyList<PendingMatter> Pending => _pending;

        public int AnsweredCount { get; private set; }

        public int MissedCount { get; private set; }

        /// <summary>台本を降らせ終え、待っている用件も無くなった。</summary>
        public bool IsOver => _nextIndex >= _script.Count && _pending.Count == 0;

        /// <summary>用件が着いた。世界の側が音を立てる（扉、電話、呼び出し）。</summary>
        public event Action<Matter> Arrived;

        /// <summary>
        /// 待たされた末に去った。**去る相手は必ず何か言って去る**（第十四節・因果は世界の中で閉じる）。
        /// SAN が減る理由は、画面の中で既に起きている。
        /// </summary>
        public event Action<Matter> Missed;

        public event Action<Matter, ResponseOption> Answered;

        /// <summary>
        /// 時間を進める。台本の用件を降らせ、待たせ、我慢が尽きたものを去らせる。
        /// 毎フレーム呼ぶ想定だが、時間の出所は外が決める。
        /// </summary>
        public void Advance(float seconds)
        {
            if (seconds <= 0f)
            {
                return;
            }

            Elapsed += seconds;

            while (_nextIndex < _script.Count && _script[_nextIndex].AtSeconds <= Elapsed)
            {
                var matter = _script[_nextIndex].Matter;
                _nextIndex++;
                _pending.Add(new PendingMatter(matter));
                Arrived?.Invoke(matter);
            }

            for (var i = _pending.Count - 1; i >= 0; i--)
            {
                var waiting = _pending[i];
                waiting.RemainingSeconds -= seconds;
                if (!waiting.IsOutOfPatience)
                {
                    continue;
                }

                _pending.RemoveAt(i);
                MissedCount++;
                Missed?.Invoke(waiting.Matter);
            }
        }

        /// <summary>その方向で、いま返事を待っている用件。無ければ null。着いた順に一つずつ相手をする。</summary>
        public PendingMatter FrontAt(ShiftFacing facing)
        {
            foreach (var waiting in _pending)
            {
                if (waiting.Matter.Facing == facing)
                {
                    return waiting;
                }
            }

            return null;
        }

        /// <summary>その方向に、返事を待っている用件があるか。</summary>
        public bool HasPendingAt(ShiftFacing facing) => FrontAt(facing) != null;

        /// <summary>
        /// いま向いている方向の用件に返事をする。向いていない方向へは返事できない——
        /// それが「一度に一つしか見られない」ことの意味である。
        /// </summary>
        public AnswerResult Answer(ShiftFacing facing, int optionIndex, float sanity)
        {
            var waiting = FrontAt(facing);
            if (waiting == null)
            {
                return new AnswerResult(AnswerOutcome.NothingThere, null, null, 0f);
            }

            var option = waiting.Matter.Option(optionIndex);
            if (option == null)
            {
                return new AnswerResult(AnswerOutcome.NoSuchOption, waiting.Matter, null, 0f);
            }

            // 言葉だけが奪われる。黙ることは最後まで残る（第三節）。
            if (!option.IsSilence && !ResponseSight.CanSpeak(sanity))
            {
                return new AnswerResult(AnswerOutcome.CannotSpeak, waiting.Matter, option, 0f);
            }

            _pending.Remove(waiting);
            AnsweredCount++;
            Answered?.Invoke(waiting.Matter, option);
            return new AnswerResult(AnswerOutcome.Answered, waiting.Matter, option, option.SanityDelta);
        }
    }
}
