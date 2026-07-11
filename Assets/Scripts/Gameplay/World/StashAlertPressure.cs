using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Zones;
using KyoumoMushoku.Gameplay.Items;
using KyoumoMushoku.Gameplay.Police;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.World
{
    /// <summary>
    /// 生活ゾーンの警戒度への常時入力を駆動する（第十二節）。段ボールを背負って歩くこと、そして保管庫に
    /// 貯め込みすぎていることは、いずれも住処の一帯に顔を覚えさせる。判定は純関数（<see cref="StashPressure"/>）に閉じ、
    /// ここは毎フレームの積み上げと、<see cref="ZoneAlertDirector"/> への申告だけを行う。
    ///
    /// 上昇は一定量たまってからまとめて申告する。毎フレーム微小に上げて変更通知を撒き散らさないためである。
    /// </summary>
    public sealed class StashAlertPressure : MonoBehaviour
    {
        [Tooltip("たまった上昇分がこの値を超えたら、まとめて警戒度へ申告する。叩き台。")]
        [SerializeField, Min(0.01f)] float _applyThreshold = 0.25f;

        ZoneAlertDirector _alerts;
        PlayerCarry _carry;
        StashSpot[] _spots;
        float _accumulated;

        void Start()
        {
            _alerts = FindFirstObjectByType<ZoneAlertDirector>();
            _carry = FindFirstObjectByType<PlayerCarry>();

            // 設置場所は静的なシーンオブジェクト（中身だけが変わる）。一度だけ集める。
            _spots = FindObjectsByType<StashSpot>(FindObjectsSortMode.None);
        }

        void Update()
        {
            if (_alerts == null)
            {
                return;
            }

            var perSecond = StashPressure.CarryRaisePerSecond(_carry != null && _carry.IsCarrying);

            if (_spots != null)
            {
                foreach (var spot in _spots)
                {
                    // 生活ゾーンへの入力なので、生活ゾーンに置かれた保管庫の貯め込みだけを数える。
                    // 商業ゾーンのコインロッカーは住処から遠く、そこへの貯め込みは住処の一帯を目立たせない（第十二節）。
                    if (spot != null && spot.HasStash && spot.Zone == AlertZoneId.Residential)
                    {
                        perSecond += StashPressure.HoardRaisePerSecond(spot.StashUsedSlots);
                    }
                }
            }

            if (perSecond <= 0f)
            {
                return;
            }

            _accumulated += perSecond * Time.deltaTime;
            if (_accumulated >= _applyThreshold)
            {
                _alerts.Raise(AlertZoneId.Residential, _accumulated);
                _accumulated = 0f;
            }
        }
    }
}
