using System;
using KyoumoMushoku.Core.Progress;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Progress
{
    /// <summary>
    /// 段階目標（第八・九節）をシーンに接続する唯一の所有者。<see cref="MilestoneBook"/> を包み、
    /// 触発イベントの記録と達成の通知を担う。各システム（就寝・購入）は、ここへ
    /// 「こういう出来事があった」と申告するだけであり、目標は何のルールも変えない。
    ///
    /// セーブ・ロードの単一の所有者は <c>GameSession</c> であり、<see cref="CaptureState"/> /
    /// <see cref="RestoreState"/> を通してのみ状態を出し入れする。
    /// </summary>
    public sealed class PlayerMilestones : MonoBehaviour
    {
        MilestoneBook _book = new MilestoneBook();

        /// <summary>目標を新たに達成した瞬間に発火する。トーストと結算画面が観測する。</summary>
        public event Action<MilestoneId> Achieved;

        /// <summary>達成状況を読むための素の帳簿。読み取り専用の用途にのみ用いる（HUD の目標一覧など）。</summary>
        public MilestoneBook Book => _book;

        public bool Has(MilestoneId id) => _book.Has(id);

        public void RecordDayBegan(int day)
        {
            if (_book.RecordDayBegan(day))
            {
                Announce(MilestoneId.SurviveThreeDays);
            }
        }

        public void RecordInnStay()
        {
            if (_book.RecordInnStay())
            {
                Announce(MilestoneId.FirstInnStay);
            }
        }

        public void RecordBackpackPurchase()
        {
            if (_book.RecordBackpackPurchase())
            {
                Announce(MilestoneId.BuyBackpack);
            }
        }

        public MilestoneState CaptureState() => _book.CaptureState();

        /// <summary>セーブデータから復元する。ロードの単一の所有者だけが呼ぶ。</summary>
        public void RestoreState(MilestoneState state) => _book.Restore(state);

        void Announce(MilestoneId id) => Achieved?.Invoke(id);
    }
}
