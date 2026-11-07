using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// Renders a key-art still (thumbnail / itch.io cover):
///   WhoMe.exe -thumbnail out.png -screen-width 1280 -screen-height 720 -screen-fullscreen 0
/// The layout adapts to the window's aspect ratio (wide = 16:9 thumbnail, tall = 630x500 cover).
public partial class Game
{
    bool TryThumbnail()
    {
        var a = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(a, "-thumbnail");
        if (i < 0) return false;
        string path = i + 1 < a.Length && !a[i + 1].StartsWith("-") ? a[i + 1] : "thumbnail.png";
        AudioListener.volume = 0f;
        new GameObject("Thumb").AddComponent<Runner>().StartCoroutine(ThumbRun(path));
        return true;
    }

    IEnumerator ThumbRun(string path)
    {
        yield return new WaitForSeconds(1.0f);          // let the rain fill the screen
        NewScreen();
        phase = Phase.Menu;
        bool wide = Screen.width / (float)Screen.height > 1.5f;
        BuildKeyArt(wide);
        for (int f = 0; f < 5; f++) yield return null;
        yield return new WaitForEndOfFrame();
        var tex = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Debug.Log($"[THUMB] wrote {tex.width}x{tex.height} to {path}");
        Application.Quit(0);
    }

    void BuildKeyArt(bool wide)
    {
        var c1 = Cases.All[0];
        // canvas is 1280 wide; the cover (630x500) is 1280x1016 in canvas units
        float wallY = wide ? 10 : 75, wallH = wide ? 330 : 360;
        float titleY = wide ? 268 : 395, titleSize = wide ? 128 : 150;
        float lineY = wallY + (wide ? 14 : 10), lineS = wide ? 150 : 165;
        float[] lineX = wide ? new[] { -110f, 100f, 310f, 515f } : new[] { -440f, -150f, 150f, 440f };
        float youX = wide ? -410 : -330, youY = wide ? -160 : -335, youS = wide ? 300 : 300;
        float stampX = wide ? 300 : 230, stampY = wide ? -265 : -330;

        // ---- line-up wall with height chart ----
        Color wall = new Color(0.16f, 0.17f, 0.25f);
        var w = UI.Img(screen, wall, null, "Wall");
        w.rectTransform.Center(0, wallY, 1400, wallH);
        for (int k = 0; k <= 6; k++)
        {
            float y = wallY - wallH / 2 + 25 + k * (wallH - 50) / 6f;
            var ln = UI.Img(screen, new Color(1, 1, 1, k % 2 == 0 ? 0.22f : 0.1f));
            ln.rectTransform.Center(0, y, 1400, k % 2 == 0 ? 3 : 2);
            if (k % 2 == 0)
            {
                var lb = UI.Txt(screen, (4 + k / 2) + " FT", 18, new Color(1, 1, 1, 0.3f), TextAnchor.MiddleLeft, FontStyle.Bold);
                lb.rectTransform.Center(-560, y + 14, 120, 24);
            }
        }

        // ---- the line-up: increasingly unlikely suspects, all staring at you ----
        var lineup = new[] { c1.suspects[4], c1.suspects[7], Cases.All[1].suspects[7], Cases.All[2].suspects[7] };
        for (int k = 0; k < lineup.Length; k++)
        {
            var s = lineup[k];
            var p = Portrait.Build(screen, s.color, s.features, lineS);
            p.Center(lineX[k], lineY, lineS, lineS);
            Vector2 toYou = new Vector2(youX - lineX[k], youY - lineY);
            foreach (var g in p.GetComponentsInChildren<GooglyEye>()) { g.fixedDir = toYou; g.radius *= 1.7f; }

            var plate = UI.Img(screen, Pal.Paper, UI.Round, "Plate");
            plate.rectTransform.Center(lineX[k], lineY - lineS * 0.78f, 56, 40);
            plate.rectTransform.localRotation = Quaternion.Euler(0, 0, k % 2 == 0 ? 4 : -4);
            UI.Shadow(plate, 3, 0.5f);
            var num = UI.Txt(plate.transform, (k + 1).ToString(), 28, Pal.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            num.rectTransform.Fill();
        }

        // ---- title ----
        var title = UI.Txt(screen, "WHO, <color=#d64545>ME?</color>", (int)titleSize, Pal.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
        title.rectTransform.Center(0, titleY, 1280, titleSize * 1.3f);
        UI.Shadow(title, 7, 0.9f);

        // ---- you: frosting on the moustache, halo on, eyes darting away ----
        var halo = UI.Img(screen, Pal.Gold, Ring(), "Halo");
        halo.rectTransform.Center(youX, youY + youS * 0.62f, youS * 0.62f, youS * 0.17f);
        var you = Portrait.Build(screen, c1.youColor, c1.youFeatures, youS);
        you.Center(youX, youY, youS, youS);
        UI.Shadow(you.GetComponentInChildren<Image>(), 8, 0.5f);
        foreach (var g in you.GetComponentsInChildren<GooglyEye>()) g.fixedDir = new Vector2(-1f, 0.55f);
        for (int k = 0; k < 3; k++)
        {
            var drop = UI.Img(screen, new Color(0.55f, 0.85f, 1f, 0.95f), UI.Circle, "Sweat");
            float ds = youS * (0.09f - k * 0.018f);
            drop.rectTransform.Center(youX + youS * (0.5f + k * 0.07f), youY + youS * (0.3f - k * 0.13f), ds, ds * 1.35f);
        }

        // ---- the stamp ----
        var stamp = UI.Txt(screen, "SHIFT THE BLAME", wide ? 64 : 70, Pal.Red, TextAnchor.MiddleCenter, FontStyle.Bold);
        stamp.rectTransform.Center(stampX, stampY, 760, 100);
        stamp.rectTransform.localRotation = Quaternion.Euler(0, 0, 6);
        UI.Outline(stamp, Pal.Red, 3);
        float bw = wide ? 640 : 700, bh = 100, t = 7;
        var box = UI.New("StampBox", screen).Center(stampX, stampY, bw, bh);
        box.localRotation = Quaternion.Euler(0, 0, 6);
        UI.Img(box, Pal.Red).rectTransform.Center(0, bh / 2, bw + t, t);
        UI.Img(box, Pal.Red).rectTransform.Center(0, -bh / 2, bw + t, t);
        UI.Img(box, Pal.Red).rectTransform.Center(-bw / 2, 0, t, bh + t);
        UI.Img(box, Pal.Red).rectTransform.Center(bw / 2, 0, t, bh + t);
    }

    static Sprite Ring()
    {
        const int n = 128;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                float a = Mathf.Clamp01((1f - d) * 40f) * Mathf.Clamp01((d - 0.7f) * 40f);
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }
}
