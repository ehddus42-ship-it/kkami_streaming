using System.Collections.Generic;
using UnityEngine;

namespace GameKamiStreaming
{
    public sealed class KkamiTableDatabase
    {
        const string TableRoot = "GameKamiStreaming/DataTables/";

        public List<ResourceRow> Resources { get; } = new List<ResourceRow>();
        public List<PieceRow> Pieces { get; } = new List<PieceRow>();
        public List<StageRow> Stages { get; } = new List<StageRow>();
        public List<SkillTreeRow> SkillTree { get; } = new List<SkillTreeRow>();
        public List<EffectRow> Effects { get; } = new List<EffectRow>();
        public List<ChatRow> Chats { get; } = new List<ChatRow>();

        readonly Dictionary<int, ResourceRow> resourcesById = new Dictionary<int, ResourceRow>();
        readonly Dictionary<int, PieceRow> piecesById = new Dictionary<int, PieceRow>();
        readonly Dictionary<string, EffectRow> effectsById = new Dictionary<string, EffectRow>();
        readonly Dictionary<string, string> skillDescriptionsById = new Dictionary<string, string>();

        public static KkamiTableDatabase Load()
        {
            var database = new KkamiTableDatabase();
            database.LoadResources();
            database.LoadPieces();
            database.LoadStages();
            database.LoadSkillTree();
            database.LoadSkillDescriptions();
            database.LoadChats();
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

        public string GetSkillDescription(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            skillDescriptionsById.TryGetValue(key, out var description);
            return description ?? string.Empty;
        }

        void LoadResources()
        {
            foreach (var row in Read("currency").Rows)
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
                    soundId = Str(row, "sound_id"),
                    effectId = Str(row, "effect_id"),
                    deathEffectId = Str(row, "death_effect_id")
                };
                Pieces.Add(data);
                piecesById[data.pieceId] = data;
            }
        }

        void LoadStages()
        {
            foreach (var row in Read("stage").Rows)
            {
                var timeLimitSeconds = Int(row, "time_limit_sec");
                var data = new StageRow
                {
                    stageId = Int(row, "stage_id"),
                    bossId = Int(row, "boss_id"),
                    timeLimitSeconds = timeLimitSeconds > 0 ? timeLimitSeconds : 30,
                    imageId = Str(row, "stageimg_id"),
                    effectId = Str(row, "effect_id")
                };

                foreach (var pair in row)
                {
                    if (!pair.Key.StartsWith("piece_") || !pair.Key.EndsWith("_weight"))
                    {
                        continue;
                    }

                    var idText = pair.Key.Substring("piece_".Length, pair.Key.Length - "piece_".Length - "_weight".Length);
                    var weight = Float(pair.Value);
                    if (int.TryParse(idText, out var pieceId) && weight > 0f)
                    {
                        data.pieceWeights.Add(new StagePieceWeight { pieceId = pieceId, weight = weight });
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
                    skillStringKey = Str(row, "skill_stringkey"),
                    skillName = Str(row, "skill_name"),
                    reinforcedType = Int(row, "reinforced_int"),
                    upgradeRank = Int(row, "upgrade_rank"),
                    upAmount = Float(row, "up_int"),
                    useSubscription = Bool(row, "sub_use"),
                    followCost = Int(row, "follow_int"),
                    watcherCost = Int(row, "watcher_int"),
                    loveCost = Int(row, "love_int"),
                    donationCost = Int(row, "donation_int"),
                    redDonationCost = Int(row, "reddonation_int"),
                    subscriberCost = Int(row, "subscriber_int"),
                    imageId = Str(row, "image_id"),
                    soundId = Str(row, "sound_id"),
                    effectId = Str(row, "effect_id"),
                    unlockPieceId = Int(row, "unlock_piece_id"),
                    unlockResourceId = Int(row, "unlock_resource_id")
                });
            }
        }

        void LoadSkillDescriptions()
        {
            foreach (var row in Read("stringkey").Rows)
            {
                var key = Str(row, "string_id");
                if (!string.IsNullOrWhiteSpace(key))
                {
                    skillDescriptionsById[key] = Str(row, "skill_description");
                }
            }
        }

        void LoadEffects()
        {
            foreach (var row in Read("res_vfx").Rows)
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

        void LoadChats()
        {
            foreach (var row in Read("chat").Rows)
            {
                var data = new ChatRow
                {
                    chatId = Int(row, "chat_id"),
                    dialogue = Str(row, "chat_dialogue"),
                    spawnWeight = Float(row, "chat_spawn"),
                    viewerImageId = Str(row, "viewer_img_id"),
                    kkamiPortraitImageId = Str(row, "kkami_portrait_img_id")
                };

                if (data.chatId > 0)
                {
                    Chats.Add(data);
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

        static float Float(Dictionary<string, string> row, string key)
        {
            return Float(Str(row, key));
        }
    }
}

