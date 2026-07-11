using System.Text;
using KyoumoMushoku.Core.Progress;
using KyoumoMushoku.Gameplay.Progress;
using TMPro;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.UI
{
    /// <summary>
    /// 段階目標の一覧を HUD に常時出す（第八節）。達成済みは打ち消し線で示す。
    /// 状態は持たない投影であり、権威は <see cref="PlayerMilestones"/> にある。
    /// </summary>
    public sealed class MilestoneHud : MonoBehaviour
    {
        // 目標は列挙の全件を定義順で出す。スライスの目標は3つで固定（第九節）。
        static readonly MilestoneId[] Order =
        {
            MilestoneId.SurviveThreeDays,
            MilestoneId.FirstInnStay,
            MilestoneId.BuyBackpack,
        };

        [SerializeField] PlayerMilestones _milestones;
        [SerializeField] TMP_Text _text;

        readonly StringBuilder _sb = new StringBuilder();

        public void Configure(PlayerMilestones milestones, TMP_Text text)
        {
            _milestones = milestones;
            _text = text;
        }

        void Update()
        {
            if (_milestones == null || _text == null)
            {
                return;
            }

            _sb.Clear();
            _sb.AppendLine(HudText.MilestoneListHeader);

            foreach (var id in Order)
            {
                _sb.AppendLine(HudText.MilestoneLine(GameTextLabels.Milestone(id), _milestones.Has(id)));
            }

            _text.text = _sb.ToString();
        }
    }
}
