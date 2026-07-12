using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameKamiStreaming
{
    public sealed class PixelNumberLabel : MonoBehaviour
    {
        const float DigitWidth = 34f;
        const float DigitHeight = 54f;

        static readonly Dictionary<char, Sprite> SpriteByCharacter = new Dictionary<char, Sprite>();

        readonly List<Image> digits = new List<Image>();
        string displayedText;

        void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            transform.localScale = Vector3.one;
            EnsureSpriteCache();

            var layout = GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = -6f;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }

            CacheExistingDigits();
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
            value = value ?? string.Empty;
            transform.localScale = Vector3.one;
            if (SpriteByCharacter.Count == 0)
            {
                Initialize();
            }

            if (displayedText == value)
            {
                return;
            }

            displayedText = value;

            while (digits.Count < value.Length)
            {
                var image = new GameObject("Digit", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<Image>();
                image.transform.SetParent(transform, false);
                ConfigureDigit(image);
                digits.Add(image);
            }

            for (var i = 0; i < digits.Count; i++)
            {
                Sprite sprite = null;
                var active = i < value.Length && SpriteByCharacter.TryGetValue(value[i], out sprite) && sprite != null;
                digits[i].gameObject.SetActive(active);
                if (active)
                {
                    digits[i].sprite = sprite;
                }
            }
        }

        static void EnsureSpriteCache()
        {
            if (SpriteByCharacter.Count > 0)
            {
                return;
            }

            for (var i = 0; i <= 9; i++)
            {
                SpriteByCharacter[(char)('0' + i)] = LoadSprite(i.ToString());
            }

            var separatorSprite = LoadSprite("dotdot");
            SpriteByCharacter['.'] = separatorSprite;
            SpriteByCharacter[':'] = separatorSprite;
        }

        void CacheExistingDigits()
        {
            digits.Clear();
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                var image = child.GetComponent<Image>();
                if (image != null)
                {
                    digits.Add(image);
                }
            }
        }

        static void ConfigureDigit(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.preserveAspect = false;
            image.raycastTarget = false;

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

