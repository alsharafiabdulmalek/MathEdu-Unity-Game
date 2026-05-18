// -----------------------------------------------------------------------------
// ArabicShaper.cs
// -----------------------------------------------------------------------------
// Converts logical-order Arabic text into pre-shaped presentation-form text
// that TextMeshPro can render as proper connected (cursive) Arabic.
//
// Why this exists:
//   Arabic letters change form depending on their position in a word:
//     • isolated (alone, no neighbours)             U+FE80..U+FEFF range
//     • initial  (start of word)
//     • medial   (middle of word)
//     • final    (end of word)
//   plus the lam-alef ligatures (لا لأ لإ لآ).
//
//   Plain Unicode codepoints U+0621..U+06FF only carry the isolated form.
//   If you hand them to TMP directly, each letter is drawn as a separate
//   disconnected glyph — exactly the symptom the user reported.
//
//   This shaper walks the string, decides each letter's contextual form,
//   substitutes the matching presentation-form codepoint from U+FE70..U+FEFC,
//   and merges lam-alef pairs into a single ligature glyph. The result is
//   then handed to TMP which renders the correct connected script.
//
// Usage:
//
//     string raw     = "\u0645\u0631\u062d\u0628\u0627";   // "مرحبا"
//     string shaped  = ArabicShaper.Shape(raw);           // 5 letters -> connected glyphs
//
// Localization.T() automatically runs every translated string through this
// shaper when the active language is Arabic, so callers don't need to know
// about it.
//
// Implementation notes:
//   • The lookup table covers the 28 Arabic letters + hamzas + tatweel +
//     teh marbuta + alef maksura, with all four forms each (where defined).
//     Right-joining letters (ا د ذ ر ز و إ أ آ ة) only have isolated + final.
//   • Harakat (diacritics, U+064B..U+065F) and the superscript alef U+0670
//     are treated as transparent — they don't break the join chain.
//   • Latin / digit / punctuation / emoji characters are left alone, so
//     mixed strings like "Hi {playerName}!" or "مرحبا Sam 👋" still work.
//   • TMP_Text.isRightToLeftText is still expected to be set to true on
//     the rendering component — this shaper produces logical-order text;
//     RTL display direction is TMP's job.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;

namespace MathEdu.Utility
{
    public static class ArabicShaper
    {
        private enum Join { U, D, L, R }   // None, Dual, Left-only, Right-only

        private struct Glyphs
        {
            public Join join;
            public int  iso, init, med, fina;
            public Glyphs(Join j, int i, int n, int m, int f) { join = j; iso = i; init = n; med = m; fina = f; }
        }

