using UnityEngine;
using UnityEngine.UI;

namespace GameKamiStreaming
{
    public sealed class DestructiblePieceView : MonoBehaviour
    {
        Image icon;
        PieceRow piece;
        float hp;
        bool deathHandledExternally;
        bool defeated;
        bool hittable = true;

        public bool IsDestroyed => piece == null || hp <= 0f;
        public bool IsHittable => hittable;
        public PieceRow Piece => piece;
        public RectTransform RectTransform => transform as RectTransform;
        public event System.Action<DestructiblePieceView> Defeated;
        public event System.Action<RectTransform, string, bool> HitFeedbackRequested;
        public event System.Action<PieceRow, RectTransform> CollectionRequested;

        public void Initialize(PieceRow row, Sprite sprite)
        {
            piece = row;
            hp = Mathf.Max(1, row.maxHp);
            icon = GetComponent<Image>();
            icon.sprite = sprite;
            icon.preserveAspect = true;
        }

        public void SetDeathHandledExternally(bool value)
        {
            deathHandledExternally = value;
        }

        public void SetHittable(bool value)
        {
            hittable = value;
        }

        public void Hit(float damage, bool playFeedback)
        {
            if (piece == null || defeated || !hittable)
            {
                return;
            }

            hp = Mathf.Max(0f, hp - Mathf.Max(0f, damage));
            var collectAfterShrink = hp <= 0f && !deathHandledExternally;
            if (playFeedback)
            {
                HitFeedbackRequested?.Invoke(transform as RectTransform, piece.effectId, collectAfterShrink);
            }

            if (hp <= 0f)
            {
                defeated = true;
                Defeated?.Invoke(this);
                if (deathHandledExternally)
                {
                    return;
                }

                if (playFeedback)
                {
                    StartCoroutine(CollectAfterHitShrink());
                }
                else
                {
                    CollectAndDestroy();
                }
            }
        }

        System.Collections.IEnumerator CollectAfterHitShrink()
        {
            yield return new WaitForSeconds(GameEffectManager.HitShrinkDurationSeconds);
            CollectAndDestroy();
        }

        void CollectAndDestroy()
        {
            RequestCollection();
            Destroy(gameObject);
        }

        public void RequestCollection()
        {
            if (piece != null)
            {
                CollectionRequested?.Invoke(piece, transform as RectTransform);
            }
        }
    }
}
