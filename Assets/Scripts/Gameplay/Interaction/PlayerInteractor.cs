using System;
using System.Collections.Generic;
using KyoumoMushoku.Gameplay.Economy;
using KyoumoMushoku.Gameplay.Items;
using KyoumoMushoku.Gameplay.Knacks;
using KyoumoMushoku.Gameplay.Player;
using KyoumoMushoku.Gameplay.Progress;
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

        IChanneledInteractable _channeling;
        float _channelSeconds;
        float _channelElapsed;

        /// <summary>現在の対象。無ければ null。UI のプロンプトはここを観測する。</summary>
        public IInteractable Current { get; private set; }
        public PlayerContext Context => _context;

        /// <summary>いま時間のかかる調べもの（漁りなど）の最中か。</summary>
        public bool IsChanneling => _channeling != null;

        /// <summary>
        /// いま最中の調べもの。何もしていなければ null。
        /// 「プレイヤーがいま何をしているか」の canonical な出所であり、警官はここを観測する。
        /// </summary>
        public IChanneledInteractable Channeling => _channeling;

        /// <summary>チャネルの進捗（0〜1）。チャネル中でなければ 0。</summary>
        public float ChannelProgress => _channeling != null && _channelSeconds > 0f
            ? Mathf.Clamp01(_channelElapsed / _channelSeconds)
            : 0f;

        /// <summary>現在の対象が変わったときに発火する。</summary>
        public event Action<IInteractable> CurrentChanged;

        /// <summary>調べものの結果を世界の言葉で伝える（「パンが出た」「空っぽだった」）。UI のトーストが観測する。</summary>
        public event Action<string> ActionReported;

        void Awake()
        {
            _input = GetComponent<IPlayerInput>();
            _context = new PlayerContext(
                transform,
                GetComponent<PlayerVitals>(),
                GetComponent<PlayerInventory>(),
                GetComponent<PlayerWallet>(),
                GetComponent<PlayerKnacks>(),
                GetComponent<PlayerCarry>(),
                GetComponent<PlayerMilestones>());
        }

        void Update()
        {
            if (_channeling != null)
            {
                TickChannel();
                return;
            }

            UpdateCurrent();

            if (Current == null || _input == null || !_input.InteractPressed || !Current.CanInteract(_context))
            {
                return;
            }

            if (Current is IChanneledInteractable channeled && channeled.ChannelSeconds(_context) > 0f)
            {
                BeginChannel(channeled);
                return;
            }

            Current.Interact(_context);

            // 相手が消えたり満杯が解消したりするため、同フレームで対象を取り直す。
            UpdateCurrent();
        }

        /// <summary>
        /// 外側の圧力（警官の警告など）が、進行中の調べものを一度だけ中断させる（第五節・第2段階）。
        /// 中断そのものは資源を消費しない。手が止まるだけであり、やり直せばまた漁れる。
        /// コツ `手を止めない`（第六節）を持つなら、警官はそもそもこれを呼ばない。
        /// </summary>
        public void InterruptChannel()
        {
            if (_channeling == null)
            {
                return;
            }

            _channeling.CancelChannel(_context);
            _channeling = null;
            UpdateCurrent();
        }

        void BeginChannel(IChanneledInteractable target)
        {
            _channeling = target;
            _channelSeconds = Mathf.Max(0.01f, target.ChannelSeconds(_context));
            _channelElapsed = 0f;
        }

        void TickChannel()
        {
            // 中断：歩き出す意思を示す、対象から離れる、続行不能になる。いずれも資源を消費しない。
            var walkedOff = _input != null && Mathf.Abs(_input.Horizontal) > 0.01f;
            var target = _channeling as IInteractable;

            if (walkedOff || target == null || !IsWithinReach(_channeling) || !target.CanInteract(_context))
            {
                _channeling.CancelChannel(_context);
                _channeling = null;
                UpdateCurrent();
                return;
            }

            _channelElapsed += Time.deltaTime;
            if (_channelElapsed < _channelSeconds)
            {
                return;
            }

            var message = _channeling.CompleteChannel(_context);
            _channeling = null;
            UpdateCurrent();

            if (!string.IsNullOrEmpty(message))
            {
                ActionReported?.Invoke(message);
            }
        }

        bool IsWithinReach(IChanneledInteractable target)
        {
            if (target is Component component && component != null)
            {
                var sqr = ((Vector2)component.transform.position - (Vector2)transform.position).sqrMagnitude;
                return sqr <= _radius * _radius;
            }

            return true;
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
