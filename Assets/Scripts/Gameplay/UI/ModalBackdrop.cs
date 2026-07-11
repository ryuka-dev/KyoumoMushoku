using UnityEngine;
using UnityEngine.UI;

namespace KyoumoMushoku.Gameplay.UI
{
    /// <summary>
    /// モーダル（店パネルなど）が開いている間だけ暗い下敷きを表示する。閉じている間は世界を覆わない。
    /// 表示状態の唯一の出所はモーダル側の <see cref="IInputModal.IsOpen"/> であり、ここはそれに追従するだけ。
    /// </summary>
    public sealed class ModalBackdrop : MonoBehaviour
    {
        // モーダルは MonoBehaviour として直列化する。インタフェース参照は Unity が保存できず、
        // ビルダーの配線が保存/再生で消えるため（InventoryView._modals と同病）。
        [SerializeField] MonoBehaviour _modal;
        [SerializeField] GameObject _backdrop;

        public void Configure(IInputModal modal, Image backdrop)
        {
            _modal = modal as MonoBehaviour;
            _backdrop = backdrop != null ? backdrop.gameObject : null;
            Apply();
        }

        void Update() => Apply();

        void Apply()
        {
            if (_backdrop == null || !(_modal is IInputModal modal))
            {
                return;
            }

            if (_backdrop.activeSelf != modal.IsOpen)
            {
                _backdrop.SetActive(modal.IsOpen);
            }
        }
    }
}
