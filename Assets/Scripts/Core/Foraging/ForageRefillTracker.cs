namespace KyoumoMushoku.Core.Foraging
{
    /// <summary>
    /// ゴミ箱の翻找次数を「いつ満タンに戻すか」を決める純粋ロジック。補充は次の2つの契機で起きる。
    ///
    /// 1. 就寝で日付が変わったとき（第一節：一部エリアの再湧き）。
    /// 2. 昼／夕から夜へ切り替わったとき（第二節：夜は資源テーブルが切り替わる）。夜に新しい窓が開く
    ///    ことで、コンビニのゴミ箱に夜の弁当が出るという設計（第七・十三節）が実際に手に入るようになる。
    ///
    /// 時計インスタンスの差し替え（ロード）に頑健なよう、特定のイベントに依存せず、日付と夜フラグを
    /// ポーリングして遷移を読む（<see cref="TrashCan"/> が Day を読むのと同じ方針）。
    /// </summary>
    public sealed class ForageRefillTracker
    {
        int _lastDay;
        bool _lastNight;
        bool _initialized;

        /// <summary>現在の日付・夜フラグを観測し、翻找次数を満タンに戻すべきなら true を返す。</summary>
        public bool Observe(int day, bool night)
        {
            var refill = false;

            if (!_initialized)
            {
                // 起動・ロード直後は満タンから始める（第一節：ロード直後は新しい1日の始まり）。
                _initialized = true;
                refill = true;
            }
            else if (day != _lastDay)
            {
                // 就寝で日付が変わった＝再湧き。
                refill = true;
            }
            else if (night && !_lastNight)
            {
                // 昼／夕→夜。夜の資源窓が開く。同じ夜のあいだは二度補充しない。
                refill = true;
            }

            _lastDay = day;
            _lastNight = night;
            return refill;
        }
    }
}
