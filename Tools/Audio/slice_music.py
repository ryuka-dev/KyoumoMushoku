"""切片の BGM（昼と夜の二曲）を合成する。素材は使わず、すべて計算で作る。

方針（2026-10-29・作者の注文）：
- 夜の街の一人きり。温かくしない・居心地よくしない。疎らなピアノと、遠くの冷たい持続音。
- 一曲 1 分半ほどで自然に消え、ゲームの側（ProtoMusic）がしばらく黙ってから次を流す。だから継ぎ目のループは作らない。
- 雨や街のざわめきは入れない（ゲームに無い天気を音で言うのは嘘になる。GameDesign 第三節）。
- 効果音（叩く音・廃棄が落ちる音・警官の合図）は聞いて判断する情報なので、その帯域と大きさを邪魔しない：
  打楽器を入れない・ピアノは柔らかい打鍵・全体を小さく作り、音量は SliceTuning の「音楽」で決める。

二曲の違い：
- 夜（ニ短調・66 BPM）：Dm9 → B♭maj7 → Gm(add9) → Asus4。長調に解決しない。低めのピアノ・長い残響・冷たい三角波の持続音。
- 昼（ヘ調の明るい側・74 BPM）：F(add9) → Am(add9) → Dm11 → Csus2。三度を抜いた開いた和音で、明るいが温かくはしない。
  高めのピアノ・短い残響・細い正弦波の持続音。人通りの中の一人。

ピアノは加算合成：弦の硬さで高い倍音が少し上にずれる・二本の弦のうなり・高い倍音ほど早く消える・柔らかいフェルトの打鍵。
出力：Assets/Audio/Music/Slice_Night.ogg・Slice_Day.ogg（ffmpeg で wav から変換）。
使い方: python Tools/Audio/slice_music.py
"""
import os
import subprocess
import sys
import tempfile
import wave

import numpy as np
from scipy.signal import butter, fftconvolve, sosfilt

SR = 44100
ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
OUT_DIR = os.path.join(ROOT, "Assets", "Audio", "Music")


def hz(m):
    return 440.0 * 2 ** ((m - 69) / 12.0)


def lowpass(x, cutoff, order=2):
    return sosfilt(butter(order, cutoff, "low", fs=SR, output="sos"), x)


def highpass(x, cutoff, order=2):
    return sosfilt(butter(order, cutoff, "high", fs=SR, output="sos"), x)


def bandcut(x, lo, hi, amount):
    """lo〜hi の帯を amount（0〜1）だけ削る。低い中域は温かさの元なので薄める。"""
    band = sosfilt(butter(2, [lo, hi], "band", fs=SR, output="sos"), x)
    return x - band * amount


def piano(rng, freq, vel, bright, dur=7.0):
    n = int(dur * SR)
    t = np.arange(n) / SR
    out = np.zeros(n)
    b = 0.00035
    brightness = bright * (0.6 + 0.8 * vel)
    for k in range(1, 14):
        fk = freq * k * np.sqrt(1 + b * k * k)
        if fk > SR * 0.45:
            break
        amp = (1.0 / k ** 1.35) * np.exp(-(k - 1) / (4.0 * brightness))
        t_fast = 0.35 / (1 + 0.25 * k)
        t_slow = (3.8 if freq < 300 else 2.6) / (1 + 0.18 * k)
        env = 0.55 * np.exp(-t / t_fast) + 0.45 * np.exp(-t / t_slow)
        detune = 1.0 + rng.uniform(0.0002, 0.0006)
        ph = rng.uniform(0, 2 * np.pi)
        out += amp * env * (np.sin(2 * np.pi * fk * t + ph) + 0.8 * np.sin(2 * np.pi * fk * detune * t + ph))
    hl = int(0.012 * SR)
    out[:hl] += lowpass(rng.standard_normal(hl), 1800) * np.linspace(1, 0, hl) * 0.25
    attack = np.minimum(1.0, t / 0.004)
    rel = np.ones(n)
    r = int(0.4 * SR)
    rel[-r:] = np.linspace(1, 0, r)
    return out * attack * rel * vel


