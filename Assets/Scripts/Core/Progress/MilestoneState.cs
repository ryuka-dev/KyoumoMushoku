using System;
using System.Collections.Generic;

namespace KyoumoMushoku.Core.Progress
{
    /// <summary>
    /// 就寝時オートセーブで永続化される段階目標の状態。
    ///
    /// 達成した目標は失われない。触発はいずれも一度きりの出来事（日付・宿泊・購入）なので、
    /// コツと違って累積カウンタは持たず、達成済みの集合だけを載せる。
    /// </summary>
    [Serializable]
    public sealed class MilestoneState
    {
        /// <summary>達成済みの目標。</summary>
        public List<MilestoneId> Achieved = new List<MilestoneId>();
    }
}
