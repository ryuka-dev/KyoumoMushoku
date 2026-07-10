using System;

namespace KyoumoMushoku.Core.Economy
{
    /// <summary>
    /// 所持金（円）の唯一の権威。借金は発生させない（初版のスコープ外）。
    /// </summary>
    public sealed class Wallet
    {
        int _yen;

        public Wallet(int initialYen = 0)
        {
            _yen = initialYen < 0 ? 0 : initialYen;
        }

        public int Yen => _yen;

        public event Action Changed;

        public bool CanAfford(int amount) => amount >= 0 && _yen >= amount;

        public void Add(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _yen += amount;
            Changed?.Invoke();
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || _yen < amount)
            {
                return false;
            }

            if (amount == 0)
            {
                return true;
            }

            _yen -= amount;
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// 医療費の徴収。所持金が足りなければ全額を取り、それ以上は請求しない（第三節）。
        /// </summary>
        /// <returns>実際に徴収できた金額。</returns>
        public int SeizeUpTo(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            var seized = _yen < amount ? _yen : amount;
            if (seized == 0)
            {
                return 0;
            }

            _yen -= seized;
            Changed?.Invoke();
            return seized;
        }
    }
}
