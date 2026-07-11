using System.Text;
using KyoumoMushoku.Core.Progress;
using KyoumoMushoku.Gameplay.DayCycle;
using KyoumoMushoku.Gameplay.Economy;
using KyoumoMushoku.Gameplay.Interaction;
using KyoumoMushoku.Gameplay.Knacks;
using KyoumoMushoku.Gameplay.Player;
using KyoumoMushoku.Gameplay.Progress;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KyoumoMushoku.Gameplay.UI
{
    /// <summary>
    /// 垂直スライスの結算画面。3日間生存（第八節）を達成した瞬間＝4日目の朝に一度だけ開き、
    /// 目標の達成状況とその時点の暮らし向きを見せる。閉じたあとは自由に続けられる（オープン型・第八節）。
    ///
    /// 開いている間は移動と通常のインタラクトを止めて入力を占有する（<see cref="IInputModal"/>）。
    /// 達成は永続するため、セーブを読み直しても二度は開かない。
    /// </summary>
    public sealed class SliceResultPanel : MonoBehaviour, IInputModal
    {
        [SerializeField] PlayerMilestones _milestones;
        [SerializeField] PlayerKnacks _knacks;
        [SerializeField] PlayerWallet _wallet;
        [SerializeField] PlayerMotor _motor;
        [SerializeField] PlayerInteractor _interactor;
        [SerializeField] TMP_Text _text;

        readonly StringBuilder _sb = new StringBuilder();

        bool _isOpen;
        bool _skipInputThisFrame;

        public bool IsOpen => _isOpen;

        public void Configure(PlayerMilestones milestones, PlayerKnacks knacks, PlayerWallet wallet,
            PlayerMotor motor, PlayerInteractor interactor, TMP_Text text)
        {
            Unsubscribe();
            _milestones = milestones;
            _knacks = knacks;
            _wallet = wallet;
            _motor = motor;
            _interactor = interactor;
            _text = text;
            Subscribe();
            HideText();
        }

        void OnEnable() => Subscribe();

        void OnDisable()
        {
            Unsubscribe();
            if (_isOpen)
            {
                // どんな終わり方でも操作を返す。モーダルに閉じ込めたまま消えない。
                Close();
            }
        }

        void Subscribe()
        {
            if (_milestones != null)
            {
                _milestones.Achieved += OnAchieved;
            }
        }

        void Unsubscribe()
        {
            if (_milestones != null)
            {
                _milestones.Achieved -= OnAchieved;
            }
        }

        // 3日間生存は一度しか達成されないので、この画面も一度しか開かない。
        void OnAchieved(MilestoneId id)
        {
            if (id == MilestoneId.SurviveThreeDays)
            {
                Open();
            }
        }

        void Open()
        {
            _isOpen = true;
            _skipInputThisFrame = true; // 就寝の一打（E）を同フレームで拾い直さない。
            SetPlayerControl(false);

            if (_text != null)
            {
                _text.text = Compose();
            }
        }

        void Close()
        {
            _isOpen = false;
            SetPlayerControl(true);
            HideText();
        }

        void Update()
        {
            if (!_isOpen)
            {
                return;
            }

            if (_skipInputThisFrame)
            {
                _skipInputThisFrame = false;
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        string Compose()
        {
            _sb.Clear();
            _sb.AppendLine(ResultText.Header);
            _sb.AppendLine();
            _sb.AppendLine(ResultText.Survived);
            _sb.AppendLine();
            _sb.AppendLine(ResultText.GoalsHeading);
            _sb.AppendLine(HudText.MilestoneLine(
                GameTextLabels.Milestone(MilestoneId.SurviveThreeDays), true));
            _sb.AppendLine(HudText.MilestoneLine(
                GameTextLabels.Milestone(MilestoneId.FirstInnStay), _milestones.Has(MilestoneId.FirstInnStay)));
            _sb.AppendLine(HudText.MilestoneLine(
                GameTextLabels.Milestone(MilestoneId.BuyBackpack), _milestones.Has(MilestoneId.BuyBackpack)));
            _sb.AppendLine();
            _sb.AppendLine(ResultText.WalletLine(_wallet != null && _wallet.Wallet != null ? _wallet.Wallet.Yen : 0));
            _sb.AppendLine(ResultText.KnacksLine(_knacks != null && _knacks.Book != null ? _knacks.Book.AcquiredCount : 0));
            _sb.AppendLine();
            _sb.AppendLine(ResultText.Closing);
            _sb.AppendLine();
            _sb.AppendLine(ResultText.ContinueHint);
            return _sb.ToString();
        }

        void SetPlayerControl(bool enabled)
        {
            if (_motor != null)
            {
                _motor.enabled = enabled;
            }

            if (_interactor != null)
            {
                _interactor.enabled = enabled;
            }
        }

        void HideText()
        {
            if (_text != null)
            {
                _text.text = string.Empty;
            }
        }
    }
}
