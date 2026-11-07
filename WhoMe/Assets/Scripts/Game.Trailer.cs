using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// Scripted trailer recorder:
///   WhoMe.exe -trailer <dir> -screen-width 1280 -screen-height 720 -screen-fullscreen 0
/// Plays a directed sequence at a locked 30 fps (Time.captureFramerate), writes every frame
/// as <dir>/f00000.jpg and mixes every sound it played into <dir>/audio.wav.
/// Encode with:  ffmpeg -framerate 30 -i f%05d.jpg -i audio.wav ... trailer.mp4
public partial class Game
{
    const int TrailerFps = 30;
    string trailerDir;
    bool recording, recMuted;
    int trailerFrame;
    float recStart;
    RectTransform overlay;
    readonly List<(float t, string name, float vol, float pitch)> recSounds = new List<(float, string, float, float)>();

    bool TryTrailer()
    {
        var a = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(a, "-trailer");
        if (i < 0) return false;
        trailerDir = i + 1 < a.Length && !a[i + 1].StartsWith("-") ? a[i + 1] : "trailer";
        Directory.CreateDirectory(trailerDir);
        AudioListener.volume = 0f;

        var cgo = new GameObject("TrailerOverlay", typeof(Canvas), typeof(CanvasScaler));
        var cv = cgo.GetComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 100;
        var sc = cgo.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1280, 720);
        sc.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        overlay = UI.New("Overlay", cgo.transform).Fill();

