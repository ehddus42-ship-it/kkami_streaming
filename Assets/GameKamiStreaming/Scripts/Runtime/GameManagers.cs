using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameKamiStreaming
{
    public interface IBossMovementArea
    {
        bool TryGetBossRandomPosition(RectTransform bossTransform, out Vector2 position);
        bool TryGetBossMovePath(RectTransform bossTransform, float stepDistance, Vector2 direction, List<Vector2> path, out Vector2 outgoingDirection, float boundsScale = 1f);
    }

    public sealed class GameResourceManager
    {
        readonly Dictionary<int, int> amountsByResourceId = new Dictionary<int, int>();

        public event Action<int, int> AmountChanged;

        public void Initialize(IEnumerable<ResourceRow> resources)
        {
            amountsByResourceId.Clear();
            foreach (var resource in resources)
            {
                amountsByResourceId[resource.resourceId] = 0;
            }
        }

        public int GetAmount(int resourceId)
        {
            return amountsByResourceId.TryGetValue(resourceId, out var amount) ? amount : 0;
        }

        public void Add(int resourceId, int amount)
        {
            var nextAmount = GetAmount(resourceId) + Mathf.Max(0, amount);
            amountsByResourceId[resourceId] = nextAmount;
            AmountChanged?.Invoke(resourceId, nextAmount);
        }

        public bool CanAfford(int resourceId, int amount)
        {
            return amount <= 0 || GetAmount(resourceId) >= amount;
        }

        public bool TrySpend(int resourceId, int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            var currentAmount = GetAmount(resourceId);
            if (currentAmount < amount)
            {
                return false;
            }

            var nextAmount = currentAmount - amount;
            amountsByResourceId[resourceId] = nextAmount;
            AmountChanged?.Invoke(resourceId, nextAmount);
            return true;
        }
    }

    public sealed class GameStageManager
    {
        readonly IReadOnlyList<StageRow> stages;

        public GameStageManager(IReadOnlyList<StageRow> stageRows)
        {
            stages = stageRows;
            CurrentIndex = stages.Count > 0 ? 0 : -1;
        }

        public int CurrentIndex { get; private set; }
        public StageRow Current => CurrentIndex >= 0 && CurrentIndex < stages.Count ? stages[CurrentIndex] : null;

        public void MoveNext()
        {
            if (stages.Count > 0)
            {
                CurrentIndex = (CurrentIndex + 1) % stages.Count;
            }
        }

        public void SelectByStageId(int stageId, int fallbackIndex)
        {
            for (var i = 0; i < stages.Count; i++)
            {
                if (stages[i].stageId == stageId)
                {
                    CurrentIndex = i;
                    return;
                }
            }

            CurrentIndex = stages.Count > 0 ? Mathf.Clamp(fallbackIndex, 0, stages.Count - 1) : -1;
        }
    }

    public sealed class GamePieceManager
    {
        readonly List<DestructiblePieceView> activePieces = new List<DestructiblePieceView>();

        public int ActiveCount => activePieces.Count;
        public IReadOnlyList<DestructiblePieceView> ActivePieces => activePieces;

        public void Register(DestructiblePieceView piece)
        {
            if (piece != null && !activePieces.Contains(piece))
            {
                activePieces.Add(piece);
            }
        }

        public void CleanupDestroyed()
        {
            for (var i = activePieces.Count - 1; i >= 0; i--)
            {
                if (activePieces[i] == null || activePieces[i].IsDestroyed)
                {
                    activePieces.RemoveAt(i);
                }
            }
        }

        public void Clear()
        {
            for (var i = activePieces.Count - 1; i >= 0; i--)
            {
                if (activePieces[i] != null)
                {
                    UnityEngine.Object.Destroy(activePieces[i].gameObject);
                }
            }
            activePieces.Clear();
        }
    }

    public sealed class GameEffectManager
    {
        public const float HitShrinkDurationSeconds = 0.035f;

        readonly MonoBehaviour coroutineHost;
        readonly KkamiTableDatabase database;
        readonly RectTransform effectLayer;
        readonly Dictionary<string, GameObject> prefabsByEffectId = new Dictionary<string, GameObject>();
        readonly HashSet<RectTransform> pulsingTargets = new HashSet<RectTransform>();

        public GameEffectManager(MonoBehaviour host, KkamiTableDatabase tableDatabase, RectTransform layer)
        {
            coroutineHost = host;
            database = tableDatabase;
            effectLayer = layer;
        }

        public void PlayHitFeedback(RectTransform source, string effectId, bool holdAtReducedScale)
        {
            var prefab = LoadPrefab(effectId);
            if (prefab != null && source != null)
            {
                UnityEngine.Object.Instantiate(prefab, source.position, Quaternion.identity, effectLayer);
            }

            // Every piece and boss gets the same non-stacking hit response. Ignore
            // overlapping requests so a rapid stream of hits can never accumulate scale.
            if (source != null && pulsingTargets.Add(source))
            {
                coroutineHost.StartCoroutine(Pulse(source, holdAtReducedScale));
            }
        }

        GameObject LoadPrefab(string effectId)
        {
            if (string.IsNullOrWhiteSpace(effectId))
            {
                return null;
            }

            if (prefabsByEffectId.TryGetValue(effectId, out var cachedPrefab))
            {
                return cachedPrefab;
            }

            var effect = database.GetEffect(effectId);
            var prefab = effect == null || string.IsNullOrWhiteSpace(effect.prefabPath)
                ? null
                : Resources.Load<GameObject>(effect.prefabPath);
            prefabsByEffectId[effectId] = prefab;
            return prefab;
        }

        IEnumerator Pulse(RectTransform target, bool holdAtReducedScale)
        {
            if (target == null)
            {
                pulsingTargets.Remove(target);
                yield break;
            }

            var baseScale = target.localScale;
            var reducedScale = baseScale * 0.94f;
            const float restoreSeconds = 0.065f;
            var elapsed = 0f;
            while (elapsed < HitShrinkDurationSeconds)
            {
                if (target == null)
                {
                    pulsingTargets.Remove(target);
                    yield break;
                }

                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(baseScale, reducedScale, Mathf.Clamp01(elapsed / HitShrinkDurationSeconds));
                yield return null;
            }

            if (holdAtReducedScale)
            {
                if (target != null)
                {
                    target.localScale = reducedScale;
                }

                pulsingTargets.Remove(target);
                yield break;
            }

            elapsed = 0f;
            while (elapsed < restoreSeconds)
            {
                if (target == null)
                {
                    pulsingTargets.Remove(target);
                    yield break;
                }

                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(reducedScale, baseScale, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / restoreSeconds)));
                yield return null;
            }

            if (target != null)
            {
                target.localScale = baseScale;
            }

            pulsingTargets.Remove(target);
        }
    }
}
