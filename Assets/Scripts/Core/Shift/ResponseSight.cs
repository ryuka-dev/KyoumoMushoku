using KyoumoMushoku.Core.Survival;

namespace KyoumoMushoku.Core.Shift
{
    /// <summary>返事がどこまで届くか。第二章の SAN の階段（`Docs/第二章試作.md` 第三節）。</summary>
    public enum ResponseReach
    {
        /// <summary>読めるし、言える。</summary>
        Speakable = 0,

        /// <summary>
        /// 読めるが、言えない。**言うべきことが見えているのに、口が動かない。**
        /// この帯がこの章の中心である。第一章では「誰も話しかけてこなくなる」という静かな喪失だったが、
        /// 返事が職務である以上、ここでは直接の失敗になる。
        /// </summary>
        Mute = 1,

        /// <summary>読めないし、言えない。`??` になる。</summary>
        Unreadable = 2,
    }

    /// <summary>
    /// SAN が返事に何をするか。**新しい閾値を作らない。** <see cref="SanityScale"/> に既にあるものが、
    /// この章の欲しい段階とそのまま一致する（第三節に SAN の閾値を集約するという規約）。
    ///
    /// <see cref="SanityScale.CanRespondToConversation"/> は第一章から「失われるのは応じることであって
    /// 聞こえることではない」と述べていた。第二章はその文をそのまま玩法にする。
    ///
    /// なお「黙っている」は常に選べる（第三節「生き延びる手段は決して塞がない」）。
    /// ここが決めるのは**言葉**の届く範囲であって、操作そのものは決して奪わない。
    /// </summary>
    public static class ResponseSight
    {
        public static ResponseReach Read(float sanity)
        {
            if (sanity < SanityScale.BreakdownThreshold)
            {
                return ResponseReach.Unreadable;
            }

            return SanityScale.CanRespondToConversation(sanity)
                ? ResponseReach.Speakable
                : ResponseReach.Mute;
        }

        /// <summary>言葉として口に出せるか。黙る選択肢はこれを問わない。</summary>
        public static bool CanSpeak(float sanity) => Read(sanity) == ResponseReach.Speakable;
    }
}
