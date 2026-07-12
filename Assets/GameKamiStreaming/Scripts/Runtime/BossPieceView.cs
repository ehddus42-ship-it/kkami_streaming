using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameKamiStreaming
{
    public sealed class BossPieceView : MonoBehaviour
    {
        public enum BossPattern
        {
            Move,
            Burrow,
            Airborne
        }

        const float DefaultMoveIntervalSeconds = 1f;
        const float DefaultMoveDurationSeconds = 0.34f;
        const float DefaultMoveStepDistance = 190f * 1.8f;
        const float MoveFrameSeconds = 0.045f;
        const float DeathFrameSeconds = 0.055f * 1.5f;
        static readonly Vector2[] MoveDirections = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        IBossMovementArea movementArea;
        DestructiblePieceView pieceView;
        Image image;
        Sprite idleSprite;
        List<Sprite> idleFrames;
        List<Sprite> moveFrames;
        List<Sprite> emergeFrames;
        List<Sprite> deathFrames;
        RectTransform rectTransform;
        Coroutine moveRoutine;
        float moveIntervalSeconds = DefaultMoveIntervalSeconds;
        float moveStepDistance = DefaultMoveStepDistance;
        float moveBoundsScale = 1f;
        float[] moveDurations;
        BossPattern pattern;
        float disappearDelaySeconds;
        float hiddenDelaySeconds;
        float emergeDisplayScale = 1f;
        float emergeDisplayOffsetY;
        RectTransform displayRectTransform;
        Vector2 lastMoveDirection;
        int idleFrameIndex;
        bool animateIdleWithMoveAnimation;
        bool defeated;
        readonly List<Vector2> movePath = new List<Vector2>();

        public void Initialize(
            IBossMovementArea area,
            DestructiblePieceView view,
            Image targetImage,
            Sprite idle,
            List<Sprite> idleAnimation,
            List<Sprite> moveAnimation,
            List<Sprite> emergeAnimation,
            List<Sprite> deathAnimation,
            float moveInterval,
            float moveStep,
            float movementBoundsScale,
            float[] moveDurationOptions,
            BossPattern bossPattern,
            float disappearDelay,
            float hiddenDelay,
            float emergeScale,
            float emergeOffsetY,
            bool animateIdle)
        {
            movementArea = area;
            pieceView = view;
            image = targetImage;
            idleSprite = idle;
            idleFrames = idleAnimation;
            moveFrames = moveAnimation;
            emergeFrames = emergeAnimation;
            deathFrames = deathAnimation;
            moveIntervalSeconds = moveInterval > 0f ? moveInterval : DefaultMoveIntervalSeconds;
            moveStepDistance = moveStep > 0f ? moveStep : DefaultMoveStepDistance;
            moveBoundsScale = Mathf.Clamp(movementBoundsScale, 0.1f, 1f);
            moveDurations = moveDurationOptions != null && moveDurationOptions.Length > 0 ? moveDurationOptions : new[] { DefaultMoveDurationSeconds };
            pattern = bossPattern;
            disappearDelaySeconds = Mathf.Max(0f, disappearDelay);
            hiddenDelaySeconds = Mathf.Max(0f, hiddenDelay);
            emergeDisplayScale = Mathf.Max(0.01f, emergeScale);
            emergeDisplayOffsetY = emergeOffsetY;
            animateIdleWithMoveAnimation = animateIdle;
            rectTransform = transform as RectTransform;
            if (!Mathf.Approximately(emergeDisplayScale, 1f) || !Mathf.Approximately(emergeDisplayOffsetY, 0f))
            {
                image = CreateDetachedDisplayImage(image);
            }
            displayRectTransform = image != null ? image.rectTransform : null;
            BringToFront();

            if (pieceView != null)
            {
                pieceView.SetDeathHandledExternally(true);
                pieceView.Defeated += HandleDefeated;
            }

            moveRoutine = StartCoroutine(pattern == BossPattern.Burrow
                ? BurrowLoop()
                : pattern == BossPattern.Airborne ? AirborneLoop() : MoveLoop());
        }

        void OnDestroy()
        {
            if (pieceView != null)
            {
                pieceView.Defeated -= HandleDefeated;
            }

        }

        IEnumerator AirborneLoop()
        {
            while (!defeated)
            {
                yield return WaitMoveInterval();
                if (defeated || movementArea == null || rectTransform == null)
                {
                    yield break;
                }

                BringToFront();
                movePath.Clear();
                var moveDirection = PickMoveDirection();
                if (movementArea.TryGetBossMovePath(rectTransform, moveStepDistance, moveDirection, movePath, out var outgoingDirection, moveBoundsScale))
                {
                    lastMoveDirection = outgoingDirection;
                    yield return FlyAlongPath(movePath);
                }
            }
        }

        IEnumerator FlyAlongPath(List<Vector2> path)
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

            var moveDuration = PickMoveDuration();
            var frameTimer = 0f;
            var frameIndex = 0;
            yield return FadeBody(1f, 0f, 0.28f);
            if (defeated)
            {
                yield break;
            }

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

            yield return FadeBody(0f, 1f, 0.28f);
            if (!animateIdleWithMoveAnimation && image != null && idleSprite != null)
            {
                image.sprite = idleSprite;
            }
        }

        IEnumerator FadeBody(float fromAlpha, float toAlpha, float duration)
        {
            if (image == null)
            {
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (defeated)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                var color = image.color;
                color.a = Mathf.Lerp(fromAlpha, toAlpha, Mathf.Clamp01(elapsed / duration));
                image.color = color;
                yield return null;
            }

            var finalColor = image.color;
            finalColor.a = toAlpha;
            image.color = finalColor;
        }

        IEnumerator MoveLoop()
        {
            while (!defeated)
            {
                yield return WaitMoveInterval();
                if (defeated || movementArea == null || rectTransform == null)
                {
                    yield break;
                }

                BringToFront();
                movePath.Clear();
                var moveDirection = PickMoveDirection();
                if (movementArea.TryGetBossMovePath(rectTransform, moveStepDistance, moveDirection, movePath, out var outgoingDirection, moveBoundsScale))
                {
                    lastMoveDirection = outgoingDirection;
                    yield return MoveAlongPath(movePath);
                }
            }
        }

        IEnumerator WaitMoveInterval()
        {
            var elapsed = 0f;
            var frameTimer = 0f;
            while (elapsed < moveIntervalSeconds)
            {
                if (defeated)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                if (animateIdleWithMoveAnimation && moveFrames != null && moveFrames.Count > 0 && image != null)
                {
                    frameTimer += Time.deltaTime;
                    if (frameTimer >= MoveFrameSeconds)
                    {
                        BringToFront();
                        image.enabled = true;
                        image.sprite = moveFrames[idleFrameIndex % moveFrames.Count];
                        idleFrameIndex++;
                        frameTimer = 0f;
                    }
                }

                yield return null;
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

            if (!animateIdleWithMoveAnimation && image != null && idleSprite != null)
            {
                image.sprite = idleSprite;
            }
        }

        IEnumerator BurrowLoop()
        {
            while (!defeated)
            {
                yield return PlayIdleFrames(disappearDelaySeconds);
                if (defeated || movementArea == null || rectTransform == null)
                {
                    yield break;
                }

                BringToFront();
                SetVisibleAndHittable(true);
                yield return PlayFrames(moveFrames, MoveFrameSeconds);
                if (defeated)
                {
                    yield break;
                }

                SetVisibleAndHittable(false);
                yield return new WaitForSeconds(hiddenDelaySeconds);
                if (defeated || movementArea == null || rectTransform == null)
                {
                    yield break;
                }

                if (movementArea.TryGetBossRandomPosition(rectTransform, out var nextPosition))
                {
                    rectTransform.anchoredPosition = nextPosition;
                }

                BringToFront();
                SetVisibleAndHittable(true);
                SetDisplayTransform(emergeDisplayScale, emergeDisplayOffsetY);
                yield return PlayFrames(emergeFrames, MoveFrameSeconds);
                ResetDisplayTransform();
                if (image != null && idleFrames != null && idleFrames.Count > 0)
                {
                    image.sprite = idleFrames[0];
                }
                else if (image != null && idleSprite != null)
                {
                    image.sprite = idleSprite;
                }
            }
        }

        IEnumerator PlayIdleFrames(float duration)
        {
            if (idleFrames == null || idleFrames.Count == 0 || image == null)
            {
                yield return new WaitForSeconds(duration);
                yield break;
            }

            var elapsed = 0f;
            var frameTimer = 0f;
            var frameIndex = 0;
            image.enabled = true;
            while (elapsed < duration)
            {
                if (defeated)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                frameTimer += Time.deltaTime;
                if (frameTimer >= MoveFrameSeconds)
                {
                    image.sprite = idleFrames[frameIndex % idleFrames.Count];
                    frameIndex++;
                    frameTimer = 0f;
                }

                yield return null;
            }
        }

        IEnumerator PlayFrames(List<Sprite> frames, float frameSeconds)
        {
            if (frames == null || frames.Count == 0 || image == null)
            {
                yield break;
            }

            image.enabled = true;
            for (var i = 0; i < frames.Count; i++)
            {
                if (defeated)
                {
                    yield break;
                }

                BringToFront();
                image.sprite = frames[i];
                yield return new WaitForSeconds(frameSeconds);
            }
        }

        Vector2 PickMoveDirection()
        {
            if (lastMoveDirection.sqrMagnitude <= 0.001f)
            {
                return MoveDirections[Random.Range(0, MoveDirections.Length)];
            }

            var oppositeDirection = -lastMoveDirection.normalized;
            Vector2 direction;
            do
            {
                direction = MoveDirections[Random.Range(0, MoveDirections.Length)];
            }
            while (Vector2.Dot(direction, oppositeDirection) >= 0.95f);

            return direction;
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
            ResetDisplayTransform();
            if (image != null)
            {
                var color = image.color;
                color.a = 1f;
                image.color = color;
            }
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
            SetVisibleAndHittable(true);
            if (pieceView != null)
            {
                pieceView.SetHittable(false);
            }

            if (deathFrames != null && deathFrames.Count > 0 && image != null)
            {
                for (var i = 0; i < deathFrames.Count; i++)
                {
                    BringToFront();
                    image.sprite = deathFrames[i];
                    yield return new WaitForSeconds(DeathFrameSeconds);
                }
            }

            if (pieceView != null && pieceView.Piece != null)
            {
                pieceView.RequestCollection();
            }

            Destroy(gameObject);
        }

        void SetVisibleAndHittable(bool value)
        {
            if (image != null)
            {
                image.enabled = value;
            }

            if (pieceView != null)
            {
                pieceView.SetHittable(value);
            }
        }

        void BringToFront()
        {
            if (rectTransform != null)
            {
                rectTransform.SetAsLastSibling();
            }
        }

        Image CreateDetachedDisplayImage(Image source)
        {
            if (source == null || rectTransform == null)
            {
                return source;
            }

            var displayObject = new GameObject("Boss Display", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var displayRect = displayObject.transform as RectTransform;
            displayRect.SetParent(rectTransform, false);
            displayRect.anchorMin = Vector2.zero;
            displayRect.anchorMax = Vector2.one;
            displayRect.offsetMin = Vector2.zero;
            displayRect.offsetMax = Vector2.zero;

            var displayImage = displayObject.GetComponent<Image>();
            displayImage.sprite = source.sprite;
            displayImage.color = source.color;
            displayImage.material = source.material;
            displayImage.preserveAspect = source.preserveAspect;
            displayImage.raycastTarget = false;
            source.enabled = false;
            return displayImage;
        }

        void SetDisplayTransform(float scale, float offsetY)
        {
            if (displayRectTransform == null)
            {
                return;
            }

            displayRectTransform.localScale = Vector3.one * scale;
            displayRectTransform.anchoredPosition = new Vector2(0f, offsetY);
        }

        void ResetDisplayTransform()
        {
            SetDisplayTransform(1f, 0f);
        }
    }
}
