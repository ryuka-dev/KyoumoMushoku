using System.Collections.Generic;

namespace KyoumoMushoku.Core.Knacks
{
    /// <summary>
    /// コツの所有者。習得しているかの問い合わせと、触発イベントの記録を提供する。
    ///
    /// 触発は具体的な行動（漁る・腐敗食を食べて生き延びる・野外で寝る・警告される）に紐づく。
    /// 閾値に達した瞬間に新規習得を確定し、その回の <c>Record*</c> は <c>true</c> を返す。
    /// 通知（世界の言葉で「覚えたな」と言う）は Gameplay 層が担う。この型は純粋データと規則だけを持つ。
    /// </summary>
    public sealed class KnackBook
    {
        KnackState _state;

        public KnackBook(KnackState state = null)
        {
            _state = state ?? new KnackState();
        }

        public bool Has(KnackId id) => _state.Acquired.Contains(id);

        /// <summary>習得済みのコツの数。結算画面の表示に使う。</summary>
        public int AcquiredCount => _state.Acquired.Count;

        public int RummageCount => _state.RummageCount;
        public int ForageWarnedCount => _state.ForageWarnedCount;
        public int OutdoorSleepCount => _state.OutdoorSleepCount;

        /// <summary>ゴミ箱を1回漁った。あたりの見分け方を習得したら true。</summary>
        public bool RecordRummage()
        {
            if (Has(KnackId.SpotDuds))
            {
                return false;
            }

            _state.RummageCount++;
            return _state.RummageCount >= KnackTuning.RummagesForSpotDuds && Acquire(KnackId.SpotDuds);
        }

        /// <summary>漁っている最中に警告された。手を止めない を習得したら true。</summary>
        public bool RecordForageWarned()
        {
            if (Has(KnackId.SteadyHands))
            {
                return false;
            }

            _state.ForageWarnedCount++;
            return _state.ForageWarnedCount >= KnackTuning.ForageWarningsForSteadyHands && Acquire(KnackId.SteadyHands);
        }

        /// <summary>野外の無料の寝床で寝た。路上の寝方 を習得したら true。</summary>
        public bool RecordOutdoorSleep()
        {
            if (Has(KnackId.StreetSleeper))
            {
                return false;
            }

            _state.OutdoorSleepCount++;
            return _state.OutdoorSleepCount >= KnackTuning.OutdoorSleepsForStreetSleeper && Acquire(KnackId.StreetSleeper);
        }

        /// <summary>腐敗した食品を食べて生き延びた。鉄の胃袋 を習得したら true（閾値1）。</summary>
        public bool RecordSurvivedRotten() => Acquire(KnackId.IronStomach);

        /// <summary>初めて警官に警告された。通りすがりの顔 を習得したら true（閾値1）。</summary>
        public bool RecordFirstWarning() => Acquire(KnackId.FamiliarFace);

        public KnackState CaptureState() => new KnackState
        {
            Acquired = new List<KnackId>(_state.Acquired),
            RummageCount = _state.RummageCount,
            ForageWarnedCount = _state.ForageWarnedCount,
            OutdoorSleepCount = _state.OutdoorSleepCount,
        };

        /// <summary>セーブデータから復元する。ロードの単一の所有者だけが呼ぶ。</summary>
        public void Restore(KnackState state)
        {
            _state = state ?? new KnackState();
        }

        /// <summary>まだ持っていなければ習得する。習得したら true。</summary>
        bool Acquire(KnackId id)
        {
            if (_state.Acquired.Contains(id))
            {
                return false;
            }

            _state.Acquired.Add(id);
            return true;
        }
    }
}
