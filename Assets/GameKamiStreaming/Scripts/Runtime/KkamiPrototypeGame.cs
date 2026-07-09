using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
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
        const float SpawnPieceSizeScale = 0.8f;
        const int MiningAttackFrameCount = 12;
        const float MiningAttackFrameSeconds = 0.025f;
        const float MiningAttackSize = 512f;
        const float MiningAttackDisplaySize = MiningAttackSize * 0.7f;
        const string MiningAttackFrameRoot = "GameKamiStreaming/Sprites/mining_attack/frame_";
        const float KkamiAppearIntervalSeconds = 5f;
        const float SkillTreeContentSize = 2600f;
        const float SkillTreeMinZoom = 0.55f;
        const float SkillTreeMaxZoom = 2.5f;
        const float SkillTreeZoomStep = 0.18f;
        const string SkillTreeBackgroundSpriteId = "skilltree_bg";
        static readonly string[] KkamiAppearSpriteIds =
        {
            "kkami_appear/love",
            "kkami_appear/confused",
            "kkami_appear/angry",
            "kkami_appear/super_angry",
            "kkami_appear/shocked",
            "kkami_appear/sad"
        };

        readonly Dictionary<int, int> resourceAmounts = new Dictionary<int, int>();
        readonly Dictionary<int, PixelNumberLabel> resourceLabels = new Dictionary<int, PixelNumberLabel>();
        readonly List<DestructiblePieceView> activePieces = new List<DestructiblePieceView>();
        readonly List<Sprite> miningAttackFrames = new List<Sprite>();
        readonly List<Sprite> kkamiAppearSprites = new List<Sprite>();

        [SerializeField] Camera uiCamera;
        [SerializeField] RectTransform canvasRoot;
        [SerializeField] RectTransform stageArea;
        [SerializeField] RectTransform pieceLayer;
        [SerializeField] RectTransform pieceDisplayLayer;
        [SerializeField] RectTransform effectLayer;
        [SerializeField] RectTransform miningCursor;
        [SerializeField] PixelNumberLabel roundTimerLabel;
        [SerializeField] RectTransform skillTreeCanvasRoot;
        [SerializeField] RectTransform skillTreeContentRoot;
        [SerializeField] Button startNextStageButton;
        [SerializeField] RectTransform spawnPoint1;
        [SerializeField] RectTransform spawnPoint2;
        [SerializeField] RectTransform spawnPoint3;
        [SerializeField] RectTransform spawnPoint4;
        [SerializeField] Image miningAttackImage;
        [SerializeField] Image kkamiAppearImage;

        KkamiTableDatabase database;
        StageRow currentStage;
        int currentStageIndex;
        float roundRemainingSeconds;
        float kkamiAppearTimer;
        float skillTreeZoom = 1f;
        Vector2 previousSkillTreePointerPosition;
        int lastKkamiAppearIndex = -1;
        int displayedRoundSecond = -1;
        bool skillTreeOpen;
        bool miningAttackPlaying;
        bool kkamiAppearChanging;
        bool skillTreeDragging;

        void Awake()
        {
            InitializeGame();
        }

        void Start()
        {
            if (!skillTreeOpen)
            {
                FillStagePieces();
            }
        }

        void Update()
        {
            if (skillTreeOpen)
            {
                UpdateSkillTreeNavigation();
                return;
            }

            UpdateRoundTimer();
            UpdateKkamiAppear();
            UpdateMiningCursor();
            UpdateMiningAttack();
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

        public void EnsureEditableSkillTreeCanvas()
        {
            EnsureEventSystem();
            if (uiCamera == null)
            {
                uiCamera = Camera.main;
            }

            EnsureSkillTreeCanvas();
            if (skillTreeCanvasRoot != null)
            {
                skillTreeCanvasRoot.gameObject.SetActive(false);
            }
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
            currentStageIndex = 0;
            currentStage = database.Stages.Count > 0 ? database.Stages[currentStageIndex] : null;
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

            EnsureEffectLayer();
            EnsurePieceDisplayLayer();
            EnsureSpawnPointReferences();
            EnsureMiningAttackView();
            EnsureKkamiAppearView();
            EnsureRoundTimerLabel();
            EnsureSkillTreeCanvas();
            ShowGameCanvas();
            ResetRoundTimer();

            var cursorImage = miningCursor.GetComponent<Image>();
            if (cursorImage != null && cursorImage.sprite == null)
            {
                cursorImage.sprite = CreateCircleSprite(96, new Color(0.2f, 0.85f, 1f, 0.12f), new Color(0.2f, 0.95f, 1f, 0.48f), 1f);
            }
            miningCursor.gameObject.SetActive(false);
        }

        void EnsureEffectLayer()
        {
            if (effectLayer == null && canvasRoot != null)
            {
                var existing = canvasRoot.Find("Effect Layer") as RectTransform;
                if (existing != null)
                {
                    effectLayer = existing;
                }
            }

            if (effectLayer == null && canvasRoot != null)
            {
                effectLayer = CreateRect("Effect Layer", canvasRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            }

            if (effectLayer == null)
            {
                return;
            }

            effectLayer.gameObject.SetActive(true);
            effectLayer.anchorMin = Vector2.zero;
            effectLayer.anchorMax = Vector2.one;
            effectLayer.pivot = new Vector2(0.5f, 0.5f);
            effectLayer.anchoredPosition = Vector2.zero;
            effectLayer.sizeDelta = Vector2.zero;
            effectLayer.localScale = Vector3.one;
            effectLayer.SetAsLastSibling();
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

            roundTimerLabel = BuildRoundTimer(canvasRoot);
            skillTreeCanvasRoot = BuildSkillTreeCanvas();
            miningCursor = CreateMiningCursor(canvasRoot);
            miningAttackImage = CreateMiningAttackView(canvasRoot);
        }

        PixelNumberLabel BuildRoundTimer(RectTransform parent)
        {
            var root = CreateRect("Round Timer", parent, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(48f, 40f), new Vector2(220f, 72f));
            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = -6f;

            var label = root.gameObject.AddComponent<PixelNumberLabel>();
            label.Initialize();
            label.SetText("0:30");
            return label;
        }

        RectTransform BuildSkillTreeCanvas()
        {
            var canvas = new GameObject("Skill Tree Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCamera;
            canvas.planeDistance = 9f;
            canvas.sortingOrder = 50;

            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var root = canvas.transform as RectTransform;
            var backdrop = AddFullScreenImage("Skill Tree Backdrop", LoadSprite(SkillTreeBackgroundSpriteId), root, Color.white);
            ConfigureSkillTreeBackdrop(backdrop);

            skillTreeContentRoot = CreateRect("Skill Tree Empty Fields", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(SkillTreeContentSize, SkillTreeContentSize));
            var panelImage = skillTreeContentRoot.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.12f, 0.13f, 0.16f, 0.92f);
            panelImage.raycastTarget = false;

            startNextStageButton = BuildNextStageButton(root);
            root.gameObject.SetActive(false);
            return root;
        }

        Button BuildNextStageButton(RectTransform parent)
        {
            var buttonRoot = CreateRect("Start Next Stage Button", parent, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-56f, 56f), new Vector2(430f, 120f));
            var image = buttonRoot.gameObject.AddComponent<Image>();
            image.color = new Color(0.95f, 0.72f, 0.18f, 1f);

            var button = buttonRoot.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(StartNextStageFromSkillTree);

            var label = CreateRect("Label", buttonRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var text = label.gameObject.AddComponent<Text>();
            text.text = "NEXT STAGE";
            text.font = LoadDefaultFont();
            text.fontSize = 42;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.08f, 0.07f, 0.04f, 1f);
            text.raycastTarget = false;

            return button;
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
            image.sprite = CreateCircleSprite(96, new Color(0.2f, 0.85f, 1f, 0.12f), new Color(0.2f, 0.95f, 1f, 0.48f), 1f);
            image.raycastTarget = false;
            cursor.gameObject.SetActive(false);
            return cursor;
        }

        Image CreateMiningAttackView(RectTransform parent)
        {
            var rect = CreateRect("Mining Attack Motion", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1f / 3f, 0f), Vector2.zero, new Vector2(MiningAttackDisplaySize, MiningAttackDisplaySize));
            var image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            rect.gameObject.SetActive(false);
            rect.SetAsLastSibling();
            return image;
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

        void EnsureSpawnPointReferences()
        {
            if (spawnPoint1 == null)
            {
                spawnPoint1 = FindRectInGameCanvas("spawnpoint1");
            }

            if (spawnPoint2 == null)
            {
                spawnPoint2 = FindRectInGameCanvas("spawnpoint2");
            }

            if (spawnPoint3 == null)
            {
                spawnPoint3 = FindRectInGameCanvas("spawnpoint3");
            }

            if (spawnPoint4 == null)
            {
                spawnPoint4 = FindRectInGameCanvas("spawnpoint4");
                if (spawnPoint4 == null)
                {
                    spawnPoint4 = FindRectInGameCanvas("spawnpoint");
                }
            }

            StabilizeSpawnPoint(spawnPoint1);
            StabilizeSpawnPoint(spawnPoint2);
            StabilizeSpawnPoint(spawnPoint3);
            StabilizeSpawnPoint(spawnPoint4);
        }

        static void StabilizeSpawnPoint(RectTransform spawnPoint)
        {
            if (spawnPoint == null)
            {
                return;
            }

            var worldPosition = spawnPoint.position;
            spawnPoint.anchorMin = new Vector2(0.5f, 0.5f);
            spawnPoint.anchorMax = new Vector2(0.5f, 0.5f);
            spawnPoint.pivot = new Vector2(0.5f, 0.5f);
            spawnPoint.sizeDelta = new Vector2(32f, 32f);
            spawnPoint.localScale = Vector3.one;
            spawnPoint.position = worldPosition;
        }

        void EnsureMiningAttackView()
        {
            LoadMiningAttackFrames();
            if (miningAttackImage != null)
            {
                var rect = miningAttackImage.rectTransform;
                miningAttackImage.raycastTarget = false;
                miningAttackImage.preserveAspect = true;
                rect.pivot = new Vector2(1f / 3f, 0f);
                rect.sizeDelta = new Vector2(MiningAttackDisplaySize, MiningAttackDisplaySize);
                rect.gameObject.SetActive(false);
                rect.SetAsLastSibling();
                return;
            }

            var existing = canvasRoot != null ? canvasRoot.Find("Mining Attack Motion") as RectTransform : null;
            if (existing != null)
            {
                miningAttackImage = existing.GetComponent<Image>();
                if (miningAttackImage == null)
                {
                    miningAttackImage = existing.gameObject.AddComponent<Image>();
                }
                miningAttackImage.raycastTarget = false;
                miningAttackImage.preserveAspect = true;
                existing.pivot = new Vector2(1f / 3f, 0f);
                existing.sizeDelta = new Vector2(MiningAttackDisplaySize, MiningAttackDisplaySize);
                existing.gameObject.SetActive(false);
                existing.SetAsLastSibling();
                return;
            }

            if (canvasRoot != null)
            {
                miningAttackImage = CreateMiningAttackView(canvasRoot);
            }
        }

        void LoadMiningAttackFrames()
        {
            if (miningAttackFrames.Count > 0)
            {
                return;
            }

            for (var i = 0; i < MiningAttackFrameCount; i++)
            {
                var framePath = MiningAttackFrameRoot + i.ToString("000");
                var texture = Resources.Load<Texture2D>(framePath);
                if (texture == null)
                {
                    var sprite = Resources.Load<Sprite>(framePath);
                    if (sprite != null)
                    {
                        miningAttackFrames.Add(sprite);
                    }
                }
                else
                {
                    miningAttackFrames.Add(Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(1f / 3f, 0f), 100f));
                }
            }
        }

        void EnsureKkamiAppearView()
        {
            LoadKkamiAppearSprites();
            if (kkamiAppearImage == null)
            {
                var existing = FindRectInGameCanvas("kkami appear");
                if (existing != null)
                {
                    kkamiAppearImage = existing.GetComponent<Image>();
                    if (kkamiAppearImage == null)
                    {
                        kkamiAppearImage = existing.gameObject.AddComponent<Image>();
                    }
                }
            }

            if (kkamiAppearImage == null)
            {
                return;
            }

            kkamiAppearImage.type = Image.Type.Simple;
            kkamiAppearImage.color = Color.white;
            kkamiAppearImage.preserveAspect = true;
            kkamiAppearImage.raycastTarget = false;
            ShowRandomKkamiAppearSprite(true);
        }

        void LoadKkamiAppearSprites()
        {
            if (kkamiAppearSprites.Count > 0)
            {
                return;
            }

            foreach (var id in KkamiAppearSpriteIds)
            {
                var sprite = LoadSprite(id);
                if (sprite != null)
                {
                    kkamiAppearSprites.Add(sprite);
                    continue;
                }

                var texture = Resources.Load<Texture2D>(SpriteRoot + id);
                if (texture != null)
                {
                    kkamiAppearSprites.Add(Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f));
                }
            }
        }

        void UpdateKkamiAppear()
        {
            if (kkamiAppearImage == null || kkamiAppearSprites.Count == 0 || kkamiAppearChanging)
            {
                return;
            }

            kkamiAppearTimer -= Time.deltaTime;
            if (kkamiAppearTimer > 0f)
            {
                return;
            }

            StartCoroutine(PlayKkamiAppearChange());
        }

        void ShowRandomKkamiAppearSprite(bool force)
        {
            if (kkamiAppearImage == null || kkamiAppearSprites.Count == 0)
            {
                return;
            }

            if (!force && kkamiAppearTimer > 0f)
            {
                return;
            }

            var nextIndex = Random.Range(0, kkamiAppearSprites.Count);
            if (kkamiAppearSprites.Count > 1 && nextIndex == lastKkamiAppearIndex)
            {
                nextIndex = (nextIndex + Random.Range(1, kkamiAppearSprites.Count)) % kkamiAppearSprites.Count;
            }

            kkamiAppearImage.sprite = kkamiAppearSprites[nextIndex];
            kkamiAppearImage.enabled = true;
            lastKkamiAppearIndex = nextIndex;
            kkamiAppearTimer = KkamiAppearIntervalSeconds;
        }

        IEnumerator PlayKkamiAppearChange()
        {
            if (kkamiAppearImage == null || kkamiAppearSprites.Count == 0)
            {
                yield break;
            }

            kkamiAppearChanging = true;
            var rect = kkamiAppearImage.rectTransform;
            var baseScale = rect.localScale;
            var targetScale = baseScale * 1.06f;
            const float growDuration = 0.06f;
            const float shrinkDuration = 0.08f;

            var elapsed = 0f;
            while (elapsed < growDuration)
            {
                if (rect == null)
                {
                    kkamiAppearChanging = false;
                    yield break;
                }

                elapsed += Time.deltaTime;
                rect.localScale = Vector3.Lerp(baseScale, targetScale, Mathf.Clamp01(elapsed / growDuration));
                yield return null;
            }

            ShowRandomKkamiAppearSprite(true);

            elapsed = 0f;
            while (elapsed < shrinkDuration)
            {
                if (rect == null)
                {
                    kkamiAppearChanging = false;
                    yield break;
                }

                elapsed += Time.deltaTime;
                rect.localScale = Vector3.Lerp(targetScale, baseScale, Mathf.Clamp01(elapsed / shrinkDuration));
                yield return null;
            }

            if (rect != null)
            {
                rect.localScale = baseScale;
            }

            kkamiAppearChanging = false;
        }

        RectTransform FindRectInGameCanvas(string objectName)
        {
            if (canvasRoot == null)
            {
                return null;
            }

            var rects = canvasRoot.GetComponentsInChildren<RectTransform>(true);
            foreach (var rect in rects)
            {
                if (rect.name == objectName)
                {
                    return rect;
                }
            }

            return null;
        }

        static RectTransform FindChildRect(RectTransform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            var rects = root.GetComponentsInChildren<RectTransform>(true);
            foreach (var rect in rects)
            {
                if (rect.name == objectName)
                {
                    return rect;
                }
            }

            return null;
        }

        void EnsureRoundTimerLabel()
        {
            if (roundTimerLabel != null)
            {
                roundTimerLabel.Initialize();
                return;
            }

            var existing = canvasRoot != null ? canvasRoot.Find("Round Timer") as RectTransform : null;
            if (existing != null)
            {
                roundTimerLabel = existing.GetComponent<PixelNumberLabel>();
                if (roundTimerLabel == null)
                {
                    roundTimerLabel = existing.gameObject.AddComponent<PixelNumberLabel>();
                }
                roundTimerLabel.Initialize();
                return;
            }

            if (canvasRoot != null)
            {
                roundTimerLabel = BuildRoundTimer(canvasRoot);
            }
        }

        void EnsureSkillTreeCanvas()
        {
            if (skillTreeCanvasRoot != null)
            {
                EnsureSkillTreeBackdrop();
                EnsureSkillTreeContentRoot();
                EnsureStartNextStageButton();
                skillTreeCanvasRoot.gameObject.SetActive(false);
                return;
            }

            var existing = GameObject.Find("Skill Tree Canvas");
            if (existing != null)
            {
                skillTreeCanvasRoot = existing.transform as RectTransform;
                startNextStageButton = existing.GetComponentInChildren<Button>(true);
                EnsureSkillTreeBackdrop();
                EnsureSkillTreeContentRoot();
                EnsureStartNextStageButton();
                skillTreeCanvasRoot.gameObject.SetActive(false);
                return;
            }

            skillTreeCanvasRoot = BuildSkillTreeCanvas();
            EnsureSkillTreeBackdrop();
            EnsureSkillTreeContentRoot();
            EnsureStartNextStageButton();
        }

        void EnsureSkillTreeBackdrop()
        {
            if (skillTreeCanvasRoot == null)
            {
                return;
            }

            var backdropRect = FindChildRect(skillTreeCanvasRoot, "Skill Tree Backdrop");
            if (backdropRect == null)
            {
                var image = AddFullScreenImage("Skill Tree Backdrop", LoadSprite(SkillTreeBackgroundSpriteId), skillTreeCanvasRoot, Color.white);
                backdropRect = image.rectTransform;
            }

            backdropRect.SetParent(skillTreeCanvasRoot, false);
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.pivot = new Vector2(0.5f, 0.5f);
            backdropRect.anchoredPosition = Vector2.zero;
            backdropRect.sizeDelta = Vector2.zero;
            backdropRect.SetAsFirstSibling();

            var backdrop = backdropRect.GetComponent<Image>();
            if (backdrop != null)
            {
                ConfigureSkillTreeBackdrop(backdrop);
            }
        }

        static void ConfigureSkillTreeBackdrop(Image backdrop)
        {
            if (backdrop == null)
            {
                return;
            }

            var sprite = LoadSprite(SkillTreeBackgroundSpriteId);
            backdrop.sprite = sprite;
            backdrop.color = Color.white;
            backdrop.type = Image.Type.Simple;
            backdrop.preserveAspect = true;
            backdrop.raycastTarget = false;

            var fitter = backdrop.GetComponent<AspectRatioFitter>();
            if (fitter == null)
            {
                fitter = backdrop.gameObject.AddComponent<AspectRatioFitter>();
            }

            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite != null && sprite.rect.height > 0f ? sprite.rect.width / sprite.rect.height : 16f / 9f;
        }

        void EnsureSkillTreeContentRoot()
        {
            if (skillTreeCanvasRoot == null)
            {
                return;
            }

            if (skillTreeContentRoot == null)
            {
                var existing = FindChildRect(skillTreeCanvasRoot, "Skill Tree Empty Fields");
                if (existing != null)
                {
                    skillTreeContentRoot = existing;
                }
            }

            if (skillTreeContentRoot == null)
            {
                skillTreeContentRoot = CreateRect("Skill Tree Empty Fields", skillTreeCanvasRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(SkillTreeContentSize, SkillTreeContentSize));
                var image = skillTreeContentRoot.gameObject.AddComponent<Image>();
                image.color = new Color(0.12f, 0.13f, 0.16f, 0.92f);
            }

            skillTreeContentRoot.SetParent(skillTreeCanvasRoot, false);
            skillTreeContentRoot.anchorMin = new Vector2(0.5f, 0.5f);
            skillTreeContentRoot.anchorMax = new Vector2(0.5f, 0.5f);
            skillTreeContentRoot.pivot = new Vector2(0.5f, 0.5f);
            skillTreeContentRoot.sizeDelta = new Vector2(SkillTreeContentSize, SkillTreeContentSize);
            skillTreeContentRoot.localScale = Vector3.one * skillTreeZoom;

            var imageComponent = skillTreeContentRoot.GetComponent<Image>();
            if (imageComponent != null)
            {
                imageComponent.color = new Color(0.12f, 0.13f, 0.16f, 0.92f);
                imageComponent.raycastTarget = false;
            }

            ClampSkillTreeContentPosition();
        }

        void EnsureStartNextStageButton()
        {
            if (startNextStageButton == null && skillTreeCanvasRoot != null)
            {
                var existing = FindChildRect(skillTreeCanvasRoot, "Start Next Stage Button");
                if (existing != null)
                {
                    startNextStageButton = existing.GetComponent<Button>();
                }
            }

            if (startNextStageButton == null || startNextStageButton.onClick == null)
            {
                return;
            }

            if (skillTreeCanvasRoot != null && startNextStageButton.transform.parent != skillTreeCanvasRoot)
            {
                startNextStageButton.transform.SetParent(skillTreeCanvasRoot, false);
            }

            var rect = startNextStageButton.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(-56f, 56f);
                rect.sizeDelta = new Vector2(430f, 120f);
                rect.localScale = Vector3.one;
                rect.SetAsLastSibling();
            }

            startNextStageButton.onClick.RemoveListener(StartNextStageFromSkillTree);
            if (!HasPersistentStartNextStageListener(startNextStageButton))
            {
                startNextStageButton.onClick.AddListener(StartNextStageFromSkillTree);
            }
        }

        bool HasPersistentStartNextStageListener(Button button)
        {
            for (var i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentTarget(i) == this && button.onClick.GetPersistentMethodName(i) == nameof(StartNextStageFromSkillTree))
                {
                    return true;
                }
            }

            return false;
        }

        void ResetRoundTimer()
        {
            roundRemainingSeconds = currentStage != null ? Mathf.Max(1, currentStage.timeLimitSeconds) : 0f;
            displayedRoundSecond = -1;
            UpdateRoundTimerLabel(true);
        }

        void UpdateRoundTimer()
        {
            if (currentStage == null || roundRemainingSeconds <= 0f)
            {
                UpdateRoundTimerLabel(false);
                return;
            }

            roundRemainingSeconds = Mathf.Max(0f, roundRemainingSeconds - Time.deltaTime);
            UpdateRoundTimerLabel(false);
            if (roundRemainingSeconds <= 0f)
            {
                OpenSkillTree();
            }
        }

        void UpdateRoundTimerLabel(bool force)
        {
            if (roundTimerLabel == null)
            {
                return;
            }

            var wholeSeconds = Mathf.CeilToInt(roundRemainingSeconds);
            if (!force && wholeSeconds == displayedRoundSecond)
            {
                return;
            }

            displayedRoundSecond = wholeSeconds;
            roundTimerLabel.SetText(FormatRoundTime(wholeSeconds));
        }

        void OpenSkillTree()
        {
            skillTreeOpen = true;
            skillTreeDragging = false;
            HideMiningAttack();
            ClearActivePieces();

            EnsureSkillTreeCanvas();
            ApplySkillTreeTransform();
            if (skillTreeCanvasRoot != null)
            {
                skillTreeCanvasRoot.gameObject.SetActive(true);
            }
        }

        public void StartNextStageFromSkillTree()
        {
            if (database.Stages.Count > 0)
            {
                currentStageIndex = (currentStageIndex + 1) % database.Stages.Count;
                currentStage = database.Stages[currentStageIndex];
            }

            skillTreeOpen = false;
            ShowGameCanvas();
            ResetRoundTimer();
            FillStagePieces();
        }

        void UpdateSkillTreeNavigation()
        {
            if (skillTreeCanvasRoot == null || skillTreeContentRoot == null)
            {
                EnsureSkillTreeCanvas();
                return;
            }

            if (TryGetPointerPosition(out var pointerPosition) && TryGetScrollDelta(out var scrollDelta) && Mathf.Abs(scrollDelta) > 0.001f)
            {
                var normalizedScroll = Mathf.Abs(scrollDelta) > 10f ? scrollDelta / 120f : scrollDelta;
                ZoomSkillTree(Mathf.Clamp(normalizedScroll, -1f, 1f), pointerPosition);
            }

            if (!TryGetPointerPosition(out pointerPosition) || !TryGetLeftMouseButton())
            {
                skillTreeDragging = false;
                return;
            }

            if (skillTreeZoom <= 1.001f || IsPointerOverNextStageButton(pointerPosition))
            {
                skillTreeDragging = false;
                return;
            }

            if (!skillTreeDragging)
            {
                previousSkillTreePointerPosition = pointerPosition;
                skillTreeDragging = true;
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(skillTreeCanvasRoot, previousSkillTreePointerPosition, uiCamera, out var previousPoint) &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(skillTreeCanvasRoot, pointerPosition, uiCamera, out var currentPoint))
            {
                skillTreeContentRoot.anchoredPosition += currentPoint - previousPoint;
                ClampSkillTreeContentPosition();
            }

            previousSkillTreePointerPosition = pointerPosition;
        }

        void ZoomSkillTree(float scrollStep, Vector2 pointerPosition)
        {
            if (skillTreeCanvasRoot == null || skillTreeContentRoot == null)
            {
                return;
            }

            var previousZoom = skillTreeZoom;
            skillTreeZoom = Mathf.Clamp(skillTreeZoom + scrollStep * SkillTreeZoomStep, SkillTreeMinZoom, SkillTreeMaxZoom);
            if (Mathf.Approximately(previousZoom, skillTreeZoom))
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(skillTreeCanvasRoot, pointerPosition, uiCamera, out var canvasPoint))
            {
                var contentPointAtPointer = (canvasPoint - skillTreeContentRoot.anchoredPosition) / previousZoom;
                skillTreeContentRoot.anchoredPosition = canvasPoint - contentPointAtPointer * skillTreeZoom;
            }

            ApplySkillTreeTransform();
        }

        void ApplySkillTreeTransform()
        {
            if (skillTreeContentRoot == null)
            {
                return;
            }

            skillTreeContentRoot.localScale = Vector3.one * skillTreeZoom;
            ClampSkillTreeContentPosition();
        }

        void ClampSkillTreeContentPosition()
        {
            if (skillTreeCanvasRoot == null || skillTreeContentRoot == null)
            {
                return;
            }

            var viewportSize = skillTreeCanvasRoot.rect.size;
            var contentSize = skillTreeContentRoot.rect.size * skillTreeZoom;
            var maxOffset = new Vector2(
                Mathf.Max(0f, (contentSize.x - viewportSize.x) * 0.5f),
                Mathf.Max(0f, (contentSize.y - viewportSize.y) * 0.5f));

            var position = skillTreeContentRoot.anchoredPosition;
            position.x = Mathf.Clamp(position.x, -maxOffset.x, maxOffset.x);
            position.y = Mathf.Clamp(position.y, -maxOffset.y, maxOffset.y);
            skillTreeContentRoot.anchoredPosition = position;
        }

        bool IsPointerOverNextStageButton(Vector2 pointerPosition)
        {
            var rect = startNextStageButton != null ? startNextStageButton.transform as RectTransform : null;
            return rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, pointerPosition, uiCamera);
        }

        void ShowGameCanvas()
        {
            if (skillTreeCanvasRoot != null)
            {
                skillTreeCanvasRoot.gameObject.SetActive(false);
            }
        }

        void ClearActivePieces()
        {
            for (var i = activePieces.Count - 1; i >= 0; i--)
            {
                if (activePieces[i] != null)
                {
                    Destroy(activePieces[i].gameObject);
                }
            }
            activePieces.Clear();
        }

        static string FormatRoundTime(int totalSeconds)
        {
            totalSeconds = Mathf.Max(0, totalSeconds);
            return (totalSeconds / 60) + ":" + (totalSeconds % 60).ToString("00");
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
            if (skillTreeOpen)
            {
                yield break;
            }

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

            var size = Random.Range(155f, 215f) * SpawnPieceSizeScale;
            var half = size * 0.5f;
            EnsurePieceDisplayLayer();
            var displayPosition = PickSpawnPosition(half);
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

        Vector2 PickSpawnPosition(float halfSize)
        {
            if (TryGetSpawnPolygon(out var polygon))
            {
                var centroid = GetCentroid(polygon);
                for (var i = 0; i < 48; i++)
                {
                    var candidate = SamplePointInPolygon(polygon);
                    if (IsPieceInsidePolygon(candidate, halfSize, polygon))
                    {
                        return candidate;
                    }
                }

                if (TryFindGridSpawnPosition(polygon, halfSize, centroid, out var gridCandidate))
                {
                    return gridCandidate;
                }

                return IsPointInsideConvexPolygon(centroid, polygon) ? centroid : polygon[0];
            }

            var rect = pieceLayer.rect;
            var anchoredPosition = new Vector2(Random.Range(rect.xMin + halfSize, rect.xMax - halfSize), Random.Range(rect.yMin + halfSize, rect.yMax - halfSize));
            var worldPosition = pieceLayer.TransformPoint(anchoredPosition);
            return (Vector2)pieceDisplayLayer.InverseTransformPoint(worldPosition);
        }

        bool TryGetSpawnPolygon(out Vector2[] points)
        {
            EnsureSpawnPointReferences();
            points = null;

            if (pieceDisplayLayer == null || spawnPoint1 == null || spawnPoint2 == null || spawnPoint3 == null || spawnPoint4 == null)
            {
                return false;
            }

            points = new[]
            {
                (Vector2)pieceDisplayLayer.InverseTransformPoint(spawnPoint1.position),
                (Vector2)pieceDisplayLayer.InverseTransformPoint(spawnPoint2.position),
                (Vector2)pieceDisplayLayer.InverseTransformPoint(spawnPoint3.position),
                (Vector2)pieceDisplayLayer.InverseTransformPoint(spawnPoint4.position)
            };

            SortPolygonPoints(points);
            return Mathf.Abs(GetSignedArea(points)) > 0.01f;
        }

        static void SortPolygonPoints(Vector2[] points)
        {
            var center = GetCentroid(points);
            for (var i = 0; i < points.Length - 1; i++)
            {
                for (var j = i + 1; j < points.Length; j++)
                {
                    var angleA = Mathf.Atan2(points[i].y - center.y, points[i].x - center.x);
                    var angleB = Mathf.Atan2(points[j].y - center.y, points[j].x - center.x);
                    if (angleA <= angleB)
                    {
                        continue;
                    }

                    var temp = points[i];
                    points[i] = points[j];
                    points[j] = temp;
                }
            }
        }

        static Vector2 SamplePointInPolygon(Vector2[] polygon)
        {
            var areaA = Mathf.Abs(Cross(polygon[1] - polygon[0], polygon[2] - polygon[0]));
            var areaB = Mathf.Abs(Cross(polygon[2] - polygon[0], polygon[3] - polygon[0]));
            return Random.value * (areaA + areaB) <= areaA
                ? SamplePointInTriangle(polygon[0], polygon[1], polygon[2])
                : SamplePointInTriangle(polygon[0], polygon[2], polygon[3]);
        }

        static Vector2 SamplePointInTriangle(Vector2 a, Vector2 b, Vector2 c)
        {
            var u = Random.value;
            var v = Random.value;
            if (u + v > 1f)
            {
                u = 1f - u;
                v = 1f - v;
            }

            return a + (b - a) * u + (c - a) * v;
        }

        static bool IsPieceInsidePolygon(Vector2 center, float halfSize, Vector2[] polygon)
        {
            return IsPointInsideConvexPolygon(center + new Vector2(-halfSize, -halfSize), polygon)
                && IsPointInsideConvexPolygon(center + new Vector2(-halfSize, halfSize), polygon)
                && IsPointInsideConvexPolygon(center + new Vector2(halfSize, halfSize), polygon)
                && IsPointInsideConvexPolygon(center + new Vector2(halfSize, -halfSize), polygon);
        }

        static bool TryFindGridSpawnPosition(Vector2[] polygon, float halfSize, Vector2 centroid, out Vector2 result)
        {
            result = centroid;
            GetPolygonBounds(polygon, out var min, out var max);

            var found = false;
            var bestDistance = float.MaxValue;
            const int divisions = 12;
            for (var y = 0; y <= divisions; y++)
            {
                for (var x = 0; x <= divisions; x++)
                {
                    var candidate = new Vector2(
                        Mathf.Lerp(min.x, max.x, x / (float)divisions),
                        Mathf.Lerp(min.y, max.y, y / (float)divisions));
                    if (!IsPieceInsidePolygon(candidate, halfSize, polygon))
                    {
                        continue;
                    }

                    var distance = (candidate - centroid).sqrMagnitude;
                    if (distance >= bestDistance)
                    {
                        continue;
                    }

                    found = true;
                    bestDistance = distance;
                    result = candidate;
                }
            }

            return found;
        }

        static void GetPolygonBounds(Vector2[] polygon, out Vector2 min, out Vector2 max)
        {
            min = polygon[0];
            max = polygon[0];
            for (var i = 1; i < polygon.Length; i++)
            {
                min = Vector2.Min(min, polygon[i]);
                max = Vector2.Max(max, polygon[i]);
            }
        }

        static bool IsPointInsideConvexPolygon(Vector2 point, Vector2[] polygon)
        {
            var sign = 0f;
            for (var i = 0; i < polygon.Length; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Length];
                var cross = Cross(b - a, point - a);
                if (Mathf.Abs(cross) <= 0.001f)
                {
                    continue;
                }

                if (Mathf.Approximately(sign, 0f))
                {
                    sign = Mathf.Sign(cross);
                }
                else if (Mathf.Sign(cross) != sign)
                {
                    return false;
                }
            }

            return true;
        }

        static Vector2 GetCentroid(Vector2[] points)
        {
            var centroid = Vector2.zero;
            for (var i = 0; i < points.Length; i++)
            {
                centroid += points[i];
            }

            return centroid / points.Length;
        }

        static float GetSignedArea(Vector2[] polygon)
        {
            var area = 0f;
            for (var i = 0; i < polygon.Length; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Length];
                area += a.x * b.y - b.x * a.y;
            }

            return area * 0.5f;
        }

        static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
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

            var insideStage = IsPointerInsideMiningArea(pointerPosition);
            miningCursor.gameObject.SetActive(insideStage);
            miningCursor.anchoredPosition = canvasPoint;
        }

        bool IsPointerInsideMiningArea(Vector2 pointerPosition)
        {
            EnsurePieceDisplayLayer();
            if (TryGetSpawnPolygon(out var polygon) &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(pieceDisplayLayer, pointerPosition, uiCamera, out var displayPoint))
            {
                return IsPointInsideConvexPolygon(displayPoint, polygon);
            }

            return pieceLayer != null && RectTransformUtility.RectangleContainsScreenPoint(pieceLayer, pointerPosition, uiCamera);
        }

        void UpdateMiningAttack()
        {
            if (miningAttackPlaying || skillTreeOpen)
            {
                return;
            }

            if (!TryGetPointerPosition(out var pointerPosition) || miningCursor == null || !miningCursor.gameObject.activeSelf)
            {
                return;
            }

            if (!IsPointerInsideMiningArea(pointerPosition))
            {
                return;
            }

            StartCoroutine(PlayMiningAttack());
        }

        IEnumerator PlayMiningAttack()
        {
            miningAttackPlaying = true;
            EnsureMiningAttackView();

            if (miningAttackFrames.Count == 0 || miningAttackImage == null)
            {
                ApplyMiningDamageAtPointer();
                miningAttackPlaying = false;
                yield break;
            }

            miningAttackImage.gameObject.SetActive(true);
            miningAttackImage.rectTransform.SetAsLastSibling();

            for (var frameIndex = 0; frameIndex < miningAttackFrames.Count; frameIndex++)
            {
                if (skillTreeOpen)
                {
                    HideMiningAttack();
                    yield break;
                }

                miningAttackImage.sprite = miningAttackFrames[frameIndex];

                var elapsed = 0f;
                while (elapsed < MiningAttackFrameSeconds)
                {
                    UpdateMiningAttackPosition();
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            HideMiningAttack();
            ApplyMiningDamageAtPointer();
            miningAttackPlaying = false;
        }

        void HideMiningAttack()
        {
            if (miningAttackImage != null)
            {
                miningAttackImage.gameObject.SetActive(false);
            }

            miningAttackPlaying = false;
        }

        bool UpdateMiningAttackPosition()
        {
            if (miningAttackImage == null ||
                canvasRoot == null ||
                !TryGetPointerPosition(out var pointerPosition) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, pointerPosition, uiCamera, out var canvasPoint))
            {
                return false;
            }

            miningAttackImage.rectTransform.anchoredPosition = canvasPoint;
            return true;
        }

        void ApplyMiningDamageAtPointer()
        {
            if (skillTreeOpen || !TryGetPointerPosition(out var pointerPosition) || !IsPointerInsideMiningArea(pointerPosition))
            {
                return;
            }

            EnsurePieceDisplayLayer();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(pieceDisplayLayer, pointerPosition, uiCamera, out var localPoint))
            {
                return;
            }

            var damage = DamagePerSecond * MiningAttackFrameSeconds * Mathf.Max(1, miningAttackFrames.Count);

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
                    piece.Hit(damage, true);
                }
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
            EnsureEffectLayer();
            if (effectLayer == null || source == null)
            {
                return;
            }

            effectLayer.SetAsLastSibling();
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
                StartCoroutine(FlyToScore(particle.rectTransform, targetPosition, i * 0.035f));
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

        IEnumerator FlyToScore(RectTransform item, Vector2 targetPosition, float delay)
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

        static bool TryGetScrollDelta(out float scrollDelta)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                scrollDelta = Mouse.current.scroll.ReadValue().y;
                return true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            scrollDelta = Input.mouseScrollDelta.y;
            return true;
#else
            scrollDelta = 0f;
            return false;
#endif
        }

        static bool TryGetLeftMouseButton()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                return Mouse.current.leftButton.isPressed;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButton(0);
#else
            return false;
#endif
        }

        static Sprite LoadSprite(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? null : UnityEngine.Resources.Load<Sprite>(SpriteRoot + id);
        }

        static Font LoadDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        static Sprite CreateCircleSprite(int size, Color fill, Color outline, float outlineThickness = 4f)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var radius = size * 0.46f;
            var outlineStart = radius - outlineThickness;

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
            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
#else
            if (eventSystem.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }
#endif

            eventSystem.transform.SetAsFirstSibling();
        }
    }
}














