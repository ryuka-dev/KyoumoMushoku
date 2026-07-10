namespace KyoumoMushoku.Gameplay.Player
{
    /// <summary>
    /// 移動速度に掛かる倍率を供給する。渇きによる減速、将来の段ボール背負い（第十一節）など、
    /// 別々の理由で速度が落ちる仕組みを、モーターに個別の依存を持たせずに合成するための境界。
    /// </summary>
    public interface IMovementSpeedModifier
    {
        /// <summary>1 が等倍。0〜1 で減速する。</summary>
        float SpeedMultiplier { get; }
    }
}
