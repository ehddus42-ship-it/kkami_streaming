using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Video;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.UI;

namespace GameKamiStreaming
{
    public sealed class KkamiPrototypeGame : MonoBehaviour, IBossMovementArea
    {
        const string SpriteRoot = "GameKamiStreaming/Sprites/";
        const string KaturiSdfFontPath = SpriteRoot + "font/Katuri SDF";
        const float FixedGameAspectRatio = 16f / 9f;
        static readonly Vector2 FixedGameReferenceResolution = new Vector2(1920f, 1080f);
        const int TargetPieceCount = 12;
        const float MiningRadius = 120f;
        const float DamagePerSecond = 7f;
        const float PieceHitboxInsetRatio = 0.24f;
        const int CollectParticleCount = 9;
        const float SpawnPieceSizeScale = 0.8f;
        const float MiningAttackFrameSeconds = 0.025f;
        const float MiningAttackSize = 512f;
        const float MiningAttackDisplaySize = MiningAttackSize * 0.7f;
        const int MiningAttackFrameCount = 12;
        const string MiningAttackSheetPath = "GameKamiStreaming/Sprites/vfx_kkami_01";
        const string MiningAttackFrameRoot = "GameKamiStreaming/Sprites/mining_attack/frame_";
        const string ManagerAnimationSheetPath = "GameKamiStreaming/Sprites/vfx_manager_01";
        const int ManagerAnimationFrameCount = 12;
        const float ManagerAnimationFrameSeconds = 0.065f;
        const float ManagerDisplaySize = 150f;
        const float ManagerDirectionOffsetDegrees = 20f;
        const float ManagerBossOneSpeedFactor = 0.5f;
        const float KkamiAppearIntervalSeconds = 5f;
        const float ChatAppearIntervalSeconds = 2.2f;
        const int MaxVisibleChatMessages = 7;
        const float SkillTreeContentSize = 4200f;
        const float SkillTreeMinZoom = 0.55f;
        const float SkillTreeMaxZoom = 2.5f;
        const float SkillTreeZoomStep = 0.18f;
        const float StageImageSaturation = 0.82f;
        const float StageImageBrightness = 0.98f;
        const string SkillTreeBackgroundSpriteId = "skilltree_bg";
        const string SkillTreeInfoSpriteId = "skilltree_info";
        const string SkillTreeTestButtonSpriteId = "skilltree_button_test";
        const string SkillTreeUsedButtonSpriteId = "skilltree_button_used";
        const string SkillTreeAvailableButtonSpriteId = "skilltree_button_available";
        const string ManagerActivationSkillKey = "SD10116";
        const string NextStageButtonSpriteId = "next_stage_button";
        const string StartScreenBackgroundSpriteId = "start_screen";
        const string StartGameButtonSpriteId = "start_button";
        const string EndingVideoRelativePath = "KkamiStreaming/ending.mp4";
        const string TemporaryStageJumpButtonGroupName = "Temporary Stage Jump Buttons";
        static readonly Dictionary<string, string> SkillTreeIconSpriteIds = new Dictionary<string, string>
        {
            { "SD10101", "skill_mining_speed" },
            { "SD10102", "skill_mining_speed" },
            { "SD10103", "skill_mining_speed" },
            { "SD10104", "skill_mining_range" },
            { "SD10105", "skill_mining_range" },
            { "SD10106", "skill_mining_range" },
            { "SD10107", "skill_mining_damage" },
            { "SD10108", "skill_mining_damage" },
            { "SD10109", "skill_mining_damage" },
            { "SD10110", "skill_piece_spawn_speed" },
            { "SD10111", "skill_piece_spawn_speed" },
            { "SD10112", "skill_piece_spawn_speed" },
            { "SD10113", "skill_mining_efficiency" },
            { "SD10114", "skill_mining_efficiency" },
            { "SD10115", "skill_mining_efficiency" },
            { "SD10116", "skill_manager" },
            { "SD10117", "skill_manager_range" },
            { "SD10118", "skill_manager_range" },
            { "SD10119", "skill_manager_range" },
            { "SD10120", "skill_manager_damage" },
            { "SD10121", "skill_manager_damage" },
            { "SD10122", "skill_manager_damage" },
            { "SD10123", "skill_manager_speed" },
            { "SD10124", "skill_manager_speed" },
            { "SD10125", "skill_manager_speed" },
            { "SD10126", "skill_manager" },
            { "SD10127", "skill_manager" },
            { "SD10128", "skill_manager" }
        };
        const int StageSpriteGroupSize = 10;
        const int StageSpriteGroupCount = 5;
        const int StageIdSpriteBase = 40001;
        const int FirstStageBackgroundEndStageNumber = 10;
        const int SecondStageBackgroundEndStageNumber = 20;
        const int ThirdStageBackgroundEndStageNumber = 30;
        const int FourthStageBackgroundEndStageNumber = 40;
        const int FifthStageBackgroundEndStageNumber = 50;
        static readonly Color FirstStageBackgroundColor = new Color(231f / 255f, 181f / 255f, 253f / 255f, 1f);
        static readonly Color SecondStageBackgroundColor = new Color(16f / 255f, 49f / 255f, 142f / 255f, 1f);
        static readonly Color ThirdStageBackgroundColor = new Color(139f / 255f, 217f / 255f, 241f / 255f, 1f);
        static readonly Color FourthStageBackgroundColor = new Color(49f / 255f, 105f / 255f, 29f / 255f, 1f);
        static readonly Color FifthStageBackgroundColor = new Color(74f / 255f, 48f / 255f, 48f / 255f, 1f);
        const int BossKillPanelCount = 5;
        static readonly int[] TemporaryStageJumpNumbers = { 9, 19, 29, 39, 49 };
        const float StageIndicatorNumberScale = 1.45f;
        const string StageIndicatorSpriteId = "stage_ui";
        const string BossFrameFormat = "000";
        static readonly string[] KkamiAppearSpriteIds =
        {
            "kkami_appear/love",
            "kkami_appear/confused",
            "kkami_appear/angry",
            "kkami_appear/super_angry",
            "kkami_appear/shocked",
            "kkami_appear/sad"
        };
        static readonly Dictionary<int, BossDefinition> BossDefinitions = new Dictionary<int, BossDefinition>
        {
            {
                30001,
                new BossDefinition
                {
                    pieceId = 30001,
                    size = 260f,
                    moveInterval = 1f,
                    moveStepDistance = 190f * 1.8f,
                    moveDurations = new[] { 0.34f },
                    animateIdleWithMoveAnimation = true,
                    moveAnimation = new AnimationSource
                    {
                        sheetPath = "GameKamiStreaming/Sprites/vfx_boss1_01",
                        legacyFrameRoot = "GameKamiStreaming/Sprites/boss/boss_1/move/frame_",
                        frameCount = 8
                    },
                    deathAnimation = new AnimationSource
                    {
                        sheetPath = "GameKamiStreaming/Sprites/vfx_boss1_02",
                        legacyFrameRoot = "GameKamiStreaming/Sprites/boss/boss_1/death/frame_",
                        frameCount = 12
                    }
                }
            },
            {
                30002,
                new BossDefinition
                {
                    pieceId = 30002,
                    size = 260f * 1.2f,
                    moveInterval = 1f,
                    moveStepDistance = 190f * 1.8f * 2f,
                    moveDurations = new[] { 0.45f, 0.32f, 0.22f, 0.14f },
                    animateIdleWithMoveAnimation = true,
                    preserveAspect = false,
                    fallbackImageId = "boss/boss_1/boss_1",
                    moveAnimation = new AnimationSource
                    {
                        sheetPath = "GameKamiStreaming/Sprites/vfx_boss2_01",
                        legacyFrameRoot = "GameKamiStreaming/Sprites/boss/boss_2/move/frame_",
                        fallbackLegacyFrameRoot = "GameKamiStreaming/Sprites/boss/boss_1/move/frame_",
                        frameCount = 12
                    },
                    deathAnimation = new AnimationSource
                    {
                        sheetPath = "GameKamiStreaming/Sprites/vfx_boss2_02",
                        legacyFrameRoot = "GameKamiStreaming/Sprites/boss/boss_2/death/frame_",
                        fallbackLegacyFrameRoot = "GameKamiStreaming/Sprites/boss/boss_1/death/frame_",
                        frameCount = 12
                    }
                }
            },
            {
                30003,
                new BossDefinition
                {
                    pieceId = 30003,
                    size = 260f * 1.2f,
                    moveInterval = 0.5f,
                    moveStepDistance = 190f * 1.8f * 2f,
                    moveDurations = new[] { 0.14f },
                    animateIdleWithMoveAnimation = true,
                    preserveAspect = false,
                    moveAnimation = new AnimationSource
                    {
                        sheetPath = "GameKamiStreaming/Sprites/vfx_boss3_01",
                        frameCount = 8
                    },
                    deathAnimation = new AnimationSource
                    {
                        sheetPath = "GameKamiStreaming/Sprites/vfx_boss3_02",
                        frameCount = 12
                    }
                }
            },
            {
                30004,
                new BossDefinition
                {
                    pieceId = 30004,
                    size = 260f,
                    moveInterval = 1f,
                    pattern = BossPieceView.BossPattern.Burrow,
                    disappearDelay = 0.7f,
                    hiddenDelay = 0.5f,
                    idleAnimation = new AnimationSource
                    {
                        sheetPath = "GameKamiStreaming/Sprites/vfx_boss4_idle",
                        frameCount = 8
                    },
                    moveAnimation = new AnimationSource
                    {
                        sheetPath = "GameKamiStreaming/Sprites/vfx_boss4_1",
                        frameCount = 16
                    },
                    emergeAnimation = new AnimationSource
                    {
                        sheetPath = "GameKamiStreaming/Sprites/vfx_boss4_1_1",
                        frameCount = 16
                    },
                    emergeDisplayScale = 2f,
                    emergeDisplayOffsetY = 13f,
                    deathAnimation = new AnimationSource
                    {
                        sheetPath = "GameKamiStreaming/Sprites/vfx_boss4_02",
                        frameCount = 16
                    }
                }
            },
            {
                30005,
                new BossDefinition
                {
                    pieceId = 30005,
                    size = 260f * 1.25f * 2f,
                    moveInterval = 1f,
                    moveStepDistance = 190f * 1.8f,
                    moveBoundsScale = 0.5f,
                    moveDurations = new[] { 0.9f },
                    animateIdleWithMoveAnimation = true,
                    pattern = BossPieceView.BossPattern.Airborne,
                    moveAnimation = new AnimationSource
                    {
                        sheetPath = "GameKamiStreaming/Sprites/vfx_boss5_01",
                        frameCount = 12
                    },
                    deathAnimation = new AnimationSource
                    {
                        sheetPath = "GameKamiStreaming/Sprites/vfx_boss5_02",
                        frameCount = 16
                    }
                }
            }
        };

        readonly Dictionary<int, PixelNumberLabel> resourceLabels = new Dictionary<int, PixelNumberLabel>();
        readonly Dictionary<int, PixelNumberLabel> skillTreeResourceLabels = new Dictionary<int, PixelNumberLabel>();
        readonly Dictionary<string, SkillTreeRow> skillTreeRowsByKey = new Dictionary<string, SkillTreeRow>();
        readonly HashSet<string> completedSkillKeys = new HashSet<string>();
        readonly Dictionary<string, List<Sprite>> bossAnimationFrames = new Dictionary<string, List<Sprite>>();
        readonly List<Sprite> miningAttackFrames = new List<Sprite>();
        readonly List<Sprite> managerAnimationFrames = new List<Sprite>();
        readonly List<ManagerAgent> managerAgents = new List<ManagerAgent>();
        readonly List<Sprite> kkamiAppearSprites = new List<Sprite>();
        readonly List<string> visibleChatMessages = new List<string>();
        readonly Image[] bossKillPanels = new Image[BossKillPanelCount];

        [SerializeField] Camera uiCamera;
        [SerializeField] RectTransform canvasRoot;
        [SerializeField] RectTransform stageArea;
        [SerializeField] RectTransform pieceLayer;
        [SerializeField] RectTransform pieceDisplayLayer;
        [SerializeField] RectTransform managerDisplayLayer;
        [SerializeField] RectTransform effectLayer;
        [SerializeField] RectTransform miningCursor;
        [SerializeField] PixelNumberLabel roundTimerLabel;
        [SerializeField] RectTransform skillTreeCanvasRoot;
        [SerializeField] RectTransform startScreenCanvasRoot;
        [SerializeField] RectTransform startGameButtonRoot;
        [SerializeField] Vector2 startGameButtonOffset = new Vector2(-56f, 56f);
        [SerializeField] Vector2 startGameButtonSize = new Vector2(480f, 320f);
        [SerializeField] Button startGameButton;
        [SerializeField] RectTransform endingVideoCanvasRoot;
        [SerializeField] RawImage endingVideoImage;
        [SerializeField] VideoPlayer endingVideoPlayer;
        [SerializeField] RectTransform skillTreeContentRoot;
        [SerializeField] RectTransform skillTreeTooltipRoot;
        TextMeshProUGUI skillTreeTooltipTitleText;
        TextMeshProUGUI skillTreeTooltipDescriptionText;
        TextMeshProUGUI skillTreeTooltipCostText;
        Text skillTreeTooltipTitleLegacyText;
        Text skillTreeTooltipDescriptionLegacyText;
        Text skillTreeTooltipCostLegacyText;
        [SerializeField] RectTransform stageIndicatorRoot;
        [SerializeField] Button startNextStageButton;
        [SerializeField] RectTransform spawnPoint1;
        [SerializeField] RectTransform spawnPoint2;
        [SerializeField] RectTransform spawnPoint3;
        [SerializeField] RectTransform spawnPoint4;
        [SerializeField] Image gameplayBackground;
        [SerializeField] Image stageImage;
        [SerializeField] PixelNumberLabel stageNumberLabel;
        [SerializeField] Image miningAttackImage;
        [SerializeField] Image kkamiAppearImage;
        [SerializeField] Image chattingAppearImage;
        [SerializeField] TextMeshProUGUI chattingAppearText;

