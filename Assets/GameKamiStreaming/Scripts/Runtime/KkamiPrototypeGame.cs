using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.UI;

namespace GameKamiStreaming
{
    public sealed class KkamiPrototypeGame : MonoBehaviour
    {
        const string SpriteRoot = "GameKamiStreaming/Sprites/";
        const int TargetPieceCount = 12;
        const float MiningRadius = 120f;
        const float DamagePerSecond = 7f;
        const float PieceHitboxInsetRatio = 0.24f;
        const int CollectParticleCount = 9;

        readonly Dictionary<int, int> resourceAmounts = new Dictionary<int, int>();
        readonly Dictionary<int, PixelNumberLabel> resourceLabels = new Dictionary<int, PixelNumberLabel>();
        readonly List<DestructiblePieceView> activePieces = new List<DestructiblePieceView>();

        [SerializeField] Camera uiCamera;
        [SerializeField] RectTransform canvasRoot;
        [SerializeField] RectTransform stageArea;
        [SerializeField] RectTransform pieceLayer;
        [SerializeField] RectTransform pieceDisplayLayer;
        [SerializeField] RectTransform effectLayer;
        [SerializeField] RectTransform miningCursor;

        KkamiTableDatabase database;
        StageRow currentStage;
        float feedbackTimer;

        void Awake()
        {
            InitializeGame();
        }

        void Start()
        {
            FillStagePieces();
        }

        void Update()
        {
            UpdateMiningCursor();
            DamagePiecesUnderCursor();
        }

        public void BuildEditableSceneLayout()
        {
            InitializeTables();
            EnsureEventSystem();
            BuildCameraAndCanvas();
            BuildScene();
            InitializeSceneReferences();
        }

        public void ConfigureSceneReferences(Camera camera, RectTransform canvas, RectTransform stage, RectTransform pieces, RectTransform displayPieces, RectTransform effects, RectTransform cursor)
        {
            uiCamera = camera;
            canvasRoot = canvas;
            stageArea = stage;
            pieceLayer = pieces;
            pieceDisplayLayer = displayPieces;
            effectLayer = effects;
            miningCursor = cursor;
        }

        public void CollectPiece(PieceRow piece, RectTransform source)
        {
            if (!resourceAmounts.ContainsKey(piece.resourceId))
            {
                resourceAmounts[piece.resourceId] = 0;
            }

            resourceAmounts[piece.resourceId] += Mathf.Max(0, piece.resourceAmount);
            if (resourceLabels.TryGetValue(piece.resourceId, out var label))
            {
                label.SetValue(resourceAmounts[piece.resourceId]);
            }

            PlayCollectBurst(source, piece.resourceId);
            StartCoroutine(RespawnAfterDelay(0.45f));
        }

        public void PlayHitFeedback(RectTransform source, string effectId)
        {
            var prefab = LoadEffectPrefab(effectId);
            if (prefab != null)
            {
                Instantiate(prefab, source.position, Quaternion.identity, effectLayer);
                return;
            }

            StartCoroutine(Pulse(source));
        }

        void InitializeGame()
        {
            InitializeTables();
            EnsureEventSystem();
            if (!HasSceneReferences())
            {
                BuildCameraAndCanvas();
                BuildScene();
            }
            InitializeSceneReferences();
        }

        void InitializeTables()
        {
            database = KkamiTableDatabase.Load();
            currentStage = database.Stages.Count > 0 ? database.Stages[0] : null;
        }

        bool HasSceneReferences()
        {
            return uiCamera != null && canvasRoot != null && stageArea != null && pieceLayer != null && effectLayer != null && miningCursor != null;
        }

        void InitializeSceneReferences()
        {
            if (uiCamera == null)
            {
                uiCamera = Camera.main;
            }

            resourceAmounts.Clear();
            resourceLabels.Clear();
            foreach (var resource in database.Resources)
            {
                resourceAmounts[resource.resourceId] = 0;
            }

            foreach (var counter in canvasRoot.GetComponentsInChildren<ResourceCounterView>(true))
            {
                if (counter.NumberLabel == null)
                {
                    continue;
                }

                counter.NumberLabel.Initialize();
                counter.NumberLabel.SetValue(0);
                resourceLabels[counter.ResourceId] = counter.NumberLabel;
                resourceAmounts[counter.ResourceId] = 0;
            }

            EnsurePieceDisplayLayer();

            var cursorImage = miningCursor.GetComponent<Image>();
            if (cursorImage != null && cursorImage.sprite == null)
            {
                cursorImage.sprite = CreateCircleSprite(96, new Color(0.2f, 0.85f, 1f, 0.18f), new Color(0.2f, 0.95f, 1f, 0.76f));
            }
            miningCursor.gameObject.SetActive(false);
        }

