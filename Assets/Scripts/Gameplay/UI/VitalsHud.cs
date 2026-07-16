using KyoumoMushoku.Gameplay.DayCycle;
using KyoumoMushoku.Gameplay.Economy;
using KyoumoMushoku.Gameplay.Survival;
using KyoumoMushoku.Gameplay.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KyoumoMushoku.Gameplay.UI
{
    /// <summary>
    /// 4つの状態と、日付・時間帯・所持金・現在ゾーンを表示する（読み取り専用）。
    /// メインで見せるのは3本の生存ゲージだが、HP も独立して表示する（第三節）。
    ///
    /// UI は Screen Space - Overlay に置き、SAN の退色を受けない。文字は常に読める。
    /// </summary>
    public sealed class VitalsHud : MonoBehaviour
    {
        [SerializeField] Image _hpFill;
        [SerializeField] Image _thirstFill;
        [SerializeField] Image _hungerFill;
        [SerializeField] Image _sanityFill;
        [SerializeField] TMP_Text _statusText;

        // 各ゲージが何かを左に添える固定ラベル。値は変わらないので Start で一度だけ入れる。
        [SerializeField] TMP_Text _hpLabel;
        [SerializeField] TMP_Text _thirstLabel;
        [SerializeField] TMP_Text _hungerLabel;
        [SerializeField] TMP_Text _sanityLabel;

        [SerializeField] PlayerVitals _vitals;
        [SerializeField] PlayerWallet _wallet;
        [SerializeField] GameClockDriver _clock;
        [SerializeField] ZoneTracker _zones;

        public void Configure(PlayerVitals vitals, PlayerWallet wallet, GameClockDriver clock, ZoneTracker zones)
        {
            _vitals = vitals;
            _wallet = wallet;
            _clock = clock;
            _zones = zones;
        }

        public void BindFills(Image hp, Image thirst, Image hunger, Image sanity, TMP_Text status)
        {
            _hpFill = hp;
            _thirstFill = thirst;
            _hungerFill = hunger;
            _sanityFill = sanity;
            _statusText = status;
        }

        public void BindLabels(TMP_Text hp, TMP_Text thirst, TMP_Text hunger, TMP_Text sanity)
        {
            _hpLabel = hp;
            _thirstLabel = thirst;
            _hungerLabel = hunger;
            _sanityLabel = sanity;
            ApplyLabels();
        }

        void Start() => ApplyLabels();

        // ゲージのラベルは固定。語の権威は HudText にあり、ここは添えるだけ（ローカライズの口子）。
        void ApplyLabels()
        {
            SetLabel(_hpLabel, HudText.HpLabel);
            SetLabel(_thirstLabel, HudText.ThirstLabel);
            SetLabel(_hungerLabel, HudText.HungerLabel);
            SetLabel(_sanityLabel, HudText.SanityLabel);
        }

        static void SetLabel(TMP_Text label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }

        void Update()
        {
            if (_vitals == null || _vitals.Vitals == null)
            {
                return;
            }

            var vitals = _vitals.Vitals;
            SetFill(_hpFill, vitals.HpFraction);
            SetFill(_thirstFill, vitals.ThirstFraction);
            SetFill(_hungerFill, vitals.HungerFraction);
            SetFill(_sanityFill, vitals.SanityFraction);

            if (_statusText != null)
            {
                _statusText.text = ComposeStatus(vitals);
            }
        }

        string ComposeStatus(Core.Survival.Vitals vitals)
        {
            var day = _clock != null && _clock.Clock != null ? _clock.Clock.Day : 1;
            var phase = _clock != null && _clock.Clock != null
                ? GameTextLabels.Phase(_clock.Clock.Phase)
                : HudText.Unknown;
            var yen = _wallet != null && _wallet.Wallet != null ? _wallet.Wallet.Yen : 0;
            var zone = _zones != null ? GameTextLabels.Zone(_zones.CurrentZone) : HudText.Unknown;
            var mood = GameTextLabels.SanityTier(vitals.SanityTier);

            return HudText.Status(day, phase, yen, mood, zone);
        }

        static void SetFill(Image image, float fraction)
        {
            if (image != null)
            {
                image.fillAmount = fraction;
            }
        }
    }
}
