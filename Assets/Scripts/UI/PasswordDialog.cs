// -----------------------------------------------------------------------------
// PasswordDialog.cs
// -----------------------------------------------------------------------------
// Lightweight modal that prompts for the parental PIN. Calls the supplied
// callback with the entered string (or null if the player cancelled).
//
// The dialog builds its own Canvas so it works from any scene, sits above any
// existing UI, and tears itself down on submit / cancel.
// -----------------------------------------------------------------------------

using System;
using MathEdu.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathEdu.UI
{
    public class PasswordDialog : MonoBehaviour
    {
        public static PasswordDialog Show(string title, string subtitle,
            Action<string> onSubmit, Action onCancel = null)
        {
            var go = new GameObject("[PasswordDialog]",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(PasswordDialog));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            var dialog = go.GetComponent<PasswordDialog>();

            var canvasRt = (RectTransform)go.transform;
            // Dimmer
            var dim = new GameObject("Dim", typeof(Image));
            dim.transform.SetParent(canvasRt, false);
            var drt = (RectTransform)dim.transform;
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);

            // Card
            var card = UIFactory.CreatePanel(canvasRt,
                new Vector2(0.1f, 0.32f), new Vector2(0.9f, 0.68f),
                UIFactory.Card, 32, "Card");

            var col = UIFactory.CreateVerticalLayout(card, 18,
                new RectOffset(28, 28, 28, 28), "Col");
            var crt = (RectTransform)col.transform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

            var titleTxt = UIFactory.CreateText((RectTransform)col.transform, title, 52,
                UIFactory.TextDark, TextAlignmentOptions.Center, "Title");
            titleTxt.fontStyle = FontStyles.Bold;
            var tle = titleTxt.gameObject.AddComponent<LayoutElement>();
            tle.preferredHeight = 80;

            var sub = UIFactory.CreateText((RectTransform)col.transform, subtitle, 30,
                UIFactory.TextDark, TextAlignmentOptions.Center, "Sub");
            var sle = sub.gameObject.AddComponent<LayoutElement>();
            sle.preferredHeight = 60;

            var input = UIFactory.CreateInputField((RectTransform)col.transform,
                "Enter PIN", 50, "PinInput");
            input.contentType = TMP_InputField.ContentType.Pin;
            input.characterLimit = 8;
            var ile = input.gameObject.AddComponent<LayoutElement>();
            ile.preferredHeight = 130;

            var btnRow = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            btnRow.transform.SetParent(col.transform, false);
            var brt = (RectTransform)btnRow.transform;
            var hl = btnRow.GetComponent<HorizontalLayoutGroup>();
            hl.spacing = 24;
            hl.padding = new RectOffset(0, 0, 8, 0);
            hl.childForceExpandWidth = true;
            hl.childAlignment = TextAnchor.MiddleCenter;
            var brle = btnRow.GetComponent<LayoutElement>();
            brle.preferredHeight = 150;

            var cancelBtn = UIFactory.CreateButton(brt, "Cancel",
                new Color(0.5f, 0.5f, 0.6f), 40, "Cancel");
            cancelBtn.onClick.AddListener(() =>
            {
                onCancel?.Invoke();
                Destroy(go);
            });

            var okBtn = UIFactory.CreateButton(brt, "OK",
                UIFactory.Primary, 40, "OK");
            okBtn.onClick.AddListener(() =>
            {
                onSubmit?.Invoke(input.text);
                Destroy(go);
            });

            return dialog;
        }
    }
}