        var runner = new GameObject("Trailer").AddComponent<Runner>();
        runner.StartCoroutine(TrailerRun(runner));
        return true;
    }

    // ---------------- recording ----------------
    IEnumerator Capture()
    {
        while (recording)
        {
            yield return new WaitForEndOfFrame();
            if (!recording) break;
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(trailerDir, $"f{trailerFrame++:00000}.jpg"), tex.EncodeToJPG(94));
            Destroy(tex);
        }
    }

    void RecSound(string name, float vol, float pitch)
    {
        if (!recording || recMuted) return;
        recSounds.Add((Time.time - recStart, name, vol, pitch));
    }

    void WriteAudio()
    {
        int sr = Sfx.SampleRate;
        float dur = trailerFrame / (float)TrailerFps;
        int n = Mathf.CeilToInt(dur * sr);
        var mix = new float[n];
        var cache = new Dictionary<string, float[]>();
        foreach (var s in recSounds)
        {
            if (!cache.TryGetValue(s.name, out var src)) cache[s.name] = src = Sfx.Samples(s.name);
            int start = Mathf.RoundToInt(s.t * sr);
            if (s.name == "music")
            {
                for (int i = start; i < n; i++) mix[i] += src[(i - start) % src.Length] * s.vol;
                continue;
            }
            for (int j = 0; ; j++)
            {
                int k = (int)(j * s.pitch);
                if (k >= src.Length || start + j >= n) break;
                mix[start + j] += src[k] * s.vol;
            }
        }
        // fade out the last 1.5 s and keep everything below clipping
        int fade = Mathf.Min(n, (int)(1.5f * sr));
        for (int i = 0; i < fade; i++) mix[n - 1 - i] *= i / (float)fade;
        float peak = 0; foreach (var v in mix) peak = Mathf.Max(peak, Mathf.Abs(v));
        float gain = peak > 0.95f ? 0.95f / peak : 1f;

        using (var w = new BinaryWriter(File.Create(Path.Combine(trailerDir, "audio.wav"))))
        {
            w.Write("RIFF".ToCharArray()); w.Write(36 + n * 2); w.Write("WAVE".ToCharArray());
            w.Write("fmt ".ToCharArray()); w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(sr); w.Write(sr * 2); w.Write((short)2); w.Write((short)16);
            w.Write("data".ToCharArray()); w.Write(n * 2);
            foreach (var v in mix) w.Write((short)Mathf.Clamp(v * gain * 32767f, -32768, 32767));
        }
    }

    // ---------------- direction helpers ----------------
    static IEnumerator Hold(float s) { yield return new WaitForSeconds(s); }

    /// Lets dialogue play out at reading pace; after `readLines` lines it skips the rest quickly.
    IEnumerator Talk(int readLines = 99, float pause = 0.4f)
    {
        int n = 0;
        while (phase == Phase.Talking)
        {
            if (typing && n >= readLines) { AdvanceDialogue(); yield return null; continue; }
            if (typing) { yield return null; continue; }
            if (n < readLines) yield return Hold(pause + (fullText ?? "").Length * 0.009f);
            n++;
            if (phase != Phase.Talking) break;
            AdvanceDialogue();
            yield return null;
        }
        yield return null;
    }

    /// Full-screen title card. `mid` runs while the screen is black (to cut to a new scene).
    IEnumerator TitleCard(string text, float hold, Action mid = null, Color? col = null, int size = 64, string sub = null, bool fadeOut = true)
    {
        var root = UI.New("Card", overlay).Fill();
        var cg = root.gameObject.AddComponent<CanvasGroup>();
        UI.Img(root, Color.black).rectTransform.Fill();
        var t = UI.Txt(root, text, size, col ?? Pal.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
        t.rectTransform.Center(0, sub != null ? 30 : 0, 1200, 220); UI.Shadow(t, 4, 0.8f);
        if (sub != null)
        {
            var st = UI.Txt(root, sub, 26, Pal.Dim, TextAnchor.MiddleCenter, FontStyle.Italic);
            st.rectTransform.Center(0, -90, 1200, 60);
        }
        Sfx.Play("stamp", 0.55f, 1.1f);
        for (float f = 0; f < 0.18f; f += Time.deltaTime)
        {
            float k = f / 0.18f;
            cg.alpha = k;
            t.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, k);
            yield return null;
        }
        cg.alpha = 1; t.rectTransform.localScale = Vector3.one;
        mid?.Invoke();
        yield return Hold(hold);
        if (!fadeOut) yield break;
        for (float f = 0; f < 0.2f; f += Time.deltaTime) { cg.alpha = 1 - f / 0.2f; yield return null; }
        Destroy(root.gameObject);
    }

    // ---------------- the trailer ----------------
    IEnumerator TrailerRun(MonoBehaviour host)
    {
        yield return Hold(0.5f);                       // let the title screen settle
        Time.captureFramerate = TrailerFps;
        yield return null;
        Sfx.OnPlay = RecSound;
        recStart = Time.time; recording = true;
        host.StartCoroutine(Capture());
        Sfx.StartMusic();

        // cold open
        yield return TitleCard("Someone ate the Duchess's\n100th birthday cake.", 2.2f, null, Pal.Text, 54);
        yield return TitleCard("It was <color=#d64545>you.</color>", 1.4f, null, Pal.Text, 84);
        yield return TitleCard("WHO, <color=#d64545>ME?</color>", 0.1f, ShowTitle, Pal.Gold, 120);
        yield return Hold(2.6f);

        // the setup
        ShowIntro(0);
        yield return Hold(2.4f);
        yield return TitleCard("Inspector Gloom is on the case.", 1.3f, () => StartCase(0), Pal.Text, 56);
        yield return Talk(1);

        // the core loop
        yield return TitleCard("So... <color=#e2b04a>SHIFT THE BLAME.</color>", 1.3f, null, Pal.Text, 64);
        OpenDossier(0);
        yield return Hold(2.0f);
        Accuse(0);                                     // the butler
        yield return Talk(2);
        yield return Hold(0.3f);

        yield return TitleCard("Just never contradict\nthe evidence.", 1.4f, null, Pal.Text, 56);
        Accuse(0);                                     // timeline rules the butler out
        yield return Talk(2);
        yield return Hold(0.4f);

        yield return TitleCard("The sillier the scapegoat...", 1.2f, null, Pal.Text, 56);
        OpenDossier(4);
        yield return Hold(1.4f);
        Accuse(4);                                     // the corgi
        yield return Talk(2);
        Shrug();                                       // dismiss the flour
        yield return Talk(1);

        // montage: stand firm through the rest of the evidence while a card covers it
        bool cardDone = false;
        host.StartCoroutine(Wrap(TitleCard("...the bigger the score.", 1.6f, null, Pal.Text, 56), () => cardDone = true));
        yield return Hold(0.2f);
        recMuted = true;
        for (int k = 0; k < 3; k++) { Accuse(4); yield return Talk(0); }
        recMuted = false;
        while (!cardDone) yield return null;

        OpenDossier(7);
        yield return Hold(1.5f);
        Accuse(7);                                     // a 224-year-old oil painting
        yield return Talk(2);
        yield return Hold(3.4f);                       // verdict screen

        // more cases
        yield return TitleCard("Three ridiculous cases.", 1.0f, () => ShowIntro(1), Pal.Text, 60);
        yield return Hold(1.8f);
        ShowIntro(2);
        yield return Hold(1.6f);

        yield return TitleCard("Nobody is above suspicion.", 1.2f, () => { StartCase(2); }, Pal.Text, 60);
        recMuted = true; yield return Talk(0); recMuted = false;
        OpenDossier(7);
        yield return Hold(2.0f);
        Accuse(7);                                     // blame the Inspector herself
        yield return Talk(2);
        yield return Hold(0.5f);

        yield return TitleCard("Just don't get caught.", 1.2f, null, Pal.Red, 64);
        suspicion = shownSusp = 86;
        Accuse(1);                                     // the captain - contradicted -> BUSTED
        yield return Talk(1);
        yield return Hold(3.8f);

        // end card
        yield return TitleCard("WHO, <color=#d64545>ME?</color>", 4.2f, null, Pal.Gold, 140,
            "Play free in your browser  -  GitHub Game Off 2026", false);

        recording = false;
        Time.captureFramerate = 0;
        yield return null;
        WriteAudio();
        Debug.Log($"[TRAILER] wrote {trailerFrame} frames ({trailerFrame / (float)TrailerFps:0.0}s) to {trailerDir}");
        Application.Quit(0);
    }

    static IEnumerator Wrap(IEnumerator co, Action done) { yield return co; done(); }
}
