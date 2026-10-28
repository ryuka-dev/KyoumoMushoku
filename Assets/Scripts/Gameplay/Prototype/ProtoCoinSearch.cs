using KyoumoMushoku.Core.Randomness;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 自販機の下に手を突っ込んで、落ちた小銭を探る。一日に一度。売る廃品とは別の、小さな金の出どころ。
    ///
    /// 何が落ちているかは、その日の始めに確定している（ゴミ箱と同じく、探るたびに引き直さない）。
    /// 日付が変わると、また誰かが落としている。
    ///
    /// 探り終えた日は触れる物ではない（開けたゴミ箱と同じく、縁も照準の輪も案内も消える。`GameDesign.md` 第十七節 規則 1）。
    /// </summary>
    public sealed class ProtoCoinSearch : MonoBehaviour, IProtoUsable, IProtoClosable
    {
        [SerializeField] ProtoDayNight _dayNight;
        [Tooltip("その日に何か落ちている確率。")]
        [SerializeField, Range(0f, 1f)] float _chance = 0.6f;
        [Tooltip("落ちているときの額。等確率で一つ選ぶ。")]
        [SerializeField] int[] _coins = { 10, 10, 50, 100, 100 };

        IRng _rng;
        int _day;
        int _yenToday;
        bool _searched;

        public void Configure(ProtoDayNight dayNight) => _dayNight = dayNight;

        void Awake() => _rng = new SystemRng();

        void Update()
        {
            var day = _dayNight != null && _dayNight.Clock != null ? _dayNight.Clock.Day : 1;
            if (day == _day)
            {
                return;
            }

            _day = day;
            _searched = false;
            _yenToday = _coins.Length > 0 && _rng.NextDouble() < _chance
                ? _coins[(int)(_rng.NextDouble() * _coins.Length) % _coins.Length]
                : 0;
        }

        /// <summary>今日まだ探っていない。</summary>
        public bool IsOpenNow => !_searched;

        public string Prompt(ProtoHands hands) => ProtoText.CoinSearchPrompt;

        public string Use(ProtoHands hands)
        {
            if (_searched)
            {
                return ProtoText.CoinAlreadySearched;
            }

            _searched = true;
            if (_yenToday <= 0)
            {
                return ProtoText.CoinNone;
            }

            hands.Wallet.Add(_yenToday);
            return ProtoText.CoinFound(_yenToday);
        }
    }
}
