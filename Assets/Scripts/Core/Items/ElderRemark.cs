using KyoumoMushoku.Core.Police;
using KyoumoMushoku.Core.Survival;

namespace KyoumoMushoku.Core.Items
{
    /// <summary>先輩ホームレスが保管庫を前にして何を言うか（第十四節）。</summary>
    public enum ElderRemarkKind
    {
        /// <summary>何も言わない。</summary>
        None = 0,

        /// <summary>事後説明。直近に起きたイベントの原因を語る。SAN を問わない。</summary>
        Aftermath = 1,

        /// <summary>噂話。予告済みイベントへの前倒しの助言。SAN 70 以上でのみ届く。</summary>
        Rumor = 2,

        /// <summary>貯め込みすぎの小言。膨らんだ箱は目立つ、という警告。</summary>
        HoardNag = 3,
    }

    /// <summary>
    /// 因果の解説者としての先輩ホームレスの発話選択（第十四節）。純関数として Core に閉じ、演出（NpcSpeech）は Gameplay。
    ///
    /// 優先順位は「起きたことの説明 → まだ起きていないことの助言（SAN 70 以上） → 貯め込みの小言」。
    /// 事後説明は SAN を問わず届き、噂話だけが SAN 70 以上を要する（低 SAN で失われるのは応じることであって聞こえること
    /// ではない・第三節）。
    /// </summary>
    public static class ElderRemark
    {
        public static ElderRemarkKind Decide(
            StashEventKind aftermath, StashEventKind forecast, int usedSlots, float sanity)
        {
            if (aftermath != StashEventKind.None)
            {
                return ElderRemarkKind.Aftermath;
            }

            if (forecast != StashEventKind.None && SanityScale.CanHearRumors(sanity))
            {
                return ElderRemarkKind.Rumor;
            }

            return usedSlots > ZoneAlertTuning.HoardThresholdSlots
                ? ElderRemarkKind.HoardNag
                : ElderRemarkKind.None;
        }
    }
}
