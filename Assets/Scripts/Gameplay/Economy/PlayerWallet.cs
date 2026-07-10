using KyoumoMushoku.Core.Economy;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Economy
{
    /// <summary>所持金の所有者。<see cref="Wallet"/> を Unity のシーンに接続する薄い包み。</summary>
    public sealed class PlayerWallet : MonoBehaviour
    {
        [SerializeField] int _startingYen = 500;

        public Wallet Wallet { get; private set; }

        void Awake()
        {
            Wallet ??= new Wallet(_startingYen);
        }

        /// <summary>セーブデータから復元する。ロードの単一の所有者だけが呼ぶ。</summary>
        public void RestoreState(int yen)
        {
            Wallet = new Wallet(yen);
        }
    }
}
