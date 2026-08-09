using System;
using System.Globalization;
using UnityEngine;

namespace BeggarEstateDefense
{
    public sealed class GameProgress
    {
        public double money;
        public double totalEarned;
        public int highestStage;
        public int clearedStage;
        
        public int fishDeaths;
        public int fishLevel;
        public int fishDeathCollection;
        public int currentLocation;
        public int[] estates;
        public HeroData[] party;

    }

    public interface IGameSaveService
    {
        GameProgress Load(BalanceConfig balance, int estateCount);
        void Save(GameProgress progress);
        void Clear(int estateCount);
    }

    public sealed class PlayerPrefsGameSaveService : IGameSaveService
    {
        const string Prefix = "BED_";

public GameProgress Load(BalanceConfig balance, int estateCount)
        {
            var result = new GameProgress
            {
                money = ReadDouble(Prefix + "Money", balance.startingMoney),
                highestStage = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Stage", 1), 1, GameData.MaxBattleStage),
                fishDeaths = Mathf.Max(0, PlayerPrefs.GetInt(Prefix + "FishDeaths", 0)),
                fishLevel = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Fish", 0), 0, 10),
                fishDeathCollection = Mathf.Max(0, PlayerPrefs.GetInt(Prefix + "FishDeathCollection", 0)),
                currentLocation = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "CurrentLocation", 0), 0, 2),
                estates = new int[estateCount],
                party = new HeroData[3]
            };
            result.totalEarned = Math.Max(0, ReadDouble(Prefix + "TotalEarned", balance.startingTotalEarned));
            result.clearedStage = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "ClearedStage", result.highestStage - 1), 0, GameData.MaxBattleStage);
            for (int i = 0; i < estateCount; i++)
                result.estates[i] = Mathf.Max(0, PlayerPrefs.GetInt(Prefix + "Estate_" + i, 0));

            for (int i = 0; i < result.party.Length; i++)
            {
                string json = PlayerPrefs.GetString(Prefix + "Hero_" + i, "");
                if (string.IsNullOrEmpty(json)) continue;
                try
                {
                    HeroData hero = JsonUtility.FromJson<HeroData>(json);
                    if (hero != null && hero.maxHp > 0f) result.party[i] = hero;
                }
                catch (Exception)
                {
                    PlayerPrefs.DeleteKey(Prefix + "Hero_" + i);
                }
            }
            return result;
        }

public void Save(GameProgress progress)
        {
            PlayerPrefs.SetString(Prefix + "Money", progress.money.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.SetString(Prefix + "TotalEarned", progress.totalEarned.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.SetInt(Prefix + "Stage", progress.highestStage);
            PlayerPrefs.SetInt(Prefix + "ClearedStage", progress.clearedStage);
            PlayerPrefs.SetInt(Prefix + "FishDeaths", progress.fishDeaths);
            PlayerPrefs.SetInt(Prefix + "Fish", progress.fishLevel);
            PlayerPrefs.SetInt(Prefix + "FishDeathCollection", progress.fishDeathCollection);
            PlayerPrefs.SetInt(Prefix + "CurrentLocation", progress.currentLocation);
            for (int i = 0; i < progress.estates.Length; i++)
                PlayerPrefs.SetInt(Prefix + "Estate_" + i, progress.estates[i]);

            for (int i = 0; i < 3; i++)
            {
                string key = Prefix + "Hero_" + i;
                HeroData hero = progress.party != null && i < progress.party.Length ? progress.party[i] : null;
                if (hero == null) PlayerPrefs.DeleteKey(key);
                else PlayerPrefs.SetString(key, JsonUtility.ToJson(hero));
            }
            PlayerPrefs.Save();
        }

public void Clear(int estateCount)
        {
            PlayerPrefs.DeleteKey(Prefix + "Money");
            PlayerPrefs.DeleteKey(Prefix + "TotalEarned");
            PlayerPrefs.DeleteKey(Prefix + "Stage");
            PlayerPrefs.DeleteKey(Prefix + "ClearedStage");
            PlayerPrefs.DeleteKey(Prefix + "FishDeaths");
            PlayerPrefs.DeleteKey(Prefix + "Fish");
            PlayerPrefs.DeleteKey(Prefix + "FishDeathCollection");
            PlayerPrefs.DeleteKey(Prefix + "CurrentLocation");
            for (int i = 0; i < estateCount; i++) PlayerPrefs.DeleteKey(Prefix + "Estate_" + i);
            for (int i = 0; i < 3; i++) PlayerPrefs.DeleteKey(Prefix + "Hero_" + i);
        }

        static double ReadDouble(string key, double fallback)
        {
            double value;
            return double.TryParse(PlayerPrefs.GetString(key, fallback.ToString(CultureInfo.InvariantCulture)), NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value : fallback;
        }
    }
}
