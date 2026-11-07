using System;
using System.Collections;
using System.IO;
using UnityEngine;

/// Scripted playthrough for CI / headless checks:
///   WhoMe.exe -autotest <screenshotDir> -screen-width 1280 -screen-height 720 -logFile log.txt
/// Plays a winning run of Case 1 (contradiction, shrug, blame-shift bonus, verdict)
/// and a losing run of Case 3 (repeated contradictions -> BUSTED), saving screenshots.
public partial class Game
{
    class Runner : MonoBehaviour { }
    string shotDir; int shotN; int failures;

    void Start()
    {
        if (TryTrailer() || TryThumbnail()) return;
        var a = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(a, "-autotest");
        if (i < 0) return;
        shotDir = i + 1 < a.Length && !a[i + 1].StartsWith("-") ? a[i + 1] : "autotest";
        Directory.CreateDirectory(shotDir);
        new GameObject("AutoTest").AddComponent<Runner>().StartCoroutine(AutoRun());
    }

    void Log(string m) => Debug.Log($"[AUTO] {m} | phase={phase} round={round} accused={accused} susp={suspicion} aud={audacity} cleared=[{string.Join(",", cleared)}] dismissed=[{string.Join(",", dismissed)}]");

    void Expect(bool ok, string what)
    {
        if (!ok) { failures++; Debug.LogError("[AUTO] FAIL: " + what); }
        else Debug.Log("[AUTO] ok: " + what);
    }

    IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(shotDir, $"{++shotN:00}_{name}.png"));
        yield return null;
        Log("shot " + name);
    }

    IEnumerator Skip()
    {
        int guard = 0;
        while (phase == Phase.Talking && guard++ < 300) { AdvanceDialogue(); yield return new WaitForSeconds(0.04f); }
        yield return new WaitForSeconds(0.25f);
    }

    IEnumerator AccuseAndSkip(int id) { Accuse(id); yield return Skip(); }

    IEnumerator AutoRun()
    {
        yield return new WaitForSeconds(1.5f);
        yield return Shot("title");

        // ---------------- Case 1: winning run ----------------
        ShowIntro(0);
        yield return new WaitForSeconds(0.6f);
        yield return Shot("intro");
        StartCase(0);
        yield return Skip();
        Expect(phase == Phase.Deciding && round == 0, "case1 starts in deciding phase");
        yield return Shot("decide0");
        OpenDossier(0);
        yield return new WaitForSeconds(0.5f);
        yield return Shot("dossier");

        yield return AccuseAndSkip(0);               // blame the butler
        Expect(accused == 0 && round == 1, "butler accepted, evidence 1 revealed");
        yield return Shot("evidence1");

        Accuse(0);                                   // stand firm -> contradicted by the timeline
        int g = 0;
        while (!cleared.Contains(0) && g++ < 200) { AdvanceDialogue(); yield return new WaitForSeconds(0.04f); }
        yield return new WaitForSeconds(0.3f);
        yield return Shot("contradiction");
        Expect(cleared.Contains(0) && accused == -1 && contradictions == 1, "butler contradiction clears him");
        yield return Skip();

        yield return AccuseAndSkip(4);               // the corgi
        Expect(accused == 4 && round == 2, "corgi accepted, evidence 2 revealed (rules corgi out)");
        Shrug();
        yield return Skip();
        Expect(dismissed.Contains(1), "evidence 2 shrugged off");
        yield return Shot("shrug");
        yield return AccuseAndSkip(4);               // stand firm: allowed, flour is dismissed
        Expect(accused == 4 && round == 3 && contradictions == 1, "firm on corgi survives thanks to shrug");
        yield return AccuseAndSkip(4);
        yield return AccuseAndSkip(4);
        Expect(round == 5, "all 5 evidence revealed");
        yield return Shot("evidence5");
        int before = audacity;
        Accuse(7);                                   // shift to the portrait
        yield return Skip();
        Expect(audacity - before == 550, "portrait shift gives 500 + 50 bonus");
        yield return new WaitForSeconds(2.4f);
        Expect(phase == Phase.Over, "case 1 closed");
        yield return Shot("verdict");

        // ---------------- Case 2: quick look ----------------
        StartCase(1);
        yield return Skip();
        yield return AccuseAndSkip(4);
        yield return Shot("case2");

        // ---------------- Case 3: losing run ----------------
        StartCase(2);
        yield return Skip();
        yield return AccuseAndSkip(1);   // captain, accepted
        yield return AccuseAndSkip(1);   // contradicted by E1
        yield return AccuseAndSkip(6);   // the moon, contradicted
        yield return Shot("case3_mid");
        yield return AccuseAndSkip(7);   // the inspector, accepted
        Expect(accused == 7 && round == 2, "inspector accepted in case 3");
        timer = 0;                       // dither -> timeout
        yield return new WaitForSeconds(0.2f);
        yield return Skip();
        yield return AccuseAndSkip(2);   // gulls, contradicted
        yield return AccuseAndSkip(3);   // fog, contradicted -> busted
        yield return new WaitForSeconds(2.6f);
        Expect(suspicion >= 100 && phase == Phase.Over, "case 3 ends BUSTED");
        yield return Shot("busted");

        Debug.Log(failures == 0 ? "[AUTO] ALL PASSED" : $"[AUTO] {failures} FAILURE(S)");
        yield return new WaitForSeconds(0.5f);
        Application.Quit(failures == 0 ? 0 : 1);
    }
}
