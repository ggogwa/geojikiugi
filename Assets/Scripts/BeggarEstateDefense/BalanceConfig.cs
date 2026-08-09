using System;
using UnityEngine;

namespace BeggarEstateDefense
{
    [Serializable]
    public sealed class BalanceConfig
    {
        public double startingMoney = 1000;
        public double startingTotalEarned;
        public string[] estateNames;
        public double[] estateCosts;
        public double[] estateIncome;
        public int[] estateStageRequirements;
        public float estateRepeatCostRate = 1f;
        public int fishPurchaseCost = 200;
        public double fishIncomeReference = 5;
        public int[] fishUpgradeCosts;
        public double[] fishSaleValues;
        public float[] fishFailureRates;
        public double[] lifestyleThresholds;
        public int begIncomePerLifestyleLevel = 15;
        public int heroCostPerStar = 500;
        public float tankHpPerStar = 520;
        public float otherHpPerStar = 230;
        public float dealerAttackPerStar = 72;
        public float tankAttackPerStar = 12;
        public float healerAttackPerStar = 9;
        public float healerHealPerStar = 28;
        public float heroStatGrowth = 1.6f;
        public float healerActionIntervalMultiplier = 1.8f;
        public int[] starChanceStageBreakpoints;
        public int[] oneStarChances;
        public int[] twoStarChances;
        public int[] threeStarChances;
        public int[] fourStarChances;
        public int[] fiveStarChances;
        public int threeStarChance = 12;
        public int twoStarCumulativeChance = 42;
        public int rerollStartCost = 100;
        public int rerollCostIncrease = 50;
        public float rerollCostGrowth = 1.6f;
        public float bossBaseHp = 1500;
        public float bossHpPerStage = 500;
        public float bossBaseDamage = 60;
        public float bossDamagePerStage = 9;
        public float bossHpGrowth = 1.30f;
        public float bossDamageGrowth = 1.14f;
        public float heroAttackInterval = .9f;
        public float bossAttackInterval = 1.25f;
        public float bossEnrageHpRatio = .45f;
        public float bossEnrageDamageMultiplier = 1.8f;
        public float bossEnrageAttackSpeedMultiplier = .65f;
        public float strongElementMultiplier = 1.5f;
        public float weakElementMultiplier = .5f;
        public float twoElementSynergy = 1.5f;
        public float threeElementSynergy = 2f;
        public int firstClearRewardBase = 3000;
        public int firstClearRewardPerStage = 2000;
        public int replayRewardBase = 1000;
        public int replayRewardPerStage = 200;
        public float replayRewardGrowth = 1.35f;

        public static BalanceConfig CreateFallback()
        {
            return new BalanceConfig
            {
                estateNames = new[] { "원룸", "투룸", "소형 상가", "오피스텔", "꼬마 빌딩", "랜드마크 타워" },
                estateCosts = new double[] { 500, 3500, 18000, 90000, 450000, 2400000 },
                estateIncome = new double[] { 2, 16, 90, 520, 3000, 18000 },
                estateStageRequirements = new[] { 1, 2, 3, 5, 7, 10 },
                fishUpgradeCosts = new[] { 0, 65, 105, 170, 275, 440, 715, 1170, 1885, 3055 },
                fishSaleValues = new double[] { 0, 250, 375, 563, 844, 1266, 1899, 2848, 4271, 6407, 9611 },
                fishFailureRates = new[] { 0f, .10f, .14f, .20f, .26f, .32f, .38f, .44f, .50f, .58f, 0f },
                lifestyleThresholds = new double[] { 0, 10000, 50000, 250000, 1250000 }
                ,starChanceStageBreakpoints = new[] { 1, 3, 5, 8, 10, 13, 16, 20 }
                ,oneStarChances = new[] { 65, 52, 38, 25, 15, 10, 5, 0 }
                ,twoStarChances = new[] { 30, 35, 37, 35, 30, 28, 23, 15 }
                ,threeStarChances = new[] { 5, 12, 20, 28, 32, 32, 32, 30 }
                ,fourStarChances = new[] { 0, 1, 5, 10, 18, 22, 28, 35 }
                ,fiveStarChances = new[] { 0, 0, 0, 2, 5, 8, 12, 20 }
            };
        }
    }

    public static class BalanceDatabase
    {
        static BalanceConfig current;
        public static BalanceConfig Current
        {
            get
            {
                if (current != null) return current;
                var asset = Resources.Load<TextAsset>("Config/game_balance");
                current = asset == null ? BalanceConfig.CreateFallback() : JsonUtility.FromJson<BalanceConfig>(asset.text);
                

                // Keep JSON-backed and fallback games on the same readable six-tier curve.
                current.estateCosts = new double[] { 500, 3500, 18000, 90000, 450000, 2400000 };
                current.estateIncome = new double[] { 2, 16, 90, 520, 3000, 18000 };
                current.estateRepeatCostRate = .65f;
if (current == null || current.estateCosts == null || current.estateCosts.Length != 6)
                    current = BalanceConfig.CreateFallback();
                return current;
            }
        }
    }
}