        // 0 in init/med/fina means "this letter doesn't have that form" — the
        // shaper falls back to the isolated form (or to the closer-defined form).
        private static readonly Dictionary<int, Glyphs> T = new Dictionary<int, Glyphs>
        {
            // Hamza
            { 0x0621, new Glyphs(Join.U, 0xFE80, 0, 0, 0) },
            // Alef madda
            { 0x0622, new Glyphs(Join.R, 0xFE81, 0, 0, 0xFE82) },
            // Alef hamza above
            { 0x0623, new Glyphs(Join.R, 0xFE83, 0, 0, 0xFE84) },
            // Waw hamza
            { 0x0624, new Glyphs(Join.R, 0xFE85, 0, 0, 0xFE86) },
            // Alef hamza below
            { 0x0625, new Glyphs(Join.R, 0xFE87, 0, 0, 0xFE88) },
            // Yeh hamza
            { 0x0626, new Glyphs(Join.D, 0xFE89, 0xFE8B, 0xFE8C, 0xFE8A) },
            // Alef
            { 0x0627, new Glyphs(Join.R, 0xFE8D, 0, 0, 0xFE8E) },
            // Beh
            { 0x0628, new Glyphs(Join.D, 0xFE8F, 0xFE91, 0xFE92, 0xFE90) },
            // Teh marbuta
            { 0x0629, new Glyphs(Join.R, 0xFE93, 0, 0, 0xFE94) },
            // Teh
            { 0x062A, new Glyphs(Join.D, 0xFE95, 0xFE97, 0xFE98, 0xFE96) },
            // Theh
            { 0x062B, new Glyphs(Join.D, 0xFE99, 0xFE9B, 0xFE9C, 0xFE9A) },
            // Jeem
            { 0x062C, new Glyphs(Join.D, 0xFE9D, 0xFE9F, 0xFEA0, 0xFE9E) },
            // Hah
            { 0x062D, new Glyphs(Join.D, 0xFEA1, 0xFEA3, 0xFEA4, 0xFEA2) },
            // Khah
            { 0x062E, new Glyphs(Join.D, 0xFEA5, 0xFEA7, 0xFEA8, 0xFEA6) },
            // Dal
            { 0x062F, new Glyphs(Join.R, 0xFEA9, 0, 0, 0xFEAA) },
            // Thal
            { 0x0630, new Glyphs(Join.R, 0xFEAB, 0, 0, 0xFEAC) },
            // Reh
            { 0x0631, new Glyphs(Join.R, 0xFEAD, 0, 0, 0xFEAE) },
            // Zain
            { 0x0632, new Glyphs(Join.R, 0xFEAF, 0, 0, 0xFEB0) },
            // Seen
            { 0x0633, new Glyphs(Join.D, 0xFEB1, 0xFEB3, 0xFEB4, 0xFEB2) },
            // Sheen
            { 0x0634, new Glyphs(Join.D, 0xFEB5, 0xFEB7, 0xFEB8, 0xFEB6) },
            // Sad
            { 0x0635, new Glyphs(Join.D, 0xFEB9, 0xFEBB, 0xFEBC, 0xFEBA) },
            // Dad
            { 0x0636, new Glyphs(Join.D, 0xFEBD, 0xFEBF, 0xFEC0, 0xFEBE) },
            // Tah
            { 0x0637, new Glyphs(Join.D, 0xFEC1, 0xFEC3, 0xFEC4, 0xFEC2) },
            // Zah
            { 0x0638, new Glyphs(Join.D, 0xFEC5, 0xFEC7, 0xFEC8, 0xFEC6) },
            // Ain
            { 0x0639, new Glyphs(Join.D, 0xFEC9, 0xFECB, 0xFECC, 0xFECA) },
            // Ghain
            { 0x063A, new Glyphs(Join.D, 0xFECD, 0xFECF, 0xFED0, 0xFECE) },
            // Tatweel - always renders as-is (acts as a kashida)
            { 0x0640, new Glyphs(Join.D, 0x0640, 0x0640, 0x0640, 0x0640) },
            // Feh
            { 0x0641, new Glyphs(Join.D, 0xFED1, 0xFED3, 0xFED4, 0xFED2) },
            // Qaf
            { 0x0642, new Glyphs(Join.D, 0xFED5, 0xFED7, 0xFED8, 0xFED6) },
            // Kaf
            { 0x0643, new Glyphs(Join.D, 0xFED9, 0xFEDB, 0xFEDC, 0xFEDA) },
            // Lam
            { 0x0644, new Glyphs(Join.D, 0xFEDD, 0xFEDF, 0xFEE0, 0xFEDE) },
            // Meem
            { 0x0645, new Glyphs(Join.D, 0xFEE1, 0xFEE3, 0xFEE4, 0xFEE2) },
            // Noon
            { 0x0646, new Glyphs(Join.D, 0xFEE5, 0xFEE7, 0xFEE8, 0xFEE6) },
            // Heh
            { 0x0647, new Glyphs(Join.D, 0xFEE9, 0xFEEB, 0xFEEC, 0xFEEA) },
            // Waw
            { 0x0648, new Glyphs(Join.R, 0xFEED, 0, 0, 0xFEEE) },
            // Alef maksura (joins on right only in modern usage)
            { 0x0649, new Glyphs(Join.D, 0xFEEF, 0xFBE8, 0xFBE9, 0xFEF0) },
            // Yeh
            { 0x064A, new Glyphs(Join.D, 0xFEF1, 0xFEF3, 0xFEF4, 0xFEF2) },
        };