        void BuildCameraAndCanvas()
        {
            uiCamera = Camera.main;
            if (uiCamera == null)
            {
                uiCamera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
                uiCamera.tag = "MainCamera";
            }

            uiCamera.clearFlags = CameraClearFlags.SolidColor;
            uiCamera.backgroundColor = new Color(0.08f, 0.09f, 0.11f, 1f);
            uiCamera.orthographic = true;
            uiCamera.orthographicSize = 9.6f;
            uiCamera.transform.position = new Vector3(0f, 0f, -10f);
            uiCamera.transform.rotation = Quaternion.identity;

            var canvas = new GameObject("Kkami Prototype Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCamera;
            canvas.planeDistance = 10f;

            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasRoot = canvas.transform as RectTransform;
        }

        void BuildScene()
        {
            AddFullScreenImage("Background", null, canvasRoot, new Color(0.09f, 0.1f, 0.12f, 1f));

            stageArea = CreateRect("Stage Area", canvasRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(1120f, 1220f));
            var stage = AddAnchoredImage("Stage", LoadSprite("stage"), stageArea, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            stage.preserveAspect = true;

            var counterPanel = CreateRect("Resource Counters", canvasRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(0f, 210f));
            var resourceIndex = 0;
            foreach (var resource in database.Resources)
            {
                BuildResourceCounter(counterPanel, resource, resourceIndex);
                resourceIndex++;
            }

            pieceLayer = CreateRect("E", stageArea, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.88f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            pieceDisplayLayer = CreateRect("Spawned Pieces", canvasRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            effectLayer = CreateRect("Effect Layer", canvasRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            effectLayer.SetAsLastSibling();

            miningCursor = CreateMiningCursor(canvasRoot);
        }

        void BuildResourceCounter(RectTransform parent, ResourceRow resource, int index)
        {
            var column = index % 3;
            var rowIndex = index / 3;
            var position = new Vector2(-350f + column * 350f, -55f - rowIndex * 98f);
            var row = CreateRect("Resource " + resource.resourceId, parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), position, new Vector2(330f, 86f));
            var bg = row.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.34f);

            var icon = AddAnchoredImage("Icon", LoadSprite(resource.imageId), row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(48f, 0f), new Vector2(72f, 72f));
            icon.preserveAspect = true;

            var labelRoot = CreateRect("Number", row, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(128f, 0f), new Vector2(-148f, 64f));
            var layout = labelRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = -6f;

            var number = labelRoot.gameObject.AddComponent<PixelNumberLabel>();
            number.Initialize();
            number.SetValue(0);
            var binding = row.gameObject.AddComponent<ResourceCounterView>();
            binding.Configure(resource.resourceId, number);
            resourceLabels[resource.resourceId] = number;
            resourceAmounts[resource.resourceId] = 0;
        }

        RectTransform CreateMiningCursor(RectTransform parent)
        {
            var cursor = CreateRect("Mining Radius", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(MiningRadius * 2f, MiningRadius * 2f));
            var image = cursor.gameObject.AddComponent<Image>();
            image.sprite = CreateCircleSprite(96, new Color(0.2f, 0.85f, 1f, 0.18f), new Color(0.2f, 0.95f, 1f, 0.76f));
            image.raycastTarget = false;
            cursor.gameObject.SetActive(false);
            return cursor;
        }


        void EnsurePieceDisplayLayer()
        {
            if (pieceDisplayLayer != null)
            {
                return;
            }

            var existing = canvasRoot != null ? canvasRoot.Find("Spawned Pieces") as RectTransform : null;
            if (existing != null)
            {
                pieceDisplayLayer = existing;
                return;
            }

            if (canvasRoot == null)
            {
                return;
            }

            pieceDisplayLayer = CreateRect("Spawned Pieces", canvasRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var effectSibling = effectLayer != null ? effectLayer.GetSiblingIndex() : canvasRoot.childCount;
            pieceDisplayLayer.SetSiblingIndex(Mathf.Max(0, effectSibling));
        }
        void FillStagePieces()
        {
            CleanupDestroyedPieceRefs();
            while (activePieces.Count < TargetPieceCount)
            {
                SpawnPieceAtRandomPosition();
            }
        }

        IEnumerator RespawnAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            CleanupDestroyedPieceRefs();
            FillStagePieces();
        }

        void SpawnPieceAtRandomPosition()
        {
            var piece = PickPiece();
            if (piece == null)
            {
                return;
            }

            var size = Random.Range(155f, 215f);
            var half = size * 0.5f;
            var rect = pieceLayer.rect;
            var anchoredPosition = new Vector2(Random.Range(rect.xMin + half, rect.xMax - half), Random.Range(rect.yMin + half, rect.yMax - half));
            EnsurePieceDisplayLayer();
            var worldPosition = pieceLayer.TransformPoint(anchoredPosition);
            var displayPosition = (Vector2)pieceDisplayLayer.InverseTransformPoint(worldPosition);
            var pieceRoot = CreateRect(piece.pieceName, pieceDisplayLayer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), displayPosition, new Vector2(size, size));
            pieceRoot.localRotation = Quaternion.identity;
            pieceRoot.localScale = Vector3.one;
            var image = pieceRoot.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            var view = pieceRoot.gameObject.AddComponent<DestructiblePieceView>();
            view.Initialize(this, piece, LoadSprite(piece.imageId));
            activePieces.Add(view);
        }

        PieceRow PickPiece()
        {
            if (currentStage == null || currentStage.pieceWeights.Count == 0)
            {
                return database.Pieces.Count > 0 ? database.Pieces[Random.Range(0, database.Pieces.Count)] : null;
            }

            var total = 0f;
            foreach (var weight in currentStage.pieceWeights)
            {
                total += Mathf.Max(0f, weight.weight);
            }

            var roll = Random.value * total;
            foreach (var weight in currentStage.pieceWeights)
            {
                roll -= Mathf.Max(0f, weight.weight);
                if (roll <= 0f)
                {
                    return database.GetPiece(weight.pieceId);
                }
            }

            return database.GetPiece(currentStage.pieceWeights[currentStage.pieceWeights.Count - 1].pieceId);
        }

        void UpdateMiningCursor()
        {
            if (!TryGetPointerPosition(out var pointerPosition) || !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, pointerPosition, uiCamera, out var canvasPoint))
            {
                miningCursor.gameObject.SetActive(false);
                return;
            }

            var insideStage = RectTransformUtility.RectangleContainsScreenPoint(pieceLayer, pointerPosition, uiCamera);
            miningCursor.gameObject.SetActive(insideStage);
            miningCursor.anchoredPosition = canvasPoint;
        }

        void DamagePiecesUnderCursor()
        {
            if (!TryGetPointerPosition(out var pointerPosition) || !miningCursor.gameObject.activeSelf)
            {
                return;
            }

            EnsurePieceDisplayLayer();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(pieceDisplayLayer, pointerPosition, uiCamera, out var localPoint))
            {
                return;
            }

            feedbackTimer -= Time.deltaTime;
            var playFeedback = feedbackTimer <= 0f;
            var damage = DamagePerSecond * Time.deltaTime;

            for (var i = activePieces.Count - 1; i >= 0; i--)
            {
                var piece = activePieces[i];
                if (piece == null || piece.IsDestroyed)
                {
                    activePieces.RemoveAt(i);
                    continue;
                }

                if (CircleOverlapsRect(localPoint, MiningRadius, piece.RectTransform))
                {
                    piece.Hit(damage, playFeedback);
                }
            }

            if (playFeedback)
            {
                feedbackTimer = 0.18f;
            }
        }

