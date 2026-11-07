using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class Pal
{
    public static readonly Color Bg = new Color(0.07f, 0.07f, 0.11f);
    public static readonly Color Panel = new Color(0.12f, 0.12f, 0.19f);
    public static readonly Color PanelLight = new Color(0.18f, 0.18f, 0.27f);
    public static readonly Color Paper = new Color(0.94f, 0.89f, 0.78f);
    public static readonly Color PaperDark = new Color(0.86f, 0.80f, 0.66f);
    public static readonly Color Ink = new Color(0.17f, 0.13f, 0.09f);
    public static readonly Color Cork = new Color(0.42f, 0.29f, 0.18f);
    public static readonly Color Red = new Color(0.84f, 0.27f, 0.27f);
    public static readonly Color Gold = new Color(0.89f, 0.69f, 0.29f);
    public static readonly Color Teal = new Color(0.31f, 0.70f, 0.66f);
    public static readonly Color Grey = new Color(0.45f, 0.45f, 0.52f);
    public static readonly Color Text = new Color(0.92f, 0.91f, 0.95f);
    public static readonly Color Dim = new Color(0.62f, 0.62f, 0.70f);
}

public static class UI
{
    static Font _font;
    public static Font Font => _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

    static Sprite _round, _circle, _vignette;
    public static Sprite Round => _round != null ? _round : (_round = MakeRounded(64, 16));
    public static Sprite Circle => _circle != null ? _circle : (_circle = MakeCircle(128));
    public static Sprite Vignette => _vignette != null ? _vignette : (_vignette = MakeVignette(256));

    static Sprite MakeRounded(int size, int r)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float fx = x + 0.5f, fy = y + 0.5f;
                float cx = Mathf.Clamp(fx, r, size - r), cy = Mathf.Clamp(fy, r, size - r);
                float d = Vector2.Distance(new Vector2(fx, fy), new Vector2(cx, cy));
                byte a = (byte)(Mathf.Clamp01(r - d + 0.5f) * 255);
                px[y * size + x] = new Color32(255, 255, 255, a);
            }
        t.SetPixels32(px); t.Apply();
        return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
    }

    static Sprite MakeCircle(int size)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[size * size];
        float c = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(c - d + 0.5f) * 255));
            }
        t.SetPixels32(px); t.Apply();
        return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    static Sprite MakeVignette(int size)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[size * size];
        float c = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / (c * 1.414f);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 1f, d)) * 0.9f;
                px[y * size + x] = new Color32(0, 0, 0, (byte)(a * 255));
            }
        t.SetPixels32(px); t.Apply();
        return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    public static RectTransform New(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    /// Place by bottom-left corner (x,y) and size, relative to the parent's bottom-left.
    public static RectTransform Place(this RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x + w / 2f, y + h / 2f);
        return rt;
    }

    /// Place by center relative to the parent's center.
    public static RectTransform Center(this RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);
        return rt;
    }

    public static RectTransform Fill(this RectTransform rt, float pad = 0)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(pad, pad); rt.offsetMax = new Vector2(-pad, -pad);
        return rt;
    }

    public static Image Img(Transform parent, Color c, Sprite s = null, string name = "Img")
    {
        var rt = New(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = s; img.color = c;
        if (s != null && s.border != Vector4.zero) img.type = Image.Type.Sliced;
        return img;
    }

    public static Text Txt(Transform parent, string s, int size, Color c, TextAnchor a = TextAnchor.MiddleCenter, FontStyle st = FontStyle.Normal)
    {
        var rt = New("Text", parent);
        rt.Fill();
        var t = rt.gameObject.AddComponent<Text>();
        t.font = Font; t.text = s; t.fontSize = size; t.color = c; t.alignment = a; t.fontStyle = st;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.supportRichText = true; t.raycastTarget = false; t.lineSpacing = 1.05f;
        return t;
    }

    public static Button Btn(Transform parent, string label, Color bg, Action click, int size = 22, Color? textCol = null)
    {
        var img = Img(parent, bg, Round, "Btn:" + label);
        var b = img.gameObject.AddComponent<Button>();
        var cb = b.colors;
        cb.normalColor = Color.white; cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f); cb.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        cb.disabledColor = new Color(0.4f, 0.4f, 0.45f, 0.45f); cb.colorMultiplier = 1.2f;
        b.colors = cb;
        b.onClick.AddListener(() => { Sfx.Play("click"); click?.Invoke(); });
        var t = Txt(img.transform, label, size, textCol ?? Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        t.rectTransform.Fill(6);
        img.gameObject.AddComponent<ButtonFx>();
        return b;
    }

    public static void Shadow(Graphic g, float d = 3, float a = 0.6f)
    {
        var s = g.gameObject.AddComponent<Shadow>();
        s.effectColor = new Color(0, 0, 0, a); s.effectDistance = new Vector2(d, -d);
    }

    public static void Outline(Graphic g, Color c, float d = 2)
    {
        var s = g.gameObject.AddComponent<Outline>();
        s.effectColor = c; s.effectDistance = new Vector2(d, -d);
    }

    public static void Fit(Text t, int min)
    {
        t.resizeTextForBestFit = true;
        t.resizeTextMinSize = min;
        t.resizeTextMaxSize = t.fontSize;
        t.verticalOverflow = VerticalWrapMode.Truncate;
    }
}