        public static string Shape(string input)
        {
            if (string.IsNullOrEmpty(input))      return input;
            if (!ContainsArabic(input))           return input;

            var sb = new StringBuilder(input.Length);
            int n = input.Length;
            int i = 0;
            while (i < n)
            {
                char ch = input[i];
                int code = ch;

                // -----------------------------------------------------------
                // Lam + Alef ligature handling (must run BEFORE general shaping
                // because it consumes two source chars into one glyph).
                // -----------------------------------------------------------
                if (code == 0x0644 && i + 1 < n)
                {
                    int next = NextNonTransparent(input, i + 1, out int nextIdx);
                    int ligature = -1;
                    if      (next == 0x0627) ligature = 0xFEFB;  // lam + alef
                    else if (next == 0x0622) ligature = 0xFEF5;  // lam + alef madda
                    else if (next == 0x0623) ligature = 0xFEF7;  // lam + alef hamza above
                    else if (next == 0x0625) ligature = 0xFEF9;  // lam + alef hamza below

                    if (ligature != -1)
                    {
                        // The lam-alef glyph takes its FINAL form when the
                        // preceding letter joins on its left; otherwise isolated.
                        bool joinsPrev = LeftJoinsFromPrev(input, i);
                        sb.Append((char)(joinsPrev ? ligature + 1 : ligature));
                        // Append any harakat that were sitting on the lam.
                        i = nextIdx + 1;
                        continue;
                    }
                }

                // -----------------------------------------------------------
                // Transparent characters: diacritics, etc. — emit unchanged.
                // -----------------------------------------------------------
                if (IsTransparent(code))
                {
                    sb.Append(ch);
                    i++;
                    continue;
                }

                // -----------------------------------------------------------
                // Non-Arabic: emit unchanged.
                // -----------------------------------------------------------
                if (!T.TryGetValue(code, out var info))
                {
                    sb.Append(ch);
                    i++;
                    continue;
                }

                // -----------------------------------------------------------
                // Decide contextual form.
                // -----------------------------------------------------------
                bool right = LeftJoinsFromPrev(input, i)  // prev can join LEFT
                             && (info.join == Join.D || info.join == Join.R);
                bool left  = RightJoinsFromNext(input, i) // next can join RIGHT
                             && (info.join == Join.D || info.join == Join.L);

                int form;
                if (right && left && info.med != 0)      form = info.med;
                else if (right && info.fina != 0)        form = info.fina;
                else if (left  && info.init != 0)        form = info.init;
                else                                     form = info.iso;

                sb.Append((char)form);
                i++;
            }
            return sb.ToString();
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        /// <summary>True if any character in <paramref name="s"/> is in any Arabic Unicode block.</summary>
        public static bool ContainsArabic(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                int c = s[i];
                if ((c >= 0x0600 && c <= 0x06FF) || // Arabic
                    (c >= 0x0750 && c <= 0x077F) || // Arabic Supplement
                    (c >= 0xFB50 && c <= 0xFDFF) || // Arabic Presentation Forms-A
                    (c >= 0xFE70 && c <= 0xFEFF))   // Arabic Presentation Forms-B
                    return true;
            }
            return false;
        }

        /// <summary>Diacritics and other marks that don't break the join chain.</summary>
        private static bool IsTransparent(int c)
        {
            // Arabic harakat + tanwin + sukun + shadda
            if (c >= 0x064B && c <= 0x065F) return true;
            // Superscript alef
            if (c == 0x0670) return true;
            // Honourifics
            if (c >= 0x0610 && c <= 0x061A) return true;
            // Arabic small high marks (Quran)
            if (c >= 0x06D6 && c <= 0x06ED) return true;
            return false;
        }

        /// <summary>
        /// Find the next non-transparent character starting at <paramref name="start"/>.
        /// Returns the codepoint and writes the index into <paramref name="idx"/>.
        /// Returns -1 if not found.
        /// </summary>
        private static int NextNonTransparent(string s, int start, out int idx)
        {
            for (int j = start; j < s.Length; j++)
            {
                if (!IsTransparent(s[j])) { idx = j; return s[j]; }
            }
            idx = s.Length;
            return -1;
        }

        /// <summary>
        /// Does the character before index <paramref name="i"/> have a left-joining
        /// shape (i.e. can it connect to <paramref name="i"/> from the right side)?
        /// Skips transparent characters.
        /// </summary>
        private static bool LeftJoinsFromPrev(string s, int i)
        {
            for (int j = i - 1; j >= 0; j--)
            {
                int c = s[j];
                if (IsTransparent(c)) continue;
                if (!T.TryGetValue(c, out var prev)) return false;
                // Prev letter must be dual or left-only joining.
                return prev.join == Join.D || prev.join == Join.L;
            }
            return false;
        }

        /// <summary>
        /// Does the character after index <paramref name="i"/> have a right-joining
        /// shape (i.e. can it connect to <paramref name="i"/> from the left side)?
        /// Skips transparent characters.
        /// </summary>
        private static bool RightJoinsFromNext(string s, int i)
        {
            for (int j = i + 1; j < s.Length; j++)
            {
                int c = s[j];
                if (IsTransparent(c)) continue;
                if (!T.TryGetValue(c, out var next)) return false;
                return next.join == Join.D || next.join == Join.R;
            }
            return false;
        }
    }
}
