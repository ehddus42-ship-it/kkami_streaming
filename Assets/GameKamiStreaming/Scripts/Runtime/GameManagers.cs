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
        readonly MonoBehaviour coroutineHost;
        readonly KkamiTableDatabase database;
        readonly RectTransform effectLayer;
        readonly Dictionary<string, GameObject> prefabsByEffectId = new Dictionary<string, GameObject>();

        public GameEffectManager(MonoBehaviour host, KkamiTableDatabase tableDatabase, RectTransform layer)
        {
            coroutineHost = host;
            database = tableDatabase;
            effectLayer = layer;
        }

        public void PlayHitFeedback(RectTransform source, string effectId)
        {
            var prefab = LoadPrefab(effectId);
            if (prefab != null)
            {
                UnityEngine.Object.Instantiate(prefab, source.position, Quaternion.identity, effectLayer);
            }
            else
            {
                coroutineHost.StartCoroutine(Pulse(source));
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

        static IEnumerator Pulse(RectTransform target)
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
    }
}
