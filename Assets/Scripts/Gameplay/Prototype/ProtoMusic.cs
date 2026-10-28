using KyoumoMushoku.Core.DayCycle;
using KyoumoMushoku.Gameplay.App;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 切片の BGM。昼と夜の二曲を、一曲流したら黙り、次の一曲はそのときの時間帯で選ぶ（順番は Core の <see cref="MusicSchedule"/>）。
    /// 曲と大きさは <see cref="SliceTuningAsset"/> の「音楽」、それにプレイヤーの音楽の音量（<see cref="PlayerOptions"/>）を掛ける。プレイヤーに付く（<see cref="ProtoHands"/> が足す）。
    ///
    /// 時刻は実時間（店の中・朝の区切りで Time.timeScale = 0 になっても曲は止めない。大きさだけ落とす）。
    /// 立体音にはしない（どこから鳴っているかは意味を持たない）。
    /// </summary>
    public sealed class ProtoMusic : MonoBehaviour
    {
        const float FadeSeconds = 1.5f;

        SliceTuningAsset _slice;
        ProtoDayNight _dayNight;
        AudioSource _source;
        MusicSchedule _schedule;
        float _duck = 1f;

        public void Configure(SliceTuningAsset slice, ProtoDayNight dayNight)
        {
            _slice = slice;
            _dayNight = dayNight;
        }

        void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0f;
            _source.ignoreListenerPause = true;
        }

        void Update()
        {
            if (_slice == null || _dayNight == null || _dayNight.Clock == null)
            {
                return;
            }

            _schedule ??= new MusicSchedule(_slice.MusicSilenceSeconds, _slice.MusicStartDelaySeconds);
            var now = Time.unscaledTime;
            var track = _schedule.Tick(now, _dayNight.IsNight);
            if (track != MusicTrack.None)
            {
                var clip = track == MusicTrack.Night ? _slice.NightMusic : _slice.DayMusic;
                if (clip != null)
                {
                    _source.clip = clip;
                    _source.Play();
                    _schedule.Started(now, clip.length);
                }
                else
                {
                    // 曲が空の時間帯は黙ったまま。黙る長さを数え直して、毎こま選び直さない。
                    _schedule.Started(now, 0f);
                }
            }

            // 遊びが止まっている間（店・区切り・捕まった後の暗幕）は小さく。
            var target = Time.timeScale < 0.5f ? _slice.MusicDuckWhilePaused : 1f;
            _duck = Mathf.MoveTowards(_duck, target, Time.unscaledDeltaTime / FadeSeconds);
            _source.volume = _slice.MusicVolume * PlayerOptions.MusicVolume * _duck;
        }
    }
}