def pad(rng, midis, dur, wave_shape, lp, hp, octave_up):
    n = int(dur * SR)
    t = np.arange(n) / SR
    out = np.zeros(n)
    for m in midis:
        f = hz(m + 12 * octave_up)
        for cents in (-6, 7):
            ph = 2 * np.pi * f * 2 ** (cents / 1200.0) * t + rng.uniform(0, 6.28)
            out += ((2 / np.pi) * np.arcsin(np.sin(ph)) if wave_shape == "tri" else np.sin(ph)) * 0.5
    swell = np.minimum(1.0, t / 2.6) * np.clip((dur - t) / 2.2, 0, 1)
    shimmer = 1.0 + 0.15 * np.sin(2 * np.pi * 0.11 * t + rng.uniform(0, 6.28))
    out = highpass(lowpass(out * swell * shimmer, lp), hp)
    return out / len(midis)


def impulse(rng, seconds, decay):
    n = int(seconds * SR)
    t = np.arange(n) / SR
    ir = np.zeros((n, 2))
    for c in range(2):
        noise = lowpass(rng.standard_normal(n) * np.exp(-t / decay), 5200)
        pre = int(0.02 * SR)
        noise[:pre] *= np.linspace(0, 1, pre)  # 初期反射を遅らせ、遠くで鳴っているように
        ir[:, c] = noise
    return ir / np.sqrt(np.sum(ir ** 2, axis=0))


def place(buf, start, sig, gain=1.0):
    i = int(max(0.0, start) * SR)
    if i >= len(buf):
        return
    k = min(len(sig), len(buf) - i)
    buf[i:i + k] += sig[:k] * gain


def cycles(base, offsets):
    """(拍, …) の並びを、拍をずらして繰り返す。"""
    return [(x[0] + off,) + tuple(x[1:]) for off in offsets for x in base]


def render(spec):
    rng = np.random.default_rng(spec["seed"])
    beat = 60.0 / spec["bpm"]
    seconds = spec["length_beats"] * beat + spec["tail"]
    n = int(seconds * SR)
    keys, low, pads = np.zeros(n), np.zeros(n), np.zeros(n)
    for b, m, v in spec["melody"]:
        jitter = rng.uniform(-0.015, 0.02) if b > 0 else 0.0
        place(keys, b * beat + jitter + 0.03, piano(rng, hz(m), v * rng.uniform(0.92, 1.05), spec["bright"]))
    for b, m, v in spec["bass"]:
        place(low, b * beat + 0.03, piano(rng, hz(m), v, spec["bright"] * 0.8, dur=8.0))
    chords = spec["chords"]
    for k, (b, chord) in enumerate(chords):
        # 持続音は和音ごとに鳴らさない。鳴らない間はピアノと残響だけになり、そこに一人きりの間ができる
        if k % 4 not in spec["pad_on"] and k != len(chords) - 1:
            continue
        nxt = chords[k + 1][0] if k + 1 < len(chords) else spec["length_beats"]
        place(pads, b * beat, pad(rng, chord, (nxt - b) * beat + 2.5, spec["pad_wave"], spec["pad_lp"], spec["pad_hp"], spec["pad_octave"]))
    dry = bandcut(keys * 0.9 + low * 0.7, 180, 520, spec["warmth_cut"])
    ir = impulse(rng, spec["reverb_seconds"], spec["reverb_decay"])
    wet = np.stack([fftconvolve(dry + pads * 0.6, ir[:, c])[:n] for c in range(2)], axis=1)
    mix = wet * spec["wet"]
    mix[:, 0] += dry * 0.55 + pads * spec["pad_level"]
    mix[:, 1] += dry * 0.50 + pads * spec["pad_level"] * 1.15
    fade = int(3.0 * SR)
    mix[-fade:] *= np.linspace(1, 0, fade)[:, None]
    return mix / (np.max(np.abs(mix)) / 0.7)


# ── 夜：ニ短調・66 BPM・32 拍の巡りを三度、最後は Asus4 のまま消える ───────────────
NIGHT_CHORDS = cycles([(0, [50, 57, 64, 65]), (8, [46, 53, 57, 62]), (16, [43, 50, 57, 58]), (24, [45, 52, 57, 62])], [0, 32, 64]) \
    + [(96, [45, 52, 57, 62])]
