using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameKamiStreaming
{
    public sealed class BossPieceView : MonoBehaviour
    {
        const float DefaultMoveIntervalSeconds = 1f;
        const float DefaultMoveDurationSeconds = 0.34f;
        const float DefaultMoveStepDistance = 190f * 1.8f;
        const float MoveFrameSeconds = 0.045f;
        const float DeathFrameSeconds = 0.055f;

        KkamiPrototypeGame owner;
        DestructiblePieceView pieceView;
        Image image;
        Sprite idleSprite;
        List<Sprite> moveFrames;
        List<Sprite> deathFrames;
        RectTransform rectTransform;
        Coroutine moveRoutine;
        float moveIntervalSeconds = DefaultMoveIntervalSeconds;
        float moveStepDistance = DefaultMoveStepDistance;
        float[] moveDurations;
        Vector2 lastMoveDirection;
        bool defeated;

        public void Initialize(KkamiPrototypeGame game, DestructiblePieceView view, Image targetImage, Sprite idle, List<Sprite> moveAnimation, List<Sprite> deathAnimation, float moveInterval, float moveStep, float[] moveDurationOptions)
        {
            owner = game;
            pieceView = view;
            image = targetImage;
            idleSprite = idle;
            moveFrames = moveAnimation;
            deathFrames = deathAnimation;
            moveIntervalSeconds = moveInterval > 0f ? moveInterval : DefaultMoveIntervalSeconds;
            moveStepDistance = moveStep > 0f ? moveStep : DefaultMoveStepDistance;
            moveDurations = moveDurationOptions != null && moveDurationOptions.Length > 0 ? moveDurationOptions : new[] { DefaultMoveDurationSeconds };
            rectTransform = transform as RectTransform;
            BringToFront();

            if (pieceView != null)
            {
                pieceView.SetDeathHandledExternally(true);
                pieceView.Defeated += HandleDefeated;
            }

            moveRoutine = StartCoroutine(MoveLoop());
        }

        void OnDestroy()
        {
            if (pieceView != null)
            {
                pieceView.Defeated -= HandleDefeated;
            }
        }

        IEnumerator MoveLoop()
        {
            while (!defeated)
            {
                yield return new WaitForSeconds(moveIntervalSeconds);
                if (defeated || owner == null || rectTransform == null)
                {
                    yield break;
                }

                BringToFront();
                var path = new List<Vector2>();
                var moveDirection = PickMoveDirection();
                if (owner.TryGetBossMovePath(rectTransform, moveStepDistance, moveDirection, path, out var outgoingDirection))
                {
                    lastMoveDirection = outgoingDirection;
                    yield return MoveAlongPath(path);
                }
            }
        }

        IEnumerator MoveAlongPath(List<Vector2> path)
        {
            if (path == null || path.Count == 0)
            {
                yield break;
            }

            var startPosition = rectTransform.anchoredPosition;
            var totalDistance = 0f;
            for (var i = 0; i < path.Count; i++)
            {
                totalDistance += Vector2.Distance(i == 0 ? startPosition : path[i - 1], path[i]);
            }

            if (totalDistance <= 0.001f)
            {
                yield break;
            }

            var frameTimer = 0f;
            var frameIndex = 0;
            var moveDuration = PickMoveDuration();

            for (var i = 0; i < path.Count; i++)
            {
                var segmentStart = rectTransform.anchoredPosition;
                var segmentEnd = path[i];
                var segmentDistance = Vector2.Distance(segmentStart, segmentEnd);
                var segmentDuration = moveDuration * (segmentDistance / totalDistance);
                var elapsed = 0f;
                while (elapsed < segmentDuration)
                {
                    if (defeated || rectTransform == null)
                    {
                        yield break;
                    }

                    BringToFront();
                    elapsed += Time.deltaTime;
                    frameTimer += Time.deltaTime;
                    rectTransform.anchoredPosition = Vector2.Lerp(segmentStart, segmentEnd, Mathf.Clamp01(elapsed / segmentDuration));

                    if (moveFrames != null && moveFrames.Count > 0 && image != null && frameTimer >= MoveFrameSeconds)
                    {
                        image.sprite = moveFrames[frameIndex % moveFrames.Count];
                        frameIndex++;
                        frameTimer = 0f;
                    }

                    yield return null;
                }

                rectTransform.anchoredPosition = segmentEnd;
            }

            if (image != null && idleSprite != null)
            {
                image.sprite = idleSprite;
            }
        }

        Vector2 PickMoveDirection()
        {
            var directions = new[]
            {
                Vector2.up,
                Vector2.down,
                Vector2.left,
                Vector2.right
            };

            var candidates = new List<Vector2>(directions);
            if (lastMoveDirection.sqrMagnitude > 0.001f && candidates.Count > 1)
            {
                candidates.RemoveAll(direction => Vector2.Dot(direction, lastMoveDirection.normalized) < -0.95f);
            }

            return candidates[Random.Range(0, candidates.Count)];
        }

        float PickMoveDuration()
        {
            if (moveDurations == null || moveDurations.Length == 0)
            {
                return DefaultMoveDurationSeconds;
            }

            return Mathf.Max(0.05f, moveDurations[Random.Range(0, moveDurations.Length)]);
        }

        void HandleDefeated(DestructiblePieceView defeatedView)
        {
            if (defeated)
            {
                return;
            }

            defeated = true;
            BringToFront();
            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
            }

            StartCoroutine(PlayDeathAndCollect());
        }

        IEnumerator PlayDeathAndCollect()
        {
            BringToFront();
            if (deathFrames != null && deathFrames.Count > 0 && image != null)
            {
                for (var i = 0; i < deathFrames.Count; i++)
                {
                    BringToFront();
                    image.sprite = deathFrames[i];
                    yield return new WaitForSeconds(DeathFrameSeconds);
                }
            }

            if (owner != null && pieceView != null && pieceView.Piece != null)
            {
                owner.CollectPiece(pieceView.Piece, transform as RectTransform);
            }

            Destroy(gameObject);
        }

        void BringToFront()
        {
            if (rectTransform != null)
            {
                rectTransform.SetAsLastSibling();
            }
        }
    }
}
