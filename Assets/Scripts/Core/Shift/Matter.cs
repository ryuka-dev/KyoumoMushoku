using System;

namespace KyoumoMushoku.Core.Shift
{
    /// <summary>
    /// 返事のひとつ。台詞そのものは外（Gameplay のテキストモジュール）から渡される。
    /// Core は文字列を運ぶだけで、内容を持たない。
    /// </summary>
    public sealed class ResponseOption
    {
        public ResponseOption(string line, float sanityDelta, bool isSilence = false)
        {
            Line = line ?? string.Empty;
            SanityDelta = sanityDelta;
            IsSilence = isSilence;
        }

        /// <summary>画面に出す一行。</summary>
        public string Line { get; }

        /// <summary>これを選んだときの SAN の増減。</summary>
        public float SanityDelta { get; }

        /// <summary>
        /// 「黙っている」。**常に選べる**（第三節「生き延びる手段は決して塞がない」）。
        /// SAN がどれだけ落ちても、プレイヤーが手詰まりになる状態は作らない。
        /// </summary>
        public bool IsSilence { get; }
    }

    /// <summary>
    /// 用件。第二章の中心の概念（`Docs/第二章試作.md` 第二節）。
    ///
    /// **用件 ＝（どの方向にあるか、いつまで待つか、何と返事できるか）**
    ///
    /// 向くことが、その用件に取り組むことである。一度に一方向しか向けないので、
    /// 向いていない方向の用件は待たされている。これが「読めない」の実装であり、
    /// 空間（見ていない方向）と時間（重なる用件）の両方を、ひとつの概念で述べる。
    /// </summary>
    public sealed class Matter
    {
        readonly ResponseOption[] _options;

        public Matter(string id, ShiftFacing facing, string prompt, float patienceSeconds,
            ResponseOption[] options, float missSanityDelta)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("用件には識別子が要る。", nameof(id));
            }

            if (options == null || options.Length == 0)
            {
                throw new ArgumentException("返事の選択肢が無い用件は、用件ではない。", nameof(options));
            }

            Id = id;
            Facing = facing;
            Prompt = prompt ?? string.Empty;
            PatienceSeconds = patienceSeconds > 0f ? patienceSeconds : 0f;
            MissSanityDelta = missSanityDelta;
            _options = options;
        }

        public string Id { get; }

        /// <summary>この用件がある方向。ここを向いていないと返事できない。</summary>
        public ShiftFacing Facing { get; }

        /// <summary>相手が言うこと。返事の前に、こちらが読む一行。</summary>
        public string Prompt { get; }

        /// <summary>待ってくれる秒数。尽きると去る。</summary>
        public float PatienceSeconds { get; }

        /// <summary>
        /// 落としたときの SAN の増減。ふつうは負である。
        /// ただし罰として数字を引くのではない——去る相手は必ず何か言って去る（第十四節）。
        /// 減る理由は、画面の中で既に起きている。
        /// </summary>
        public float MissSanityDelta { get; }

        public int OptionCount => _options.Length;

        public ResponseOption Option(int index) =>
            index >= 0 && index < _options.Length ? _options[index] : null;
    }

    /// <summary>台本の一行。いつ、どの用件が降ってくるか。</summary>
    public readonly struct ScheduledMatter
    {
        public ScheduledMatter(float atSeconds, Matter matter)
        {
            AtSeconds = atSeconds > 0f ? atSeconds : 0f;
            Matter = matter;
        }

        public float AtSeconds { get; }

        public Matter Matter { get; }
    }
}
