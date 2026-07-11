using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Gameplay.Survival;
using KyoumoMushoku.Gameplay.UI;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.World
{
    /// <summary>
    /// 路地裏を仕切る先輩ホームレス。初版では「生活ゾーンにおける因果の解説者」として振る舞う（第十四節）。
    /// 場所代の取り立ては後続（5b-4）で同じ NPC に足す。
    ///
    /// 発話は頭上の世界内テキスト（<see cref="NpcSpeech"/>）。彼の小言はプレイヤーの返事を必要としないので、
    /// SAN がどれだけ落ちても事後説明の経路は死なない。噂話（前倒しの助言）だけが SAN 70 以上を要する（第三節）。
    /// 何を言うかの選択は純関数（<see cref="ElderRemark"/>）に閉じ、ここは世界の言葉への翻訳と発話だけを行う。
    /// </summary>
    public sealed class ElderHomeless : MonoBehaviour
    {
        [SerializeField] NpcSpeech _speech;

        PlayerVitals _vitals;

        void Start()
        {
            _vitals = FindFirstObjectByType<PlayerVitals>();
        }

        public void BindSpeech(NpcSpeech speech) => _speech = speech;

        /// <summary>初めて段ボール箱を置いたとき、罰の前にルールを提示する（第十二節）。</summary>
        public void SayPlacementRule() =>
            Say(StashText.ElderPlacementRule);

        /// <summary>場所代を受け取ったときの一言（第十二節）。支払いは世界の中の彼を通して行われる。</summary>
        public void SayRentPaid() =>
            Say(StashText.ElderRentPaid);

        /// <summary>
        /// 今日の場所代を払った箱を、その日のうちに担いで持ち出したときの一言。払った代償が
        /// 無駄になることを世界の言葉で告げる（見えない代償を可視化する・第十四節）。
        /// rent の扱いそのものは変えない――回収は従来どおりこの箱の据え置きを畳む。
        /// </summary>
        public void SayRentForfeited() =>
            Say(StashText.ElderRentForfeited);

        /// <summary>
        /// 保管庫を開けた瞬間の一言。起きたことの説明を最優先し、次に噂話（SAN 70 以上）、最後に貯め込みの小言。
        /// 何も言うことがなければ黙る。
        /// </summary>
        public void Comment(StashEventKind aftermath, int aftermathLost, StashEventKind forecast, int usedSlots)
        {
            var sanity = _vitals != null && _vitals.Vitals != null ? _vitals.Vitals.Sanity : 0f;

            switch (ElderRemark.Decide(aftermath, forecast, usedSlots, sanity))
            {
                case ElderRemarkKind.Aftermath:
                    Say(StashText.ElderAftermath(aftermath, aftermathLost));
                    break;
                case ElderRemarkKind.Rumor:
                    Say(StashText.ElderRumor(forecast));
                    break;
                case ElderRemarkKind.HoardNag:
                    Say(StashText.ElderHoardNag);
                    break;
            }
        }

        void Say(string line)
        {
            if (_speech != null && !string.IsNullOrEmpty(line))
            {
                _speech.Say(line);
            }
        }
    }
}
