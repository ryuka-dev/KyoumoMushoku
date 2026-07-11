using System;
using System.Collections.Generic;

namespace KyoumoMushoku.Core.Knacks
{
    /// <summary>
    /// 就寝時オートセーブで永続化されるコツの状態。
    ///
    /// 習得したコツは失われない（SAN が低くても消えない・第六節）。触発カウンタは複数日・複数セッションを
    /// またいで累積するため、習得済みの集合とともにここに載せる。閾値1のコツ（鉄の胃袋・通りすがりの顔）は
    /// 習得済みフラグがそのままカウンタを兼ねるので、専用のカウンタは持たない。
    /// </summary>
    [Serializable]
    public sealed class KnackState
    {
        /// <summary>習得済みのコツ。</summary>
        public List<KnackId> Acquired = new List<KnackId>();

        /// <summary>あたりの見分け方に向けた、ゴミ箱を漁った累積回数。</summary>
        public int RummageCount;

        /// <summary>手を止めない に向けた、漁り中に警告された累積回数。</summary>
        public int ForageWarnedCount;

        /// <summary>路上の寝方 に向けた、野外の無料の寝床で就寝した累積回数。</summary>
        public int OutdoorSleepCount;
    }
}
