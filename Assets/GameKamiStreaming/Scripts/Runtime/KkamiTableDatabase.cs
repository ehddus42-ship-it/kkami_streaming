using System.Collections.Generic;
using UnityEngine;

namespace GameKamiStreaming
{
    public sealed class KkamiTableDatabase
    {
        const string TableRoot = "GameKamiStreaming/DataTables/";

        public readonly List<ResourceRow> Resources = new List<ResourceRow>();
        public readonly List<PieceRow> Pieces = new List<PieceRow>();
        public readonly List<StageRow> Stages = new List<StageRow>();
        public readonly List<SkillTreeRow> SkillTree = new List<SkillTreeRow>();
        public readonly List<EffectRow> Effects = new List<EffectRow>();

        readonly Dictionary<int, ResourceRow> resourcesById = new Dictionary<int, ResourceRow>();
        readonly Dictionary<int, PieceRow> piecesById = new Dictionary<int, PieceRow>();
        readonly Dictionary<string, EffectRow> effectsById = new Dictionary<string, EffectRow>();

        public static KkamiTableDatabase Load()
        {
            var database = new KkamiTableDatabase();
            database.LoadResources();
            database.LoadPieces();
            database.LoadStages();
            database.LoadSkillTree();
            database.LoadEffects();
            return database;
        }

        public ResourceRow GetResource(int id)
        {
            resourcesById.TryGetValue(id, out var row);
            return row;
        }

        public PieceRow GetPiece(int id)
        {
            piecesById.TryGetValue(id, out var row);
            return row;
        }

        public EffectRow GetEffect(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            effectsById.TryGetValue(id, out var row);
            return row;
        }

        void LoadResources()
        {
            foreach (var row in Read("resource").Rows)
            {
                var data = new ResourceRow
                {
                    resourceId = Int(row, "resource_id"),
                    resourceName = Str(row, "res_name"),
                    imageId = Str(row, "resimg_id"),
                    effectId = Str(row, "effect_id")
                };
                Resources.Add(data);
                resourcesById[data.resourceId] = data;
            }
        }

        void LoadPieces()
        {
            foreach (var row in Read("piece").Rows)
            {
                var data = new PieceRow
                {
                    pieceId = Int(row, "piece_id"),
                    pieceName = Str(row, "piece_name"),
                    resourceId = Int(row, "resource_id"),
                    resourceAmount = Int(row, "resource_int"),
                    maxHp = Mathf.Max(1, Int(row, "hp_int")),
                    imageId = Str(row, "pieceimg_id"),
                    effectId = Str(row, "effect_id")
                };
                Pieces.Add(data);
                piecesById[data.pieceId] = data;
            }
        }

        void LoadStages()
        {
            foreach (var row in Read("stage").Rows)
            {
                var data = new StageRow
                {
                    stageId = Int(row, "stage_id"),
                    bossId = Int(row, "boss_id"),
                    effectId = Str(row, "effect_id")
                };

                foreach (var pair in row)
                {
                    if (!pair.Key.StartsWith("piece_") || !pair.Key.EndsWith("_weight"))
                    {
                        continue;
                    }

                    var idText = pair.Key.Substring("piece_".Length, pair.Key.Length - "piece_".Length - "_weight".Length);
                    if (int.TryParse(idText, out var pieceId) && Float(pair.Value) > 0f)
                    {
                        data.pieceWeights.Add(new StagePieceWeight { pieceId = pieceId, weight = Float(pair.Value) });
                    }
                }

                Stages.Add(data);
            }
        }

        void LoadSkillTree()
        {
            foreach (var row in Read("skilltree").Rows)
            {
                SkillTree.Add(new SkillTreeRow
                {
                    tileId = Int(row, "tile_id"),
                    reinforcedType = Int(row, "reinforced_int"),
                    upAmount = Int(row, "up_int"),
                    useSubscription = Bool(row, "sub_use"),
                    followCost = Int(row, "follow_int"),
                    watcherCost = Int(row, "watcher_int"),
                    loveCost = Int(row, "love_int"),
                    donationCost = Int(row, "donation_int"),
                    redDonationCost = Int(row, "reddonation_int"),
                    effectId = Str(row, "effect_id"),
                    unlockPieceId = Int(row, "unlock_piece_id"),
                    unlockResourceId = Int(row, "unlock_resource_id")
                });
            }
        }

        void LoadEffects()
        {
            foreach (var row in Read("effects").Rows)
            {
                var data = new EffectRow
                {
                    effectId = Str(row, "effect_id"),
                    effectName = Str(row, "effect_name"),
                    prefabPath = Str(row, "prefab_path")
                };
                if (!string.IsNullOrWhiteSpace(data.effectId))
                {
                    Effects.Add(data);
                    effectsById[data.effectId] = data;
                }
            }
        }

        static CsvTable Read(string tableName)
        {
            var asset = UnityEngine.Resources.Load<TextAsset>(TableRoot + tableName);
            if (asset == null)
            {
                Debug.LogWarning($"Missing table: {TableRoot}{tableName}");
                return new CsvTable();
            }

            return CsvTable.Parse(asset.text);
        }

        static string Str(Dictionary<string, string> row, string key)
        {
            return row.TryGetValue(key, out var value) ? value : string.Empty;
        }

        static int Int(Dictionary<string, string> row, string key)
        {
            return int.TryParse(Str(row, key), out var value) ? value : 0;
        }

        static bool Bool(Dictionary<string, string> row, string key)
        {
            var value = Str(row, key).ToLowerInvariant();
            return value == "true" || value == "1" || value == "y";
        }

        static float Float(string value)
        {
            return float.TryParse(value, out var parsed) ? parsed : 0f;
        }
    }
}

