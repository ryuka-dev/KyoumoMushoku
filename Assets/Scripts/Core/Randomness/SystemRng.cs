using System;

namespace KyoumoMushoku.Core.Randomness
{
    /// <summary>
    /// <see cref="IRng"/> の既定の実装。<see cref="System.Random"/> を包むだけの薄い層であり、
    /// 乱数源という平台の都合をゲームプレイ側に漏らさない。
    /// </summary>
    public sealed class SystemRng : IRng
    {
        readonly Random _random;

        public SystemRng()
            : this(Environment.TickCount)
        {
        }

        public SystemRng(int seed)
        {
            _random = new Random(seed);
        }

        public double NextDouble() => _random.NextDouble();
    }
}
