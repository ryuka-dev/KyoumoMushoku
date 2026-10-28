namespace KyoumoMushoku.Core.DayCycle
{
    /// <summary>BGM の曲。昼と夜の二曲（夕方は昼の曲）。</summary>
    public enum MusicTrack
    {
        None = 0,
        Day = 1,
        Night = 2,
    }

    /// <summary>
    /// BGM をいつ・どの曲で鳴らすかを決める純粋ロジック（表現層に依存しない）。
    ///
    /// 一曲が終わったら黙る（<see cref="SilenceSeconds"/> 秒）。黙っている間は街の音だけになり、それ自体が一人きりの間になる。
    /// 時間帯が変わっても、鳴っている曲はその曲のまま最後まで流す。次の一曲を始めるときに、そのときの時間帯で選ぶ。
    /// 曲が流れていない間に時間帯が変わったら、黙る残りを待たずにすぐ新しい曲を始める（夜になった、が音で分かる）。
    /// 時刻は呼び手が渡す実時間の秒（ゲームの時計の早回しでは進めない）。
    /// </summary>
    public sealed class MusicSchedule
    {
        readonly float _silenceSeconds;
        readonly float _startDelaySeconds;
        float _waitUntil;
        float _playingUntil = -1f;
        bool _nightAtLastPick;
        bool _started;

        /// <param name="silenceSeconds">一曲が終わってから次の曲までの黙る秒数（0 以上）。</param>
        /// <param name="startDelaySeconds">遊び始めてから最初の一曲までの秒数（0 以上）。</param>
        public MusicSchedule(float silenceSeconds, float startDelaySeconds)
        {
            _silenceSeconds = silenceSeconds < 0f ? 0f : silenceSeconds;
            _startDelaySeconds = startDelaySeconds < 0f ? 0f : startDelaySeconds;
        }

        public float SilenceSeconds => _silenceSeconds;

        /// <summary>いま曲が流れているはずか（<see cref="Tick"/> が最後に始めた曲の長さの内か）。</summary>
        public bool IsPlaying(float now) => _playingUntil >= 0f && now < _playingUntil;

        /// <summary>
        /// 時刻と時間帯を観測し、いま新しい曲を始めるならその曲を返す（始めないなら <see cref="MusicTrack.None"/>）。
        /// 曲を始めたら、呼び手は <see cref="Started"/> で長さを知らせる。
        /// </summary>
        public MusicTrack Tick(float now, bool night)
        {
            if (!_started)
            {
                _started = true;
                _waitUntil = now + _startDelaySeconds;
                _nightAtLastPick = night;
            }

            if (IsPlaying(now))
            {
                // 流れている間の切り替わりは数えない（曲が終わった後の黙る間を飛ばす理由にしない）。
                _nightAtLastPick = night;
                return MusicTrack.None;
            }

            var phaseFlipped = night != _nightAtLastPick;
            if (now < _waitUntil && !phaseFlipped)
            {
                return MusicTrack.None;
            }

            _nightAtLastPick = night;
            return night ? MusicTrack.Night : MusicTrack.Day;
        }

        /// <summary>曲を始めた。長さ（秒）が尽きたら黙り、その後に次の曲を選ぶ。</summary>
        public void Started(float now, float lengthSeconds)
        {
            var length = lengthSeconds < 0f ? 0f : lengthSeconds;
            _playingUntil = now + length;
            _waitUntil = _playingUntil + _silenceSeconds;
        }

        /// <summary>曲を途中で止めた（気を失った・最初から等）。黙る長さを数え直す。</summary>
        public void Stopped(float now)
        {
            _playingUntil = -1f;
            _waitUntil = now + _silenceSeconds;
        }
    }
}
