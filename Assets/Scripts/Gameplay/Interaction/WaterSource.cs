using KyoumoMushoku.Core.Survival;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Interaction
{
    /// <summary>
    /// 無料の水源。公園の水道と公衆トイレを兼ねる（第十三節）。
    /// その場で飲むだけで、容器には汲まない（容器はゴミ箱と店で手に入る Phase 2/4 の話）。
    ///
    /// 公衆トイレは尊厳と引き換えの生存であり、飲むと SAN を削る。
    /// これは `飲みかけのペットボトル` と同じ文法であり、本作では繰り返し用いる。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class WaterSource : MonoBehaviour, IInteractable
    {
        [SerializeField] string _label = "水を飲む";
        [SerializeField] float _thirstRestored = 30f;

        [Tooltip("公衆トイレなど、飲むと SAN を削る水源では負値にする。公園の水道は 0。")]
        [SerializeField] float _sanityCost;

        public void Configure(string label, float thirstRestored, float sanityCost)
        {
            _label = label;
            _thirstRestored = thirstRestored;
            _sanityCost = sanityCost;
        }

        void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        public bool CanInteract(PlayerContext player) => player.Vitals != null && player.Vitals.Vitals.IsAlive;

        public string Describe(PlayerContext player) => _label;

        public void Interact(PlayerContext player)
        {
            player.Vitals.Vitals.Apply(new VitalsDelta
            {
                Thirst = _thirstRestored,
                Sanity = _sanityCost,
            });
        }
    }
}