        KkamiTableDatabase database;
        GameResourceManager resourceManager;
        GameStageManager stageManager;
        GamePieceManager pieceManager;
        GameEffectManager effectManager;
        StageRow currentStage => stageManager != null ? stageManager.Current : null;
        int currentStageIndex => stageManager != null ? stageManager.CurrentIndex : 0;
        float roundRemainingSeconds;
        float kkamiAppearTimer;
        float chatAppearTimer;
        float skillTreeZoom = 1f;
        Vector2 previousSkillTreePointerPosition;
        int lastKkamiAppearIndex = -1;
        int displayedRoundSecond = -1;
        bool skillTreeOpen;
        bool miningAttackPlaying;
        bool kkamiAppearChanging;
        bool skillTreeDragging;
        bool currentStageBossSpawned;
        bool gameStarted;
        bool endingVideoPlaying;
        bool endingVideoCompleted;
        bool endingVideoFailed;
        Material stageDesaturationMaterial;
        static TMP_FontAsset katuriSdfFont;
        RenderTexture endingVideoRenderTexture;
        float miningSpeedMultiplier = 1f;
        float miningRangeMultiplier = 1f;
        float miningDamageMultiplier = 1f;
        float pieceSpawnSpeedMultiplier = 1f;
        float miningEfficiencyMultiplier = 1f;
        int managerCount;
        float managerRangeMultiplier = 1f;
        float managerDamageMultiplier = 1f;
        float managerSpeedMultiplier = 1f;
        Coroutine managerRoutine;
        int lastScreenWidth;
        int lastScreenHeight;

        sealed class BossDefinition
        {
            public int pieceId;
            public float size;
            public float moveInterval;
            public float moveStepDistance;
            public float moveBoundsScale = 1f;
            public float[] moveDurations;
            public bool animateIdleWithMoveAnimation;
            public bool preserveAspect = true;
            public BossPieceView.BossPattern pattern = BossPieceView.BossPattern.Move;
            public float disappearDelay;
            public float hiddenDelay;
            public float emergeDisplayScale = 1f;
            public float emergeDisplayOffsetY;
            public string fallbackImageId;
            public AnimationSource idleAnimation;
            public AnimationSource moveAnimation;
            public AnimationSource emergeAnimation;
            public AnimationSource deathAnimation;
        }

        sealed class AnimationSource
        {
            public string sheetPath;
            public string legacyFrameRoot;
            public string fallbackLegacyFrameRoot;
            public int frameCount;
        }

        sealed class ManagerAgent
        {
            public RectTransform rectTransform;
            public Image image;
            public Vector2 destination;
            public bool launched;
            public int frameIndex;
            public float frameTimer;
        }

        sealed class SkillTreePointerRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
        {
            KkamiPrototypeGame owner;
            SkillTreeRow row;

            public void Configure(KkamiPrototypeGame owner, SkillTreeRow row)
            {
                this.owner = owner;
                this.row = row;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (owner != null)
                {
                    owner.ShowSkillTreeTooltip(row);
                }
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (owner != null)
                {
                    owner.HideSkillTreeTooltip(eventData);
                }
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                if (owner != null)
                {
                    owner.TryPurchaseSkill(row);
                }
            }
        }

        void Awake()
        {
            InitializeGame();
            ApplyFixedGameAspectRatio();
        }

        void Start()
        {
            if (gameStarted && !skillTreeOpen)
            {
                FillStagePieces();
            }
        }

        void Update()
        {
            ApplyFixedGameAspectRatio();

            if (!gameStarted)
            {
                return;
            }

            if (skillTreeOpen)
            {
                UpdateSkillTreeNavigation();
                return;
            }

            UpdateRoundTimer();
            UpdateKkamiAppear();
            UpdateChattingAppear();
            UpdateMiningCursor();
            UpdateManagerAgents();
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

            ApplyFixedGameAspectRatio();

            EnsureSkillTreeCanvas();
            ConfigureCanvasScaler(skillTreeCanvasRoot);
            if (skillTreeCanvasRoot != null)
            {
                skillTreeCanvasRoot.gameObject.SetActive(false);
            }
        }

        public void CollectPiece(PieceRow piece, RectTransform source)
        {
            var amount = Mathf.Max(1, Mathf.RoundToInt(piece.resourceAmount * miningEfficiencyMultiplier));
            resourceManager.Add(piece.resourceId, amount);

            PlayCollectBurst(source, piece.resourceId);
            StartCoroutine(RespawnAfterDelay(0.45f / Mathf.Max(0.01f, pieceSpawnSpeedMultiplier)));
        }

        public void EnsureEditableStartScreen()
        {
            EnsureEventSystem();
            if (uiCamera == null)
            {
                uiCamera = Camera.main;
            }

            ApplyFixedGameAspectRatio();
            EnsureStartScreenCanvas();
            EnsureEndingVideoCanvas();
            if (startScreenCanvasRoot != null)
            {
                startScreenCanvasRoot.gameObject.SetActive(true);
            }
        }

