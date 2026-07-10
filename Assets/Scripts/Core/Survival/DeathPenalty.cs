namespace KyoumoMushoku.Core.Survival
{
    /// <summary>
    /// 死亡（HP0）は行き倒れであってゲームオーバーではない。第三節を参照。
    /// 段階の区切りと減少量は叩き台であり、プレイテストで調整する。
    /// </summary>
    public static class DeathPenalty
    {
        /// <summary>
        /// 死亡直前の SAN が高いほど大きく削られ、低いほど削られにくい。
        /// 既に低い者をさらに叩き潰すと、回復不能な悪循環に陥るためである。
        /// </summary>
        public static float SanityLoss(float sanityAtDeath)
        {
            if (sanityAtDeath >= 80f)
            {
                return 20f;
            }

            if (sanityAtDeath >= 50f)
            {
                return 10f;
            }

            return sanityAtDeath >= 20f ? 5f : 2f;
        }
    }
}
