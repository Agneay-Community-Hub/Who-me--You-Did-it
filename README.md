# Who, Me?

*A comedy detective game where **you** did it, and someone else is going down for it.*

Made for **GitHub Game Off 2026**. Theme: **SHIFT**, as in shifting the blame.

You're the culprit. Inspector Prudence Gloom is on the case. Accuse a suspect, then watch the
Inspector bring out new evidence after every accusation. If your scapegoat contradicts **any**
evidence on the board, they're cleared and her suspicion of you goes up. Keep **shifting the blame**
onto more and more unlikely suspects (the butler, the corgi, the robot vacuum, a 224-year-old
oil painting...) and never contradict the facts.

## How to play

| Action | What it does |
|---|---|
| **Click a suspect** | Opens their case file. The clues are in the file. |
| **ACCUSE / SHIFT THE BLAME** | Blames that suspect. More absurd suspects earn more **Audacity**. |
| **STAND FIRM** | Keeps blaming your current scapegoat (lowers Suspicion a little). |
| **SHRUG IT OFF** | Once per case, dismisses the newest evidence. Costs Suspicion. |
| **Space / click** | Advances dialogue. **M** toggles music. |

- A contradiction adds **+20% Suspicion** and clears that suspect.
- Every new piece of evidence adds **+5%**. Shifting adds **+3%**. Taking too long adds **+12%**.
- Shifting to a *sillier* suspect earns a **Blame Shift Bonus**.
- If Suspicion reaches **100%**, you're **BUSTED**.

### Cases
1. **The Midnight Cake**: you ate the Duchess's 100th birthday cake.
2. **The Moonstone Swap**: you swapped a priceless gem for a gumball.
3. **Lights Out at Puffin Point**: you switched off the lighthouse to get some sleep.

## Tech

- **Unity 6 (6000.4.3f1)** with uGUI. The project was created and built entirely from the Unity CLI.
- **No imported assets.** All art (cartoon portraits with googly eyes that follow your cursor) is
  built in code from circles and rounded rectangles. All audio (a noir walking-bass loop, SFX,
  sad trombone) is synthesised when the game starts. See `Assets/Scripts/Sfx.cs`.
- Everything is created at runtime from `Game.cs`. The only scene is an empty camera.

```
WhoMe/Assets/Scripts/Game.cs      game flow, UI, rules
WhoMe/Assets/Scripts/Cases.cs     all case content (suspects, evidence, jokes)
WhoMe/Assets/Scripts/Portrait.cs  procedural character faces
WhoMe/Assets/Scripts/Sfx.cs       procedural audio
WhoMe/Assets/Scripts/UIKit.cs     UI helpers, sprites, small effects
WhoMe/Assets/Editor/Builder.cs    CLI build + content validation
```

### Building from the command line

```bash
UNITY="C:/Program Files/Unity/Hub/Editor/6000.4.3f1/Editor/Unity.exe"
# check that every case stays solvable after each piece of evidence
"$UNITY" -batchmode -quit -nographics -projectPath WhoMe -executeMethod Builder.Validate -logFile -
# WebGL build (for itch.io) -> WhoMe/Builds/WebGL
"$UNITY" -batchmode -quit -nographics -projectPath WhoMe -executeMethod Builder.BuildWebGL -logFile -
# Windows build -> WhoMe/Builds/Windows/WhoMe.exe
"$UNITY" -batchmode -quit -nographics -projectPath WhoMe -executeMethod Builder.BuildWindows -logFile -
```

### Automated playthrough

The Windows build has a scripted test mode. It plays a winning run of Case 1 (contradiction,
shrug, blame-shift bonus, verdict) and a losing run of Case 3 (BUSTED), checks the rules at
each step, and saves screenshots:

```bash
WhoMe/Builds/Windows/WhoMe.exe -autotest shots -screen-width 1280 -screen-height 720 -screen-fullscreen 0 -logFile auto.log
# look for "[AUTO] ALL PASSED" in auto.log
```

### Recording the trailer

The Windows build can also record a scripted trailer at a locked 30 fps. It saves every frame and
mixes every sound it played into a WAV file:

```bash
WhoMe/Builds/Windows/WhoMe.exe -trailer trailer -screen-width 1280 -screen-height 720 -screen-fullscreen 0
ffmpeg -framerate 30 -i trailer/f%05d.jpg -i trailer/audio.wav -vf "scale=1920:1080:flags=lanczos,format=yuv420p" -c:v libx264 -crf 18 -af loudnorm=I=-16:TP=-1.5 -c:a aac -b:a 192k -shortest WhoMe-Trailer.mp4
```

### Rendering the thumbnail / cover art

```bash
# 16:9 thumbnail (YouTube, social)
WhoMe/Builds/Windows/WhoMe.exe -thumbnail WhoMe-Thumbnail.png -screen-width 1280 -screen-height 720 -screen-fullscreen 0
# itch.io cover: render at 945x750, then scale to 630x500
WhoMe/Builds/Windows/WhoMe.exe -thumbnail cover.png -screen-width 945 -screen-height 750 -screen-fullscreen 0
ffmpeg -i cover.png -vf scale=630:500:flags=lanczos WhoMe-Cover.png
```

To publish on itch.io, zip the **contents** of `WhoMe/Builds/WebGL` (so `index.html` is at the
root of the zip), upload it as an HTML project, and set the viewport to 1280 × 720.

### Adding a case

Add a new `CaseData` entry to `Cases.All` in `Cases.cs`. Each `Evidence.rules` entry maps a
suspect index to the Inspector's rebuttal. Make sure at least one suspect (ideally the most
absurd one) survives every piece of evidence, then run `Builder.Validate`.
