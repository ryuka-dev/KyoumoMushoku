using UnityEngine;

namespace KyoumoMushoku.Gameplay.World
{
    /// <summary>
    /// 死亡時の強制的な搬送先（第三節）。就寝場所とは別枠であり、公園側の最も安全で
    /// 最も貧しい端に置く。奥へ深入りしていたほど、歩いて戻る距離が長い。
    ///
    /// 医療費は死亡した地区の物価水準に連動した固定額とする。初版は1街区・病院1箇所のため、
    /// ここに初期街区の額を持つ。街区が増えたら、死亡ゾーンに応じて選ぶ形へ広げる。
    /// </summary>
    public sealed class Hospital : MonoBehaviour, IRespawnPoint
    {
        public const string Id = "hospital";

        [SerializeField] int _medicalFeeYen = 500;

        public string RespawnId => Id;
        public Vector3 SpawnPosition => transform.position;

        public int MedicalFeeYen => _medicalFeeYen;

        public void Configure(int medicalFeeYen) => _medicalFeeYen = medicalFeeYen;
    }
}
