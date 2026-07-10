using UnityEngine;

namespace KyoumoMushoku.Gameplay.World
{
    /// <summary>
    /// 地上と地下通路をつなぐ階段。
    /// 移動距離そのものは短縮しない（地下通路は歩いて渡る必要がある）。
    /// Phase 0 では階段の登り降りを瞬間移動で代用している。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class Stairwell : MonoBehaviour
    {
        const float ReentryBlockSeconds = 0.5f;

        [SerializeField] Stairwell _linked;
        [SerializeField] Transform _arrivalPoint;

        float _blockedUntil;

        public void Configure(Stairwell linked, Transform arrivalPoint)
        {
            _linked = linked;
            _arrivalPoint = arrivalPoint;
        }

        void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (Time.time < _blockedUntil || _linked == null || _linked._arrivalPoint == null)
            {
                return;
            }

            if (!other.TryGetComponent(out Rigidbody2D body))
            {
                return;
            }

            // 出口側を一時的に塞がないと、到着した瞬間に押し戻される。
            _linked._blockedUntil = Time.time + ReentryBlockSeconds;

            body.position = _linked._arrivalPoint.position;
            body.linearVelocity = Vector2.zero;
        }
    }
}
