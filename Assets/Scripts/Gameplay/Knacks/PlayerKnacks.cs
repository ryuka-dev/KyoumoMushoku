using System;
using KyoumoMushoku.Core.Knacks;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Knacks
{
    /// <summary>
    /// コツ（生活の知恵・第六節）をシーンに接続する唯一の所有者。<see cref="KnackBook"/> を包み、
    /// 触発イベントの記録と習得の通知を担う。各システム（漁り・消費・就寝・警察）は、ここへ
    /// 「こういう出来事があった」と申告するか、<see cref="Has"/> でルール変化を読むだけである。
    ///
    /// セーブ・ロードの単一の所有者は <c>GameSession</c> であり、<see cref="CaptureState"/> /
    /// <see cref="RestoreState"/> を通してのみコツの状態を出し入れする。
    /// </summary>
    public sealed class PlayerKnacks : MonoBehaviour
    {
        KnackBook _book = new KnackBook();

        /// <summary>コツを新規習得した瞬間に発火する。習得の瞬間は明示的に通知する（第六節）。</summary>
        public event Action<KnackId> Acquired;

        /// <summary>
        /// ルール変化を読むための素の帳簿。読み取り専用の用途にのみ用いる
        /// （<c>Consumption</c> が鉄の胃袋の有無を見るなど）。習得を伴う記録は必ず <c>Record*</c> を通す。
        /// </summary>
        public KnackBook Book => _book;

        public bool Has(KnackId id) => _book.Has(id);

        public void RecordRummage()
        {
            if (_book.RecordRummage())
            {
                Announce(KnackId.SpotDuds);
            }
        }

        public void RecordForageWarned()
        {
            if (_book.RecordForageWarned())
            {
                Announce(KnackId.SteadyHands);
            }
        }

        public void RecordOutdoorSleep()
        {
            if (_book.RecordOutdoorSleep())
            {
                Announce(KnackId.StreetSleeper);
            }
        }

        public void RecordSurvivedRotten()
        {
            if (_book.RecordSurvivedRotten())
            {
                Announce(KnackId.IronStomach);
            }
        }

        public void RecordFirstWarning()
        {
            if (_book.RecordFirstWarning())
            {
                Announce(KnackId.FamiliarFace);
            }
        }

        public KnackState CaptureState() => _book.CaptureState();

        /// <summary>セーブデータから復元する。ロードの単一の所有者だけが呼ぶ。</summary>
        public void RestoreState(KnackState state) => _book.Restore(state);

        void Announce(KnackId id) => Acquired?.Invoke(id);
    }
}
