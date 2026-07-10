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
        bool deathHandledExternally;
        bool defeated;
        bool hittable = true;

        public bool IsDestroyed => piece == null || hp <= 0f;
        public bool IsHittable => hittable;
        public PieceRow Piece => piece;
        public RectTransform RectTransform => transform as RectTransform;
        public event System.Action<DestructiblePieceView> Defeated;

        public void Initialize(KkamiPrototypeGame game, PieceRow row, Sprite sprite)
        {
            owner = game;
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
            if (playFeedback)
            {
                owner.PlayHitFeedback(transform as RectTransform, piece.effectId);
            }

            if (hp <= 0f)
            {
                defeated = true;
                Defeated?.Invoke(this);
                if (deathHandledExternally)
                {
                    return;
                }

                owner.CollectPiece(piece, transform as RectTransform);
                Destroy(gameObject);
            }
        }
    }
}
