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
        IInputModal _modal;
        GameObject _backdrop;

        public void Configure(IInputModal modal, Image backdrop)
        {
            _modal = modal;
            _backdrop = backdrop != null ? backdrop.gameObject : null;
            Apply();
        }

        void Update() => Apply();

        void Apply()
        {
            if (_backdrop == null || _modal == null)
            {
                return;
            }

            if (_backdrop.activeSelf != _modal.IsOpen)
            {
                _backdrop.SetActive(_modal.IsOpen);
            }
        }
    }
}
