// -----------------------------------------------------------------------------
// AvatarTile.cs
// -----------------------------------------------------------------------------
// One selectable tile inside the Player Setup avatar grid. Renders the
// avatar's Sprite if assigned, otherwise an emoji on a tinted circle. Shows a
// bright outline when selected.
// -----------------------------------------------------------------------------

using System;
using MathEdu.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MathEdu.UI
{
    public class AvatarTile : MonoBehaviour, IPointerClickHandler
    {
        public event Action<AvatarData> onSelected;

        private AvatarData _avatar;
        private Image      _frame;
        private Image      _portrait;
        private TextMeshProUGUI _emoji;
        private TextMeshProUGUI _name;
        private bool _selected;

        public static AvatarTile Spawn(RectTransform parent, AvatarData avatar)
        {
            var go = new GameObject($"Avatar_{avatar.avatarId}",
                typeof(RectTransform), typeof(Image), typeof(AvatarTile));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(260, 320);

            var frame = go.GetComponent<Image>();
            frame.sprite = DefaultSprite.RoundedRect(28);
            frame.type   = Image.Type.Sliced;
            frame.color  = new Color(1, 1, 1, 0.10f);

            var col = UIFactory.CreateVerticalLayout(rt, 8,
                new RectOffset(12, 12, 12, 12), "Col");
            var crt = (RectTransform)col.transform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

            // Circle / portrait
            var portraitGo = new GameObject("Portrait", typeof(Image), typeof(LayoutElement));
            portraitGo.transform.SetParent(col.transform, false);
            var portrait = portraitGo.GetComponent<Image>();
            portrait.sprite = DefaultSprite.Circle();
            portrait.color  = avatar.tint;
            var ple = portraitGo.GetComponent<LayoutElement>();
            ple.preferredHeight = 200; ple.preferredWidth = 200;

            var emoji = UIFactory.CreateText((RectTransform)portraitGo.transform,
                avatar.emoji, 130, Color.white, TextAlignmentOptions.Center, "Emoji");
            emoji.fontStyle = FontStyles.Bold;

            if (avatar.sprite != null)
            {
                portrait.sprite = avatar.sprite;
                portrait.color  = Color.white;
                portrait.preserveAspect = true;
                emoji.text = "";
            }

            var nameLbl = UIFactory.CreateText((RectTransform)col.transform,
                avatar.displayName, 30, Color.white,
                TextAlignmentOptions.Center, "Name");
            nameLbl.fontStyle = FontStyles.Bold;

            var tile = go.GetComponent<AvatarTile>();
            tile._avatar   = avatar;
            tile._frame    = frame;
            tile._portrait = portrait;
            tile._emoji    = emoji;
            tile._name     = nameLbl;
            tile.SetSelected(false);
            return tile;
        }

        public AvatarData Avatar => _avatar;

        public void SetSelected(bool isSelected)
        {
            _selected = isSelected;
            if (_frame != null)
            {
                _frame.color = isSelected
                    ? new Color(1f, 0.9f, 0.3f, 0.45f)
                    : new Color(1, 1, 1, 0.10f);
            }
            transform.localScale = isSelected ? Vector3.one * 1.05f : Vector3.one;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            onSelected?.Invoke(_avatar);
        }
    }
}
