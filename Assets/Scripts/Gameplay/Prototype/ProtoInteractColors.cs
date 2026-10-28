using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 触れる物の色分け。縁取り（<see cref="ProtoOutline"/>）と照準の輪（<see cref="ProtoHud"/>）が同じ色で話す。
    /// 探る物（ゴミ箱・ゴミ袋・自販機の下）は暖色、それ以外の使う物（自販機で買う・コンビニに入る・水を飲む・ベンチで寝る）は寒色。
    /// </summary>
    public static class ProtoInteractColors
    {
        public static readonly Color Search = new Color(1f, 0.78f, 0.35f, 1f);
        public static readonly Color Use = new Color(0.45f, 0.82f, 1f, 1f);

        /// <summary>探る物か（中から何かが出てくるかもしれない物）。</summary>
        public static bool IsSearch(Component interactable) =>
            interactable is ProtoTrashCan || interactable is ProtoCoinSearch;

        public static Color For(Component interactable) => IsSearch(interactable) ? Search : Use;
    }
}