        void CleanupDestroyedPieceRefs()
        {
            for (var i = activePieces.Count - 1; i >= 0; i--)
            {
                if (activePieces[i] == null || activePieces[i].IsDestroyed)
                {
                    activePieces.RemoveAt(i);
                }
            }
        }

        void PlayCollectBurst(RectTransform source, int resourceId)
        {
            var resource = database.GetResource(resourceId);
            var sprite = resource != null ? LoadSprite(resource.imageId) : null;
            var startPosition = (Vector2)effectLayer.InverseTransformPoint(source.position);
            var target = GetResourceScoreTarget(resourceId);

            if (target == null)
            {
                for (var i = 0; i < CollectParticleCount; i++)
                {
                    var particle = AddAnchoredImage("Resource Burst", sprite, effectLayer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), startPosition, new Vector2(52f, 52f));
                    StartCoroutine(FlyAndFade(particle.rectTransform, Random.insideUnitCircle.normalized * Random.Range(120f, 260f)));
                }
                return;
            }

            var targetPosition = (Vector2)effectLayer.InverseTransformPoint(target.position);
            for (var i = 0; i < CollectParticleCount; i++)
            {
                var offset = Random.insideUnitCircle * 26f;
                var particle = AddAnchoredImage("Resource Score Fly", sprite, effectLayer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), startPosition + offset, new Vector2(52f, 52f));
                StartCoroutine(FlyToScore(particle.rectTransform, target, targetPosition, i * 0.035f, i == CollectParticleCount - 1));
            }
        }

        IEnumerator Pulse(RectTransform target)
        {
            if (target == null)
            {
                yield break;
            }

            var baseScale = target.localScale;
            target.localScale = baseScale * 1.04f;
            yield return new WaitForSeconds(0.04f);
            if (target != null)
            {
                target.localScale = baseScale;
            }
        }

        IEnumerator FlyAndFade(RectTransform item, Vector2 velocity)
        {
            var image = item.GetComponent<Image>();
            var elapsed = 0f;
            const float duration = 0.55f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                item.anchoredPosition += velocity * Time.deltaTime;
                item.localScale = Vector3.one * Mathf.Lerp(1.1f, 0.25f, elapsed / duration);
                if (image != null)
                {
                    var color = image.color;
                    color.a = Mathf.Lerp(1f, 0f, elapsed / duration);
                    image.color = color;
                }
                yield return null;
            }
            Destroy(item.gameObject);
        }

        IEnumerator FlyToScore(RectTransform item, RectTransform target, Vector2 targetPosition, float delay, bool pulseTarget)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            var image = item.GetComponent<Image>();
            var startPosition = item.anchoredPosition;
            var lift = new Vector2(Random.Range(-90f, 90f), Random.Range(150f, 230f));
            var controlPosition = Vector2.Lerp(startPosition, targetPosition, 0.45f) + lift;
            var elapsed = 0f;
            var duration = Random.Range(0.48f, 0.66f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                item.anchoredPosition = QuadraticBezier(startPosition, controlPosition, targetPosition, eased);
                item.localScale = Vector3.one * Mathf.Lerp(1.05f, 0.42f, eased);

                if (image != null)
                {
                    var color = image.color;
                    color.a = Mathf.Lerp(1f, 0f, Mathf.Clamp01((t - 0.78f) / 0.22f));
                    image.color = color;
                }

                yield return null;
            }

            if (pulseTarget && target != null)
            {
                StartCoroutine(Pulse(target));
            }

            Destroy(item.gameObject);
        }

        RectTransform GetResourceScoreTarget(int resourceId)
        {
            if (!resourceLabels.TryGetValue(resourceId, out var label) || label == null)
            {
                return null;
            }

            return label.transform as RectTransform;
        }

        static Vector2 QuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float t)
        {
            var a = Vector2.Lerp(start, control, t);
            var b = Vector2.Lerp(control, end, t);
            return Vector2.Lerp(a, b, t);
        }
        GameObject LoadEffectPrefab(string effectId)
        {
            var effect = database.GetEffect(effectId);
            if (effect == null || string.IsNullOrWhiteSpace(effect.prefabPath))
            {
                return null;
            }

            return UnityEngine.Resources.Load<GameObject>(effect.prefabPath);
        }

        static Image AddFullScreenImage(string name, Sprite sprite, RectTransform parent, Color color)
        {
            var image = AddAnchoredImage(name, sprite, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static Image AddAnchoredImage(string name, Sprite sprite, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var rect = CreateRect(name, parent, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        static RectTransform CreateRect(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            return rect;
        }


        static bool CircleOverlapsRect(Vector2 circleCenter, float radius, RectTransform rectTransform)
        {
            var rect = rectTransform.rect;
            var rectCenter = rectTransform.anchoredPosition;
            var inset = Mathf.Min(rect.width, rect.height) * PieceHitboxInsetRatio;
            var min = rectCenter + rect.min + new Vector2(inset, inset);
            var max = rectCenter + rect.max - new Vector2(inset, inset);
            var closest = new Vector2(
                Mathf.Clamp(circleCenter.x, min.x, max.x),
                Mathf.Clamp(circleCenter.y, min.y, max.y));

            return (closest - circleCenter).sqrMagnitude <= radius * radius;
        }
        static bool TryGetPointerPosition(out Vector2 screenPosition)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            screenPosition = Input.mousePosition;
            return true;
#else
            screenPosition = Vector2.zero;
            return false;
#endif
        }

        static Sprite LoadSprite(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? null : UnityEngine.Resources.Load<Sprite>(SpriteRoot + id);
        }

        static Sprite CreateCircleSprite(int size, Color fill, Color outline)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var radius = size * 0.46f;
            var outlineStart = radius - 4f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center);
                    var color = Color.clear;
                    if (distance <= radius)
                    {
                        color = distance >= outlineStart ? outline : fill;
                    }
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem.transform.SetAsFirstSibling();
        }
    }
}














