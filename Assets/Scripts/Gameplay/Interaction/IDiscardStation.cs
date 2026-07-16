namespace KyoumoMushoku.Gameplay.Interaction
{
    /// <summary>
    /// 持ち物を捨てられる場所（ゴミ箱）。調べる（E）とは独立した動詞で、UI は G を割り当てる。
    ///
    /// 捨てるのは無条件である。漁りの残量・時間帯・ゾーンに関わらず、いつでも何でも捨てられる。
    /// 捨てた物はただ消えるだけで、世界には何も起きない——注目度・警戒度・時計のいずれも動かない。
    /// カバンを身軽にする以外の意味を持たせないことが仕様である。
    /// </summary>
    public interface IDiscardStation
    {
    }
}
