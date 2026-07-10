namespace KyoumoMushoku.Core.Foraging
{
    /// <summary>
    /// 抽選に使う乱数の供給口。乱数源をこの境界の内側に留めることで、
    /// 産出テーブルと食品状態の抽選（第十一節）を決定的に検証できるようにする。
    /// </summary>
    public interface IRng
    {
        /// <summary>0 以上 1 未満の一様乱数。</summary>
        double NextDouble();
    }
}
