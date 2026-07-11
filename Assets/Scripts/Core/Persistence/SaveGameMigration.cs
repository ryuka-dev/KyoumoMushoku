using System.Collections.Generic;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Knacks;
using KyoumoMushoku.Core.Police;

namespace KyoumoMushoku.Core.Persistence
{
    /// <summary>
    /// 古い版のセーブを現在の版へ引き上げる。読み込み経路でのみ、検証より前に呼ぶ。
    ///
    /// 引き上げは片道である。未来の版（このビルドが知らない版）は黙って読まず、明示的に失敗させる。
    /// 知らない形式を推測で解釈すると、ユーザーのデータを静かに壊す（第十一節）。
    /// </summary>
    public static class SaveGameMigration
    {
        /// <summary>
        /// 必要なら <paramref name="save"/> をその場で引き上げる。
        /// 成功した場合、<c>save.Version</c> は <see cref="SaveGame.CurrentVersion"/> になっている。
        /// </summary>
        public static bool TryUpgrade(SaveGame save, out string error)
        {
            if (save is null)
            {
                error = "セーブデータが空である。";
                return false;
            }

            if (save.Version > SaveGame.CurrentVersion)
            {
                error = $"このビルドより新しいセーブデータである（版 {save.Version}。想定は {SaveGame.CurrentVersion} 以下）。";
                return false;
            }

            if (save.Version < SaveGame.OldestSupportedVersion)
            {
                error = $"対応していない古いセーブデータの版（{save.Version}）。";
                return false;
            }

            if (save.Version == 1)
            {
                UpgradeFrom1To2(save);
            }

            if (save.Version == 2)
            {
                UpgradeFrom2To3(save);
            }

            if (save.Version == 3)
            {
                UpgradeFrom3To4(save);
            }

            if (save.Version == 4)
            {
                UpgradeFrom4To5(save);
            }

            if (save.Version == 5)
            {
                UpgradeFrom5To6(save);
            }

            error = null;
            return true;
        }

        /// <summary>
        /// 版 1 は警戒度を持たない。まだ誰にも顔を覚えられていない状態、すなわちすべて 0 として引き上げる。
        /// これは実際に版 1 が表していた世界と一致する（警察はまだ存在しなかった）。
        /// </summary>
        static void UpgradeFrom1To2(SaveGame save)
        {
            save.ZoneAlerts = new ZoneAlertState();
            save.Version = 2;
        }

        /// <summary>
        /// 版 2 は誰もコツを知らない。まだ何も習得しておらず、触発カウンタもすべて 0 の状態として引き上げる。
        /// これは実際に版 2 が表していた世界と一致する（コツはまだ存在しなかった）。
        /// </summary>
        static void UpgradeFrom2To3(SaveGame save)
        {
            save.Knacks = new KnackState();
            save.Version = 3;
        }

        /// <summary>版 3 は背負いスロットを持たない。何も担いでいない状態として引き上げる。</summary>
        static void UpgradeFrom3To4(SaveGame save)
        {
            save.CarrySlot = new CarrySlotState();
            save.Version = 4;
        }

        /// <summary>
        /// 版 4 は保管庫という概念を持たない。まだどこにも財産を置いていない状態、すなわち保管庫なしとして引き上げる。
        /// これは実際に版 4 が表していた世界と一致する（拠点に置く器はまだ存在しなかった）。
        /// </summary>
        static void UpgradeFrom4To5(SaveGame save)
        {
            save.Stashes = new List<StashState>();
            save.Version = 5;
        }

        /// <summary>
        /// 版 5 は保管庫イベントという概念を持たない。まだ何も予告されていない状態として引き上げる。
        /// これは実際に版 5 が表していた世界と一致する（イベントはまだ存在しなかった）。
        /// </summary>
        static void UpgradeFrom5To6(SaveGame save)
        {
            save.PendingStashEvents = new List<PendingStashEvent>();
            save.Version = 6;
        }
    }
}
