using UnityEngine;
using UnityEngine.UI;

/// Builds little procedural cartoon faces out of circles and rounded rects.
/// Features are comma separated tags, e.g. "mustache,monocle,tophat".
public static class Portrait
{
    public static RectTransform Build(Transform parent, Color skin, string features, float s)
    {
        var root = UI.New("Portrait", parent);
        root.sizeDelta = new Vector2(s, s);
        string f = "," + (features ?? "") + ",";
        bool Has(string k) => f.Contains("," + k + ",");
        Color dark = new Color(0.12f, 0.1f, 0.1f);
        Color skinDark = skin * 0.72f; skinDark.a = 1;

        // --- behind the head ---
        if (Has("frame"))
        {
            Add(root, Pal.Gold, UI.Round, 0, 0, s * 1.22f, s * 1.22f);
            Add(root, new Color(0.25f, 0.16f, 0.1f), UI.Round, 0, 0, s * 1.08f, s * 1.08f);
        }
        if (Has("trench"))
        {
            Add(root, new Color(0.55f, 0.45f, 0.3f), UI.Round, 0, -s * 0.52f, s * 0.95f, s * 0.35f);
        }
        if (Has("claws"))
        {
            Add(root, skinDark, UI.Circle, -s * 0.55f, -s * 0.05f, s * 0.32f, s * 0.32f);
            Add(root, skinDark, UI.Circle, s * 0.55f, -s * 0.05f, s * 0.32f, s * 0.32f);
        }
        if (Has("ears"))
        {
            Add(root, skinDark, null, -s * 0.32f, s * 0.36f, s * 0.3f, s * 0.3f, 45);
            Add(root, skinDark, null, s * 0.32f, s * 0.36f, s * 0.3f, s * 0.3f, 45);
        }
        if (Has("wisps"))
        {
            var w = new Color(skin.r, skin.g, skin.b, 0.45f);
            Add(root, w, UI.Circle, -s * 0.45f, s * 0.2f, s * 0.45f, s * 0.45f);
            Add(root, w, UI.Circle, s * 0.48f, -s * 0.1f, s * 0.5f, s * 0.5f);
            Add(root, w, UI.Circle, s * 0.1f, -s * 0.48f, s * 0.4f, s * 0.4f);
        }

        // --- head ---
        Add(root, skin, UI.Circle, 0, 0, s, s);

        // --- before eyes ---
        if (Has("mask")) Add(root, dark, UI.Round, 0, s * 0.08f, s * 0.86f, s * 0.24f);
        if (Has("glasses"))
        {
            Add(root, dark, UI.Circle, -s * 0.2f, s * 0.08f, s * 0.33f, s * 0.33f);
            Add(root, dark, UI.Circle, s * 0.2f, s * 0.08f, s * 0.33f, s * 0.33f);
        }
        if (Has("monocle")) Add(root, Pal.Gold, UI.Circle, s * 0.2f, s * 0.08f, s * 0.35f, s * 0.35f);
        if (Has("craters"))
        {
            Add(root, skinDark, UI.Circle, -s * 0.25f, -s * 0.25f, s * 0.16f, s * 0.16f);
            Add(root, skinDark, UI.Circle, s * 0.28f, -s * 0.18f, s * 0.1f, s * 0.1f);
            Add(root, skinDark, UI.Circle, s * 0.05f, s * 0.33f, s * 0.12f, s * 0.12f);
        }

        // --- eyes ---
        for (int i = -1; i <= 1; i += 2)
        {
            var eye = Add(root, Color.white, UI.Circle, i * s * 0.2f, s * 0.08f, s * 0.26f, s * 0.26f);
            var pupil = Add(eye, dark, UI.Circle, 0, 0, s * 0.12f, s * 0.12f);
            pupil.gameObject.AddComponent<GooglyEye>().radius = s * 0.055f;
        }

        // --- mouth & face front ---
        if (Has("beak")) Add(root, new Color(0.98f, 0.62f, 0.2f), null, 0, -s * 0.13f, s * 0.17f, s * 0.17f, 45);
        else Add(root, dark, UI.Round, 0, -s * 0.24f, s * 0.28f, s * 0.06f);
        if (Has("mustache")) Add(root, new Color(0.3f, 0.2f, 0.12f), UI.Round, 0, -s * 0.14f, s * 0.48f, s * 0.11f);
        if (Has("frosting")) Add(root, new Color(1f, 0.55f, 0.75f), UI.Circle, s * 0.14f, -s * 0.12f, s * 0.12f, s * 0.12f);
        if (Has("beard")) Add(root, new Color(0.95f, 0.95f, 0.95f), UI.Circle, 0, -s * 0.36f, s * 0.62f, s * 0.38f);
        if (Has("bowtie"))
        {
            Add(root, dark, null, -s * 0.1f, -s * 0.55f, s * 0.16f, s * 0.16f, 45);
            Add(root, dark, null, s * 0.1f, -s * 0.55f, s * 0.16f, s * 0.16f, 45);
        }

        // --- on top ---
        if (Has("tophat"))
        {
            Add(root, dark, null, 0, s * 0.62f, s * 0.48f, s * 0.42f);
            Add(root, dark, UI.Round, 0, s * 0.41f, s * 0.78f, s * 0.08f);
        }
        if (Has("fedora"))
        {
            Add(root, new Color(0.22f, 0.2f, 0.22f), UI.Round, 0, s * 0.5f, s * 0.6f, s * 0.3f);
            Add(root, Pal.Red, null, 0, s * 0.41f, s * 0.6f, s * 0.06f);
            Add(root, new Color(0.22f, 0.2f, 0.22f), UI.Round, 0, s * 0.37f, s * 1.0f, s * 0.08f);
        }
        if (Has("cap"))
        {
            Add(root, new Color(0.2f, 0.32f, 0.6f), UI.Round, 0, s * 0.42f, s * 0.72f, s * 0.24f);
            Add(root, new Color(0.15f, 0.24f, 0.45f), UI.Round, s * 0.26f, s * 0.32f, s * 0.46f, s * 0.08f);
        }
        if (Has("chef"))
        {
            Add(root, Color.white, UI.Circle, 0, s * 0.58f, s * 0.62f, s * 0.42f);
            Add(root, new Color(0.92f, 0.92f, 0.92f), UI.Round, 0, s * 0.4f, s * 0.56f, s * 0.14f);
        }
        if (Has("crown"))
        {
            for (int i = -1; i <= 1; i++) Add(root, Pal.Gold, null, i * s * 0.17f, s * 0.52f + (i == 0 ? s * 0.05f : 0), s * 0.15f, s * 0.15f, 45);
            Add(root, Pal.Gold, null, 0, s * 0.44f, s * 0.5f, s * 0.08f);
        }
        if (Has("laurel"))
        {
            var g = new Color(0.45f, 0.7f, 0.35f);
            for (int i = -2; i <= 2; i++)
                Add(root, g, UI.Circle, i * s * 0.17f, s * 0.42f - Mathf.Abs(i) * s * 0.06f, s * 0.16f, s * 0.1f, i * -15);
        }
        if (Has("beret")) Add(root, dark, UI.Round, -s * 0.06f, s * 0.46f, s * 0.72f, s * 0.2f, 8);
        if (Has("bow"))
        {
            var c = new Color(1f, 0.45f, 0.65f);
            Add(root, c, null, -s * 0.13f, s * 0.44f, s * 0.2f, s * 0.2f, 45);
            Add(root, c, null, s * 0.13f, s * 0.44f, s * 0.2f, s * 0.2f, 45);
            Add(root, c * 0.8f, UI.Circle, 0, s * 0.44f, s * 0.12f, s * 0.12f);
        }
        if (Has("antenna"))
        {
            Add(root, dark, null, 0, s * 0.6f, s * 0.04f, s * 0.24f);
            Add(root, Pal.Red, UI.Circle, 0, s * 0.72f, s * 0.12f, s * 0.12f);
        }
        if (Has("bandana")) Add(root, Pal.Red, UI.Round, 0, s * 0.36f, s * 0.8f, s * 0.14f, -5);

        return root;
    }

    static RectTransform Add(RectTransform parent, Color c, Sprite s, float x, float y, float w, float h, float rot = 0)
    {
        var img = UI.Img(parent, c, s, "part");
        img.raycastTarget = false;
        var rt = img.rectTransform.Center(x, y, w, h);
        if (rot != 0) rt.localRotation = Quaternion.Euler(0, 0, rot);
        return rt;
    }
}
