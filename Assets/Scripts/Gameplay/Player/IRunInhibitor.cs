namespace KyoumoMushoku.Gameplay.Player
{
    /// <summary>
    /// 走行を封じる理由を供給する。段ボールを背負っている間は走れない（第十一節）など、
    /// 別々の理由をモーターに個別の依存を持たせずに合成するための境界。
    /// </summary>
    public interface IRunInhibitor
    {
        /// <summary>いま走行が封じられているか。</summary>
        bool InhibitsRun { get; }
    }
}
