using UnityEngine;

namespace KyoumoMushoku.Gameplay.World
{
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform _target;
        [SerializeField] Vector2 _offset = new(0f, 2f);
        [SerializeField] float _smoothing = 8f;

        public void Configure(Transform target) => _target = target;

        void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            var desired = new Vector3(
                _target.position.x + _offset.x,
                _target.position.y + _offset.y,
                transform.position.z);

            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-_smoothing * Time.deltaTime));
        }
    }
}
