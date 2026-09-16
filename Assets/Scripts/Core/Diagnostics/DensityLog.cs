using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace KyoumoMushoku.Core.Diagnostics
{
    /// <summary>
    /// プレイヤーが世界に対して起こした行動の種別。密度の計測にだけ使う分類であり、
    /// ゲームプレイはこの列挙を一切参照しない。
    /// </summary>
    public enum ActKind
    {
        Forage,
        Shop,
        Sleep,
        Water,
        Stash,
        Pickup,
        Hospital,
        Transit,
        Other,
    }

    /// <summary>1ゲームデイぶんの密度の記録。</summary>
    public sealed class DensityDay
    {
        readonly int[] _acts = new int[DensityLog.KindCount];

        public DensityDay(int day) => Day = day;

        public int Day { get; }

        /// <summary>その日のあいだに時計が進んだ秒数。</summary>
        public float ClockSeconds { get; internal set; }

        /// <summary>その日に費やした実時間の秒数。一時停止中は進まない。</summary>
        public float RealSeconds { get; internal set; }

        /// <summary>その日にとった行動の総数。＝その日に入った決断の数。</summary>
        public int Acts { get; private set; }

        /// <summary>そのうち夜にとったもの。夜の補充窓を使ったかは、夜の漁りの数で判る。</summary>
        public int NightActs { get; private set; }

        public int NightForages { get; private set; }

        /// <summary>就寝した時刻（その日の経過秒）。まだ寝ていなければ負。</summary>
        public float SleepAtClockSeconds { get; internal set; } = -1f;

        /// <summary>
        /// 行動が焼いた時計の秒数。ソフトクロックは実時間と等倍で進む（<c>GameClockDriver</c> が
        /// 毎フレーム <c>Advance(Time.deltaTime)</c> する）ので、時計と実時間の差は、すべて行動が
        /// 追加で進めた分である。バイト1シフト＝90秒、漁り1回＝15／18／22秒がここに積まれる。
        /// </summary>
        public float ActClockSeconds => Math.Max(0f, ClockSeconds - RealSeconds);

        public int Count(ActKind kind) => _acts[(int)kind];

        internal void Add(ActKind kind, bool night)
        {
            _acts[(int)kind]++;
            Acts++;

            if (!night)
            {
                return;
            }

            NightActs++;
            if (kind == ActKind.Forage)
            {
                NightForages++;
            }
        }
    }

    /// <summary>
    /// 密度の計測（`Docs/作業計画.md` 第四節 M1）。一局のあいだ、日ごとに「時計・実時間・行動数」を
    /// 積み、読める報告に整える純粋ロジックである。
    ///
    /// 手で記録するのをやめるために書いた。M1 が二週間動かなかったのは、計測が「遊びながら
    /// 行動数と就寝時刻を手記する」作業だったからである。作品自身が吐けば、一局遊ぶだけで済む。
    ///
    /// これは診断であり、ゲームプレイには一切影響しない（CLAUDE.md 第7条）。誰もこの数字を
    /// 読まなくても、遊びは同じように動く。
    /// </summary>
    public sealed class DensityLog
    {
        public const int KindCount = 9;

        static readonly string[] KindLabels =
        {
            "漁り", "店", "就寝", "水", "保管庫", "拾う", "病院", "階段", "その他",
        };

        readonly List<DensityDay> _days = new List<DensityDay>();

        DensityDay _current;
        float _dayRealStart;

        public IReadOnlyList<DensityDay> Days => _days;

        /// <summary>いま記録中の日。まだ一度も観測していなければ null。</summary>
        public DensityDay Current => _current;

        /// <summary>
        /// 現在の日付・時計・実時間を観測する。毎フレーム呼ぶ。日付が変わったら前日を閉じ、
        /// 新しい日を開く。特定の時計インスタンスのイベントには依存しない——ロードで時計そのものが
        /// 差し替わるためである（<c>ForageRefillTracker</c> と同じ方針）。
        /// </summary>
        public void Observe(int day, float clockElapsedInDay, float realSeconds)
        {
            if (_current == null || _current.Day != day)
            {
                _current = new DensityDay(day);
                _days.Add(_current);
                _dayRealStart = realSeconds;
            }

            _current.ClockSeconds = Math.Max(0f, clockElapsedInDay);
            _current.RealSeconds = Math.Max(0f, realSeconds - _dayRealStart);
        }

        /// <summary>
        /// 行動を1つ記録する。数えるのは実際に起きた行動だけであり、中断されたチャネルは数えない
        /// （中断は資源を消費しないので、決断でもない）。
        /// </summary>
        public void Record(ActKind kind, bool night, float clockElapsedInDay)
        {
            if (_current == null)
            {
                return;
            }

            _current.Add(kind, night);

            if (kind == ActKind.Sleep && _current.SleepAtClockSeconds < 0f)
            {
                _current.SleepAtClockSeconds = Math.Max(0f, clockElapsedInDay);
            }
        }

        public float TotalRealSeconds
        {
            get
            {
                var total = 0f;
                foreach (var day in _days)
                {
                    total += day.RealSeconds;
                }

                return total;
            }
        }

        public int TotalActs
        {
            get
            {
                var total = 0;
                foreach (var day in _days)
                {
                    total += day.Acts;
                }

                return total;
            }
        }

        public int TotalCount(ActKind kind)
        {
            var total = 0;
            foreach (var day in _days)
            {
                total += day.Count(kind);
            }

            return total;
        }

        /// <summary>オーバーレイに出す一行。ASCII に限る（開発用フォントの都合）。</summary>
        public string OverlayLine()
        {
            if (_current == null)
            {
                return "Acts -";
            }

            return string.Format(CultureInfo.InvariantCulture,
                "Acts {0} (day {1} night {2})    burn {3}",
                TotalActs, _current.Acts, _current.NightActs, Mmss(_current.ActClockSeconds));
        }

        /// <summary>一局ぶんの報告。ファイルに落として読む。</summary>
        public string Format(DateTime generatedAt)
        {
            var text = new StringBuilder();
            text.Append("今日も無職。 密度報告\n");
            text.Append("生成: ").Append(generatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)).Append('\n');
            text.Append("出口: 一局の実時間 20 分（Docs/作業計画.md 第三節）\n\n");

            text.Append("日  時計     実時間   比      行動が焼いた時計  行動数  夜の行動  夜の漁り  就寝時刻\n");
            foreach (var day in _days)
            {
                text.Append(day.Day.ToString(CultureInfo.InvariantCulture).PadRight(4));
                text.Append(Mmss(day.ClockSeconds).PadRight(9));
                text.Append(Mmss(day.RealSeconds).PadRight(9));
                text.Append(Ratio(day).PadRight(8));
                text.Append(Mmss(day.ActClockSeconds).PadRight(18));
                text.Append(day.Acts.ToString(CultureInfo.InvariantCulture).PadRight(8));
                text.Append(day.NightActs.ToString(CultureInfo.InvariantCulture).PadRight(10));
                text.Append(day.NightForages.ToString(CultureInfo.InvariantCulture).PadRight(10));
                text.Append(day.SleepAtClockSeconds < 0f ? "未就寝" : Mmss(day.SleepAtClockSeconds));
                text.Append('\n');
            }

            var acts = TotalActs;
            var real = TotalRealSeconds;
            text.Append('\n');
            text.Append("合計  実時間 ").Append(Mmss(real));
            text.Append("   行動 ").Append(acts.ToString(CultureInfo.InvariantCulture));
            text.Append("   1行動あたりの実時間 ");
            text.Append(acts > 0 ? (real / acts).ToString("F1", CultureInfo.InvariantCulture) + " 秒" : "-");
            text.Append('\n');

            text.Append("内訳  ");
            for (var i = 0; i < KindCount; i++)
            {
                text.Append(KindLabels[i]).Append(' ');
                text.Append(TotalCount((ActKind)i).ToString(CultureInfo.InvariantCulture)).Append("   ");
            }

            text.Append("\n\n");
            text.Append(Reading);
            return text.ToString();
        }

        const string Reading =
            "読み方\n" +
            "  「行動が焼いた時計」＝ 時計 − 実時間。ソフトクロックは実時間と等倍で進むので、\n" +
            "  差は行動が追加で焼いた分である（バイト1シフト＝90秒、漁り1回＝15／18／22秒）。\n" +
            "  ここが大きいほど、一日は歩かずに行動だけで溶けている。それが密度問題の正体であり、\n" +
            "  正確な言い方は「内容が少ない」ではなく「一日に3〜5個の決断しか入らない」である。\n" +
            "\n" +
            "  この報告で切り分けたいのは次の二つ（Docs/作業計画.md 第七節）。\n" +
            "  ・DaySchedule を伸ばして長くなり、行動数も増えた → 時計が締まりすぎていた。5c は不要。\n" +
            "  ・長くなったが行動の内訳が同じ三つのままだった → 内容が薄い。5c と場面の充填が要る。\n" +
            "\n" +
            "  数えていないもの：店の中の売買とバイトは「店」1回として数える。パネルを開いたあとの\n" +
            "  やりとりまでは数えていない。ただしバイトの+90秒は時計に載るので、\n" +
            "  「行動が焼いた時計」の側には正しく出る。\n";

        static string Ratio(DensityDay day)
        {
            if (day.RealSeconds < 0.5f)
            {
                return "-";
            }

            return "x" + (day.ClockSeconds / day.RealSeconds).ToString("F2", CultureInfo.InvariantCulture);
        }

        static string Mmss(float seconds)
        {
            var total = (int)Math.Max(0f, seconds);
            return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", total / 60, total % 60);
        }
    }
}
