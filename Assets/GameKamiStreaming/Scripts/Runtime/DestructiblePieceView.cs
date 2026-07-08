using UnityEngine;
using UnityEngine.UI;

namespace GameKamiStreaming
{
    public sealed class DestructiblePieceView : MonoBehaviour
    {
        Image icon;
        KkamiPrototypeGame owner;
        PieceRow piece;
        float hp;

        public bool IsDestroyed => piece == null || hp <= 0f;
        public RectTransform RectTransform => transform as RectTransform;

        public void Initialize(KkamiPrototypeGame game, PieceRow row, Sprite sprite)
        {
            owner = game;
            piece = row;
            hp = Mathf.Max(1, row.maxHp);
            icon = GetComponent<Image>();
            icon.sprite = sprite;
            icon.preserveAspect = true;
        }

        public void Hit(float damage, bool playFeedback)
        {
            if (piece == null)
            {
                return;
            }

            hp = Mathf.Max(0f, hp - Mathf.Max(0f, damage));
            if (playFeedback)
            {
                owner.PlayHitFeedback(transform as RectTransform, piece.effectId);
            }

            if (hp <= 0f)
            {
                owner.CollectPiece(piece, transform as RectTransform);
                Destroy(gameObject);
            }
        }
    }
}
