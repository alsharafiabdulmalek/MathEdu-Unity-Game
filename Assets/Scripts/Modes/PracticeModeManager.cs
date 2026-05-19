// -----------------------------------------------------------------------------
// PracticeModeManager.cs
// -----------------------------------------------------------------------------
// Untimed, hint-friendly run through the level's questions. Mistakes do not
// penalise - the goal is mastery rather than speed.
// -----------------------------------------------------------------------------

using MathEdu.Gameplay;
using MathEdu.UI;
using MathEdu.Utility;
using UnityEngine;

namespace MathEdu.Modes
{
    public class PracticeModeManager : GameplayManagerBase
    {
        protected override string HeaderTitle => Localization.T("modesel.practice");
        protected override Color  HeaderColor => UIFactory.Success;
        protected override bool   ShowHint    => true;
    }
}
