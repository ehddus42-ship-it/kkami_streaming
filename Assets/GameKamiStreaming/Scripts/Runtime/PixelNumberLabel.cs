using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameKamiStreaming
{
    public sealed class PixelNumberLabel : MonoBehaviour
    {
        const float DigitWidth = 34f;
        const float DigitHeight = 54f;

        readonly List<Image> digits = new List<Image>();
        readonly Dictionary<char, Sprite> spriteByChar = new Dictionary<char, Sprite>();

        public void Initialize()
        {
            spriteByChar.Clear();
            for (var i = 0; i <= 9; i++)
            {
                spriteByChar[(char)('0' + i)] = LoadSprite(i.ToString());
            }
            spriteByChar['.'] = LoadSprite("dotdot");

            var layout = GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = -6f;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }

            for (var i = 0; i < digits.Count; i++)
            {
                ConfigureDigit(digits[i]);
            }
        }

        public void SetValue(int value)
        {
            SetText(Mathf.Max(0, value).ToString());
        }

        public void SetText(string value)
        {
            while (digits.Count < value.Length)
            {
                var image = new GameObject("Digit", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<Image>();
                image.transform.SetParent(transform, false);
                image.preserveAspect = true;
                ConfigureDigit(image);
                digits.Add(image);
            }

            for (var i = 0; i < digits.Count; i++)
            {
                Sprite sprite = null;
                var active = i < value.Length && spriteByChar.TryGetValue(value[i], out sprite) && sprite != null;
                digits[i].gameObject.SetActive(active);
                if (active)
                {
                    digits[i].sprite = sprite;
                    ConfigureDigit(digits[i]);
                }
            }
        }

        static void ConfigureDigit(Image image)
        {
            if (image == null)
            {
                return;
            }

            var rect = image.transform as RectTransform;
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(DigitWidth, DigitHeight);
            }

            var layoutElement = image.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = image.gameObject.AddComponent<LayoutElement>();
            }
            layoutElement.preferredWidth = DigitWidth;
            layoutElement.preferredHeight = DigitHeight;
            layoutElement.minWidth = DigitWidth;
            layoutElement.minHeight = DigitHeight;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;
        }

        static Sprite LoadSprite(string id)
        {
            return UnityEngine.Resources.Load<Sprite>("GameKamiStreaming/Sprites/" + id);
        }
    }
}

