using KyoumoMushoku.Core.Survival;
using KyoumoMushoku.Gameplay.Interaction;
using KyoumoMushoku.Gameplay.UI;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.World
{
    /// <summary>
    /// 死亡時の強制的な搬送先（第三節）。就寝場所とは別枠であり、公園側の最も安全で
    /// 最も貧しい端に置く。奥へ深入りしていたほど、歩いて戻る距離が長い。
    ///
    /// 併せて、金を払って怪我を治す場所でもある。無料の就寝では HP はゆっくりしか戻らないため、
    /// 早く全快したければここで医療費を払う。「金が買っているのは確実性である」（第三節）。
    /// 支払いも治療も病院という世界の中の行為として閉じる（第十四節）。
    ///
    /// 医療費は死亡した地区の物価水準に連動した固定額とする。初版は1街区・病院1箇所のため、
    /// ここに初期街区の額を持つ。街区が増えたら、死亡ゾーンに応じて選ぶ形へ広げる。
    /// </summary>
    public sealed class Hospital : MonoBehaviour, IInteractable, IRespawnPoint
    {
        public const string Id = "hospital";

        [SerializeField] int _medicalFeeYen = 500;

        public string RespawnId => Id;
        public Vector3 SpawnPosition => transform.position;

        public int MedicalFeeYen => _medicalFeeYen;

        public void Configure(int medicalFeeYen) => _medicalFeeYen = medicalFeeYen;

        /// <summary>治療に応じられるのは、生きていて・傷があり・医療費を払えるときだけ。</summary>
        public bool CanInteract(PlayerContext player)
        {
            if (player.Vitals == null || !player.Vitals.Vitals.IsAlive)
            {
                return false;
            }

            var vitals = player.Vitals.Vitals;
            if (vitals.Hp >= vitals.Tuning.MaxHp)
            {
                return false;
            }

            return player.Wallet != null && player.Wallet.Wallet.CanAfford(_medicalFeeYen);
        }

        public string Describe(PlayerContext player)
        {
            if (player.Vitals != null && player.Vitals.Vitals.Hp >= player.Vitals.Vitals.Tuning.MaxHp)
            {
                return WorldText.TreatmentNotNeeded;
            }

            if (player.Wallet == null || !player.Wallet.Wallet.CanAfford(_medicalFeeYen))
            {
                return WorldText.TreatmentCannotAfford(_medicalFeeYen);
            }

            return WorldText.TreatmentOffer(_medicalFeeYen);
        }

        public void Interact(PlayerContext player)
        {
            if (player.Vitals == null || player.Wallet == null)
            {
                return;
            }

            var vitals = player.Vitals.Vitals;
            if (vitals.Hp >= vitals.Tuning.MaxHp)
            {
                return;
            }

            if (!player.Wallet.Wallet.TrySpend(_medicalFeeYen))
            {
                return;
            }

            // HP のみ全快。渇き・空腹・SAN は治療の対象ではない（金が買うのは怪我の確実な回復だけ）。
            // Vitals.Apply が最大値でクランプするため、MaxHp を足せば満タンになる。
            vitals.Apply(new VitalsDelta { Hp = vitals.Tuning.MaxHp });
        }
    }
}
