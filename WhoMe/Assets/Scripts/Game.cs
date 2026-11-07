using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class Game : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindAnyObjectByType<Game>() != null) return;
        new GameObject("Game").AddComponent<Game>();
    }

    // ---------------- tuning ----------------
    const float TimeLimit = 35f;
    const float SuspPerEvidence = 5f, SuspContradiction = 20f, SuspShift = 3f, SuspFirm = -5f, SuspShrug = 15f, SuspTimeout = 12f;
    const int ShiftBonus = 50;

    // ---------------- scene refs ----------------
    RectTransform stage, screen, modal;
    readonly System.Random rng = new System.Random();
    int floatsActive;

    // ---------------- case state ----------------
    CaseData cs; int caseIndex;
    int round, accused, audacity, contradictions, shifts;
    float suspicion, shownSusp, timer;
    bool shrugUsed;
    HashSet<int> cleared = new HashSet<int>(), dismissed = new HashSet<int>(), accusedBefore = new HashSet<int>();
    enum Phase { Menu, Talking, Deciding, Over }
    Phase phase = Phase.Menu;

    // ---------------- main screen widgets ----------------
    RectTransform evidenceList, suspBarFill, timerFill;
    Image suspBarImg;
    Text suspPct, audacityTxt, roundTxt, dlgName, dlgText, dlgHint;
    CanvasGroup inspGroup, youGroup;
    Button firmBtn, shrugBtn;
    Text firmLbl, shrugLbl;
    readonly List<Card> cards = new List<Card>();
    readonly List<RectTransform> evidenceCards = new List<RectTransform>();

    class Card
    {
        public RectTransform rt; public Image bg; public GameObject accusedTag, clearedTag;
    }

    struct Line { public string who, text; public bool insp, you; }
    readonly Queue<Line> lines = new Queue<Line>();
    Action onLinesDone;
    Coroutine typeCo;
    bool typing; string fullText;

    // =====================================================================
    void Awake()
    {
        Application.targetFrameRate = 60;
        var cam = Camera.main;
        if (cam == null)
        {
            var cg = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = cg.AddComponent<Camera>();
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Pal.Bg;
        cam.orthographic = true;

        if (FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        cgo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = cgo.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1280, 720);
        sc.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        UI.Img(cgo.transform, Pal.Bg, null, "BG").rectTransform.Fill();
        var rain = UI.New("Rain", cgo.transform).Fill();
        rain.gameObject.AddComponent<Rain>();
        var vig = UI.Img(cgo.transform, Color.white, UI.Vignette, "Vignette");
        vig.rectTransform.Fill(); vig.raycastTarget = false;

        stage = UI.New("Stage", cgo.transform).Center(0, 0, 1280, 720);
        Sfx.Init(gameObject);
        ShowTitle();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.anyKeyDown) Sfx.StartMusic();
        if (Input.GetKeyDown(KeyCode.M)) Sfx.ToggleMusic();

        if (phase == Phase.Menu || phase == Phase.Over) return;

        if (modal != null && Input.GetKeyDown(KeyCode.Escape)) CloseModal();
        if (modal == null && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))) AdvanceDialogue();

        // suspicion bar
        shownSusp = Mathf.MoveTowards(shownSusp, suspicion, 70f * Time.deltaTime);
        if (suspBarFill != null)
        {
            float k = Mathf.Clamp01(shownSusp / 100f);
            suspBarFill.sizeDelta = new Vector2(296 * k, suspBarFill.sizeDelta.y);
            var col = Color.Lerp(Pal.Teal, Pal.Red, k);
            if (k > 0.7f) col = Color.Lerp(col, Color.white, (Mathf.Sin(Time.time * 10) * 0.5f + 0.5f) * 0.35f);
            suspBarImg.color = col;
            suspPct.text = Mathf.RoundToInt(shownSusp) + "%";
        }

        if (phase == Phase.Deciding)
        {
            timer -= Time.deltaTime;
            if (timerFill != null)
            {
                float k = Mathf.Clamp01(timer / TimeLimit);
                timerFill.sizeDelta = new Vector2(812 * k, timerFill.sizeDelta.y);
                timerFill.GetComponent<Image>().color = k < 0.3f ? Pal.Red : Pal.Gold;
            }
            if (timer <= 0) Timeout();
        }
    }

    // =====================================================================
    // SCREENS
    // =====================================================================
    void NewScreen()
    {
        StopAllCoroutines();
        typing = false; lines.Clear(); onLinesDone = null;
        if (screen != null) Destroy(screen.gameObject);
        modal = null;
        cards.Clear(); evidenceCards.Clear();
        suspBarFill = null; timerFill = null;
        screen = UI.New("Screen", stage).Fill();
        floatsActive = 0;
    }

    // ---------------- TITLE ----------------
    void ShowTitle()
    {
        NewScreen();
        phase = Phase.Menu;

        var title = UI.Txt(screen, "WHO, <color=#d64545>ME?</color>", 112, Pal.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
        title.rectTransform.Place(0, 520, 1280, 140);
        UI.Shadow(title, 5, 0.8f);
        var sub = UI.Txt(screen, "A comedy detective game where <b>you</b> did it - and somebody else is going down for it.", 22, Pal.Dim, TextAnchor.MiddleCenter, FontStyle.Italic);
        sub.rectTransform.Place(0, 478, 1280, 40);

        // parade of suspects
        var parade = new List<(Color, string)>();
        foreach (var c in Cases.All) foreach (var s in c.suspects) parade.Add((s.color, s.features));
        int count = 9;
        for (int i = 0; i < count; i++)
        {
            var p = parade[(i * 5 + 3) % parade.Count];
            bool center = i == count / 2;
            var pr = center ? Portrait.Build(screen, Cases.InspectorColor, Cases.InspectorFeatures, 110)
                            : Portrait.Build(screen, p.Item1, p.Item2, 62);
            pr.Center(-480 + i * 120, center ? 36 : 30, pr.sizeDelta.x, pr.sizeDelta.y);
            var b = pr.gameObject.AddComponent<Bob>(); b.phase = i * 0.7f; b.amp = center ? 4 : 6;
        }
        var cap = UI.Txt(screen, "Inspector Prudence Gloom is on the case. Unfortunately for you, the case is you.", 16, Pal.Dim, TextAnchor.MiddleCenter, FontStyle.Italic);
        cap.rectTransform.Place(0, 300, 1280, 30);

        for (int i = 0; i < Cases.All.Length; i++)
        {
            int idx = i;
            int best = PlayerPrefs.GetInt("best" + i, -1);
            string extra = best >= 0 ? "   <size=15><color=#e2b04a>best: " + best + " - " + Rank(best, Cases.All[i]) + "</color></size>" : "";
            var b = UI.Btn(screen, "CASE " + (i + 1) + ":  " + Cases.All[i].title + extra, i == 0 ? Pal.Red : Pal.PanelLight, () => ShowIntro(idx), 22);
            b.GetComponent<RectTransform>().Place(340, 240 - i * 64, 600, 52);
        }

        var how = UI.Btn(screen, "HOW TO PLAY", Pal.Panel, ShowHowTo, 18);
        how.GetComponent<RectTransform>().Place(340, 34, 290, 44);
        var mus = UI.Btn(screen, Sfx.MusicOn ? "MUSIC: ON  (M)" : "MUSIC: OFF  (M)", Pal.Panel, null, 18);
        mus.onClick.AddListener(() => { Sfx.ToggleMusic(); mus.GetComponentInChildren<Text>().text = Sfx.MusicOn ? "MUSIC: ON  (M)" : "MUSIC: OFF  (M)"; });
        mus.GetComponent<RectTransform>().Place(650, 34, 290, 44);

        var foot = UI.Txt(screen, "GitHub Game Off 2026  -  theme: SHIFT (the blame)", 13, Pal.Grey, TextAnchor.LowerRight);
        foot.rectTransform.Place(0, 6, 1270, 20);
    }

    void ShowHowTo()
    {
        var p = OpenModal(760, 560);
        var t = UI.Txt(p, "HOW TO GET AWAY WITH IT", 30, Pal.Ink, TextAnchor.UpperCenter, FontStyle.Bold);
        t.rectTransform.Place(0, 500, 760, 44);
        var body = UI.Txt(p,
            "You committed the crime. Inspector Gloom doesn't know that. Yet.\n\n" +
            "<b>1. Accuse.</b> Click a suspect card, read their file, then ACCUSE them.\n" +
            "<b>2. Evidence.</b> After every accusation, the Inspector reveals NEW EVIDENCE.\n" +
            "<b>3. Don't contradict it.</b> If your scapegoat clashes with ANY evidence on the board, " +
            "that's a <color=#d64545><b>CONTRADICTION</b></color>: they're cleared and Suspicion rises. Suspect files hold the clues.\n" +
            "<b>4. Shift the blame.</b> STAND FIRM to keep blaming the same suspect, or SHIFT to someone new when the evidence closes in.\n" +
            "<b>5. Be audacious.</b> Sillier scapegoats earn more AUDACITY. Shifting to someone even sillier earns a bonus.\n" +
            "<b>6. Shrug it off.</b> Once per case you can dismiss the newest piece of evidence. It looks shifty, though.\n" +
            "<b>7. Don't dither.</b> Running out of time looks suspicious.\n\n" +
            "Hit <color=#d64545><b>100% Suspicion</b></color> and you're BUSTED.",
            19, Pal.Ink, TextAnchor.UpperLeft);
        body.rectTransform.Place(40, 80, 680, 410);
        var b = UI.Btn(p, "GOT IT", Pal.Red, CloseModal, 20);
        b.GetComponent<RectTransform>().Place(280, 22, 200, 48);
    }

    // ---------------- INTRO ----------------
    void ShowIntro(int i)
    {
        NewScreen();
        phase = Phase.Menu;
        caseIndex = i; cs = Cases.All[i];
        Sfx.Play("sting");

        var h = UI.Txt(screen, "CASE " + (i + 1) + ":  " + cs.title.ToUpper(), 48, Pal.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
        h.rectTransform.Place(0, 615, 1280, 70); UI.Shadow(h, 4);
        var loc = UI.Txt(screen, cs.location, 20, Pal.Dim, TextAnchor.MiddleCenter, FontStyle.Italic);
        loc.rectTransform.Place(0, 585, 1280, 30);

        var crime = UI.Img(screen, Pal.Paper, UI.Round, "Crime");
        crime.rectTransform.Place(90, 130, 520, 430);
        crime.rectTransform.localRotation = Quaternion.Euler(0, 0, 1.5f);
        UI.Shadow(crime, 6, 0.5f);
        var ch = UI.Txt(crime.transform, "THE CRIME", 30, Pal.Ink, TextAnchor.UpperCenter, FontStyle.Bold);
        ch.rectTransform.Place(0, 370, 520, 44);
        var cs2 = UI.Txt(crime.transform, "(what everyone knows)", 16, Pal.Grey, TextAnchor.UpperCenter, FontStyle.Italic);
        cs2.rectTransform.Place(0, 346, 520, 26);
        var ct = UI.Txt(crime.transform, cs.crime, 22, Pal.Ink, TextAnchor.UpperLeft);
        ct.rectTransform.Place(32, 30, 456, 300);

        var truth = UI.Img(screen, new Color(0.2f, 0.08f, 0.09f), UI.Round, "Truth");
        truth.rectTransform.Place(670, 130, 520, 430);
        truth.rectTransform.localRotation = Quaternion.Euler(0, 0, -1.5f);
        UI.Shadow(truth, 6, 0.5f);
        var th = UI.Txt(truth.transform, "THE TRUTH", 30, Pal.Red, TextAnchor.UpperCenter, FontStyle.Bold);
        th.rectTransform.Place(0, 370, 520, 44);
        var ts = UI.Txt(truth.transform, "(what only you know - shh)", 16, Pal.Dim, TextAnchor.UpperCenter, FontStyle.Italic);
        ts.rectTransform.Place(0, 346, 520, 26);
        var tt = UI.Txt(truth.transform, cs.truth, 19, Pal.Text, TextAnchor.UpperLeft);
        tt.rectTransform.Place(32, 30, 456, 310);
        var you = Portrait.Build(truth.transform, cs.youColor, cs.youFeatures, 70);
        you.Center(205, -160, 70, 70);

        var stamp = UI.Txt(screen, "CONFIDENTIAL", 40, new Color(0.84f, 0.27f, 0.27f, 0.8f), TextAnchor.MiddleCenter, FontStyle.Bold);
        stamp.rectTransform.Place(700, 150, 340, 60); stamp.rectTransform.localRotation = Quaternion.Euler(0, 0, 10);
        UI.Outline(stamp, new Color(0.84f, 0.27f, 0.27f, 0.8f), 2);

        var go = UI.Btn(screen, "FACE INSPECTOR GLOOM  >", Pal.Red, () => StartCase(i), 24);
        go.GetComponent<RectTransform>().Place(440, 40, 400, 60);
        var back = UI.Btn(screen, "< BACK", Pal.Panel, ShowTitle, 18);
        back.GetComponent<RectTransform>().Place(30, 50, 140, 44);
    }

    // =====================================================================
    // MAIN INTERROGATION SCREEN
    // =====================================================================
    void StartCase(int i)
    {
        caseIndex = i; cs = Cases.All[i];
        round = 0; accused = -1; audacity = 0; contradictions = 0; shifts = 0;
        suspicion = 0; shownSusp = 0; shrugUsed = false;
        cleared.Clear(); dismissed.Clear(); accusedBefore.Clear();
        BuildMain();
        phase = Phase.Talking;
        var ls = new List<Line>();
        foreach (var l in cs.intro)
        {
            if (l.StartsWith("(")) ls.Add(Sys(l)); else ls.Add(Insp(l));
        }
        Say(BeginDecide, ls.ToArray());
    }

    void BuildMain()
    {
        NewScreen();

        // ---- top bar
        var top = UI.Img(screen, Pal.Panel, UI.Round, "TopBar");
        top.rectTransform.Place(16, 664, 1248, 48);
        var ct = UI.Txt(top.transform, "<b>CASE " + (caseIndex + 1) + ":</b> " + cs.title, 20, Pal.Gold, TextAnchor.MiddleLeft);
        ct.rectTransform.Place(16, 0, 420, 48);
        var sl = UI.Txt(top.transform, "SUSPICION OF YOU", 14, Pal.Dim, TextAnchor.MiddleRight, FontStyle.Bold);
        sl.rectTransform.Place(440, 0, 150, 48);
        var barBg = UI.Img(top.transform, new Color(0, 0, 0, 0.5f), UI.Round, "SuspBar");
        barBg.rectTransform.Place(600, 13, 300, 22);
        suspBarImg = UI.Img(barBg.transform, Pal.Teal, UI.Round, "Fill");
        suspBarFill = suspBarImg.rectTransform;
        suspBarFill.anchorMin = suspBarFill.anchorMax = new Vector2(0, 0.5f);
        suspBarFill.pivot = new Vector2(0, 0.5f);
        suspBarFill.anchoredPosition = new Vector2(2, 0);
        suspBarFill.sizeDelta = new Vector2(0, 18);
        suspPct = UI.Txt(barBg.transform, "0%", 14, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        UI.Shadow(suspPct, 1, 0.8f);
        audacityTxt = UI.Txt(top.transform, "", 20, Pal.Text, TextAnchor.MiddleRight);
        audacityTxt.rectTransform.Place(910, 0, 322, 48);
        UpdateAudacity();

        // ---- evidence board
        var board = UI.Img(screen, Pal.Cork, UI.Round, "Board");
        board.rectTransform.Place(16, 16, 420, 638);
        var bh = UI.Txt(board.transform, "EVIDENCE BOARD", 20, Pal.Paper, TextAnchor.MiddleLeft, FontStyle.Bold);
        bh.rectTransform.Place(18, 598, 250, 36);
        roundTxt = UI.Txt(board.transform, "", 15, Pal.PaperDark, TextAnchor.MiddleRight);
        roundTxt.rectTransform.Place(200, 598, 202, 36);
        evidenceList = UI.New("List", board.transform).Place(0, 0, 420, 598);
        var empty = UI.Txt(evidenceList, "No evidence yet.\n\nMake your first accusation and see what turns up.", 17, new Color(1, 1, 1, 0.45f), TextAnchor.MiddleCenter, FontStyle.Italic);
        empty.rectTransform.Place(30, 200, 360, 200);
        empty.name = "Empty";

        // ---- dialogue box
        var dlg = UI.Img(screen, Pal.Panel, UI.Round, "Dialogue");
        dlg.rectTransform.Place(452, 466, 812, 188);
        dlg.gameObject.AddComponent<Clickable>().onClick = AdvanceDialogue;
        dlg.GetComponent<Clickable>().hoverScale = false;

        var ip = Portrait.Build(dlg.transform, Cases.InspectorColor, Cases.InspectorFeatures, 96);
        ip.Place(22, 30, 96, 96);
        inspGroup = ip.gameObject.AddComponent<CanvasGroup>();
        var iname = UI.Txt(dlg.transform, "GLOOM", 12, Pal.Dim, TextAnchor.MiddleCenter, FontStyle.Bold);
        iname.rectTransform.Place(10, 6, 120, 18);

        var yp = Portrait.Build(dlg.transform, cs.youColor, cs.youFeatures, 96);
        yp.Place(694, 30, 96, 96);
        youGroup = yp.gameObject.AddComponent<CanvasGroup>();
        var yname = UI.Txt(dlg.transform, "YOU", 12, Pal.Dim, TextAnchor.MiddleCenter, FontStyle.Bold);
        yname.rectTransform.Place(682, 6, 120, 18);

        dlgName = UI.Txt(dlg.transform, "", 18, Pal.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
        dlgName.rectTransform.Place(146, 146, 520, 30);
        dlgText = UI.Txt(dlg.transform, "", 21, Pal.Text, TextAnchor.UpperLeft);
        dlgText.rectTransform.Place(146, 24, 524, 122);
        dlgHint = UI.Txt(dlg.transform, "click / space  >>", 13, Pal.Dim, TextAnchor.LowerRight, FontStyle.Italic);
        dlgHint.rectTransform.Place(450, 6, 220, 20);

        // ---- timer
        var tb = UI.Img(screen, new Color(0, 0, 0, 0.4f), null, "TimerBg");
        tb.rectTransform.Place(452, 456, 812, 6);
        timerFill = UI.Img(tb.transform, Pal.Gold, null, "Timer").rectTransform;
        timerFill.anchorMin = timerFill.anchorMax = new Vector2(0, 0.5f);
        timerFill.pivot = new Vector2(0, 0.5f);
        timerFill.anchoredPosition = Vector2.zero;
        timerFill.sizeDelta = new Vector2(812, 6);

        // ---- action buttons
        firmBtn = UI.Btn(screen, "STAND FIRM", Pal.Teal, () => { if (accused >= 0) Accuse(accused); }, 18);
        firmBtn.GetComponent<RectTransform>().Place(452, 404, 480, 42);
        firmLbl = firmBtn.GetComponentInChildren<Text>();
        shrugBtn = UI.Btn(screen, "SHRUG IT OFF (1)", Pal.PanelLight, Shrug, 18);
        shrugBtn.GetComponent<RectTransform>().Place(944, 404, 320, 42);
        shrugLbl = shrugBtn.GetComponentInChildren<Text>();

        // ---- suspects
        for (int i = 0; i < cs.suspects.Length; i++)
        {
            int col = i % 4, row = i / 4;
            var s = cs.suspects[i];
            var c = new Card();
            c.bg = UI.Img(screen, Pal.PanelLight, UI.Round, "Card" + i);
            c.rt = c.bg.rectTransform.Place(452 + col * 206, 206 - row * 194, 194, 184);
            int idx = i;
            c.bg.gameObject.AddComponent<Clickable>().onClick = () => OpenDossier(idx);
            var pr = Portrait.Build(c.rt, s.color, s.features, 78);
            pr.Center(0, 34, 78, 78);
            var n = UI.Txt(c.rt, s.name, 17, Pal.Text, TextAnchor.MiddleCenter, FontStyle.Bold); UI.Fit(n, 11);
            n.rectTransform.Place(4, 34, 186, 26);
            var r = UI.Txt(c.rt, s.role, 13, Pal.Dim, TextAnchor.MiddleCenter, FontStyle.Italic); UI.Fit(r, 10);
            r.rectTransform.Place(4, 16, 186, 20);
            AbsurdityDots(c.rt, s.absurdity, 0, -82, 10);

            var tag = UI.Img(c.rt, Pal.Red, UI.Round, "Accused");
            tag.rectTransform.Center(0, 82, 120, 26);
            var tt = UI.Txt(tag.transform, "ACCUSED", 14, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            c.accusedTag = tag.gameObject; c.accusedTag.SetActive(false);

            var cl = UI.Img(c.rt, new Color(0.05f, 0.05f, 0.08f, 0.72f), UI.Round, "Cleared");
            cl.rectTransform.Fill(); cl.raycastTarget = false;
            var clt = UI.Txt(cl.transform, "CLEARED", 30, Pal.Red, TextAnchor.MiddleCenter, FontStyle.Bold);
            clt.rectTransform.localRotation = Quaternion.Euler(0, 0, 18);
            UI.Outline(clt, new Color(0, 0, 0, 0.6f), 2);
            c.clearedTag = cl.gameObject; c.clearedTag.SetActive(false);
            cards.Add(c);
        }
        RefreshMain();
    }

    void AbsurdityDots(RectTransform parent, int n, float cx, float cy, float size, Color? empty = null)
    {
        for (int k = 0; k < 5; k++)
        {
            var d = UI.Img(parent, k < n ? Pal.Gold : (empty ?? new Color(1, 1, 1, 0.15f)), UI.Circle, "dot");
            d.raycastTarget = false;
            d.rectTransform.Center(cx + (k - 2) * (size + 4), cy, size, size);
        }
    }

    void RefreshMain()
    {
        if (cards.Count == 0) return;
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].accusedTag.SetActive(i == accused);
            cards[i].clearedTag.SetActive(cleared.Contains(i));
            cards[i].bg.color = i == accused ? new Color(0.32f, 0.17f, 0.2f) : Pal.PanelLight;
        }
        roundTxt.text = "revealed " + round + " / " + cs.evidence.Length;
        bool deciding = phase == Phase.Deciding;
        firmBtn.interactable = deciding && accused >= 0;
        firmLbl.text = accused >= 0 ? "STAND FIRM: it was " + cs.suspects[accused].name : "STAND FIRM";
        bool canShrug = deciding && !shrugUsed && round > 0 && !dismissed.Contains(round - 1);
        shrugBtn.interactable = canShrug;
        shrugLbl.text = shrugUsed ? "SHRUG IT OFF (used)" : "SHRUG IT OFF (1)";
        dlgHint.gameObject.SetActive(phase == Phase.Talking);
        if (timerFill != null && !deciding) timerFill.sizeDelta = new Vector2(0, 6);
    }

    void UpdateAudacity()
    {
        if (audacityTxt != null) audacityTxt.text = "AUDACITY  <b><color=#e2b04a>" + audacity + "</color></b>";
    }

    void RefreshEvidence(int highlight)
    {
        foreach (var e in evidenceCards) Destroy(e.gameObject);
        evidenceCards.Clear();
        var empty = evidenceList.Find("Empty");
        if (empty != null) empty.gameObject.SetActive(round == 0);

        int n = round;
        float h = n <= 4 ? 132 : n == 5 ? 108 : 92;
        float gap = 6;
        for (int k = 0; k < n; k++)
        {
            int i = n - 1 - k; // newest on top
            var ev = cs.evidence[i];
            bool dis = dismissed.Contains(i);
            var card = UI.Img(evidenceList, dis ? new Color(0.7f, 0.66f, 0.58f) : Pal.Paper, UI.Round, "Ev" + i);
            var rt = card.rectTransform.Place(14, 598 - 8 - (k + 1) * h - k * gap + gap, 392, h);
            rt.localRotation = Quaternion.Euler(0, 0, ((i * 37) % 5 - 2) * 0.5f);
            UI.Shadow(card, 3, 0.4f);
            var pin = UI.Img(rt, i == highlight ? Pal.Red : Pal.Teal, UI.Circle, "pin");
            pin.rectTransform.Center(0, h / 2 - 8, 14, 14);
            var t = UI.Txt(rt, "<b>#" + (i + 1) + "  " + ev.title.ToUpper() + "</b>\n" + ev.text, h > 100 ? 16 : 14, Pal.Ink, TextAnchor.UpperLeft);
            t.rectTransform.Place(14, 6, 364, h - 18);
            if (dis)
            {
                var st = UI.Txt(rt, "SHRUGGED OFF", 30, new Color(0.84f, 0.27f, 0.27f, 0.85f), TextAnchor.MiddleCenter, FontStyle.Bold);
                st.rectTransform.localRotation = Quaternion.Euler(0, 0, -8);
                UI.Outline(st, new Color(0.84f, 0.27f, 0.27f, 0.6f), 1);
            }
            if (i == highlight) StartCoroutine(PopIn(rt));
            evidenceCards.Add(rt);
        }
    }

    // =====================================================================
    // DIALOGUE
    // =====================================================================
    Line Insp(string t) => new Line { who = "INSPECTOR GLOOM", text = t, insp = true };
    Line You(string t) => new Line { who = cs.you.ToUpper(), text = t, you = true };
    Line Sys(string t) => new Line { who = "", text = t };

    void Say(Action done, params Line[] ls)
    {
        lines.Clear();
        foreach (var l in ls) lines.Enqueue(l);
        onLinesDone = done;
        phase = Phase.Talking;
        RefreshMain();
        AdvanceDialogue();
    }

    void AdvanceDialogue()
    {
        if (phase != Phase.Talking) return;
        if (typing)
        {
            if (typeCo != null) StopCoroutine(typeCo);
            typing = false; dlgText.text = fullText;
            return;
        }
        if (lines.Count > 0)
        {
            typeCo = StartCoroutine(TypeLine(lines.Dequeue()));
            return;
        }
        var d = onLinesDone; onLinesDone = null;
        d?.Invoke();
    }

    IEnumerator TypeLine(Line l)
    {
        typing = true;
        fullText = l.text;
        dlgName.text = l.who;
        dlgName.color = l.insp ? Pal.Gold : l.you ? Pal.Teal : Pal.Dim;
        dlgText.fontStyle = l.who == "" ? FontStyle.Italic : FontStyle.Normal;
        dlgText.color = l.who == "" ? Pal.Dim : Pal.Text;
        inspGroup.alpha = l.insp ? 1f : 0.35f;
        youGroup.alpha = l.you ? 1f : 0.35f;
        var speaker = l.insp ? inspGroup.transform : l.you ? youGroup.transform : null;
        if (speaker != null) StartCoroutine(Hop(speaker));
        int step = trailerDir != null ? 2 : 1;   // trailer types faster
        for (int i = 1; i <= fullText.Length; i += step)
        {
            if (i > fullText.Length) i = fullText.Length;
            dlgText.text = fullText.Substring(0, i);
            char c = fullText[i - 1];
            if (i % 2 == 0 && c != ' ' && l.who != "") Sfx.Blip(l.insp);
            yield return new WaitForSeconds(c == '.' || c == '?' || c == '!' ? 0.09f : 0.016f);
        }
        typing = false;
    }

    // =====================================================================
    // GAME LOGIC
    // =====================================================================
    void BeginDecide()
    {
        if (phase == Phase.Over) return;
        phase = Phase.Deciding;
        timer = TimeLimit;
        dlgName.text = "";
        dlgText.text = accused < 0
            ? "<i>Click a suspect to read their file and accuse them.</i>"
            : "<i>Stand firm, shift the blame - or shrug it off. Choose quickly...</i>";
        dlgText.fontStyle = FontStyle.Normal; dlgText.color = Pal.Dim;
        inspGroup.alpha = 1f; youGroup.alpha = 1f;
        RefreshMain();
    }

    void Accuse(int id)
    {
        CloseModal();
        if (phase != Phase.Deciding || cleared.Contains(id)) return;
        var s = cs.suspects[id];
        bool firm = id == accused;
        var pitch = firm ? You(Pick(FirmLines).Replace("{0}", s.name)) : You(s.pitch);

        int bad = -1;
        for (int e = 0; e < round; e++)
            if (!dismissed.Contains(e) && cs.evidence[e].rules.ContainsKey(id)) { bad = e; break; }

        if (bad >= 0) Say(() => Contradiction(id, bad), pitch);
        else Say(() => Accepted(id), pitch);
    }

    void Contradiction(int id, int e)
    {
        contradictions++;
        cleared.Add(id);
        if (accused == id) accused = -1;
        Sfx.Play("buzz");
        StartCoroutine(Stamp("CONTRADICTION!", Pal.Red));
        StartCoroutine(Shake(screen, 0.4f, 12));
        if (e < evidenceCards.Count) { int k = round - 1 - e; if (k >= 0 && k < evidenceCards.Count) StartCoroutine(Flash(evidenceCards[k])); }
        AddSuspicion(SuspContradiction);
        RefreshMain();
        if (phase == Phase.Over) return;
        Say(BeginDecide, Insp("Evidence #" + (e + 1) + ": " + cs.evidence[e].rules[id]), Insp(Pick(TryAgainLines)));
    }

    void Accepted(int id)
    {
        var s = cs.suspects[id];
        int gain = s.absurdity * 100;
        string reaction;
        if (accused == id)
        {
            AddSuspicion(SuspFirm);
            reaction = Pick(FirmReactions);
        }
        else
        {
            if (accused >= 0)
            {
                shifts++;
                AddSuspicion(SuspShift);
                if (s.absurdity > cs.suspects[accused].absurdity) { gain += ShiftBonus; StartCoroutine(Float("BLAME SHIFT BONUS +" + ShiftBonus, Pal.Teal, new Vector2(860, 380), 0.25f)); }
            }
            reaction = accusedBefore.Contains(id) ? Pick(ReturnReactions) : s.reaction;
            accused = id;
            accusedBefore.Add(id);
        }
        audacity += gain;
        UpdateAudacity();
        Sfx.Play("ding", 0.7f);
        StartCoroutine(Float("+" + gain + " AUDACITY", Pal.Gold, new Vector2(1100, 600), 0f));
        RefreshMain();
        if (phase == Phase.Over) return;
        Say(NextRound, Insp(reaction));
    }

    void NextRound()
    {
        if (round >= cs.evidence.Length)
        {
            phase = Phase.Over;
            StartCoroutine(CaseClosed());
            return;
        }
        round++;
        var ev = cs.evidence[round - 1];
        Sfx.Play("paper");
        RefreshEvidence(round - 1);
        AddSuspicion(SuspPerEvidence);
        if (phase == Phase.Over) return;
        var ls = new List<Line> { Insp(Pick(NewEvidenceLines)), Insp(ev.text) };
        if (ev.aside != null) ls.Add(Insp(ev.aside));
        Say(BeginDecide, ls.ToArray());
    }

    void Shrug()
    {
        if (phase != Phase.Deciding || shrugUsed || round == 0 || dismissed.Contains(round - 1)) return;
        shrugUsed = true;
        dismissed.Add(round - 1);
        Sfx.Play("stamp");
        RefreshEvidence(-1);
        AddSuspicion(SuspShrug);
        RefreshMain();
        if (phase == Phase.Over) return;
        Say(BeginDecide,
            You("Evidence #" + round + "? Pfft. That's obviously a smudge. Or a pigeon. Next."),
            Insp("...Fine. I'll disregard it. But I am watching you very closely."));
    }

    void Timeout()
    {
        CloseModal();
        AddSuspicion(SuspTimeout);
        Sfx.Play("buzz", 0.6f);
        StartCoroutine(Shake(screen, 0.25f, 6));
        if (phase == Phase.Over) return;
        Say(BeginDecide, Insp(Pick(ImpatientLines)));
    }

    void AddSuspicion(float d)
    {
        suspicion = Mathf.Clamp(suspicion + d, 0, 100);
        if (d > 0) StartCoroutine(Float("+" + Mathf.RoundToInt(d) + "% SUSPICION", Pal.Red, new Vector2(750, 600), 0.1f));
        else if (d < 0) StartCoroutine(Float(Mathf.RoundToInt(d) + "% SUSPICION", Pal.Teal, new Vector2(750, 600), 0.1f));
        if (suspicion >= 100 && phase != Phase.Over)
        {
            phase = Phase.Over;
            lines.Clear(); onLinesDone = null;
            StartCoroutine(Busted());
        }
    }

    string Pick(string[] a) => a[rng.Next(a.Length)];

    static readonly string[] FirmLines =
    {
        "I stand by it. It was {0}.", "Still {0}. Obviously.", "I've never been more sure of anything. {0}.",
        "{0}. Write it down. In pen.", "My story hasn't changed. {0} did it.",
    };
    static readonly string[] FirmReactions =
    {
        "Hm. Consistent. I'll give you that.", "The evidence doesn't rule them out. Yet.", "Your confidence is... noted.",
        "Annoyingly, that still fits.", "Very well. For now.",
    };
    static readonly string[] ReturnReactions =
    {
        "Back to them again, are we?", "Round and round we go.", "You've blamed them before. Make up your mind.",
    };
    static readonly string[] TryAgainLines =
    {
        "So it wasn't them. Who, then? Choose carefully.", "Contradicted by the evidence. That's rather suspicious of YOU.",
        "Wrong. You're sweating. Why are you sweating?", "Try again. And this time, try the truth. Ha. Joking. Unless...",
    };
    static readonly string[] NewEvidenceLines =
    {
        "New evidence has come to light!", "Aha. Forensics just sent this over.", "Well, well. Look what turned up.",
        "Hold on. Something new.", "My constable found this. He's very thorough. He's also a dog.",
    };
    static readonly string[] ImpatientLines =
    {
        "Tick tock. Silence is VERY suspicious.", "I haven't got all night. Well, I have. But you haven't.",
        "Lost for words? Interesting. Very interesting.", "Hello? Is this thing on? Is YOUR thing on?",
    };

    // =====================================================================
    // ENDINGS
    // =====================================================================
    string Rank(int score, CaseData c)
    {
        float max = 500f * (c.evidence.Length + 1);
        float k = score / max;
        if (k >= 0.9f) return "Grand Shifter Supreme";
        if (k >= 0.75f) return "Master of Misdirection";
        if (k >= 0.6f) return "Shameless Weasel";
        if (k >= 0.45f) return "Smooth Operator";
        return "Nervous Fibber";
    }

    IEnumerator CaseClosed()
    {
        Sfx.Play("stamp");
        StartCoroutine(Stamp("CASE CLOSED", Pal.Gold));
        yield return new WaitForSeconds(1.6f);
        Sfx.Play("fanfare");
        int best = PlayerPrefs.GetInt("best" + caseIndex, -1);
        bool record = audacity > best;
        if (record) { PlayerPrefs.SetInt("best" + caseIndex, audacity); PlayerPrefs.Save(); }

        NewScreen();
        phase = Phase.Over;
        var s = cs.suspects[accused];
        var h = UI.Txt(screen, "CASE CLOSED", 64, Pal.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
        h.rectTransform.Place(0, 620, 1280, 80); UI.Shadow(h, 4);
        var sub = UI.Txt(screen, "You got away with it. Somebody else did not.", 20, Pal.Dim, TextAnchor.MiddleCenter, FontStyle.Italic);
        sub.rectTransform.Place(0, 590, 1280, 30);

        var paper = UI.Img(screen, Pal.Paper, UI.Round, "Verdict");
        paper.rectTransform.Place(110, 150, 1060, 420); UI.Shadow(paper, 6, 0.5f);
        var pr = Portrait.Build(paper.transform, s.color, s.features, 170);
        pr.Center(-370, 60, 170, 170);
        var bars = UI.New("bars", paper.transform).Center(-370, 60, 200, 200);
        for (int i = 0; i < 5; i++) { var b = UI.Img(bars, new Color(0.15f, 0.15f, 0.18f, 0.85f), null, "bar"); b.rectTransform.Center(-80 + i * 40, 0, 8, 210); b.raycastTarget = false; }
        StartCoroutine(DropIn(bars));
        var nm = UI.Txt(paper.transform, s.name.ToUpper(), 26, Pal.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
        nm.rectTransform.Center(-370, -100, 300, 34); UI.Fit(nm, 14);
        var g = UI.Txt(paper.transform, "GUILTY", 34, Pal.Red, TextAnchor.MiddleCenter, FontStyle.Bold);
        g.rectTransform.Center(-370, -142, 300, 40); g.rectTransform.localRotation = Quaternion.Euler(0, 0, -6);
        UI.Outline(g, new Color(0.84f, 0.27f, 0.27f, 0.6f), 1);

        var vt = UI.Txt(paper.transform, s.verdict + "\n\n<i>" + cs.epilogue + "</i>", 20, Pal.Ink, TextAnchor.UpperLeft);
        vt.rectTransform.Place(340, 150, 680, 240);

        string rank = Rank(audacity, cs);
        var stats = UI.Txt(paper.transform,
            "AUDACITY: <b>" + audacity + "</b>" + (record ? "  <color=#d64545>NEW BEST!</color>" : "") +
            "\nSUSPICION: <b>" + Mathf.RoundToInt(suspicion) + "%</b>     SHIFTS: <b>" + shifts + "</b>     CONTRADICTIONS: <b>" + contradictions + "</b>\n" +
            "RANK: <b><color=#b0402f>" + rank.ToUpper() + "</color></b>",
            19, Pal.Ink, TextAnchor.UpperLeft);
        stats.rectTransform.Place(340, 24, 700, 96);

        if (caseIndex + 1 < Cases.All.Length)
        {
            int next = caseIndex + 1;
            var nb = UI.Btn(screen, "NEXT CASE  >", Pal.Red, () => ShowIntro(next), 22);
            nb.GetComponent<RectTransform>().Place(760, 56, 260, 56);
        }
        else
        {
            var fin = UI.Txt(screen, "You've gotten away with everything. Inspector Gloom is in jail. Thanks for playing!", 18, Pal.Gold, TextAnchor.MiddleCenter, FontStyle.Italic);
            fin.rectTransform.Place(0, 112, 1280, 30);
        }
        var rb = UI.Btn(screen, "RETRY CASE", Pal.PanelLight, () => StartCase(caseIndex), 18);
        rb.GetComponent<RectTransform>().Place(470, 56, 270, 56);
        var mb = UI.Btn(screen, "MENU", Pal.Panel, ShowTitle, 18);
        mb.GetComponent<RectTransform>().Place(260, 56, 190, 56);
    }

    IEnumerator Busted()
    {
        CloseModal();
        Sfx.Play("sting");
        StartCoroutine(Stamp("BUSTED!", Pal.Red));
        StartCoroutine(Shake(screen, 0.6f, 16));
        yield return new WaitForSeconds(1.8f);
        Sfx.Play("sadTrombone");

        NewScreen();
        phase = Phase.Over;
        var h = UI.Txt(screen, "BUSTED!", 96, Pal.Red, TextAnchor.MiddleCenter, FontStyle.Bold);
        h.rectTransform.Place(0, 570, 1280, 120); UI.Shadow(h, 5);
        var ip = Portrait.Build(screen, Cases.InspectorColor, Cases.InspectorFeatures, 150);
        ip.Center(-160, 90, 150, 150);
        var yp = Portrait.Build(screen, cs.youColor, cs.youFeatures, 150);
        yp.Center(160, 90, 150, 150);
        for (int i = 0; i < 5; i++) { var b = UI.Img(yp, new Color(0.1f, 0.1f, 0.12f, 0.9f), null, "bar"); b.rectTransform.Center(-64 + i * 32, 0, 7, 180); }
        var t = UI.Txt(screen, cs.busted, 24, Pal.Text, TextAnchor.MiddleCenter, FontStyle.Italic);
        t.rectTransform.Place(190, 190, 900, 120);
        var tip = UI.Txt(screen, "Tip: read the suspect files closely - every contradiction is hidden in there. And don't dither.", 16, Pal.Dim, TextAnchor.MiddleCenter);
        tip.rectTransform.Place(0, 140, 1280, 30);
        var rb = UI.Btn(screen, "TRY AGAIN", Pal.Red, () => StartCase(caseIndex), 22);
        rb.GetComponent<RectTransform>().Place(650, 60, 240, 56);
        var mb = UI.Btn(screen, "MENU", Pal.Panel, ShowTitle, 20);
        mb.GetComponent<RectTransform>().Place(390, 60, 240, 56);
    }

    // =====================================================================
    // MODALS
    // =====================================================================
    RectTransform OpenModal(float w, float h)
    {
        CloseModal();
        modal = UI.New("Modal", screen).Fill();
        var dim = UI.Img(modal, new Color(0, 0, 0, 0.65f), null, "Dim");
        dim.rectTransform.Fill();
        var cl = dim.gameObject.AddComponent<Clickable>(); cl.hoverScale = false; cl.onClick = CloseModal;
        var p = UI.Img(modal, Pal.Paper, UI.Round, "Panel");
        p.rectTransform.Center(0, 0, w, h);
        UI.Shadow(p, 8, 0.6f);
        StartCoroutine(PopIn(p.rectTransform));
        Sfx.Play("paper", 0.7f);
        return p.rectTransform;
    }

    void CloseModal()
    {
        if (modal != null) Destroy(modal.gameObject);
        modal = null;
    }

    void OpenDossier(int id)
    {
        var s = cs.suspects[id];
        var p = OpenModal(640, 440);
        var hdr = UI.Txt(p, "CASE FILE  #" + (caseIndex + 1) + "-" + (id + 1).ToString("00"), 14, Pal.Grey, TextAnchor.UpperLeft, FontStyle.Bold);
        hdr.rectTransform.Place(24, 404, 400, 24);
        var pr = Portrait.Build(p, s.color, s.features, 130);
        pr.Place(46, 262, 110, 110);
        var nm = UI.Txt(p, s.name, 32, Pal.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
        nm.rectTransform.Place(200, 340, 420, 44);
        var rl = UI.Txt(p, s.role, 18, Pal.Grey, TextAnchor.MiddleLeft, FontStyle.Italic);
        rl.rectTransform.Place(200, 310, 420, 28);
        var al = UI.Txt(p, "ABSURDITY", 13, Pal.Grey, TextAnchor.MiddleLeft, FontStyle.Bold);
        al.rectTransform.Place(200, 276, 100, 24);
        var dotsHolder = UI.New("dots", p).Place(290, 276, 110, 24);
        AbsurdityDots(dotsHolder, s.absurdity, 0, 0, 12, new Color(0, 0, 0, 0.15f));
        var pts = UI.Txt(p, "+" + (s.absurdity * 100) + " audacity / round", 13, new Color(0.6f, 0.45f, 0.15f), TextAnchor.MiddleLeft, FontStyle.Italic);
        pts.rectTransform.Place(410, 276, 210, 24);

        var dt = UI.Txt(p, s.dossier, 20, Pal.Ink, TextAnchor.UpperLeft); UI.Fit(dt, 14);
        dt.rectTransform.Place(36, 92, 568, 136);

        if (cleared.Contains(id))
        {
            var st = UI.Txt(p, "CLEARED BY THE EVIDENCE", 30, Pal.Red, TextAnchor.MiddleCenter, FontStyle.Bold);
            st.rectTransform.Place(0, 24, 470, 56); st.rectTransform.localRotation = Quaternion.Euler(0, 0, -4);
            UI.Outline(st, new Color(0.84f, 0.27f, 0.27f, 0.5f), 1);
        }
        else if (phase == Phase.Deciding)
        {
            string label = id == accused ? "STAND FIRM" : accused < 0 ? "ACCUSE!" : "SHIFT THE BLAME!";
            var b = UI.Btn(p, label, Pal.Red, () => Accuse(id), 24);
            b.GetComponent<RectTransform>().Place(36, 24, 400, 56);
        }
        else
        {
            var w = UI.Txt(p, "(wait for the Inspector to finish talking...)", 17, Pal.Grey, TextAnchor.MiddleLeft, FontStyle.Italic);
            w.rectTransform.Place(36, 24, 420, 56);
        }
        var back = UI.Btn(p, "BACK", Pal.Grey, CloseModal, 20);
        back.GetComponent<RectTransform>().Place(470, 24, 134, 56);
    }

    // =====================================================================
    // JUICE
    // =====================================================================
    IEnumerator PopIn(RectTransform rt)
    {
        float t = 0;
        while (t < 0.25f && rt != null)
        {
            t += Time.deltaTime;
            float k = t / 0.25f;
            float s = 1 + Mathf.Sin(k * Mathf.PI) * 0.12f;
            s *= Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(k * 2));
            rt.localScale = new Vector3(s, s, 1);
            yield return null;
        }
        if (rt != null) rt.localScale = Vector3.one;
    }

    IEnumerator Hop(Transform tr)
    {
        float t = 0;
        while (t < 0.2f && tr != null)
        {
            t += Time.deltaTime;
            float s = 1 + Mathf.Sin(t / 0.2f * Mathf.PI) * 0.08f;
            tr.localScale = new Vector3(s, s, 1);
            yield return null;
        }
        if (tr != null) tr.localScale = Vector3.one;
    }

    IEnumerator Shake(RectTransform rt, float dur, float mag)
    {
        if (rt == null) yield break;
        float t = 0;
        while (t < dur && rt != null)
        {
            t += Time.deltaTime;
            float m = mag * (1 - t / dur);
            rt.anchoredPosition = new Vector2(UnityEngine.Random.Range(-m, m), UnityEngine.Random.Range(-m, m));
            yield return null;
        }
        if (rt != null) rt.anchoredPosition = Vector2.zero;
    }

    IEnumerator Flash(RectTransform rt)
    {
        var img = rt.GetComponent<Image>();
        var orig = img.color;
        for (int i = 0; i < 6 && img != null; i++)
        {
            img.color = i % 2 == 0 ? new Color(1f, 0.6f, 0.6f) : orig;
            yield return new WaitForSeconds(0.12f);
        }
        if (img != null) img.color = orig;
    }

    IEnumerator Stamp(string text, Color c)
    {
        var holder = UI.New("Stamp", screen).Fill();
        var t = UI.Txt(holder, text, 92, c, TextAnchor.MiddleCenter, FontStyle.Bold);
        UI.Outline(t, new Color(0, 0, 0, 0.8f), 4);
        holder.localRotation = Quaternion.Euler(0, 0, -8);
        Sfx.Play("stamp");
        float time = 0;
        while (time < 1.4f)
        {
            time += Time.deltaTime;
            float s = time < 0.15f ? Mathf.Lerp(3f, 1f, time / 0.15f) : 1f;
            holder.localScale = new Vector3(s, s, 1);
            var col = c; col.a = time > 1.0f ? 1 - (time - 1.0f) / 0.4f : 1;
            t.color = col;
            yield return null;
        }
        Destroy(holder.gameObject);
    }

    IEnumerator Float(string text, Color c, Vector2 pos, float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);
        int slot = floatsActive++;
        var rt = UI.New("Float", screen).Place(pos.x - 200, pos.y - slot * 32, 400, 40);
        var t = UI.Txt(rt, text, 22, c, TextAnchor.MiddleCenter, FontStyle.Bold);
        UI.Outline(t, new Color(0, 0, 0, 0.8f), 2);
        float time = 0;
        while (time < 1.2f)
        {
            time += Time.deltaTime;
            rt.anchoredPosition += new Vector2(0, 40 * Time.deltaTime);
            var col = c; col.a = 1 - Mathf.Clamp01((time - 0.6f) / 0.6f);
            t.color = col;
            yield return null;
        }
        floatsActive--;
        Destroy(rt.gameObject);
    }

    IEnumerator DropIn(RectTransform rt)
    {
        var target = rt.anchoredPosition;
        float t = 0;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / 0.5f);
            float bounce = 1 - Mathf.Abs(Mathf.Cos(k * Mathf.PI * 2.5f)) * (1 - k);
            rt.anchoredPosition = target + new Vector2(0, (1 - bounce) * 400);
            yield return null;
        }
        rt.anchoredPosition = target;
        Sfx.Play("stamp");
    }
}