        public void PlayHitFeedback(RectTransform source, string effectId)
        {
            effectManager.PlayHitFeedback(source, effectId);
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
            resourceManager = new GameResourceManager();
            resourceManager.Initialize(database.Resources);
            resourceManager.AmountChanged += HandleResourceAmountChanged;
            stageManager = new GameStageManager(database.Stages);
            pieceManager = new GamePieceManager();
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

            ApplyFixedGameAspectRatio();
            ConfigureCanvasScaler(canvasRoot);

            resourceLabels.Clear();
            skillTreeResourceLabels.Clear();
            resourceManager.Initialize(database.Resources);
            ConvertExistingTextToKaturiSdf();

            foreach (var counter in canvasRoot.GetComponentsInChildren<ResourceCounterView>(true))
            {
                if (counter.NumberLabel == null)
                {
                    continue;
                }

                counter.NumberLabel.Initialize();
                counter.NumberLabel.SetValue(0);
                resourceLabels[counter.ResourceId] = counter.NumberLabel;
            }

            EnsureEffectLayer();
            effectManager = new GameEffectManager(this, database, effectLayer);
            EnsureStageImage();
            EnsurePieceDisplayLayer();
            EnsureSpawnPointReferences();
            EnsureMiningAttackView();
            EnsureKkamiAppearView();
            EnsureChattingAppearView();
            EnsureBossKillPanels();
            EnsureRoundTimerLabel();
            EnsureStageIndicator();
            EnsureSkillTreeCanvas();
            ConfigureCanvasScaler(skillTreeCanvasRoot);
            EnsureStartScreenCanvas();
            EnsureEndingVideoCanvas();
            RefreshAllResourceLabels();
            ApplyCurrentStageSprite();
            RefreshStageNumberLabel();
            ResetRoundTimer();

            var cursorImage = miningCursor.GetComponent<Image>();
            if (cursorImage != null && cursorImage.sprite == null)
            {
                cursorImage.sprite = CreateCircleSprite(96, new Color(0.2f, 0.85f, 1f, 0.12f), new Color(0.2f, 0.95f, 1f, 0.48f), 1f);
            }
            RefreshMiningCursor();
            miningCursor.gameObject.SetActive(false);
            ShowStartScreen();
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

        void EnsureStageImage()
        {
            if (stageImage == null && stageArea != null)
            {
                var existing = stageArea.Find("Stage") as RectTransform;
                if (existing != null)
                {
                    stageImage = existing.GetComponent<Image>();
                    if (stageImage == null)
                    {
                        stageImage = existing.gameObject.AddComponent<Image>();
                    }
                }
            }

            if (stageImage == null || stageArea == null)
            {
                return;
            }

            stageImage.raycastTarget = false;
            stageImage.preserveAspect = true;
            EnsureStageDesaturationMaterial();
        }

        void EnsureStageDesaturationMaterial()
        {
            if (stageImage == null)
            {
                return;
            }

            var shader = Shader.Find("UI/StageDesaturated");
            if (shader == null)
            {
                return;
            }

            if (stageDesaturationMaterial == null || stageDesaturationMaterial.shader != shader)
            {
                stageDesaturationMaterial = new Material(shader);
                stageDesaturationMaterial.name = "Stage Desaturation Material";
                stageDesaturationMaterial.SetFloat("_Saturation", StageImageSaturation);
                stageDesaturationMaterial.SetFloat("_Brightness", StageImageBrightness);
            }

            stageImage.material = stageDesaturationMaterial;
        }

        void ApplyCurrentStageSprite()
        {
            ApplyCurrentStageBackgroundColor();
            EnsureStageImage();
            if (stageImage == null)
            {
                return;
            }

            var sprite = LoadSprite(GetCurrentStageSpriteId());
            if (sprite == null)
            {
                sprite = LoadSprite("stage");
            }

            stageImage.sprite = sprite;
        }

        void ApplyCurrentStageBackgroundColor()
        {
            if (gameplayBackground == null && canvasRoot != null)
            {
                gameplayBackground = canvasRoot.Find("Background")?.GetComponent<Image>();
            }

            if (gameplayBackground == null)
            {
                return;
            }

            var stageNumber = currentStage != null
                ? currentStage.stageId - StageIdSpriteBase + 1
                : currentStageIndex + 1;
            gameplayBackground.color = stageNumber <= FirstStageBackgroundEndStageNumber
                ? FirstStageBackgroundColor
                : stageNumber <= SecondStageBackgroundEndStageNumber
                    ? SecondStageBackgroundColor
                    : stageNumber <= ThirdStageBackgroundEndStageNumber
                        ? ThirdStageBackgroundColor
                        : stageNumber <= FourthStageBackgroundEndStageNumber
                            ? FourthStageBackgroundColor
                    : stageNumber <= FifthStageBackgroundEndStageNumber && stageNumber >= 41
                        ? FifthStageBackgroundColor
                    : new Color(0.09f, 0.1f, 0.12f, 1f);
        }

        string GetCurrentStageSpriteId()
        {
            if (currentStage != null && !string.IsNullOrWhiteSpace(currentStage.imageId))
            {
                return currentStage.imageId;
            }

            var stageOffset = currentStage != null ? currentStage.stageId - StageIdSpriteBase : currentStageIndex;
            if (stageOffset < 0)
            {
                stageOffset = currentStageIndex;
            }

            var group = Mathf.Clamp((stageOffset / StageSpriteGroupSize) + 1, 1, StageSpriteGroupCount);
            return "img_stage" + group + "_01";
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
            ConfigureCanvasScaler(scaler);

            canvasRoot = canvas.transform as RectTransform;
        }

        void ApplyFixedGameAspectRatio()
        {
            if (uiCamera == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            if (lastScreenWidth == Screen.width && lastScreenHeight == Screen.height)
            {
                return;
            }

            var screenAspectRatio = (float)Screen.width / Screen.height;
            if (screenAspectRatio > FixedGameAspectRatio)
            {
                var width = FixedGameAspectRatio / screenAspectRatio;
                uiCamera.rect = new Rect((1f - width) * 0.5f, 0f, width, 1f);
            }
            else
            {
                var height = screenAspectRatio / FixedGameAspectRatio;
                uiCamera.rect = new Rect(0f, (1f - height) * 0.5f, 1f, height);
            }

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
        }

        static void ConfigureCanvasScaler(CanvasScaler scaler)
        {
            if (scaler == null)
            {
                return;
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = FixedGameReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        static void ConfigureCanvasScaler(RectTransform root)
        {
            if (root != null)
            {
                ConfigureCanvasScaler(root.GetComponent<CanvasScaler>());
            }
        }

        void EnsureStartScreenCanvas()
        {
            if (startScreenCanvasRoot == null)
            {
                var canvas = new GameObject("Start Screen Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = uiCamera;
                canvas.planeDistance = 8f;
                canvas.sortingOrder = 100;
                ConfigureCanvasScaler(canvas.GetComponent<CanvasScaler>());
                startScreenCanvasRoot = canvas.transform as RectTransform;

                var background = AddFullScreenImage("Start Screen Background", LoadSprite(StartScreenBackgroundSpriteId), startScreenCanvasRoot, Color.white);
                background.preserveAspect = true;
                background.raycastTarget = false;
            }
            else
            {
                var canvas = startScreenCanvasRoot.GetComponent<Canvas>();
                if (canvas != null)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = uiCamera;
                    canvas.planeDistance = 8f;
                    canvas.sortingOrder = 100;
                }

                ConfigureCanvasScaler(startScreenCanvasRoot);
                var background = FindChildRect(startScreenCanvasRoot, "Start Screen Background");
                if (background == null)
                {
                    var backgroundImage = AddFullScreenImage("Start Screen Background", LoadSprite(StartScreenBackgroundSpriteId), startScreenCanvasRoot, Color.white);
                    backgroundImage.preserveAspect = true;
                    backgroundImage.raycastTarget = false;
                }
            }

            if (startGameButtonRoot == null && startScreenCanvasRoot != null)
            {
                startGameButtonRoot = FindChildRect(startScreenCanvasRoot, "Start Game Button");
            }

            if (startGameButtonRoot == null && startScreenCanvasRoot != null)
            {
                startGameButtonRoot = CreateRect("Start Game Button", startScreenCanvasRoot, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), startGameButtonOffset, startGameButtonSize);
            }

            if (startGameButtonRoot == null)
            {
                return;
            }

            var image = startGameButtonRoot.GetComponent<Image>();
            if (image == null)
            {
                image = startGameButtonRoot.gameObject.AddComponent<Image>();
            }

            image.sprite = LoadSprite(StartGameButtonSpriteId);
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = true;

            startGameButton = startGameButtonRoot.GetComponent<Button>();
            if (startGameButton == null)
            {
                startGameButton = startGameButtonRoot.gameObject.AddComponent<Button>();
            }

            startGameButton.targetGraphic = image;
            startGameButton.onClick.RemoveListener(StartGameFromStartScreen);
            if (!HasPersistentStartGameListener(startGameButton))
            {
                startGameButton.onClick.AddListener(StartGameFromStartScreen);
            }
        }

        void EnsureEndingVideoCanvas()
        {
            if (endingVideoCanvasRoot == null)
            {
                var canvas = new GameObject("Ending Video Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = uiCamera;
                canvas.planeDistance = 7f;
                canvas.sortingOrder = 200;
                ConfigureCanvasScaler(canvas.GetComponent<CanvasScaler>());
                endingVideoCanvasRoot = canvas.transform as RectTransform;

                AddFullScreenImage("Ending Video Background", null, endingVideoCanvasRoot, Color.black).raycastTarget = false;
                var videoRect = CreateRect("Ending Video", endingVideoCanvasRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                endingVideoImage = videoRect.gameObject.AddComponent<RawImage>();
                endingVideoImage.color = Color.white;
                endingVideoImage.raycastTarget = false;
                endingVideoPlayer = endingVideoCanvasRoot.gameObject.AddComponent<VideoPlayer>();
            }
            else
            {
                var canvas = endingVideoCanvasRoot.GetComponent<Canvas>();
                if (canvas != null)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = uiCamera;
                    canvas.planeDistance = 7f;
                    canvas.sortingOrder = 200;
                }

                ConfigureCanvasScaler(endingVideoCanvasRoot);
                if (endingVideoImage == null)
                {
                    endingVideoImage = FindChildRect(endingVideoCanvasRoot, "Ending Video")?.GetComponent<RawImage>();
                }

                if (endingVideoPlayer == null)
                {
                    endingVideoPlayer = endingVideoCanvasRoot.GetComponent<VideoPlayer>();
                }
            }

            if (endingVideoPlayer == null)
            {
                return;
            }

            if (endingVideoRenderTexture == null)
            {
                endingVideoRenderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32)
                {
                    name = "Ending Video Render Texture"
                };
                endingVideoRenderTexture.Create();
            }

            endingVideoPlayer.source = VideoSource.Url;
            endingVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            endingVideoPlayer.targetTexture = endingVideoRenderTexture;
            endingVideoPlayer.playOnAwake = false;
            endingVideoPlayer.waitForFirstFrame = true;
            endingVideoPlayer.skipOnDrop = true;
            endingVideoPlayer.isLooping = false;
            endingVideoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            endingVideoPlayer.loopPointReached -= HandleEndingVideoCompleted;
            endingVideoPlayer.loopPointReached += HandleEndingVideoCompleted;
            endingVideoPlayer.errorReceived -= HandleEndingVideoError;
            endingVideoPlayer.errorReceived += HandleEndingVideoError;

            if (endingVideoImage != null)
            {
                endingVideoImage.texture = endingVideoRenderTexture;
            }

            if (!endingVideoPlaying)
            {
                endingVideoCanvasRoot.gameObject.SetActive(false);
            }
        }

        IEnumerator PlayEndingVideoThenReturnToStartScreen()
        {
            if (endingVideoPlaying)
            {
                yield break;
            }

            EnsureEndingVideoCanvas();
            if (endingVideoPlayer == null || endingVideoCanvasRoot == null)
            {
                ReturnToStartScreenAfterEnding();
                yield break;
            }

            endingVideoPlaying = true;
            endingVideoCompleted = false;
            endingVideoFailed = false;
            gameStarted = false;
            ShowEndingVideoCanvas();

            endingVideoPlayer.Stop();
            endingVideoPlayer.url = Path.Combine(Application.streamingAssetsPath, EndingVideoRelativePath);
            endingVideoPlayer.Prepare();
            while (!endingVideoPlayer.isPrepared && !endingVideoFailed)
            {
                yield return null;
            }

            if (endingVideoFailed)
            {
                ReturnToStartScreenAfterEnding();
                yield break;
            }

            endingVideoPlayer.Play();
            while (!endingVideoCompleted && !endingVideoFailed)
            {
                yield return null;
            }

            ReturnToStartScreenAfterEnding();
        }

        void HandleEndingVideoCompleted(VideoPlayer source)
        {
            endingVideoCompleted = true;
        }

        void HandleEndingVideoError(VideoPlayer source, string message)
        {
            endingVideoFailed = true;
            Debug.LogWarning("Unable to play the ending video: " + message);
        }

        void ReturnToStartScreenAfterEnding()
        {
            if (endingVideoPlayer != null)
            {
                endingVideoPlayer.Stop();
            }

            ClearActivePieces();
            if (stageManager != null)
            {
                stageManager.SelectByStageId(StageIdSpriteBase, 0);
            }

            for (var i = 0; i < bossKillPanels.Length; i++)
            {
                SetImageAlpha(bossKillPanels[i], 0f);
            }

            endingVideoPlaying = false;
            ShowStartScreen();
        }

        void BuildScene()
        {
            gameplayBackground = AddFullScreenImage("Background", null, canvasRoot, FirstStageBackgroundColor);

            stageArea = CreateRect("Stage Area", canvasRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(1120f, 1220f));
            var stage = AddAnchoredImage("Stage", LoadSprite("stage"), stageArea, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            stage.preserveAspect = true;
            stageImage = stage;

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
            stageIndicatorRoot = BuildStageIndicator(canvasRoot);
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

        RectTransform BuildStageIndicator(RectTransform parent)
        {
            var root = CreateRect("Stage Indicator", parent, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(32f, 122f), new Vector2(430f, 128f));
            var image = root.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(StageIndicatorSpriteId);
            image.preserveAspect = true;
            image.raycastTarget = false;

            var numberRoot = CreateRect("Stage Number", root, new Vector2(0.52f, 0.5f), new Vector2(0.92f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 80f));
            numberRoot.localScale = Vector3.one * StageIndicatorNumberScale;
            var layout = numberRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = -6f;

            stageNumberLabel = numberRoot.gameObject.AddComponent<PixelNumberLabel>();
            stageNumberLabel.Initialize();
            RefreshStageNumberLabel();
            root.SetAsLastSibling();
            return root;
        }

        RectTransform BuildSkillTreeCanvas()
        {
            var canvas = new GameObject("Skill Tree Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCamera;
            canvas.planeDistance = 9f;
            canvas.sortingOrder = 50;

            var scaler = canvas.GetComponent<CanvasScaler>();
            ConfigureCanvasScaler(scaler);

            var root = canvas.transform as RectTransform;
            var backdrop = AddFullScreenImage("Skill Tree Backdrop", LoadSprite(SkillTreeBackgroundSpriteId), root, Color.white);
            ConfigureSkillTreeBackdrop(backdrop);

            skillTreeContentRoot = CreateRect("Skill Tree Empty Fields", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(SkillTreeContentSize, SkillTreeContentSize));
            var panelImage = skillTreeContentRoot.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.12f, 0.13f, 0.16f, 0.92f);
            panelImage.raycastTarget = false;

            BuildSkillTreeResourceWallet(root);
            BuildSkillTreeTooltip(root);
            BuildTestSkillTreeButton(root);
            BuildTemporaryStageJumpButtons(root);
            startNextStageButton = BuildNextStageButton(root);
            root.gameObject.SetActive(false);
            return root;
        }

        Button BuildNextStageButton(RectTransform parent)
        {
            var buttonRoot = CreateRect("Start Next Stage Button", parent, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-56f, 56f), new Vector2(215f, 215f));
            var image = buttonRoot.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(NextStageButtonSpriteId);
            image.color = Color.white;
            image.preserveAspect = true;

            var button = buttonRoot.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(StartNextStageFromSkillTree);

            var label = CreateRect("Label", buttonRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var text = label.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = "NEXT STAGE";
            text.font = LoadKaturiSdfFont();
            text.fontSize = 42;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.08f, 0.07f, 0.04f, 1f);
            text.raycastTarget = false;
            label.gameObject.SetActive(false);

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
        }

        RectTransform BuildSkillTreeResourceWallet(RectTransform parent)
        {
            var wallet = CreateRect("Skill Tree Resource Wallet", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -36f), new Vector2(292f, 520f));
            var bg = wallet.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.42f);
            bg.raycastTarget = false;

            var layout = wallet.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            foreach (var resource in database.Resources)
            {
                BuildSkillTreeResourceCounter(wallet, resource);
            }

            RegisterSkillTreeResourceLabels(wallet);
            return wallet;
        }

        void BuildSkillTreeResourceCounter(RectTransform parent, ResourceRow resource)
        {
            var row = CreateRect("Skill Tree Resource " + resource.resourceId, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(268f, 72f));
            var bg = row.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.24f);
            bg.raycastTarget = false;

            var icon = AddAnchoredImage("Icon", LoadSprite(resource.imageId), row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(36f, 0f), new Vector2(54f, 54f));
            icon.preserveAspect = true;

            var labelRoot = CreateRect("Number", row, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(86f, 0f), new Vector2(-96f, 56f));
            var layout = labelRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = -6f;

            var number = labelRoot.gameObject.AddComponent<PixelNumberLabel>();
            number.Initialize();
            number.SetValue(GetResourceAmount(resource.resourceId));

            var binding = row.gameObject.AddComponent<ResourceCounterView>();
            binding.Configure(resource.resourceId, number);
            skillTreeResourceLabels[resource.resourceId] = number;
        }

        RectTransform BuildTestSkillTreeButton(RectTransform parent)
        {
            var rect = CreateRect("Skill Tree Test Button", parent, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-56f, -56f), new Vector2(100f, 100f));
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(SkillTreeTestButtonSpriteId);
            image.preserveAspect = true;
            image.raycastTarget = true;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var trigger = rect.gameObject.AddComponent<EventTrigger>();
            AddPointerEvent(trigger, EventTriggerType.PointerEnter, ShowSkillTreeTooltip);
            AddPointerEvent(trigger, EventTriggerType.PointerExit, HideSkillTreeTooltip);
            return rect;
        }

        RectTransform BuildTemporaryStageJumpButtons(RectTransform parent)
        {
            var group = CreateRect(TemporaryStageJumpButtonGroupName, parent, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-56f, 520f), new Vector2(420f, 84f));
            for (var i = 0; i < TemporaryStageJumpNumbers.Length; i++)
            {
                BuildTemporaryStageJumpButton(group, TemporaryStageJumpNumbers[i], i);
            }

            return group;
        }

        RectTransform BuildTemporaryStageJumpButton(RectTransform parent, int stageNumber, int index)
        {
            var x = (index - (TemporaryStageJumpNumbers.Length - 1) * 0.5f) * 76f;
            var rect = CreateRect("Temporary Stage Jump " + stageNumber, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(70f, 70f));
            ConfigureTemporaryStageJumpButton(rect, stageNumber);
            return rect;
        }

        RectTransform BuildSkillTreeTooltip(RectTransform parent)
        {
            var root = CreateRect("Skill Tree Detail Tooltip", parent, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-44f, -150f), new Vector2(780f, 780f));
            var image = root.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(SkillTreeInfoSpriteId);
            image.preserveAspect = true;
            image.raycastTarget = false;

            var title = CreateText("Title", root, new Vector2(0.18f, 0.62f), new Vector2(0.82f, 0.76f), "TEST SKILL", 38, FontStyle.Bold);
            title.alignment = TextAlignmentOptions.MidlineLeft;

            var body = CreateText("Description", root, new Vector2(0.18f, 0.36f), new Vector2(0.82f, 0.60f), "강화 설명 데이터 연결 예정", 28, FontStyle.Normal);
            body.alignment = TextAlignmentOptions.TopLeft;

            var cost = CreateText("Cost", root, new Vector2(0.18f, 0.24f), new Vector2(0.82f, 0.34f), "COST 0", 28, FontStyle.Bold);
            cost.alignment = TextAlignmentOptions.MidlineLeft;

            skillTreeTooltipTitleText = title;
            skillTreeTooltipDescriptionText = body;
            skillTreeTooltipCostText = cost;

