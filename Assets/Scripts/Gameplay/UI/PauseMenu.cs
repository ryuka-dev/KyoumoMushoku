using KyoumoMushoku.Gameplay.App;
using KyoumoMushoku.Gameplay.Interaction;
using KyoumoMushoku.Gameplay.Player;
using KyoumoMushoku.Gameplay.Session;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KyoumoMushoku.Gameplay.UI
{
    /// <summary>
    /// 正式なポーズ画面。Esc で開閉する（<see cref="IInputModal"/>）。開いている間は時間を止め
    /// （<see cref="Time.timeScale"/> = 0）、移動と通常のインタラクトを止めて入力を占有する。
    ///
    /// 本メニューは「ゲームに戻る／設定／ゲームを終了」。設定は2階層目で「フレーム上限／セーブデータの
    /// 場所を開く」。フレーム上限・フォルダを開くの実体はアプリ設定の所有者（<see cref="FrameRateSetting"/>・
    /// <see cref="SaveFolder"/>）に委ね、ここは選択と表示だけを持つ。
    ///
    /// 他のモーダル（店・段ボール箱・捨てる・結算）が開いている間は開かない。相手を閉じた同じ Esc で
    /// 巻き込まれて開かないよう、直前フレームのモーダル状態を見て抑える。
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour, IInputModal
    {
        enum Level { Closed, Main, Settings }

        [SerializeField] PlayerMotor _motor;
        [SerializeField] PlayerInteractor _interactor;
        [SerializeField] TMP_Text _text;
        [SerializeField] GameSession _session;

        // 他のモーダル。インタフェース配列は Unity が直列化できないため MonoBehaviour で持ち、IInputModal へ戻す
        // （InventoryView._modals・StashPanel._spots と同病：シーン参照は要 [SerializeField]）。
        [SerializeField] MonoBehaviour[] _otherModals = System.Array.Empty<MonoBehaviour>();

        readonly StringBuilder _sb = new StringBuilder();

        Level _level = Level.Closed;
        bool _skipInputThisFrame;
        bool _otherModalOpenLastFrame;
        float _savedTimeScale = 1f;
        string _feedback = string.Empty;

        public bool IsOpen => _level != Level.Closed;

        public void Configure(PlayerMotor motor, PlayerInteractor interactor, GameSession session, TMP_Text text,
            params IInputModal[] otherModals)
        {
            _motor = motor;
            _interactor = interactor;
            _session = session;
            _text = text;

            var list = new System.Collections.Generic.List<MonoBehaviour>();
            if (otherModals != null)
            {
                foreach (var modal in otherModals)
                {
                    if (modal is MonoBehaviour behaviour)
                    {
                        list.Add(behaviour);
                    }
                }
            }

            _otherModals = list.ToArray();
            HideText();
        }

        void OnDisable()
        {
            if (IsOpen)
            {
                // どんな終わり方でも時間と操作を返す。止めたまま消えない。
                Time.timeScale = _savedTimeScale;
                SetPlayerControl(true);
                _level = Level.Closed;
            }
        }

        void Update()
        {
            if (_text == null)
            {
                return;
            }

            var keyboard = Keyboard.current;
            var anyOther = AnyOtherModalOpen();

            if (_level == Level.Closed)
            {
                // 他のモーダルが開いている／直前まで開いていたら、その Esc は相手のもの。巻き込まれて開かない。
                if (keyboard != null && !anyOther && !_otherModalOpenLastFrame &&
                    keyboard.escapeKey.wasPressedThisFrame)
                {
                    Open();
                }

                _otherModalOpenLastFrame = anyOther;
                return;
            }

            if (_skipInputThisFrame)
            {
                _skipInputThisFrame = false;
                keyboard = null; // 開く一打（Esc）を同フレームで拾い直さない。描画だけ行う。
            }

            if (keyboard != null)
            {
                if (_level == Level.Main)
                {
                    HandleMain(keyboard);
                }
                else
                {
                    HandleSettings(keyboard);
                }
            }

            if (_level != Level.Closed)
            {
                _text.text = Compose();
            }

            _otherModalOpenLastFrame = false;
        }

        void HandleMain(Keyboard keyboard)
        {
            if (keyboard.escapeKey.wasPressedThisFrame || keyboard.digit1Key.wasPressedThisFrame)
            {
                Close(); // ゲームに戻る
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                _level = Level.Settings;
                _feedback = string.Empty;
            }
            else if (keyboard.digit3Key.wasPressedThisFrame)
            {
                Quit();
            }
        }

        void HandleSettings(Keyboard keyboard)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                _level = Level.Main; // 一段戻る
                _feedback = string.Empty;
            }
            else if (keyboard.digit1Key.wasPressedThisFrame)
            {
                FrameRateSetting.Cycle();
                _feedback = string.Empty;
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                _feedback = SaveFolder.Reveal() ? PauseText.SaveFolderOpened : PauseText.SaveFolderFailed;
            }
        }

        void Open()
        {
            _level = Level.Main;
            _feedback = string.Empty;
            _skipInputThisFrame = true;
            _savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            SetPlayerControl(false);
            StopPlayer();
            _text.text = Compose();
        }

        void Close()
        {
            _level = Level.Closed;
            Time.timeScale = _savedTimeScale;
            SetPlayerControl(true);
            HideText();
        }

        void Quit()
        {
            // 終了時にオートセーブ（プレイヤーの選択）。書き出しは就寝と同じ経路を通り、
            // 位置は最後に寝た就寝場所へ紐づく（セーブ機構の権威は GameSession）。
            _session?.SaveNow();

            Time.timeScale = _savedTimeScale; // エディタへ戻したときに止まったままにしない。

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        bool AnyOtherModalOpen()
        {
            foreach (var behaviour in _otherModals)
            {
                if (behaviour is IInputModal modal && modal.IsOpen)
                {
                    return true;
                }
            }

            return false;
        }

        string Compose()
        {
            _sb.Clear();

            if (_level == Level.Settings)
            {
                _sb.AppendLine(PauseText.SettingsHeader);
                _sb.AppendLine();
                _sb.AppendLine("1. " + PauseText.FrameLimitLine(FrameRateSetting.Current));
                _sb.AppendLine("2. " + PauseText.OpenSaveFolder);
                if (!string.IsNullOrEmpty(_feedback))
                {
                    _sb.AppendLine();
                    _sb.AppendLine(_feedback);
                }

                _sb.AppendLine();
                _sb.AppendLine(PauseText.SettingsHint);
                return _sb.ToString();
            }

            _sb.AppendLine(PauseText.Header);
            _sb.AppendLine();
            _sb.AppendLine("1. " + PauseText.Resume);
            _sb.AppendLine("2. " + PauseText.Settings);
            _sb.AppendLine("3. " + PauseText.Quit);
            _sb.AppendLine();
            _sb.AppendLine(PauseText.MainHint);
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

        void StopPlayer()
        {
            if (_interactor != null && _interactor.Context?.Transform != null &&
                _interactor.Context.Transform.TryGetComponent(out Rigidbody2D body))
            {
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
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
