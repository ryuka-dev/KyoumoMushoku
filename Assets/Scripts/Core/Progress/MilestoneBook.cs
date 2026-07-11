using System.Collections.Generic;

namespace KyoumoMushoku.Core.Progress
{
    /// <summary>
    /// 段階目標の所有者。達成しているかの問い合わせと、触発イベントの記録を提供する。
    ///
    /// 触発は具体的な出来事（新しい日を迎える・安宿に泊まる・バックパックを買う）に紐づく。
    /// 達成した瞬間の <c>Record*</c> は <c>true</c> を返す。通知（トースト・結算画面）は
    /// Gameplay 層が担う。この型は純粋データと規則だけを持つ。
    /// </summary>
    public sealed class MilestoneBook
    {
        MilestoneState _state;

        public MilestoneBook(MilestoneState state = null)
        {
            _state = state ?? new MilestoneState();
        }

        public bool Has(MilestoneId id) => _state.Achieved.Contains(id);

        /// <summary>達成済みの目標の数。結算画面の表示に使う。</summary>
        public int AchievedCount => _state.Achieved.Count;

        /// <summary>
        /// 新しい日が始まった。生存目標の日数を生き延びていたら（＝その翌日を迎えたら）
        /// 3日間生存 を達成して true。
        /// </summary>
        public bool RecordDayBegan(int day) =>
            day > MilestoneTuning.SurvivalDays && Acquire(MilestoneId.SurviveThreeDays);

        /// <summary>安宿（有料の就寝場所）に泊まった。初回なら達成して true。</summary>
        public bool RecordInnStay() => Acquire(MilestoneId.FirstInnStay);

        /// <summary>バックパック（容量拡張の装備）を購入した。初回なら達成して true。</summary>
        public bool RecordBackpackPurchase() => Acquire(MilestoneId.BuyBackpack);

        public MilestoneState CaptureState() => new MilestoneState
        {
            Achieved = new List<MilestoneId>(_state.Achieved),
        };

        /// <summary>セーブデータから復元する。ロードの単一の所有者だけが呼ぶ。</summary>
        public void Restore(MilestoneState state)
        {
            _state = state ?? new MilestoneState();
        }

        /// <summary>まだ達成していなければ達成する。達成したら true。</summary>
        bool Acquire(MilestoneId id)
        {
            if (_state.Achieved.Contains(id))
            {
                return false;
            }

            _state.Achieved.Add(id);
            return true;
        }
    }
}
