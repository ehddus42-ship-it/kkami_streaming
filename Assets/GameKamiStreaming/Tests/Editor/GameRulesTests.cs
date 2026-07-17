using System.Collections.Generic;
using System.Reflection;
using TMPro;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace GameKamiStreaming.Tests
{
    public sealed class SkillTreeTooltipUiTests
    {
        [Test]
        public void EnsureTooltipText_ConvertsLegacyTextThroughChildTmp()
        {
            var gameObject = new GameObject(
                "Legacy Tooltip Text",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));

            try
            {
                var rect = gameObject.GetComponent<RectTransform>();
                var legacyText = gameObject.GetComponent<Text>();
                legacyText.text = "스킬 설명";

                var method = typeof(KkamiPrototypeGame).GetMethod(
                    "EnsureSkillTreeTooltipText",
                    BindingFlags.Static | BindingFlags.NonPublic);

                Assert.That(method, Is.Not.Null);
                var tmpText = method.Invoke(null, new object[]
                {
                    rect,
                    28f,
                    FontStyles.Normal,
                    TextAlignmentOptions.TopLeft
                }) as TextMeshProUGUI;

                Assert.That(tmpText, Is.Not.Null);
                Assert.That(tmpText.transform.parent, Is.EqualTo(rect));
                Assert.That(tmpText.text, Is.EqualTo("스킬 설명"));
                Assert.That(tmpText.font, Is.Not.Null);
                Assert.That(tmpText.font.name, Is.EqualTo("Katuri SDF"));
                Assert.That(legacyText.enabled, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }

    public sealed class CsvTableTests
    {
        [Test]
        public void Parse_HandlesBomQuotedCommaAndEscapedQuote()
        {
            var table = CsvTable.Parse("\ufeffid,text\r\n1,\"hello, \"\"kkami\"\"\"\r\n");

            Assert.That(table.Headers, Is.EqualTo(new[] { "id", "text" }));
            Assert.That(table.Rows, Has.Count.EqualTo(1));
            Assert.That(table.Rows[0]["text"], Is.EqualTo("hello, \"kkami\""));
        }
    }

    public sealed class ResourceManagerTests
    {
        [Test]
        public void TrySpend_UpdatesAmountAndRejectsInsufficientBalance()
        {
            var manager = new GameResourceManager();
            manager.Initialize(new[] { new ResourceRow { resourceId = 20001 } });
            var changedAmounts = new List<int>();
            manager.AmountChanged += (_, amount) => changedAmounts.Add(amount);

            manager.Add(20001, 10);
            var spent = manager.TrySpend(20001, 4);
            var rejected = manager.TrySpend(20001, 7);

            Assert.That(spent, Is.True);
            Assert.That(rejected, Is.False);
            Assert.That(manager.GetAmount(20001), Is.EqualTo(6));
            Assert.That(changedAmounts, Is.EqualTo(new[] { 10, 6 }));
        }
    }

    public sealed class StageProgressionTests
    {
        [Test]
        public void EvaluateTimeout_RequiresLivingBossToTriggerGameOver()
        {
            var normalStage = new StageRow { bossId = 0 };
            var bossStage = new StageRow { bossId = 30001 };

            Assert.That(StageProgressionRules.EvaluateTimeout(normalStage, false), Is.EqualTo(StageTimeoutOutcome.OpenSkillTree));
            Assert.That(StageProgressionRules.EvaluateTimeout(bossStage, true), Is.EqualTo(StageTimeoutOutcome.OpenSkillTree));
            Assert.That(StageProgressionRules.EvaluateTimeout(bossStage, false), Is.EqualTo(StageTimeoutOutcome.GameOver));
        }

        [Test]
        public void StageManager_SelectsByIdAndWrapsAfterLastStage()
        {
            var manager = new GameStageManager(new[]
            {
                new StageRow { stageId = 40001 },
                new StageRow { stageId = 40002 }
            });

            manager.SelectByStageId(40002, 0);
            Assert.That(manager.Current.stageId, Is.EqualTo(40002));

            manager.MoveNext();
            Assert.That(manager.Current.stageId, Is.EqualTo(40001));
        }
    }

    public sealed class SkillProgressionTests
    {
        [TestCase(10, 0, 10)]
        [TestCase(10, 1, 20)]
        [TestCase(10, 2, 40)]
        [TestCase(10, -1, 10)]
        [TestCase(0, 5, 0)]
        public void GetCurrentCost_DoublesPerPurchase(int baseCost, int count, int expected)
        {
            Assert.That(SkillProgressionRules.GetCurrentCost(baseCost, count), Is.EqualTo(expected));
        }

        [Test]
        public void GetCurrentCost_SaturatesInsteadOfOverflowing()
        {
            Assert.That(SkillProgressionRules.GetCurrentCost(int.MaxValue, 30), Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void HasPrerequisite_RequiresPreviousRankCompletion()
        {
            var firstRank = new SkillTreeRow
            {
                skillStringKey = "SD10101",
                reinforcedType = 1,
                upgradeRank = 1,
                upgradeCount = 3
            };
            var secondRank = new SkillTreeRow
            {
                skillStringKey = "SD10102",
                reinforcedType = 1,
                upgradeRank = 2,
                upgradeCount = 3
            };
            var skills = new[] { firstRank, secondRank };

            Assert.That(SkillProgressionRules.HasPrerequisite(secondRank, skills, _ => 2, true), Is.False);
            Assert.That(SkillProgressionRules.HasPrerequisite(secondRank, skills, _ => 3, true), Is.True);
        }

        [Test]
        public void HasPrerequisite_GatesManagerUpgradesBehindActivation()
        {
            var managerUpgrade = new SkillTreeRow
            {
                skillStringKey = "SD10117",
                reinforcedType = 7,
                upgradeRank = 1
            };

            Assert.That(SkillProgressionRules.HasPrerequisite(managerUpgrade, new[] { managerUpgrade }, _ => 0, false), Is.False);
            Assert.That(SkillProgressionRules.HasPrerequisite(managerUpgrade, new[] { managerUpgrade }, _ => 0, true), Is.True);
        }

        [TestCase(1, 1.1f)]
        [TestCase(2, 1.3f)]
        [TestCase(3, 1.5f)]
        public void GetRankBasedPercentMultiplier_UsesRankRule(int rank, float expected)
        {
            var row = new SkillTreeRow { upgradeRank = rank, upAmount = 99f };
            Assert.That(SkillProgressionRules.GetRankBasedPercentMultiplier(row), Is.EqualTo(expected).Within(0.0001f));
        }
    }
}