            root.gameObject.SetActive(false);
            skillTreeTooltipRoot = root;
            return root;
        }

        RectTransform CreateMiningCursor(RectTransform parent)
        {
            var cursor = CreateRect("Mining Radius", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(GetMiningRadius() * 2f, GetMiningRadius() * 2f));
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

        void EnsureManagerDisplayLayer()
        {
            EnsurePieceDisplayLayer();
            if (canvasRoot == null)
            {
                return;
            }

            if (managerDisplayLayer == null)
            {
                managerDisplayLayer = canvasRoot.Find("Manager Display Layer") as RectTransform;
            }

            if (managerDisplayLayer == null)
            {
                managerDisplayLayer = CreateRect("Manager Display Layer", canvasRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            }

            managerDisplayLayer.SetParent(canvasRoot, false);
            managerDisplayLayer.anchorMin = Vector2.zero;
            managerDisplayLayer.anchorMax = Vector2.one;
            managerDisplayLayer.pivot = new Vector2(0.5f, 0.5f);
            managerDisplayLayer.anchoredPosition = Vector2.zero;
            managerDisplayLayer.sizeDelta = Vector2.zero;
            managerDisplayLayer.localScale = Vector3.one;
            if (pieceDisplayLayer != null)
            {
                managerDisplayLayer.SetSiblingIndex(Mathf.Min(canvasRoot.childCount - 1, pieceDisplayLayer.GetSiblingIndex() + 1));
            }
        }

        void LoadManagerAnimationFrames()
        {
            if (managerAnimationFrames.Count > 0)
            {
                return;
            }

            managerAnimationFrames.AddRange(LoadAnimationFrames(new AnimationSource
            {
                sheetPath = ManagerAnimationSheetPath,
                frameCount = ManagerAnimationFrameCount
            }, new Vector2(0.5f, 0.5f)));
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

            miningAttackFrames.AddRange(LoadAnimationFrames(new AnimationSource
            {
                sheetPath = MiningAttackSheetPath,
                legacyFrameRoot = MiningAttackFrameRoot,
                frameCount = MiningAttackFrameCount
            }, new Vector2(1f / 3f, 0f)));
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

        void EnsureChattingAppearView()
        {
            var root = FindRectInGameCanvas("chatting appear");
            if (root == null)
            {
                return;
            }

            var chattingCanvas = root.GetComponent<Canvas>();
            if (chattingCanvas == null)
            {
                chattingCanvas = root.gameObject.AddComponent<Canvas>();
            }
            chattingCanvas.overrideSorting = true;
            chattingCanvas.sortingOrder = 100;

            chattingAppearImage = root.GetComponent<Image>();
            if (chattingAppearImage == null)
            {
                chattingAppearImage = root.gameObject.AddComponent<Image>();
            }

            chattingAppearImage.sprite = LoadSprite("chatting");
            chattingAppearImage.type = Image.Type.Simple;
            chattingAppearImage.color = Color.white;
            chattingAppearImage.preserveAspect = false;
            chattingAppearImage.raycastTarget = false;

            var textRect = root.Find("Chat Messages") as RectTransform;
            if (textRect == null)
            {
                textRect = CreateRect("Chat Messages", root, new Vector2(0.18f, 0.16f), new Vector2(0.82f, 0.79f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            }

            chattingAppearText = textRect.GetComponent<TextMeshProUGUI>();
            if (chattingAppearText == null)
            {
                chattingAppearText = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            }

            chattingAppearText.font = LoadKaturiSdfFont();
            chattingAppearText.fontSize = 80;
            chattingAppearText.enableAutoSizing = true;
            chattingAppearText.fontSizeMin = 16;
            chattingAppearText.fontSizeMax = 80;
            chattingAppearText.alignment = TextAlignmentOptions.TopLeft;
            chattingAppearText.textWrappingMode = TextWrappingModes.Normal;
            chattingAppearText.overflowMode = TextOverflowModes.Truncate;
            chattingAppearText.color = new Color(0.22f, 0.09f, 0.30f, 0.96f);
            chattingAppearText.raycastTarget = false;

            visibleChatMessages.Clear();
            chatAppearTimer = 0f;
            AppendRandomChatMessage();
        }

        void EnsureBossKillPanels()
        {
            for (var i = 0; i < BossKillPanelCount; i++)
            {
                var panelRoot = FindRectInGameCanvas("bosskill" + (i + 1));
                var panel = panelRoot != null ? panelRoot.GetComponent<Image>() : null;
                bossKillPanels[i] = panel;
                if (panel == null)
                {
                    continue;
                }

                panel.sprite = LoadSprite("bosskillmark" + (i + 1));
                panel.type = Image.Type.Simple;
                panel.preserveAspect = true;
                panel.raycastTarget = false;
                NormalizeAspectScale(panel.rectTransform);
                SetImageAlpha(panel, 0f);
            }
        }

        static void NormalizeAspectScale(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            var scale = rect.localScale;
            var isCentered = rect.anchorMin == new Vector2(0.5f, 0.5f) && rect.anchorMax == new Vector2(0.5f, 0.5f);
            if (isCentered && Mathf.Approximately(scale.x, 1f) && Mathf.Approximately(scale.y, 1f) && rect.sizeDelta.x > 0f && rect.sizeDelta.y > 0f)
            {
                return;
            }

            var parent = rect.parent as RectTransform;
            var parentSize = parent != null ? parent.rect.size : Vector2.one;
            var scaledWidth = Mathf.Abs(parentSize.x * scale.x);
            var scaledHeight = Mathf.Abs(parentSize.y * scale.y);
            var squareSize = Mathf.Sqrt(Mathf.Abs(scaledWidth * scaledHeight));
            if (squareSize <= 0f)
            {
                squareSize = 1f;
            }

            var offset = Vector2.Scale(rect.anchoredPosition, new Vector2(scale.x, scale.y));
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(squareSize, squareSize);
            rect.localScale = Vector3.one;
        }

        void HandleBossDefeated(DestructiblePieceView defeatedView)
        {
            var piece = defeatedView != null ? defeatedView.Piece : null;
            if (piece == null || piece.pieceId < 30001 || piece.pieceId > 30005)
            {
                return;
            }

            var panelIndex = piece.pieceId - 30001;
            if (panelIndex < 0 || panelIndex >= bossKillPanels.Length)
            {
                return;
            }

            var panel = bossKillPanels[panelIndex];
            if (panel == null)
            {
                return;
            }

            if (panel.sprite == null)
            {
                panel.sprite = LoadSprite("bosskillmark" + (panelIndex + 1));
            }

            panel.gameObject.SetActive(true);
            SetImageAlpha(panel, 1f);

            if (piece.pieceId == 30005)
            {
                StartCoroutine(PlayEndingVideoThenReturnToStartScreen());
            }
        }

        static void SetImageAlpha(Image image, float alpha)
        {
            if (image == null)
            {
                return;
            }

            var color = image.color;
            color.a = Mathf.Clamp01(alpha);
            image.color = color;
        }

        void UpdateChattingAppear()
        {
            if (chattingAppearText == null || database == null || database.Chats.Count == 0)
            {
                return;
            }

            chatAppearTimer -= Time.deltaTime;
            if (chatAppearTimer <= 0f)
            {
                AppendRandomChatMessage();
            }
        }

        void AppendRandomChatMessage()
        {
            if (chattingAppearText == null || database == null || database.Chats.Count == 0)
            {
                return;
            }

            var totalWeight = 0f;
            for (var i = 0; i < database.Chats.Count; i++)
            {
                totalWeight += Mathf.Max(0f, database.Chats[i].spawnWeight);
            }

            var roll = totalWeight > 0f ? Random.value * totalWeight : Random.Range(0, database.Chats.Count);
            var selected = database.Chats[database.Chats.Count - 1];
            if (totalWeight > 0f)
            {
                for (var i = 0; i < database.Chats.Count; i++)
                {
                    roll -= Mathf.Max(0f, database.Chats[i].spawnWeight);
                    if (roll <= 0f)
                    {
                        selected = database.Chats[i];
                        break;
                    }
                }
            }
            else
            {
                selected = database.Chats[Mathf.Clamp((int)roll, 0, database.Chats.Count - 1)];
            }

            if (!string.IsNullOrWhiteSpace(selected.dialogue))
            {
                visibleChatMessages.Add(selected.dialogue);
                while (visibleChatMessages.Count > MaxVisibleChatMessages)
                {
                    visibleChatMessages.RemoveAt(0);
                }
                chattingAppearText.text = string.Join("\n", visibleChatMessages);
            }

            chatAppearTimer = ChatAppearIntervalSeconds;
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

        void EnsureStageIndicator()
        {
            if (canvasRoot == null)
            {
                return;
            }

            if (stageIndicatorRoot == null)
            {
                stageIndicatorRoot = FindRectInGameCanvas("Stage Indicator");
            }

            if (stageIndicatorRoot == null)
            {
                stageIndicatorRoot = BuildStageIndicator(canvasRoot);
            }

            stageIndicatorRoot.SetParent(canvasRoot, false);
            stageIndicatorRoot.anchorMin = new Vector2(0f, 0f);
            stageIndicatorRoot.anchorMax = new Vector2(0f, 0f);
            stageIndicatorRoot.pivot = new Vector2(0f, 0f);
            stageIndicatorRoot.anchoredPosition = new Vector2(32f, 122f);
            stageIndicatorRoot.sizeDelta = new Vector2(430f, 128f);
            stageIndicatorRoot.localScale = Vector3.one;
            stageIndicatorRoot.SetAsLastSibling();

            var image = stageIndicatorRoot.GetComponent<Image>();
            if (image == null)
            {
                image = stageIndicatorRoot.gameObject.AddComponent<Image>();
            }
            image.sprite = LoadSprite(StageIndicatorSpriteId);
            image.preserveAspect = true;
            image.raycastTarget = false;

            var numberRoot = FindChildRect(stageIndicatorRoot, "Stage Number");
            if (numberRoot == null)
            {
                numberRoot = CreateRect("Stage Number", stageIndicatorRoot, new Vector2(0.52f, 0.5f), new Vector2(0.92f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 80f));
            }
            numberRoot.anchorMin = new Vector2(0.52f, 0.5f);
            numberRoot.anchorMax = new Vector2(0.92f, 0.5f);
            numberRoot.pivot = new Vector2(0.5f, 0.5f);
            numberRoot.anchoredPosition = Vector2.zero;
            numberRoot.sizeDelta = new Vector2(0f, 80f);
            numberRoot.localScale = Vector3.one * StageIndicatorNumberScale;

            var layout = numberRoot.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = numberRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            }
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = -6f;

            stageNumberLabel = numberRoot.GetComponent<PixelNumberLabel>();
            if (stageNumberLabel == null)
            {
                stageNumberLabel = numberRoot.gameObject.AddComponent<PixelNumberLabel>();
            }
            stageNumberLabel.Initialize();
            RefreshStageNumberLabel();
        }

        void RefreshStageNumberLabel()
        {
            if (stageNumberLabel == null)
            {
                return;
            }

            stageNumberLabel.SetValue(GetCurrentStageNumber());
        }

        int GetCurrentStageNumber()
        {
            var stageOffset = currentStage != null ? currentStage.stageId - StageIdSpriteBase : currentStageIndex;
            if (stageOffset < 0)
            {
                stageOffset = currentStageIndex;
            }

            return Mathf.Max(1, stageOffset + 1);
        }

        void EnsureSkillTreeCanvas()
        {
            if (skillTreeCanvasRoot != null)
            {
                EnsureSkillTreeBackdrop();
                EnsureSkillTreeContentRoot();
                EnsureSkillTreeOverlayImage();
                EnsureStartNextStageButton();
                EnsureSkillTreeResourceWallet();
                EnsureSkillTreeTooltip();
                EnsureSkillTreeDataButtons();
                EnsureTestSkillTreeButton();
                EnsureTemporaryStageJumpButtons();
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
                EnsureSkillTreeOverlayImage();
                EnsureStartNextStageButton();
                EnsureSkillTreeResourceWallet();
                EnsureSkillTreeTooltip();
                EnsureSkillTreeDataButtons();
                EnsureTestSkillTreeButton();
                EnsureTemporaryStageJumpButtons();
                skillTreeCanvasRoot.gameObject.SetActive(false);
                return;
            }

            skillTreeCanvasRoot = BuildSkillTreeCanvas();
            EnsureSkillTreeBackdrop();
            EnsureSkillTreeContentRoot();
            EnsureSkillTreeOverlayImage();
            EnsureStartNextStageButton();
            EnsureSkillTreeResourceWallet();
            EnsureSkillTreeTooltip();
            EnsureSkillTreeDataButtons();
            EnsureTestSkillTreeButton();
            EnsureTemporaryStageJumpButtons();
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

        void EnsureSkillTreeOverlayImage()
        {
            if (skillTreeCanvasRoot == null)
            {
                return;
            }

            var sleepy = FindChildRect(skillTreeCanvasRoot, "sleepykkami");
            if (sleepy == null)
            {
                return;
            }

            sleepy.SetParent(skillTreeCanvasRoot, false);
            if (skillTreeContentRoot != null)
            {
                sleepy.SetSiblingIndex(skillTreeContentRoot.GetSiblingIndex() + 1);
            }

            var image = sleepy.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = false;
                image.preserveAspect = true;
                image.color = Color.white;
            }
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
                rect.sizeDelta = new Vector2(215f, 215f);
                rect.localScale = Vector3.one;
                rect.SetAsLastSibling();
            }

            var image = startNextStageButton.GetComponent<Image>();
            if (image == null)
            {
                image = startNextStageButton.gameObject.AddComponent<Image>();
            }
            image.sprite = LoadSprite(NextStageButtonSpriteId);
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = true;
            startNextStageButton.targetGraphic = image;

            var label = FindChildRect(startNextStageButton.transform as RectTransform, "Label");
            if (label != null)
            {
                label.gameObject.SetActive(false);
            }

            startNextStageButton.onClick.RemoveListener(StartNextStageFromSkillTree);
            if (!HasPersistentStartNextStageListener(startNextStageButton))
            {
                startNextStageButton.onClick.AddListener(StartNextStageFromSkillTree);
            }
        }

        void EnsureSkillTreeResourceWallet()
        {
            if (database == null)
            {
                InitializeTables();
            }

            if (skillTreeCanvasRoot == null || database == null)
            {
                return;
            }

            var wallet = FindChildRect(skillTreeCanvasRoot, "Skill Tree Resource Wallet");
            if (wallet == null)
            {
                wallet = BuildSkillTreeResourceWallet(skillTreeCanvasRoot);
            }

            wallet.SetParent(skillTreeCanvasRoot, false);
            wallet.anchorMin = new Vector2(0f, 1f);
            wallet.anchorMax = new Vector2(0f, 1f);
            wallet.pivot = new Vector2(0f, 1f);
            wallet.anchoredPosition = new Vector2(36f, -36f);
            wallet.sizeDelta = new Vector2(292f, 520f);
            wallet.SetAsLastSibling();

            var image = wallet.GetComponent<Image>();
            if (image == null)
            {
                image = wallet.gameObject.AddComponent<Image>();
            }
            image.color = new Color(0f, 0f, 0f, 0.42f);
            image.raycastTarget = false;

            var layout = wallet.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = wallet.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            RegisterSkillTreeResourceLabels(wallet);
            foreach (var resource in database.Resources)
            {
                if (!skillTreeResourceLabels.ContainsKey(resource.resourceId))
                {
                    BuildSkillTreeResourceCounter(wallet, resource);
                }
            }

            RegisterSkillTreeResourceLabels(wallet);
            RefreshAllResourceLabels();
        }

        void EnsureTestSkillTreeButton()
        {
            if (skillTreeCanvasRoot == null)
            {
                return;
            }

            var rect = FindChildRect(skillTreeCanvasRoot, "Skill Tree Test Button");
            if (rect == null)
            {
                rect = BuildTestSkillTreeButton(skillTreeCanvasRoot);
            }

            rect.SetParent(skillTreeCanvasRoot, false);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-56f, -56f);
            rect.sizeDelta = new Vector2(100f, 100f);
            rect.localScale = Vector3.one;
            rect.SetAsLastSibling();

            var image = rect.GetComponent<Image>();
            if (image == null)
            {
                image = rect.gameObject.AddComponent<Image>();
            }
            image.sprite = LoadSprite(SkillTreeTestButtonSpriteId);
            image.preserveAspect = true;
            image.raycastTarget = true;

            var button = rect.GetComponent<Button>();
            if (button == null)
            {
                button = rect.gameObject.AddComponent<Button>();
            }
            button.targetGraphic = image;

            var trigger = rect.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = rect.gameObject.AddComponent<EventTrigger>();
            }
            trigger.triggers.Clear();
            AddPointerEvent(trigger, EventTriggerType.PointerEnter, ShowSkillTreeTooltip);
            AddPointerEvent(trigger, EventTriggerType.PointerExit, HideSkillTreeTooltip);
        }

        void EnsureTemporaryStageJumpButtons()
        {
            if (skillTreeCanvasRoot == null)
            {
                return;
            }

            var group = FindChildRect(skillTreeCanvasRoot, TemporaryStageJumpButtonGroupName);
            if (group == null)
            {
                group = BuildTemporaryStageJumpButtons(skillTreeCanvasRoot);
            }

            group.SetParent(skillTreeCanvasRoot, false);
            group.anchorMin = new Vector2(1f, 0f);
            group.anchorMax = new Vector2(1f, 0f);
            group.pivot = new Vector2(1f, 0f);
            group.anchoredPosition = new Vector2(-56f, 520f);
            group.sizeDelta = new Vector2(420f, 84f);
            group.localScale = Vector3.one;
            group.SetAsLastSibling();

            for (var i = 0; i < TemporaryStageJumpNumbers.Length; i++)
            {
                var stageNumber = TemporaryStageJumpNumbers[i];
                var rect = FindChildRect(group, "Temporary Stage Jump " + stageNumber);
                if (rect == null)
                {
                    rect = BuildTemporaryStageJumpButton(group, stageNumber, i);
                }

                var x = (i - (TemporaryStageJumpNumbers.Length - 1) * 0.5f) * 76f;
                rect.SetParent(group, false);
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(x, 0f);
                rect.sizeDelta = new Vector2(70f, 70f);
                rect.localScale = Vector3.one;
                ConfigureTemporaryStageJumpButton(rect, stageNumber);
            }
        }

        void ConfigureTemporaryStageJumpButton(RectTransform rect, int stageNumber)
        {
            var image = rect.GetComponent<Image>();
            if (image == null)
            {
                image = rect.gameObject.AddComponent<Image>();
            }

            image.sprite = LoadSprite(SkillTreeTestButtonSpriteId);
            image.preserveAspect = true;
            image.raycastTarget = true;

            var button = rect.GetComponent<Button>();
            if (button == null)
            {
                button = rect.gameObject.AddComponent<Button>();
            }

            button.targetGraphic = image;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => StartStageFromSkillTree(stageNumber));

            var label = FindChildRect(rect, "Label");
            if (label == null)
            {
                label = CreateRect("Label", rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            }

            label.SetParent(rect, false);
            label.anchorMin = Vector2.zero;
            label.anchorMax = Vector2.one;
            label.pivot = new Vector2(0.5f, 0.5f);
            label.anchoredPosition = Vector2.zero;
            label.sizeDelta = Vector2.zero;
            label.localScale = Vector3.one;

            var text = label.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = label.gameObject.AddComponent<TextMeshProUGUI>();
            }

            text.text = stageNumber.ToString();
            text.font = LoadKaturiSdfFont();
            text.fontSize = 24;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
        }

        void EnsureSkillTreeDataButtons()
        {
            if (skillTreeContentRoot == null || database == null)
            {
                return;
            }

            skillTreeRowsByKey.Clear();
            foreach (var row in database.SkillTree)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.skillStringKey))
                {
                    continue;
                }

                var rect = FindChildRect(skillTreeContentRoot, row.skillStringKey);
                if (rect == null)
                {
                    continue;
                }

                var image = rect.GetComponent<Image>();
                if (image != null)
                {
                    ApplySkillTreeButtonState(rect, row);
                }

                ConfigureSkillTreeIcon(rect, row.skillStringKey);

                var button = rect.GetComponent<Button>();
                if (button == null)
                {
                    button = rect.gameObject.AddComponent<Button>();
                }
                button.interactable = !IsSkillCompleted(row);
                button.targetGraphic = image;

                var relay = rect.GetComponent<SkillTreePointerRelay>();
                if (relay == null)
                {
                    relay = rect.gameObject.AddComponent<SkillTreePointerRelay>();
                }
                relay.Configure(this, row);

                var trigger = rect.GetComponent<EventTrigger>();
                if (trigger == null)
                {
                    trigger = rect.gameObject.AddComponent<EventTrigger>();
                }

                trigger.triggers.Clear();
                var capturedRow = row;
                AddPointerEvent(trigger, EventTriggerType.PointerEnter, _ => ShowSkillTreeTooltip(capturedRow));
                AddPointerEvent(trigger, EventTriggerType.PointerExit, HideSkillTreeTooltip);
                skillTreeRowsByKey[row.skillStringKey] = row;
            }
        }

        bool IsSkillCompleted(SkillTreeRow row)
        {
            return row != null && !string.IsNullOrWhiteSpace(row.skillStringKey) && completedSkillKeys.Contains(row.skillStringKey);
        }

        void ApplySkillTreeButtonState(RectTransform rect, SkillTreeRow row)
        {
            if (rect == null)
            {
                return;
            }

            var image = rect.GetComponent<Image>();
            if (image != null)
            {
                var spriteId = IsSkillCompleted(row)
                    ? SkillTreeUsedButtonSpriteId
                    : IsSkillPurchasable(row)
                        ? SkillTreeAvailableButtonSpriteId
                        : SkillTreeTestButtonSpriteId;
                image.sprite = LoadSprite(spriteId);
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.raycastTarget = true;
            }

            var button = rect.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = !IsSkillCompleted(row);
                button.targetGraphic = image;
            }
        }

        void RefreshSkillTreeButton(SkillTreeRow row)
        {
            if (skillTreeContentRoot == null || row == null)
            {
                return;
            }

            var rect = FindChildRect(skillTreeContentRoot, row.skillStringKey);
            ApplySkillTreeButtonState(rect, row);
        }

        void RefreshSkillTreeButtonStates()
        {
            if (skillTreeContentRoot == null)
            {
                return;
            }

            foreach (var row in skillTreeRowsByKey.Values)
            {
                RefreshSkillTreeButton(row);
            }
        }

        void ConfigureSkillTreeIcon(RectTransform skillButton, string skillKey)
        {
            if (skillButton == null)
            {
                return;
            }

            SkillTreeIconSpriteIds.TryGetValue(skillKey, out var spriteId);
            var iconRect = FindChildRect(skillButton, "Skill Tree Icon");
            if (string.IsNullOrWhiteSpace(spriteId))
            {
                if (iconRect != null)
                {
                    iconRect.gameObject.SetActive(false);
                }

                return;
            }

            if (iconRect == null)
            {
                iconRect = CreateRect("Skill Tree Icon", skillButton, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(112f, 112f));
            }

            iconRect.SetParent(skillButton, false);
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(112f, 112f);
            iconRect.localScale = Vector3.one;
            iconRect.SetAsLastSibling();
            iconRect.gameObject.SetActive(true);

            var icon = iconRect.GetComponent<Image>();
            if (icon == null)
            {
                icon = iconRect.gameObject.AddComponent<Image>();
            }

            icon.sprite = LoadSprite(spriteId);
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }

        void EnsureSkillTreeTooltip()
        {
            if (skillTreeCanvasRoot == null)
            {
                return;
            }

            skillTreeTooltipRoot = FindChildRect(skillTreeCanvasRoot, "Skill Tree Detail Tooltip");
            if (skillTreeTooltipRoot == null)
            {
                BuildSkillTreeTooltip(skillTreeCanvasRoot);
                return;
            }

            skillTreeTooltipRoot.SetParent(skillTreeCanvasRoot, false);
            skillTreeTooltipRoot.anchorMin = new Vector2(1f, 1f);
            skillTreeTooltipRoot.anchorMax = new Vector2(1f, 1f);
            skillTreeTooltipRoot.pivot = new Vector2(1f, 1f);
            skillTreeTooltipRoot.anchoredPosition = new Vector2(-44f, -150f);
            skillTreeTooltipRoot.sizeDelta = new Vector2(780f, 780f);
            skillTreeTooltipRoot.localScale = Vector3.one;
            skillTreeTooltipRoot.SetAsLastSibling();

            var image = skillTreeTooltipRoot.GetComponent<Image>();
            if (image == null)
            {
                image = skillTreeTooltipRoot.gameObject.AddComponent<Image>();
            }
            image.sprite = LoadSprite(SkillTreeInfoSpriteId);
            image.preserveAspect = true;
            image.raycastTarget = false;
            var titleRect = FindChildRect(skillTreeTooltipRoot, "Title");
            var descriptionRect = FindChildRect(skillTreeTooltipRoot, "Description");
            var costRect = FindChildRect(skillTreeTooltipRoot, "Cost");
            skillTreeTooltipTitleText = titleRect?.GetComponent<TextMeshProUGUI>();
            skillTreeTooltipDescriptionText = descriptionRect?.GetComponent<TextMeshProUGUI>();
            skillTreeTooltipCostText = costRect?.GetComponent<TextMeshProUGUI>();
            skillTreeTooltipTitleLegacyText = titleRect?.GetComponent<Text>();
            skillTreeTooltipDescriptionLegacyText = descriptionRect?.GetComponent<Text>();
            skillTreeTooltipCostLegacyText = costRect?.GetComponent<Text>();
            skillTreeTooltipRoot.gameObject.SetActive(false);
        }

        void ShowSkillTreeTooltip(BaseEventData eventData)
        {
            ShowSkillTreeTooltip((SkillTreeRow)null);
        }

        void ShowSkillTreeTooltip(SkillTreeRow row)
        {
            EnsureSkillTreeTooltip();
            if (skillTreeTooltipRoot != null)
            {
                SetSkillTreeTooltipContent(row);
                skillTreeTooltipRoot.gameObject.SetActive(true);
                skillTreeTooltipRoot.SetAsLastSibling();
            }
        }

        void SetSkillTreeTooltipContent(SkillTreeRow row)
        {
            if (row == null)
            {
                SetSkillTreeTooltipText(skillTreeTooltipTitleText, skillTreeTooltipTitleLegacyText, "TEST SKILL");
                SetSkillTreeTooltipText(skillTreeTooltipDescriptionText, skillTreeTooltipDescriptionLegacyText, "테스트 스킬");
                SetSkillTreeTooltipText(skillTreeTooltipCostText, skillTreeTooltipCostLegacyText, "COST 0");
                return;
            }

            SetSkillTreeTooltipText(skillTreeTooltipTitleText, skillTreeTooltipTitleLegacyText, string.IsNullOrWhiteSpace(row.skillName) ? "스킬 강화" : row.skillName);

            var description = database.GetSkillDescription(row.skillStringKey);
            SetSkillTreeTooltipText(skillTreeTooltipDescriptionText, skillTreeTooltipDescriptionLegacyText, string.IsNullOrWhiteSpace(description) ? "효과 정보 없음" : description);

            var costs = new List<string>();
            AddSkillTreeCost(costs, row.followCost, 20001);
            AddSkillTreeCost(costs, row.watcherCost, 20002);
            AddSkillTreeCost(costs, row.loveCost, 20003);
            AddSkillTreeCost(costs, row.donationCost, 20004);
            AddSkillTreeCost(costs, row.redDonationCost, 20005);
            AddSkillTreeCost(costs, row.subscriberCost, 20006);
            SetSkillTreeTooltipText(skillTreeTooltipCostText, skillTreeTooltipCostLegacyText, costs.Count > 0 ? string.Join(" / ", costs.ToArray()) : "COST 0");
        }

        static void SetSkillTreeTooltipText(TextMeshProUGUI tmpText, Text legacyText, string value)
        {
            if (tmpText != null)
            {
                tmpText.text = value;
            }

            if (legacyText != null)
            {
                legacyText.text = value;
            }
        }

        void AddSkillTreeCost(List<string> costs, int amount, int resourceId)
        {
            if (amount <= 0)
            {
                return;
            }

            var resource = database.GetResource(resourceId);
            var name = resource != null && !string.IsNullOrWhiteSpace(resource.resourceName) ? resource.resourceName : resourceId.ToString();
            costs.Add(name + " " + amount);
        }

        void TryPurchaseSkill(SkillTreeRow row)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.skillStringKey) || IsSkillCompleted(row))
            {
                return;
            }

            ShowSkillTreeTooltip(row);
            if (!HasSkillPrerequisite(row))
            {
                SetSkillTreeTooltipText(skillTreeTooltipCostText, skillTreeTooltipCostLegacyText, RequiresManagerActivation(row) && !IsManagerActivationComplete()
                    ? "매니저 활성화 강화 필요"
                    : "이전 강화 완료 필요");
                return;
            }

            if (!CanAffordSkill(row))
            {
                SetSkillTreeTooltipText(skillTreeTooltipCostText, skillTreeTooltipCostLegacyText, "재화 부족");
                return;
            }

            SpendSkillCosts(row);
            ApplySkillEffect(row);
            completedSkillKeys.Add(row.skillStringKey);
            RefreshSkillTreeButtonStates();

            var completedDescription = (skillTreeTooltipDescriptionText != null
                ? skillTreeTooltipDescriptionText.text
                : skillTreeTooltipDescriptionLegacyText != null ? skillTreeTooltipDescriptionLegacyText.text : string.Empty) + "\n강화 완료";
            SetSkillTreeTooltipText(skillTreeTooltipDescriptionText, skillTreeTooltipDescriptionLegacyText, completedDescription);
        }

        bool HasSkillPrerequisite(SkillTreeRow row)
        {
            if (row == null)
            {
                return false;
            }

            if (RequiresManagerActivation(row) && !IsManagerActivationComplete())
            {
                return false;
            }

            if (row.upgradeRank <= 1 || database == null)
            {
                return true;
            }

            foreach (var previousRow in database.SkillTree)
            {
                if (previousRow != null &&
                    previousRow.reinforcedType == row.reinforcedType &&
                    previousRow.upgradeRank == row.upgradeRank - 1)
                {
                    return IsSkillCompleted(previousRow);
                }
            }

            return true;
        }

        static bool RequiresManagerActivation(SkillTreeRow row)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.skillStringKey))
            {
                return false;
            }

            return string.CompareOrdinal(row.skillStringKey, "SD10117") >= 0 &&
                string.CompareOrdinal(row.skillStringKey, "SD10128") <= 0;
        }

        bool IsManagerActivationComplete()
        {
            return completedSkillKeys.Contains(ManagerActivationSkillKey);
        }

        bool CanAffordSkill(SkillTreeRow row)
        {
            return resourceManager != null &&
                resourceManager.CanAfford(20001, row.followCost) &&
                resourceManager.CanAfford(20002, row.watcherCost) &&
                resourceManager.CanAfford(20003, row.loveCost) &&
                resourceManager.CanAfford(20004, row.donationCost) &&
                resourceManager.CanAfford(20005, row.redDonationCost) &&
                resourceManager.CanAfford(20006, row.subscriberCost);
        }

        bool IsSkillPurchasable(SkillTreeRow row)
        {
            return row != null && !IsSkillCompleted(row) && HasSkillPrerequisite(row) && CanAffordSkill(row);
        }

        void SpendSkillCosts(SkillTreeRow row)
        {
            resourceManager.TrySpend(20001, row.followCost);
            resourceManager.TrySpend(20002, row.watcherCost);
            resourceManager.TrySpend(20003, row.loveCost);
            resourceManager.TrySpend(20004, row.donationCost);
            resourceManager.TrySpend(20005, row.redDonationCost);
            resourceManager.TrySpend(20006, row.subscriberCost);
        }

        void ApplySkillEffect(SkillTreeRow row)
        {
            if (row == null)
            {
                return;
            }

            switch (row.reinforcedType)
            {
                case 1:
                    miningSpeedMultiplier *= GetSkillMultiplier(row);
                    break;
                case 2:
                    miningRangeMultiplier *= GetSkillMultiplier(row);
                    RefreshMiningCursor();
                    break;
                case 3:
                    miningDamageMultiplier *= GetDescriptionBasedPercentMultiplier(row);
                    break;
                case 4:
                    pieceSpawnSpeedMultiplier *= GetSkillMultiplier(row);
                    break;
                case 5:
                    miningEfficiencyMultiplier *= GetSkillMultiplier(row);
                    break;
                case 6:
                    managerCount += Mathf.Max(1, Mathf.RoundToInt(row.upAmount));
                    StartManagerRoutine();
                    break;
                case 7:
                    managerRangeMultiplier *= GetSkillMultiplier(row);
                    break;
                case 8:
                    managerDamageMultiplier *= GetSkillMultiplier(row);
                    break;
                case 9:
                    managerSpeedMultiplier *= GetDescriptionBasedPercentMultiplier(row);
                    break;
                case 10:
                    managerCount += Mathf.Max(1, Mathf.RoundToInt(row.upAmount));
                    StartManagerRoutine();
                    break;
            }
        }

        static float GetSkillMultiplier(SkillTreeRow row)
        {
            return 1f + Mathf.Max(0f, row != null ? row.upAmount : 0f);
        }

        static float GetDescriptionBasedPercentMultiplier(SkillTreeRow row)
        {
            if (row == null)
            {
                return 1f;
            }

            if (row.upgradeRank == 1)
            {
                return 1.1f;
            }

            if (row.upgradeRank == 2)
            {
                return 1.3f;
            }

            if (row.upgradeRank == 3)
            {
                return 1.5f;
            }

            return row.upAmount >= 1f ? row.upAmount : 1f + Mathf.Max(0f, row.upAmount);
        }

        void StartManagerRoutine()
        {
            EnsureManagerAgents();
            if (managerRoutine == null)
            {
                managerRoutine = StartCoroutine(ManagerLoop());
            }
        }

        IEnumerator ManagerLoop()
        {
            while (managerCount > 0)
            {
                var delay = Mathf.Clamp(0.8f / Mathf.Max(0.1f, managerSpeedMultiplier), 0.12f, 2f);
                yield return new WaitForSeconds(delay);

                if (skillTreeOpen || pieceManager == null)
                {
                    continue;
                }

                EnsureManagerAgents();
                pieceManager.CleanupDestroyed();
                for (var i = 0; i < managerAgents.Count; i++)
                {
                    var manager = managerAgents[i];
                    if (manager == null || !manager.launched || manager.rectTransform == null)
                    {
                        continue;
                    }

                    var target = FindManagerTarget(manager.rectTransform.anchoredPosition);
                    if (target == null)
                    {
                        continue;
                    }

                    var damage = DamagePerSecond * MiningAttackFrameSeconds * Mathf.Max(1, miningAttackFrames.Count) * managerDamageMultiplier;
                    target.Hit(damage, true);
                }
            }

            managerRoutine = null;
        }

        void UpdateManagerAgents()
        {
            if (skillTreeOpen || managerCount <= 0)
            {
                SetManagerDisplayVisible(false);
                return;
            }

            EnsureManagerAgents();
            SetManagerDisplayVisible(true);
            TryLaunchManagerAgents();

            var moveSpeed = GetManagerMoveSpeed();
            for (var i = 0; i < managerAgents.Count; i++)
            {
                var manager = managerAgents[i];
                if (manager == null || manager.rectTransform == null)
                {
                    continue;
                }

                if (manager.launched)
                {
                    manager.rectTransform.anchoredPosition = Vector2.MoveTowards(
                        manager.rectTransform.anchoredPosition,
                        manager.destination,
                        moveSpeed * Time.deltaTime);
                }

                UpdateManagerAnimation(manager);
            }
        }

        void EnsureManagerAgents()
        {
            if (managerCount <= 0)
            {
                return;
            }

            EnsureManagerDisplayLayer();
            LoadManagerAnimationFrames();
            if (managerDisplayLayer == null)
            {
                return;
            }

            SetManagerDisplayVisible(!skillTreeOpen);

            while (managerAgents.Count < managerCount)
            {
                var root = CreateRect("Manager " + (managerAgents.Count + 1), managerDisplayLayer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(ManagerDisplaySize, ManagerDisplaySize));
                var image = root.gameObject.AddComponent<Image>();
                image.sprite = managerAnimationFrames.Count > 0 ? managerAnimationFrames[0] : LoadSprite("img_manager_01");
                image.color = Color.white;
                image.preserveAspect = true;
                image.raycastTarget = false;
                root.SetAsLastSibling();
                managerAgents.Add(new ManagerAgent
                {
                    rectTransform = root,
                    image = image
                });
            }

            while (managerAgents.Count > managerCount)
            {
                var lastIndex = managerAgents.Count - 1;
                var manager = managerAgents[lastIndex];
                if (manager != null && manager.rectTransform != null)
                {
                    Destroy(manager.rectTransform.gameObject);
                }
                managerAgents.RemoveAt(lastIndex);
            }
        }

        void TryLaunchManagerAgents()
        {
            if (!TryGetCurrentMiningPosition(out var startPosition) || !TryGetSpawnPolygon(out var spawnPolygon))
            {
                return;
            }

            var center = GetCentroid(spawnPolygon);
            var toCenter = center - startPosition;
            var distance = toCenter.magnitude;
            var baseDirection = distance > 0.001f ? toCenter / distance : Vector2.up;
            distance = Mathf.Max(distance, 1f);

            for (var i = 0; i < managerAgents.Count; i++)
            {
                var manager = managerAgents[i];
                if (manager == null || manager.launched || manager.rectTransform == null)
                {
                    continue;
                }

                var direction = RotateVector(baseDirection, GetManagerDirectionOffset(i));
                manager.rectTransform.anchoredPosition = startPosition;
                manager.destination = startPosition + direction * distance;
                manager.launched = true;
                manager.rectTransform.SetAsLastSibling();
            }
        }

        bool TryGetCurrentMiningPosition(out Vector2 position)
        {
            position = Vector2.zero;
            return miningCursor != null &&
                miningCursor.gameObject.activeInHierarchy &&
                TryGetPointerPosition(out var pointerPosition) &&
                IsPointerInsideMiningArea(pointerPosition) &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(pieceDisplayLayer, pointerPosition, uiCamera, out position);
        }

        static float GetManagerDirectionOffset(int managerIndex)
        {
            if (managerIndex == 1)
            {
                return -ManagerDirectionOffsetDegrees;
            }

            return managerIndex == 2 ? ManagerDirectionOffsetDegrees : 0f;
        }

        static Vector2 RotateVector(Vector2 vector, float degrees)
        {
            return Quaternion.Euler(0f, 0f, degrees) * vector;
        }

        float GetManagerMoveSpeed()
        {
            var bossOneSpeed = 190f * 1.8f;
            if (BossDefinitions.TryGetValue(30001, out var bossOneDefinition) && bossOneDefinition != null)
            {
                bossOneSpeed = bossOneDefinition.moveStepDistance / Mathf.Max(0.01f, bossOneDefinition.moveInterval);
            }

            return bossOneSpeed * ManagerBossOneSpeedFactor * managerSpeedMultiplier;
        }

        void UpdateManagerAnimation(ManagerAgent manager)
        {
            if (manager == null || manager.image == null || managerAnimationFrames.Count == 0)
            {
                return;
            }

            manager.frameTimer += Time.deltaTime;
            if (manager.frameTimer < ManagerAnimationFrameSeconds)
            {
                return;
            }

            manager.image.sprite = managerAnimationFrames[manager.frameIndex % managerAnimationFrames.Count];
            manager.frameIndex++;
            manager.frameTimer = 0f;
        }

        void ResetManagerAgents()
        {
            for (var i = 0; i < managerAgents.Count; i++)
            {
                var manager = managerAgents[i];
                if (manager != null && manager.rectTransform != null)
                {
                    Destroy(manager.rectTransform.gameObject);
                }
            }

            managerAgents.Clear();
        }

        void SetManagerDisplayVisible(bool visible)
        {
            if (managerDisplayLayer != null)
            {
                managerDisplayLayer.gameObject.SetActive(visible);
            }
        }

        DestructiblePieceView FindManagerTarget(Vector2 managerPosition)
        {
            if (pieceManager == null)
            {
                return null;
            }

            var range = GetManagerRange();
            var activePieces = pieceManager.ActivePieces;
            for (var i = 0; i < activePieces.Count; i++)
            {
                var piece = activePieces[i];
                if (piece != null && !piece.IsDestroyed && piece.IsHittable && CircleOverlapsRect(managerPosition, range, piece.RectTransform))
                {
                    return piece;
                }
            }

            return null;
        }

        float GetMiningRadius()
        {
            return MiningRadius * miningRangeMultiplier;
        }

        float GetManagerRange()
        {
            return MiningRadius * managerRangeMultiplier;
        }

        void RefreshMiningCursor()
        {
            if (miningCursor != null)
            {
                var diameter = GetMiningRadius() * 2f;
                miningCursor.sizeDelta = new Vector2(diameter, diameter);
            }
        }

        void HideSkillTreeTooltip(BaseEventData eventData)
        {
            if (skillTreeTooltipRoot != null)
            {
                skillTreeTooltipRoot.gameObject.SetActive(false);
            }
        }

        void RegisterSkillTreeResourceLabels(RectTransform wallet)
        {
            skillTreeResourceLabels.Clear();
            foreach (var counter in wallet.GetComponentsInChildren<ResourceCounterView>(true))
            {
                if (counter.NumberLabel == null)
                {
                    continue;
                }

                counter.NumberLabel.Initialize();
                counter.NumberLabel.SetValue(GetResourceAmount(counter.ResourceId));
                skillTreeResourceLabels[counter.ResourceId] = counter.NumberLabel;
            }
        }

        int GetResourceAmount(int resourceId)
        {
            return resourceManager != null ? resourceManager.GetAmount(resourceId) : 0;
        }

        void HandleResourceAmountChanged(int resourceId, int amount)
        {
            RefreshResourceLabels(resourceId);
            RefreshSkillTreeButtonStates();
        }

        void RefreshResourceLabels(int resourceId)
        {
            var amount = GetResourceAmount(resourceId);
            if (resourceLabels.TryGetValue(resourceId, out var mainLabel) && mainLabel != null)
            {
                mainLabel.SetValue(amount);
            }

            if (skillTreeResourceLabels.TryGetValue(resourceId, out var skillTreeLabel) && skillTreeLabel != null)
            {
                skillTreeLabel.SetValue(amount);
            }
        }

        void RefreshAllResourceLabels()
        {
            if (database == null)
            {
                return;
            }

            foreach (var resource in database.Resources)
            {
                RefreshResourceLabels(resource.resourceId);
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

        bool HasPersistentStartGameListener(Button button)
        {
            for (var i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentTarget(i) == this && button.onClick.GetPersistentMethodName(i) == nameof(StartGameFromStartScreen))
                {
                    return true;
                }
            }

            return false;
        }

        void ResetRoundTimer()
        {
            currentStageBossSpawned = false;
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
            SetManagerDisplayVisible(false);
            if (chattingAppearImage != null)
            {
                chattingAppearImage.gameObject.SetActive(false);
            }

            EnsureSkillTreeCanvas();
            ApplySkillTreeTransform();
            if (skillTreeCanvasRoot != null)
            {
                skillTreeCanvasRoot.gameObject.SetActive(true);
            }
        }

        public void StartNextStageFromSkillTree()
        {
            stageManager.MoveNext();

            StartSelectedStageFromSkillTree();
        }

        public void StartGameFromStartScreen()
        {
            if (gameStarted)
            {
                return;
            }

            gameStarted = true;
            ShowGameCanvas();
            ApplyCurrentStageSprite();
            RefreshStageNumberLabel();
            ResetRoundTimer();
            ResetManagerAgents();
            SetManagerDisplayVisible(managerCount > 0);
            FillStagePieces();
        }

        void StartStageFromSkillTree(int stageNumber)
        {
            if (database == null)
            {
                InitializeTables();
            }

            if (database != null && database.Stages.Count > 0)
            {
                var stageIndex = FindStageIndexByNumber(stageNumber);
                stageManager.SelectByStageId(StageIdSpriteBase + Mathf.Max(1, stageNumber) - 1, stageIndex);
            }

            StartSelectedStageFromSkillTree();
        }

        void StartSelectedStageFromSkillTree()
        {
            skillTreeOpen = false;
            ShowGameCanvas();
            ApplyCurrentStageSprite();
            RefreshStageNumberLabel();
            ResetRoundTimer();
            ResetManagerAgents();
            SetManagerDisplayVisible(managerCount > 0);
            FillStagePieces();
        }

        int FindStageIndexByNumber(int stageNumber)
        {
            if (database == null || database.Stages.Count == 0)
            {
                return 0;
            }

            var targetStageId = StageIdSpriteBase + Mathf.Max(1, stageNumber) - 1;
            for (var i = 0; i < database.Stages.Count; i++)
            {
                if (database.Stages[i].stageId == targetStageId)
                {
                    return i;
                }
            }

            return Mathf.Clamp(stageNumber - 1, 0, database.Stages.Count - 1);
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

            if (skillTreeZoom <= 1.001f || IsPointerOverFixedSkillTreeUi(pointerPosition))
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

        bool IsPointerOverFixedSkillTreeUi(Vector2 pointerPosition)
        {
            var rect = startNextStageButton != null ? startNextStageButton.transform as RectTransform : null;
            if (rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, pointerPosition, uiCamera))
            {
                return true;
            }

            rect = FindChildRect(skillTreeCanvasRoot, "Skill Tree Resource Wallet");
            if (rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, pointerPosition, uiCamera))
            {
                return true;
            }

            rect = FindChildRect(skillTreeCanvasRoot, "Skill Tree Test Button");
            if (rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, pointerPosition, uiCamera))
            {
                return true;
            }

            rect = FindChildRect(skillTreeCanvasRoot, TemporaryStageJumpButtonGroupName);
            if (rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, pointerPosition, uiCamera))
            {
                return true;
            }

            return skillTreeTooltipRoot != null &&
                skillTreeTooltipRoot.gameObject.activeInHierarchy &&
                RectTransformUtility.RectangleContainsScreenPoint(skillTreeTooltipRoot, pointerPosition, uiCamera);
        }

        void ShowGameCanvas()
        {
            if (canvasRoot != null)
            {
                canvasRoot.gameObject.SetActive(true);
            }

            if (skillTreeCanvasRoot != null)
            {
                skillTreeCanvasRoot.gameObject.SetActive(false);
            }

            if (startScreenCanvasRoot != null)
            {
                startScreenCanvasRoot.gameObject.SetActive(false);
            }

            if (endingVideoCanvasRoot != null)
            {
                endingVideoCanvasRoot.gameObject.SetActive(false);
            }

            if (chattingAppearImage != null)
            {
                chattingAppearImage.gameObject.SetActive(true);
            }
        }

        void ClearActivePieces()
        {
            pieceManager.Clear();
        }

        static string FormatRoundTime(int totalSeconds)
        {
            totalSeconds = Mathf.Max(0, totalSeconds);
            return (totalSeconds / 60) + ":" + (totalSeconds % 60).ToString("00");
        }

        void FillStagePieces()
        {
            CleanupDestroyedPieceRefs();
            SpawnBossForCurrentStageIfNeeded();
            var spawnAttemptsRemaining = TargetPieceCount * 4;
            while (pieceManager.ActiveCount < TargetPieceCount && spawnAttemptsRemaining-- > 0)
            {
                if (!SpawnPieceAtRandomPosition())
                {
                    break;
                }
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

        bool SpawnPieceAtRandomPosition()
        {
            var piece = PickPiece();
            if (piece == null)
            {
                return false;
            }

            var size = Random.Range(155f, 215f) * SpawnPieceSizeScale;
            var half = size * 0.5f;
            EnsurePieceDisplayLayer();
            if (!TryPickSpawnPosition(half, out var displayPosition))
            {
                return false;
            }
            var pieceRoot = CreateRect(piece.pieceName, pieceDisplayLayer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), displayPosition, new Vector2(size, size));
            pieceRoot.localRotation = Quaternion.identity;
            pieceRoot.localScale = Vector3.one;
            var image = pieceRoot.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            var view = pieceRoot.gameObject.AddComponent<DestructiblePieceView>();
            view.Initialize(piece, LoadSprite(piece.imageId));
            RegisterPieceView(view);
            BringActiveBossesToFront();
            return true;
        }

        void SpawnBossForCurrentStageIfNeeded()
        {
            if (currentStage == null || currentStage.bossId <= 0 || currentStageBossSpawned)
            {
                return;
            }

            var bossPiece = database.GetPiece(currentStage.bossId);
            if (bossPiece == null)
            {
                return;
            }

            BossDefinitions.TryGetValue(bossPiece.pieceId, out var definition);
            var size = definition != null ? definition.size : 260f;
            var half = size * 0.5f;
            EnsurePieceDisplayLayer();
            if (!TryPickSpawnPosition(half, out var displayPosition))
            {
                return;
            }
            currentStageBossSpawned = true;
            var bossRoot = CreateRect(bossPiece.pieceName, pieceDisplayLayer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), displayPosition, new Vector2(size, size));
            bossRoot.localRotation = Quaternion.identity;
            bossRoot.localScale = Vector3.one;

            var image = bossRoot.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            var idleFrames = definition != null ? LoadAnimationFrames(definition.idleAnimation, new Vector2(0.5f, 0.5f)) : new List<Sprite>();
            var moveFrames = definition != null ? LoadAnimationFrames(definition.moveAnimation, new Vector2(0.5f, 0.5f)) : new List<Sprite>();
            var emergeFrames = definition != null ? LoadAnimationFrames(definition.emergeAnimation, new Vector2(0.5f, 0.5f)) : new List<Sprite>();
            var deathFrames = definition != null ? LoadAnimationFrames(definition.deathAnimation, new Vector2(0.5f, 0.5f)) : new List<Sprite>();
            var idleSprite = idleFrames.Count > 0
                ? idleFrames[0]
                : definition != null && definition.animateIdleWithMoveAnimation && moveFrames.Count > 0
                    ? moveFrames[0]
                    : LoadSprite(bossPiece.imageId);
            if (idleSprite == null && moveFrames.Count > 0)
            {
                idleSprite = moveFrames[0];
            }
            if (idleSprite == null && definition != null && !string.IsNullOrWhiteSpace(definition.fallbackImageId))
            {
                idleSprite = LoadSprite(definition.fallbackImageId);
            }
            if (idleSprite == null && definition == null)
            {
                Destroy(bossRoot.gameObject);
                return;
            }

            var view = bossRoot.gameObject.AddComponent<DestructiblePieceView>();
            view.Initialize(bossPiece, idleSprite);
            view.Defeated += HandleBossDefeated;
            image.preserveAspect = definition == null || definition.preserveAspect;
            RegisterPieceView(view);
            bossRoot.SetAsLastSibling();

            if (definition != null)
            {
                var bossView = bossRoot.gameObject.AddComponent<BossPieceView>();
                bossView.Initialize(
                    this,
                    view,
                    image,
                    idleSprite,
                    idleFrames,
                    moveFrames,
                    emergeFrames,
                    deathFrames,
                    definition.moveInterval,
                    definition.moveStepDistance,
                    definition.moveBoundsScale,
                    definition.moveDurations,
                    definition.pattern,
                    definition.disappearDelay,
                    definition.hiddenDelay,
                    definition.emergeDisplayScale,
                    definition.emergeDisplayOffsetY,
                    definition.animateIdleWithMoveAnimation);
            }
        }

        void BringActiveBossesToFront()
        {
            var activePieces = pieceManager.ActivePieces;
            for (var i = 0; i < activePieces.Count; i++)
            {
                var piece = activePieces[i];
                if (piece == null || piece.IsDestroyed || piece.GetComponent<BossPieceView>() == null)
                {
                    continue;
                }

                piece.RectTransform.SetAsLastSibling();
            }
        }

        List<Sprite> LoadAnimationFrames(AnimationSource source, Vector2 pivot)
        {
            if (source == null)
            {
                return new List<Sprite>();
            }

            var cacheKey = source.sheetPath + "|" + source.legacyFrameRoot + "|" + source.fallbackLegacyFrameRoot + "|" + source.frameCount + "|" + pivot;
            if (bossAnimationFrames.TryGetValue(cacheKey, out var cachedFrames))
            {
                return cachedFrames;
            }

            var frames = new List<Sprite>();
            if (!string.IsNullOrWhiteSpace(source.sheetPath))
            {
                var slicedSprites = Resources.LoadAll<Sprite>(source.sheetPath);
                if (slicedSprites != null && slicedSprites.Length > 1)
                {
                    var sortedSprites = new List<Sprite>(slicedSprites);
                    sortedSprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
                    frames.AddRange(sortedSprites);
                }

                if (frames.Count == 0)
                {
                    var texture = Resources.Load<Texture2D>(source.sheetPath);
                    if (texture != null)
                    {
                        AddFramesFromSpriteSheet(frames, texture, source.frameCount, pivot);
                    }
                }

                if (frames.Count == 0)
                {
                    var sprite = Resources.Load<Sprite>(source.sheetPath);
                    if (sprite != null)
                    {
                        frames.Add(sprite);
                    }
                }
            }

            if (frames.Count == 0 && !string.IsNullOrWhiteSpace(source.legacyFrameRoot))
            {
                AddLegacyAnimationFrames(frames, source.legacyFrameRoot, source.frameCount, pivot);
            }

            if (frames.Count == 0 && !string.IsNullOrWhiteSpace(source.fallbackLegacyFrameRoot))
            {
                AddLegacyAnimationFrames(frames, source.fallbackLegacyFrameRoot, source.frameCount, pivot);
            }

            bossAnimationFrames[cacheKey] = frames;
            return frames;
        }

        static void AddLegacyAnimationFrames(List<Sprite> frames, string frameRoot, int frameCount, Vector2 pivot)
        {
            for (var i = 0; i < frameCount; i++)
            {
                var path = frameRoot + i.ToString(BossFrameFormat);
                var texture = Resources.Load<Texture2D>(path);
                if (texture != null)
                {
                    frames.Add(Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), pivot, 100f));
                    continue;
                }

                var sprite = Resources.Load<Sprite>(path);
                if (sprite != null)
                {
                    frames.Add(sprite);
                }
            }
        }

        static void AddFramesFromSpriteSheet(List<Sprite> frames, Texture2D texture, int frameCount, Vector2 pivot)
        {
            if (texture == null)
            {
                return;
            }

            frameCount = Mathf.Max(1, frameCount);
            var columns = frameCount;
            var rows = 1;
            if (texture.width < texture.height * frameCount * 0.45f)
            {
                columns = Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(frameCount * texture.width / (float)Mathf.Max(1, texture.height))));
                columns = Mathf.Clamp(columns, 1, frameCount);
                while (columns > 1 && frameCount % columns != 0)
                {
                    columns--;
                }

                rows = Mathf.Max(1, Mathf.CeilToInt(frameCount / (float)columns));
            }

            var frameWidth = texture.width / (float)columns;
            var frameHeight = texture.height / (float)rows;

            for (var i = 0; i < frameCount; i++)
            {
                var column = i % columns;
                var rowFromTop = i / columns;
                var y = texture.height - (rowFromTop + 1) * frameHeight;
                var rect = new Rect(column * frameWidth, y, frameWidth, frameHeight);
                if (rect.width > 0f && rect.height > 0f)
                {
                    frames.Add(Sprite.Create(texture, rect, pivot, 100f));
                }
            }
        }

        bool TryPickSpawnPosition(float halfSize, out Vector2 position)
        {
            position = Vector2.zero;
            if (TryGetSpawnPolygon(out var polygon))
            {
                var centroid = GetCentroid(polygon);
                for (var i = 0; i < 48; i++)
                {
                    var candidate = SamplePointInPolygon(polygon);
                    if (IsPieceInsidePolygon(candidate, halfSize, polygon))
                    {
                        position = candidate;
                        return true;
                    }
                }

                if (TryFindGridSpawnPosition(polygon, halfSize, centroid, out var gridCandidate))
                {
                    position = gridCandidate;
                    return true;
                }

                return false;
            }

            if (pieceLayer == null || pieceDisplayLayer == null)
            {
                return false;
            }

            var rect = pieceLayer.rect;
            var min = new Vector2(rect.xMin + halfSize, rect.yMin + halfSize);
            var max = new Vector2(rect.xMax - halfSize, rect.yMax - halfSize);
            if (min.x > max.x || min.y > max.y)
            {
                return false;
            }

            var anchoredPosition = new Vector2(Random.Range(min.x, max.x), Random.Range(min.y, max.y));
            var worldPosition = pieceLayer.TransformPoint(anchoredPosition);
            position = (Vector2)pieceDisplayLayer.InverseTransformPoint(worldPosition);
            return true;
        }

        public bool TryGetBossRandomPosition(RectTransform bossTransform, out Vector2 position)
        {
            position = bossTransform != null ? bossTransform.anchoredPosition : Vector2.zero;
            if (bossTransform == null || !TryGetSpawnPolygon(out var polygon))
            {
                return false;
            }

            var halfSize = Mathf.Max(bossTransform.rect.width, bossTransform.rect.height) * 0.5f;
            var currentPosition = bossTransform.anchoredPosition;
            var minimumDistance = Mathf.Max(halfSize * 1.4f, 160f);
            var bestCandidate = currentPosition;
            var bestDistance = 0f;

            for (var i = 0; i < 32; i++)
            {
                if (!TryPickSpawnPosition(halfSize, out var candidate))
                {
                    continue;
                }
                var distance = Vector2.Distance(currentPosition, candidate);
                if (distance >= minimumDistance)
                {
                    position = candidate;
                    return true;
                }

                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    bestCandidate = candidate;
                }
            }

            if (bestDistance > 1f)
            {
                position = bestCandidate;
                return true;
            }

            var center = GetCentroid(polygon);
            if (IsPieceInsidePolygon(center, halfSize, polygon))
            {
                position = center;
                return true;
            }

            return false;
        }

        public bool TryGetBossMovePath(RectTransform bossTransform, float stepDistance, Vector2 direction, List<Vector2> path, out Vector2 outgoingDirection, float boundsScale = 1f)
        {
            if (path != null)
            {
                path.Clear();
            }

            outgoingDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
            if (bossTransform == null || !TryGetSpawnPolygon(out var polygon))
            {
                return false;
            }

            var halfSize = Mathf.Max(bossTransform.rect.width, bossTransform.rect.height) * 0.5f * Mathf.Clamp(boundsScale, 0.1f, 1f);
            var currentPosition = bossTransform.anchoredPosition;
            var center = GetCentroid(polygon);
            if (!IsPieceInsidePolygon(currentPosition, halfSize, polygon))
            {
                if (!IsPieceInsidePolygon(center, halfSize, polygon))
                {
                    return false;
                }

                currentPosition = center;
                path?.Add(currentPosition);
            }

            var remainingDistance = Mathf.Max(0f, stepDistance);
            const int MaxBounceCount = 4;
            for (var bounce = 0; bounce <= MaxBounceCount && remainingDistance > 1f; bounce++)
            {
                var candidate = currentPosition + outgoingDirection * remainingDistance;
                if (IsPieceInsidePolygon(candidate, halfSize, polygon))
                {
                    path?.Add(candidate);
                    return path == null || path.Count > 0;
                }

                var travelDistance = FindMaxInsideTravelDistance(currentPosition, outgoingDirection, remainingDistance, halfSize, polygon);
                if (travelDistance > 0.5f)
                {
                    currentPosition += outgoingDirection * travelDistance;
                    path?.Add(currentPosition);
                    remainingDistance -= travelDistance;
                }
                else
                {
                    remainingDistance = Mathf.Max(0f, remainingDistance - 1f);
                }

                var normal = FindBossBounceNormal(currentPosition + outgoingDirection * 2f, halfSize, polygon);
                outgoingDirection = Vector2.Reflect(outgoingDirection, normal).normalized;
                if (outgoingDirection.sqrMagnitude <= 0.001f || !IsPieceInsidePolygon(currentPosition + outgoingDirection * 2f, halfSize, polygon))
                {
                    outgoingDirection = (center - currentPosition).sqrMagnitude > 0.001f ? (center - currentPosition).normalized : Vector2.right;
                }
            }

            if (path != null && path.Count > 0)
            {
                return true;
            }

            if (IsPieceInsidePolygon(center, halfSize, polygon))
            {
                path?.Add(center);
                outgoingDirection = (center - currentPosition).sqrMagnitude > 0.001f ? (center - currentPosition).normalized : outgoingDirection;
                return true;
            }

            return false;
        }

        static float FindMaxInsideTravelDistance(Vector2 origin, Vector2 direction, float maxDistance, float halfSize, Vector2[] polygon)
        {
            var low = 0f;
            var high = maxDistance;
            for (var i = 0; i < 12; i++)
            {
                var mid = (low + high) * 0.5f;
                var candidate = origin + direction * mid;
                if (IsPieceInsidePolygon(candidate, halfSize, polygon))
                {
                    low = mid;
                }
                else
                {
                    high = mid;
                }
            }

            return low;
        }

        static Vector2 FindBossBounceNormal(Vector2 outsideCenter, float halfSize, Vector2[] polygon)
        {
            var corners = new[]
            {
                outsideCenter + new Vector2(-halfSize, -halfSize),
                outsideCenter + new Vector2(-halfSize, halfSize),
                outsideCenter + new Vector2(halfSize, halfSize),
                outsideCenter + new Vector2(halfSize, -halfSize)
            };
            var signedArea = GetSignedArea(polygon);
            var bestNormal = Vector2.up;
            var smallestDistance = float.MaxValue;

            for (var i = 0; i < polygon.Length; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Length];
                var edge = b - a;
                if (edge.sqrMagnitude <= 0.001f)
                {
                    continue;
                }

                var inwardNormal = signedArea >= 0f
                    ? new Vector2(-edge.y, edge.x).normalized
                    : new Vector2(edge.y, -edge.x).normalized;

                for (var j = 0; j < corners.Length; j++)
                {
                    var distance = Vector2.Dot(corners[j] - a, inwardNormal);
                    if (distance < smallestDistance)
                    {
                        smallestDistance = distance;
                        bestNormal = inwardNormal;
                    }
                }
            }

            return bestNormal;
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
            BringMiningVisualsToFront();
        }

        void BringMiningVisualsToFront()
        {
            if (miningCursor != null)
            {
                miningCursor.SetAsLastSibling();
            }

            if (miningAttackImage != null)
            {
                miningAttackImage.rectTransform.SetAsLastSibling();
            }
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
            BringMiningVisualsToFront();

            var frameSeconds = MiningAttackFrameSeconds / Mathf.Max(0.1f, miningSpeedMultiplier);
            for (var frameIndex = 0; frameIndex < miningAttackFrames.Count; frameIndex++)
            {
                if (skillTreeOpen)
                {
                    HideMiningAttack();
                    yield break;
                }

                miningAttackImage.sprite = miningAttackFrames[frameIndex];

                var elapsed = 0f;
                while (elapsed < frameSeconds)
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
            BringMiningVisualsToFront();
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

            var damage = DamagePerSecond * MiningAttackFrameSeconds * Mathf.Max(1, miningAttackFrames.Count) * miningDamageMultiplier;
            var miningRadius = GetMiningRadius();
            pieceManager.CleanupDestroyed();
            var activePieces = pieceManager.ActivePieces;

            for (var i = activePieces.Count - 1; i >= 0; i--)
            {
                var piece = activePieces[i];
                if (piece == null || piece.IsDestroyed)
                {
                    continue;
                }

                if (piece.IsHittable && CircleOverlapsRect(localPoint, miningRadius, piece.RectTransform))
                {
                    piece.Hit(damage, true);
                }
            }
        }

        void CleanupDestroyedPieceRefs()
        {
            pieceManager.CleanupDestroyed();
        }

        void RegisterPieceView(DestructiblePieceView view)
        {
            view.HitFeedbackRequested += PlayHitFeedback;
            view.CollectionRequested += CollectPiece;
            pieceManager.Register(view);
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

        static TextMeshProUGUI CreateText(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, string value, int fontSize, FontStyle fontStyle)
        {
            var rect = CreateRect(name, parent, anchorMin, anchorMax, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = LoadKaturiSdfFont();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle == FontStyle.Bold ? FontStyles.Bold : FontStyles.Normal;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.2f, 0.08f, 0.3f, 0.95f);
            text.raycastTarget = false;
            return text;
        }

        static void AddPointerEvent(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction<BaseEventData> callback)
        {
            var entry = new EventTrigger.Entry { eventID = eventType };
            entry.callback.AddListener(callback);
            trigger.triggers.Add(entry);
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
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            var path = SpriteRoot + id;
            var sprite = UnityEngine.Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            var texture = UnityEngine.Resources.Load<Texture2D>(path);
            return texture != null
                ? Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f)
                : null;
        }

        void ConvertExistingTextToKaturiSdf()
        {
            if (canvasRoot == null)
            {
                return;
            }

            foreach (var legacyText in canvasRoot.GetComponentsInChildren<Text>(true))
            {
                var text = legacyText.GetComponent<TextMeshProUGUI>();
                if (text == null)
                {
                    text = legacyText.gameObject.AddComponent<TextMeshProUGUI>();
                }

                text.text = legacyText.text;
                text.font = LoadKaturiSdfFont();
                text.fontSize = legacyText.fontSize;
                text.fontStyle = legacyText.fontStyle == FontStyle.Bold ? FontStyles.Bold : FontStyles.Normal;
                text.alignment = ConvertTextAlignment(legacyText.alignment);
                text.color = legacyText.color;
                text.raycastTarget = legacyText.raycastTarget;
                text.enableAutoSizing = legacyText.resizeTextForBestFit;
                text.fontSizeMin = legacyText.resizeTextMinSize;
                text.fontSizeMax = legacyText.resizeTextMaxSize;
                text.textWrappingMode = legacyText.horizontalOverflow == HorizontalWrapMode.Wrap
                    ? TextWrappingModes.Normal
                    : TextWrappingModes.NoWrap;
                text.overflowMode = legacyText.verticalOverflow == VerticalWrapMode.Truncate
                    ? TextOverflowModes.Truncate
                    : TextOverflowModes.Overflow;
                Destroy(legacyText);
            }
        }

        void ShowStartScreen()
        {
            gameStarted = false;
            if (canvasRoot != null)
            {
                canvasRoot.gameObject.SetActive(false);
            }

            if (skillTreeCanvasRoot != null)
            {
                skillTreeCanvasRoot.gameObject.SetActive(false);
            }

            if (startScreenCanvasRoot != null)
            {
                startScreenCanvasRoot.gameObject.SetActive(true);
            }

            if (endingVideoCanvasRoot != null)
            {
                endingVideoCanvasRoot.gameObject.SetActive(false);
            }
        }

        void ShowEndingVideoCanvas()
        {
            if (canvasRoot != null)
            {
                canvasRoot.gameObject.SetActive(false);
            }

            if (skillTreeCanvasRoot != null)
            {
                skillTreeCanvasRoot.gameObject.SetActive(false);
            }

            if (startScreenCanvasRoot != null)
            {
                startScreenCanvasRoot.gameObject.SetActive(false);
            }

            if (endingVideoCanvasRoot != null)
            {
                endingVideoCanvasRoot.gameObject.SetActive(true);
            }
        }

        static TMP_FontAsset LoadKaturiSdfFont()
        {
            if (katuriSdfFont == null)
            {
                katuriSdfFont = Resources.Load<TMP_FontAsset>(KaturiSdfFontPath);
            }

            return katuriSdfFont;
        }

        static TextAlignmentOptions ConvertTextAlignment(TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.MidlineLeft;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.MidlineRight;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
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














