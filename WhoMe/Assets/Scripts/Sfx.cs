using System;
using System.Collections.Generic;
using UnityEngine;

/// All audio is synthesised at startup - no audio files needed.
public static class Sfx
{
    const int SR = 22050;
    static AudioSource fx, music;
    static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    static System.Random rng = new System.Random(7);
    public static bool MusicOn = true;
    /// Trailer recorder hook: (clip name, volume, pitch) for every sound played.
    public static Action<string, float, float> OnPlay;
    public static float[] Samples(string name) { var c = clips[name]; var d = new float[c.samples]; c.GetData(d, 0); return d; }
    public const int SampleRate = SR;
    public const float MusicVolume = 0.32f;

    public static void Init(GameObject host)
    {
        fx = host.AddComponent<AudioSource>();
        fx.playOnAwake = false;
        music = host.AddComponent<AudioSource>();
        music.loop = true; music.volume = 0.32f; music.playOnAwake = false;

        clips["blipLow"] = Make(0.035f, t => Sq(t, 330) * Env(t, 0.002f, 0.035f) * 0.10f);
        clips["blipHigh"] = Make(0.035f, t => Sq(t, 620) * Env(t, 0.002f, 0.035f) * 0.08f);
        clips["click"] = Make(0.05f, t => Math.Sin(2 * Math.PI * 1100 * t) * Math.Exp(-t * 90) * 0.35f);
        clips["hover"] = Make(0.03f, t => Math.Sin(2 * Math.PI * 1700 * t) * Math.Exp(-t * 160) * 0.12f);
        clips["paper"] = MakeNoise(0.22f, (t, n) => n * Env(t, 0.01f, 0.22f) * 0.35f, 0.55f);
        clips["ding"] = Make(0.7f, t => (Math.Sin(2 * Math.PI * 1046 * t) + 0.6 * Math.Sin(2 * Math.PI * 1568 * t)) * Math.Exp(-t * 6) * 0.25f);
        clips["buzz"] = Make(0.5f, t => (Saw(t, 98) + Saw(t, 104)) * Env(t, 0.005f, 0.5f) * 0.22f);
        clips["stamp"] = MakeNoise(0.3f, (t, n) => (n * Math.Exp(-t * 30) * 0.6 + Math.Sin(2 * Math.PI * (90 - t * 120) * t) * Math.Exp(-t * 12) * 0.8) * 0.5f, 0.15f);
        clips["sting"] = Make(1.6f, t =>
        {
            double f = t < 0.25 ? 293.7 : t < 0.5 ? 277.2 : 220;
            double lt = t < 0.25 ? t : t < 0.5 ? t - 0.25 : t - 0.5;
            double len = t < 0.5 ? 0.24 : 1.1;
            return (Sq(t, f) * 0.5 + Math.Sin(2 * Math.PI * f / 2 * t)) * Env(lt, 0.01, len) * 0.18;
        });
        clips["fanfare"] = Make(1.3f, t =>
        {
            double[] notes = { 523.3, 659.3, 784, 1046.5 };
            int i = Math.Min(3, (int)(t / 0.13));
            double lt = t - i * 0.13;
            double len = i == 3 ? 0.9 : 0.13;
            return (Sq(t, notes[i]) * 0.4 + Math.Sin(2 * Math.PI * notes[i] * t)) * Env(lt, 0.005, len) * 0.16;
        });
        clips["sadTrombone"] = Make(2.4f, t =>
        {
            double[] notes = { 233, 220, 207.7, 196 };
            int i = Math.Min(3, (int)(t / 0.45));
            double lt = t - i * 0.45;
            double len = i == 3 ? 1.0 : 0.42;
            double vib = i == 3 ? Math.Sin(2 * Math.PI * 5 * t) * 4 : 0;
            double f = notes[i] + vib;
            return (Saw(t, f) * 0.5 + Math.Sin(2 * Math.PI * f * t)) * Env(lt, 0.03, len) * 0.2;
        });
        clips["music"] = MakeMusic();
    }

    public static void Play(string name, float vol = 1f, float pitch = 1f)
    {
        if (fx == null || !clips.TryGetValue(name, out var c)) return;
        fx.pitch = pitch;
        fx.PlayOneShot(c, vol);
        OnPlay?.Invoke(name, vol, pitch);
    }

    public static void Blip(bool low)
    {
        if (fx == null) return;
        fx.pitch = 0.9f + (float)rng.NextDouble() * 0.2f;
        fx.PlayOneShot(clips[low ? "blipLow" : "blipHigh"], 1f);
        OnPlay?.Invoke(low ? "blipLow" : "blipHigh", 1f, fx.pitch);
    }

