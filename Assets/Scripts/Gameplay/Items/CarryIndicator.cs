using TMPro;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Items
{
    /// <summary>
    /// 段ボールを背負っている状態を、プレイヤーに見える形で示す（第十一・十四節）。機能上の代償
    /// （減速・走行不可・注目上昇）は <see cref="PlayerCarry"/> が既に効かせているが、それだけでは
    /// 「なぜ遅いのか」がプレイヤーに伝わらない。ここは状態の所有者ではなく、<see cref="PlayerCarry"/> を
    /// 毎フレーム読むだけの投影であり、世界内の見た目（占位 sprite）と HUD の一言を切り替える。
    ///
    /// どちらの参照も無くても成立する（灰箱では占位、美術工程で差し替える）。
    /// </summary>
    [RequireComponent(typeof(PlayerCarry))]
    public sealed class CarryIndicator : MonoBehaviour
    {
        [Tooltip("背負っている間、プレイヤーの姿に重ねて見せる占位 sprite（世界内の表現・無くても可）。")]
        [SerializeField] SpriteRenderer _carriedSprite;

        [Tooltip("背負っている間、HUD に出す一言（無くても可）。")]
        [SerializeField] TMP_Text _hudLabel;

        [Tooltip("HUD に出す背負い中の文言（叩き台）。")]
        [SerializeField] string _carryingText = "背負中：段ボール";

        PlayerCarry _carry;

        void Awake()
        {
            _carry = GetComponent<PlayerCarry>();
            Apply(false); // 初期は非表示。Update が実状態に合わせて上書きする。
        }

        void Update()
        {
            Apply(_carry != null && _carry.IsCarrying);
        }

        void Apply(bool carrying)
        {
            if (_carriedSprite != null)
            {
                _carriedSprite.enabled = carrying;
            }

            if (_hudLabel != null)
            {
                _hudLabel.text = carrying ? _carryingText : string.Empty;
            }
        }

        public void Bind(SpriteRenderer carriedSprite, TMP_Text hudLabel)
        {
            _carriedSprite = carriedSprite;
            _hudLabel = hudLabel;
        }
    }
}
