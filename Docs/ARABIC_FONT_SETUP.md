# Arabic Font Setup

> **Symptom:** Arabic letters appear as **square boxes** (□□□□) inside the
> game when you switch the language to Arabic.
>
> **Cause:** TextMeshPro needs a real TTF (or OTF) file with Arabic glyph
> outlines to build its SDF font atlas. The default `LiberationSans` font
> that ships with TMP Essentials only covers Latin characters, and the
> `Font.CreateDynamicFontFromOSFont` runtime fallback exposes an OS font
> *handle* but not the underlying glyph data — so TMP draws every Arabic
> codepoint as the "missing glyph" tofu box.
>
> **Fix:** drop a free Arabic TTF into `Assets/Resources/Fonts/`. That's it.
> The code auto-detects the font, builds a `TMP_FontAsset` at runtime, and
> registers it as a global TMP fallback so **every** text component
> (localized or not) can render Arabic glyphs.

---

## 1. Download a free Arabic font (30 seconds)

We recommend **Noto Sans Arabic** — it's free, has full Arabic coverage, and
looks clean on phone screens.

1. Open https://fonts.google.com/noto/specimen/Noto+Sans+Arabic in any browser.
2. Click **Get font** → **Download all**.
3. Unzip the downloaded `Noto_Sans_Arabic.zip`.
4. Inside the unzipped folder, find `static/NotoSansArabic-Regular.ttf`
   (or one of the other weight variants — the code searches for the
   *Regular* variant first).

Alternatives the code also recognizes — use any one of these:

| Font | Style | Size | URL |
|---|---|---|---|
| **Noto Sans Arabic** | Clean sans-serif (recommended) | ~190 KB | https://fonts.google.com/noto/specimen/Noto+Sans+Arabic |
| **Cairo** | Geometric / modern | ~150 KB | https://fonts.google.com/specimen/Cairo |
| **Amiri** | Classical / traditional | ~480 KB | https://fonts.google.com/specimen/Amiri |
| **Noto Naskh Arabic** | Traditional naskh | ~180 KB | https://fonts.google.com/noto/specimen/Noto+Naskh+Arabic |

All four are licensed under the **SIL Open Font License** which lets you
bundle them in commercial apps for free.

## 2. Drop it into Unity (10 seconds)

1. In the Unity **Project window**, navigate to `Assets/`.
2. If `Resources/` doesn't exist yet, create it: right-click `Assets/` →
   **Create** → **Folder** → name it `Resources`.
3. Inside `Resources/`, create another folder named `Fonts` (same right-click
   menu).
4. Drag the `NotoSansArabic-Regular.ttf` file from Finder/Explorer onto the
   `Assets/Resources/Fonts/` folder in the Project window.

Unity will import the file automatically. You should now see
`NotoSansArabic-Regular.ttf` listed under `Assets/Resources/Fonts/`.

## 3. Test (10 seconds)

1. Open `Assets/Scenes/Bootstrap.unity`.
2. Press ▶ **Play**.
3. Go to Settings (⚙) and tap **العربية**.
4. All Arabic text should now render as proper Arabic letters instead of
   square boxes.

### Console check

Look in the Unity Console for one of these messages — they confirm the
font was found and registered:

```
[Localization] Created TMP font from Fonts/NotoSansArabic-Regular
[Localization] Registered Fonts/NotoSansArabic-Regular SDF (Runtime) as TMP global fallback. All TMP texts can now render Arabic glyphs.
```

If instead you see:

```
[Localization] *** Arabic text will appear as SQUARE BOXES ***
```

your `Resources/Fonts/` folder is empty or the file name doesn't match
one of the expected names. Re-check step 2.

## 4. Rebuild for Android / iOS

The TTF lives under `Assets/Resources/`, so Unity automatically bundles
it with every build. Just rebuild your APK / iOS Xcode project and
install on the device — no extra steps.

---

## (Optional, better quality) Pre-bake an SDF atlas

The runtime conversion via `TMP_FontAsset.CreateFontAsset(ttf)` works
fine but generates the SDF atlas on first use, which costs a frame or
two of stutter the first time an Arabic character appears. For shipped
builds you can pre-bake the atlas:

1. Window → **TextMeshPro** → **Font Asset Creator**.
2. **Source Font File:** drag `NotoSansArabic-Regular.ttf`.
3. **Atlas Resolution:** 1024 × 1024.
4. **Character Set:** **Unicode Range (Hex)** → paste:
   ```
   0020-007E,00A0-00FF,0600-06FF,FE70-FEFF
   ```
   That covers basic Latin + Latin supplement + Arabic + Arabic
   presentation forms (the contextual letter shapes needed for proper
   Arabic text shaping).
5. **Render Mode:** SDFAA.
6. Click **Generate Font Atlas**, wait ~10 seconds.
7. Click **Save** and save the file as
   `Assets/Resources/Fonts/Arabic SDF.asset`.

The code looks for `Resources/Fonts/Arabic SDF` first, so this
pre-baked asset takes priority over the runtime-converted TTF.

---

## How the code finds the font

In `LocalizationManager.cs`, `Localization.ArabicFont` tries these paths
in order:

1. `Resources.Load<TMP_FontAsset>("Fonts/Arabic SDF")` — the preauthored
   asset.
2. `Resources.Load<Font>("Fonts/NotoSansArabic-Regular")` and similar
   names — raw TTF, converted to a TMP font asset at runtime.
3. `Font.CreateDynamicFontFromOSFont(...)` — last-resort OS font (the
   one that caused the square boxes you saw).

Whenever any of the above succeeds, the font is added to
`TMP_Settings.instance.fallbackFontAssets` so **every** TMP text in the
project can render Arabic glyphs through the global TMP fallback chain.
This means even strings that weren't routed through `Localization.T()`
(e.g. user input, debug messages) will render correctly if they happen
to contain Arabic characters.