    public static void StartMusic()
    {
        if (music == null || music.isPlaying || !MusicOn) return;
        music.clip = clips["music"]; music.Play();
        OnPlay?.Invoke("music", MusicVolume, 1f);
    }

    public static void ToggleMusic()
    {
        MusicOn = !MusicOn;
        if (MusicOn) StartMusic(); else music.Stop();
    }

    // ---------- synthesis helpers ----------
    static double Sq(double t, double f) => Math.Sin(2 * Math.PI * f * t) >= 0 ? 1 : -1;
    static double Saw(double t, double f) { double p = t * f; return 2 * (p - Math.Floor(p + 0.5)); }
    static double Env(double t, double a, double len)
    {
        if (t < 0 || t > len) return 0;
        if (t < a) return t / a;
        return Math.Pow(1 - (t - a) / (len - a), 2);
    }

    static AudioClip Make(float dur, Func<double, double> f)
    {
        int n = (int)(dur * SR);
        var d = new float[n];
        for (int i = 0; i < n; i++) d[i] = (float)f(i / (double)SR);
        var c = AudioClip.Create("sfx", n, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }

    static AudioClip MakeNoise(float dur, Func<double, double, double> f, double smooth)
    {
        int n = (int)(dur * SR);
        var d = new float[n];
        double prev = 0;
        for (int i = 0; i < n; i++)
        {
            double w = rng.NextDouble() * 2 - 1;
            prev = prev * smooth + w * (1 - smooth);
            d[i] = (float)f(i / (double)SR, prev * 2);
        }
        var c = AudioClip.Create("noise", n, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }

    static double Midi(int m) => 440.0 * Math.Pow(2, (m - 69) / 12.0);

    /// A sneaky little noir walking-bass loop in D minor with brushes and soft chord stabs.
    static AudioClip MakeMusic()
    {
        const double bpm = 92, beat = 60 / bpm;
        int[][] bass =
        {
            new[] { 38, 41, 45, 48 }, new[] { 43, 46, 50, 46 }, new[] { 40, 43, 45, 49 }, new[] { 50, 48, 45, 41 },
            new[] { 46, 50, 53, 50 }, new[] { 45, 49, 52, 49 }, new[] { 38, 45, 41, 44 }, new[] { 45, 43, 41, 40 },
        };
        int[][] chords =
        {
            new[] { 62, 65, 69, 72 }, new[] { 67, 70, 74, 77 }, new[] { 64, 67, 70, 73 }, new[] { 62, 65, 69, 72 },
            new[] { 65, 69, 70, 74 }, new[] { 64, 67, 69, 73 }, new[] { 62, 65, 69, 74 }, new[] { 61, 64, 67, 69 },
        };
        int bars = bass.Length;
        double total = bars * 4 * beat;
        int n = (int)(total * SR);
        var d = new float[n];
        var nrng = new System.Random(3);
        double hp = 0, prevW = 0;

        for (int i = 0; i < n; i++)
        {
            double t = i / (double)SR;
            int b = (int)(t / beat);
            int bar = (b / 4) % bars, bi = b % 4;
            double bt = t - b * beat;
            double s = 0;

            // walking bass (plucked)
            double bf = Midi(bass[bar][bi]);
            s += (Math.Sin(2 * Math.PI * bf * t) + 0.35 * Math.Sin(4 * Math.PI * bf * t)) * Math.Exp(-bt * 5) * 0.45 * Math.Min(1, bt * 200);

            // chord stabs on the "and" of 2 and on 4, swung
            double stab1 = 1 * beat + beat * 0.66;
            double barT = t - (b / 4) * 4 * beat;
            foreach (var st in new[] { stab1, 3 * beat })
            {
                double lt = barT - st;
                if (lt >= 0 && lt < 0.7)
                {
                    double e = Math.Exp(-lt * 7) * Math.Min(1, lt * 300) * 0.035;
                    foreach (var m in chords[bar]) s += Math.Sin(2 * Math.PI * Midi(m) * t) * e;
                }
            }

            // brushes: swung 8ths, accent on 2 & 4
            double w = nrng.NextDouble() * 2 - 1;
            hp = 0.6 * (hp + w - prevW); prevW = w;
            double sw = bt < beat * 0.66 ? bt : bt - beat * 0.66;
            double acc = (bi % 2 == 1 && bt < beat * 0.66) ? 0.12 : 0.05;
            s += hp * Math.Exp(-sw * 28) * acc;

            d[i] = (float)(s * 0.8);
        }
        var c = AudioClip.Create("music", n, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }
}
