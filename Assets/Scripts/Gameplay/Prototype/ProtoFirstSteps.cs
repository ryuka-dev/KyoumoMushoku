using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 最初の一手（ゴミ箱を叩いて開ける）を、主人公の独り言で教える。一日目だけ・最初のゴミ箱が開くまで。
    /// 操作説明の板は出さない（第十四節：予告も説明も世界の中で）。照準の案内（<see cref="ProtoHands.AimPrompt"/>）と組で働く。
    ///
    /// 言うのは三つまで：始まってすぐの空腹、初めてゴミ箱に照準を合わせたとき、しばらくゴミ箱に近づかないとき。
    /// </summary>
    public sealed class ProtoFirstSteps : MonoBehaviour
    {
        [SerializeField] ProtoHands _hands;
        [SerializeField] ProtoDayNight _dayNight;
        [SerializeField] ProtoSession _session;

        [Tooltip("始まってから最初の独り言までの秒数。")]
        [SerializeField, Min(0f)] float _openingDelay = 1.5f;

        [Tooltip("この秒数が過ぎても一度もゴミ箱に照準を合わせていなければ、近くのゴミ箱を思い出させる。")]
        [SerializeField, Min(1f)] float _nudgeAfter = 35f;

        [Tooltip("独り言を出しておく秒数（周りを見回していても読めるように、ふつうの一言より長く）。")]
        [SerializeField, Min(1f)] float _lineSeconds = 4f;

        float _elapsed;
        bool _saidHungry;
        bool _saidAim;
        bool _saidNudge;

        public void Configure(ProtoHands hands, ProtoDayNight dayNight, ProtoSession session)
        {
            _hands = hands;
            _dayNight = dayNight;
            _session = session;
        }

        void Update()
        {
            if (_hands == null || _dayNight == null || _dayNight.Clock == null)
            {
                return;
            }

            if (_session != null && _session.State != ProtoSession.SessionState.Playing)
            {
                return;
            }

            // 一日目を過ぎたら、教えることはもう無い。
            if (_dayNight.Clock.Day > 1)
            {
                enabled = false;
                return;
            }

            var aimed = _hands.AimedTrashCan;
            if (aimed != null && aimed.IsOpened)
            {
                // 一つ開けた。叩けば開くことはもう知っている。
                enabled = false;
                return;
            }

            _elapsed += Time.deltaTime;
            if (!_saidHungry && _elapsed >= _openingDelay)
            {
                _saidHungry = true;
                _hands.Say(ProtoText.FirstStepHungry, _lineSeconds);
            }

            if (!_saidAim && aimed != null)
            {
                _saidAim = true;
                _hands.Say(ProtoText.FirstStepAimCan, _lineSeconds);
            }

            if (!_saidAim && !_saidNudge && _elapsed >= _nudgeAfter)
            {
                _saidNudge = true;
                _hands.Say(ProtoText.FirstStepNudge, _lineSeconds);
            }
        }
    }
}
