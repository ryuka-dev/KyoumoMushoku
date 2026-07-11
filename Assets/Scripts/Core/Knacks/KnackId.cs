namespace KyoumoMushoku.Core.Knacks
{
    /// <summary>
    /// 初版で実装する5つのコツ（第六節）。無職者はレベルアップしない。ただ、この街の勝手を覚えていく。
    /// すべてのコツはルール変化であり、原則としてパーセンテージの数値上昇ではない。
    ///
    /// 識別子であって表示名ではない。触発条件・ルール変化・検証する命題は設計文書 第六節の表を参照。
    /// </summary>
    public enum KnackId
    {
        /// <summary>あたりの見分け方（漁り・命題②）。ゴミ箱を10個漁ると、次に出るものが先に読める。</summary>
        SpotDuds = 0,

        /// <summary>手を止めない（漁り・命題②③）。漁り中に3回警告されると、警告で手が止まらなくなる。</summary>
        SteadyHands = 1,

        /// <summary>鉄の胃袋（生存・命題①）。腐敗食を食べて生き延びると、腐敗の代償が半減する。</summary>
        IronStomach = 2,

        /// <summary>路上の寝方（生存・命題①）。野外で2回寝ると、回復が上がり撤去を招きにくくなる。</summary>
        StreetSleeper = 3,

        /// <summary>通りすがりの顔（都市適応・命題③）。初めて警告されると、各遭遇の1回目の警告では顔を覚えられない。</summary>
        FamiliarFace = 4,
    }
}