public class ButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    float target = 1f;
    Button b;
    void Awake() { b = GetComponent<Button>(); }
    public void OnPointerEnter(PointerEventData e)
    {
        if (b != null && !b.interactable) return;
        target = 1.05f; Sfx.Play("hover", 0.5f);
    }
    public void OnPointerExit(PointerEventData e) { target = 1f; }
    void Update()
    {
        float s = Mathf.Lerp(transform.localScale.x, target, Time.unscaledDeltaTime * 14f);
        transform.localScale = new Vector3(s, s, 1);
    }
}

/// Generic clickable area (for cards / dialogue box).
public class Clickable : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Action onClick;
    public bool hoverScale = true;
    float target = 1f;
    public void OnPointerClick(PointerEventData e) { onClick?.Invoke(); }
    public void OnPointerEnter(PointerEventData e) { if (hoverScale) { target = 1.04f; Sfx.Play("hover", 0.4f); } }
    public void OnPointerExit(PointerEventData e) { target = 1f; }
    void Update()
    {
        if (!hoverScale) return;
        float s = Mathf.Lerp(transform.localScale.x, target, Time.unscaledDeltaTime * 14f);
        transform.localScale = new Vector3(s, s, 1);
    }
}

/// Pupils that follow the mouse. The eyes are watching. Especially the portrait's.
public class GooglyEye : MonoBehaviour
{
    public float radius = 4f;
    /// When set, the pupil stares in this direction instead of following the mouse (used for stills).
    public Vector2? fixedDir;
    RectTransform rt;
    void Awake() { rt = (RectTransform)transform; }
    void Update()
    {
        if (fixedDir.HasValue) { rt.anchoredPosition = fixedDir.Value.normalized * radius; return; }
        var parent = (RectTransform)rt.parent;
        Vector2 eye = RectTransformUtility.WorldToScreenPoint(null, parent.position);
        Vector2 d = (Vector2)Input.mousePosition - eye;
        float k = Mathf.Clamp01(d.magnitude / 220f);
        rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, d.normalized * radius * k, Time.unscaledDeltaTime * 12f);
    }
}

public class Bob : MonoBehaviour
{
    public float amp = 6f, speed = 2f, phase;
    Vector2 basePos; bool init;
    void Update()
    {
        var rt = (RectTransform)transform;
        if (!init) { basePos = rt.anchoredPosition; init = true; }
        rt.anchoredPosition = basePos + new Vector2(0, Mathf.Sin(Time.time * speed + phase) * amp);
    }
}

/// Noir rain streaks behind everything.
public class Rain : MonoBehaviour
{
    RectTransform[] drops; float[] speeds;
    void Start()
    {
        int n = 60;
        drops = new RectTransform[n]; speeds = new float[n];
        for (int i = 0; i < n; i++)
        {
            var img = UI.Img(transform, new Color(0.6f, 0.65f, 0.85f, UnityEngine.Random.Range(0.04f, 0.12f)), null, "drop");
            img.raycastTarget = false;
            drops[i] = img.rectTransform;
            drops[i].anchorMin = drops[i].anchorMax = Vector2.zero;
            drops[i].sizeDelta = new Vector2(2, UnityEngine.Random.Range(30, 70));
            drops[i].localRotation = Quaternion.Euler(0, 0, 12);
            drops[i].anchoredPosition = new Vector2(UnityEngine.Random.Range(0, 1600), UnityEngine.Random.Range(0, 900));
            speeds[i] = UnityEngine.Random.Range(500, 900);
        }
    }
    void Update()
    {
        var size = ((RectTransform)transform).rect.size;
        for (int i = 0; i < drops.Length; i++)
        {
            var p = drops[i].anchoredPosition;
            p += new Vector2(-0.21f, -1f) * speeds[i] * Time.deltaTime;
            if (p.y < -80) p = new Vector2(UnityEngine.Random.Range(0, size.x + 200), size.y + 80);
            drops[i].anchoredPosition = p;
        }
    }
}
