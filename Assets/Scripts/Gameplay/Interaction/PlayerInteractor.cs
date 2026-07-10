using System;
using System.Collections.Generic;
using KyoumoMushoku.Gameplay.Economy;
using KyoumoMushoku.Gameplay.Items;
using KyoumoMushoku.Gameplay.Player;
using KyoumoMushoku.Gameplay.Survival;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Interaction
{
    /// <summary>
    /// プレイヤーの周りにある <see cref="IInteractable"/> を探し、最も近いものを現在の対象とする。
    /// インタラクトの入力（第五節の「調べる」）を、実際の相手の <see cref="IInteractable.Interact"/> に橋渡しする。
    ///
    /// 探索は物理レイヤ（Interactable）で絞り、無関係なコライダを走査しない。
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] float _radius = 1.6f;
        [SerializeField] LayerMask _interactableLayers = ~0;

        readonly Collider2D[] _hits = new Collider2D[16];

        PlayerContext _context;
        IPlayerInput _input;

        /// <summary>現在の対象。無ければ null。UI のプロンプトはここを観測する。</summary>
        public IInteractable Current { get; private set; }
        public PlayerContext Context => _context;

        /// <summary>現在の対象が変わったときに発火する。</summary>
        public event Action<IInteractable> CurrentChanged;

        void Awake()
        {
            _input = GetComponent<IPlayerInput>();
            _context = new PlayerContext(
                transform,
                GetComponent<PlayerVitals>(),
                GetComponent<PlayerInventory>(),
                GetComponent<PlayerWallet>());
        }

        void Update()
        {
            UpdateCurrent();

            if (Current != null && _input != null && _input.InteractPressed && Current.CanInteract(_context))
            {
                Current.Interact(_context);

                // 相手が消えたり満杯が解消したりするため、同フレームで対象を取り直す。
                UpdateCurrent();
            }
        }

        void UpdateCurrent()
        {
            var filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = _interactableLayers,
                useTriggers = true,
            };

            var count = Physics2D.OverlapCircle(transform.position, _radius, filter, _hits);

            IInteractable nearest = null;
            var nearestSqr = float.MaxValue;
            var origin = (Vector2)transform.position;

            for (var i = 0; i < count; i++)
            {
                var hit = _hits[i];
                if (hit == null || !hit.TryGetComponent(out IInteractable interactable))
                {
                    continue;
                }

                var sqr = ((Vector2)hit.transform.position - origin).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = interactable;
                }
            }

            if (!ReferenceEquals(nearest, Current))
            {
                Current = nearest;
                CurrentChanged?.Invoke(nearest);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.95f, 0.85f, 0.3f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
