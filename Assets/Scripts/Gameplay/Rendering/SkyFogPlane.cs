using UnityEngine;

namespace KyoumoMushoku.Gameplay.Rendering
{
    /// <summary>
    /// 空色の霧板。奥の層の手前に1枚挟み、その向こうにある絵を空へ溶かす（空気遠近）。
    /// 板は重ねてよく、Far のように2枚を透かして見る層はそのぶん濃く霞む
    /// （実効の濃さは 1-(1-a1)(1-a2) で、板ごとの <see cref="_density"/> は寄与ぶんを表す）。
    ///
    /// 色の権威は <see cref="DayNightLight2D"/> の空色であり、この板はそれを読むだけで
    /// 時刻を所有しない（<see cref="SanityColorGrade"/> が SAN を読むのと同じ姿勢）。
    /// 見た目の値を変えるだけで、ゲームプレイの状態には一切触れない（第七節）。
    ///
    /// 板は Light2D で照らさない（Unlit の材質を割り当てる）。空色にはすでに時刻ぶんの
    /// 明るさが入っており、さらに灯りを掛けると二重に暗くなるためである。
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SkyFogPlane : MonoBehaviour
    {
        [SerializeField] DayNightLight2D _light;
        [SerializeField] SpriteRenderer _plane;

        [Tooltip("この板が足す霞の濃さ。1 でこの板より奥がすべて空色に沈む。")]
        [Range(0f, 1f)]
        [SerializeField] float _density = 0.55f;

        void Reset() => _plane = GetComponent<SpriteRenderer>();

        void OnValidate()
        {
            if (_plane == null)
            {
                _plane = GetComponent<SpriteRenderer>();
            }

            Apply();
        }

        /// <summary>空色は <see cref="DayNightLight2D"/> が Update で決める。
        /// 同じフレームのうちに追従するため、読むのは LateUpdate にする。</summary>
        void LateUpdate() => Apply();

        /// <summary>
        /// 濃さの実体は <see cref="SpriteRenderer"/> の color.a である。つまりここが押し込まなければ
        /// Inspector の <see cref="_density"/> は絵に出ない。編集中も効かせるためにこの型は
        /// <c>[ExecuteAlways]</c> である（再生中しか回らないと、編集中に動かしても無反応になる）。
        /// </summary>
        void Apply()
        {
            if (_light == null || _plane == null)
            {
                return;
            }

            Color sky = _light.SkyColor;
            sky.a = _density;

            // 同じ値でも代入するとシーンが汚れ続け、未保存の変更が消えなくなる。変わった時だけ書く。
            if (_plane.color != sky)
            {
                _plane.color = sky;
            }
        }
    }
}
