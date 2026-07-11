using System;

namespace KyoumoMushoku.Core.Shop
{
    /// <summary>
    /// その日にコンビニへ売った廃品の合計額。1日あたりの買い取り上限（第十三節）を数えるための帳簿。
    ///
    /// これは一過性の状態である。就寝＝日境界でリセットし（<see cref="BeginNextDay"/>）、就寝の瞬間に
    /// セーブされるため、ロード時は必ず新しい1日＝0 から始まる。したがってセーブ形式には載せない。
    /// これはゴミ箱のリポップが日境界で満タンに戻るのと同じ扱いである（第二節）。
    /// </summary>
    public sealed class SalvageLedger
    {
        public int SoldTodayYen { get; private set; }

        public void Add(int yen)
        {
            if (yen > 0)
            {
                SoldTodayYen += yen;
            }
        }

        /// <summary>その日の上限までに、あと何円ぶん買い取ってもらえるか。</summary>
        public int RemainingToday(int dailyCapYen) => Math.Max(0, dailyCapYen - SoldTodayYen);

        /// <summary>新しい1日。店主の財布も気分も入れ替わる。就寝経路からのみ呼ぶ。</summary>
        public void BeginNextDay() => SoldTodayYen = 0;
    }
}
