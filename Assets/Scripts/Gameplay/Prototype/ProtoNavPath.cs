using UnityEngine;
using UnityEngine.AI;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 一人の警官の道。目標が変わったら、または一定の間ごとに求め直し、次の曲がり角を返す。
    /// 道が求まらなければ目標そのもの（まっすぐ歩く。以前の歩き方と同じ）。
    /// </summary>
    public sealed class ProtoNavPath
    {
        const float RepathSeconds = 0.5f;
        const float RetargetDistance = 0.75f;
        const float CornerReached = 0.35f;

        // NavMeshPath は MonoBehaviour の生成中には作れないので、この型は Awake 以降に作る（ProtoPolice.Awake）。
        readonly NavMeshPath _path = new NavMeshPath();
        Vector3[] _corners = new Vector3[0];
        int _index;
        Vector3 _target;
        float _repathAt = -1f;
        bool _hasPath;

        /// <summary>いま道に乗って歩いているか（診断）。</summary>
        public bool HasPath => _hasPath;

        /// <summary><paramref name="from"/> から <paramref name="target"/> へ向かうとき、次に歩いて行く点。</summary>
        public Vector3 Steer(ProtoStreetNav nav, Vector3 from, Vector3 target, float now)
        {
            var retarget = Flat(target - _target).sqrMagnitude > RetargetDistance * RetargetDistance;
            if (retarget || now >= _repathAt)
            {
                _target = target;
                _repathAt = now + RepathSeconds;
                _hasPath = nav != null && nav.TryPath(from, target, _path);
                _corners = _hasPath ? _path.corners : new Vector3[0];
                // corners[0] は始点。最初の曲がり角から歩く。
                _index = _corners.Length > 1 ? 1 : 0;
            }

            if (!_hasPath || _corners.Length == 0)
            {
                return target;
            }

            while (_index < _corners.Length - 1 && Flat(_corners[_index] - from).sqrMagnitude <= CornerReached * CornerReached)
            {
                _index++;
            }

            // 最後の曲がり角（歩ける所に寄せた終点）まで来たら、本当の目標へ（止まる距離の判定は目標で行う）。
            return _index >= _corners.Length - 1 && Flat(_corners[_corners.Length - 1] - from).sqrMagnitude <= CornerReached * CornerReached
                ? target
                : _corners[_index];
        }

        /// <summary>次に呼ばれたとき、必ず求め直す（連れて行かれた後に瞬間移動したときなど）。</summary>
        public void Invalidate() => _repathAt = -1f;

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