NIGHT = dict(
    seed=29, bpm=66, length_beats=104, tail=4.0, bright=0.9,
    chords=NIGHT_CHORDS,
    melody=[
        # 一巡目：疎ら
        (0, 74, .55), (3, 69, .32), (6, 77, .48), (7, 76, .36), (12, 74, .42), (16, 70, .38), (18, 69, .30),
        (24, 76, .42), (26, 74, .32), (28, 69, .26),
        # 二巡目：少しだけ動く
        (32, 74, .46), (35, 69, .28), (38, 77, .42), (39, 79, .36), (40, 77, .30), (44, 72, .36), (46, 69, .28),
        (48, 70, .36), (49, 72, .30), (50, 74, .34), (54, 70, .24), (56, 76, .40), (57, 74, .30), (59, 72, .28), (60, 69, .26),
        # 三巡目：また疎らに戻る
        (64, 74, .44), (68, 81, .30), (70, 77, .34), (76, 74, .30), (80, 70, .28), (84, 69, .22), (88, 76, .30), (92, 74, .22),
        # 終わり：解決しない一音
        (96, 76, .30),
    ],
    bass=[(0, 38, .30), (8, 46, .26), (16, 43, .26), (24, 45, .24), (32, 38, .28), (48, 43, .24), (56, 45, .22),
          (64, 38, .26), (72, 46, .22), (80, 43, .22), (88, 45, .20), (96, 33, .22)],
    pad_wave="tri", pad_lp=2400, pad_hp=380, pad_octave=1, pad_level=0.10, pad_on=(0, 2),
    warmth_cut=0.35, reverb_seconds=3.8, reverb_decay=1.35, wet=0.6,
)

# ── 昼：ヘ調の明るい側・74 BPM・三度を抜いた開いた和音 ─────────────────────────
DAY_CHORDS = cycles([(0, [53, 60, 64, 67]), (8, [57, 64, 71, 72]), (16, [50, 57, 60, 67]), (24, [48, 55, 62, 67])], [0, 32, 64]) \
    + [(96, [48, 55, 62, 67])]
DAY = dict(
    seed=74, bpm=74, length_beats=104, tail=4.0, bright=1.1,
    chords=DAY_CHORDS,
    melody=[
        (0, 76, .45), (2, 79, .32), (4, 81, .38), (6, 79, .28), (10, 76, .30), (16, 74, .36), (18, 72, .28), (19, 74, .26),
        (24, 79, .34), (26, 74, .26), (28, 72, .24),
        (32, 76, .42), (33, 79, .30), (34, 81, .34), (35, 84, .30), (36, 83, .28), (40, 81, .30), (44, 76, .26),
        (48, 77, .32), (50, 76, .28), (52, 72, .24), (56, 74, .32), (58, 79, .28), (62, 74, .22),
        (64, 76, .38), (68, 81, .30), (72, 79, .28), (80, 74, .28), (88, 72, .30), (92, 67, .22),
        (96, 74, .26),
    ],
    bass=[(0, 41, .24), (8, 45, .20), (16, 38, .22), (24, 36, .20), (32, 41, .22), (48, 38, .20), (56, 36, .18),
          (64, 41, .22), (80, 38, .18), (96, 36, .20)],
    pad_wave="sine", pad_lp=3600, pad_hp=520, pad_octave=1, pad_level=0.07, pad_on=(0, 3),
    warmth_cut=0.45, reverb_seconds=2.4, reverb_decay=0.8, wet=0.38,
)


def write_wav(path, stereo):
    data = (np.clip(stereo, -1, 1) * 32767).astype(np.int16)
    with wave.open(path, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    keep_wav = "--wav" in sys.argv
    for name, spec in (("Slice_Night", NIGHT), ("Slice_Day", DAY)):
        mix = render(spec)
        tmp = os.path.join(tempfile.gettempdir(), name + ".wav")
        write_wav(tmp, mix)
        ogg = os.path.join(OUT_DIR, name + ".ogg")
        subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", tmp, "-c:a", "libvorbis", "-q:a", "6", ogg], check=True)
        if keep_wav:
            print("wav:", tmp)
        print(ogg, f"{len(mix) / SR:.1f}s")


if __name__ == "__main__":
    main()
