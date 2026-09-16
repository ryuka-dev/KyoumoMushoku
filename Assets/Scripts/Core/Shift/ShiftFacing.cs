namespace KyoumoMushoku.Core.Shift
{
    /// <summary>
    /// 深夜シフトで向ける方向（第二章・`Docs/第二章試作.md` 第二節）。移動はできず、これだけが選べる。
    ///
    /// 一度に一方向しか向けないことが、この章の命題「読めない」の実装である。
    /// 向いていない方向で事は起き続ける。振り向くまで、何が来たのかは分からない。
    /// </summary>
    public enum ShiftFacing
    {
        /// <summary>レジ。カウンターの向こうに客が立つ。</summary>
        Counter = 0,

        /// <summary>扉。誰が入ってくるかは、開いて初めて分かる。</summary>
        Door = 1,

        /// <summary>棚。品出しと、客が立ち止まる場所。</summary>
        Shelf = 2,

        /// <summary>防犯モニタ。店の中で唯一、見ていない場所が映る。ただし白黒で、遅い。</summary>
        Monitor = 3,
    }
}
