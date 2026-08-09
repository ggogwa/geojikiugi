using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BeggarEstateDefense
{
    public enum CombatRole { Tank, Dealer, Healer }
    public enum CombatElement { Fire, Water, Grass, Neutral }

    [Serializable]
    public sealed class HeroData
    {
        public string name;
        public CombatRole role;
        public CombatElement element;
        public int stars;
        public int cost;
        public float maxHp;
        public float attack;
        public float heal;
        public float hp;
        public HeroData Clone() { return (HeroData)MemberwiseClone(); }
    }

    public static class GameData
    {
        public const int MaxBattleStage = 20;
        static readonly IGameSaveService SaveService = new PlayerPrefsGameSaveService();
        public static double Money = BalanceDatabase.Current.startingMoney;
        public static double TotalEarned = BalanceDatabase.Current.startingTotalEarned;
        public static int FishLevel;
        public static int FishDeaths;
        public static int FishDeathCollection;
        public static int CurrentLocation;
        public static readonly string[] LocationNames = { "주택가", "상업 지구", "역세권" };
        public static readonly float[] LocationIncomeMultipliers = { .9f, 1.15f, 1.35f };
        public static readonly string[] FishDeathCauses =
        {
            "찬물에 놀라서 심장마비", "비닐봉지를 해파리로 착각", "거울 속 자신과 눈싸움",
            "너무 밝은 햇빛에 기절", "강화 망치 소리에 쇼크", "주인의 기대가 너무 무거움"
        };

        public static readonly int[] Estates = new int[6];
        public static string[] EstateNames { get { return BalanceDatabase.Current.estateNames; } }
        public static double[] EstateCosts { get { return BalanceDatabase.Current.estateCosts; } }
        public static double[] EstateIncome { get { return BalanceDatabase.Current.estateIncome; } }
        public static int[] EstateStageRequirements { get { return BalanceDatabase.Current.estateStageRequirements; } }
        public static int[] FishUpgradeCosts { get { return BalanceDatabase.Current.fishUpgradeCosts; } }
        public static double[] FishSaleValues { get { return BalanceDatabase.Current.fishSaleValues; } }
        public static float[] FishFailureRates { get { return BalanceDatabase.Current.fishFailureRates; } }
        public static int HighestStage = 1;
        public static int ClearedStage;
        public static readonly HeroData[] Party = new HeroData[3];

        public static double PassiveIncome
        {
            get
            {
                double value = 0;
                for (int i = 0; i < Estates.Length; i++) value += Estates[i] * EstateIncome[i];
                return value * PassiveIncomeMultiplier * LocationIncomeMultipliers[Mathf.Clamp(CurrentLocation, 0, LocationIncomeMultipliers.Length - 1)];
            }
        }

        public static double PassiveIncomeMultiplier
        {
            get
            {
                double multiplier = 1d;
                if (ClearedStage >= 12) multiplier *= 1.15d;
                if (ClearedStage >= 15) multiplier *= 1.20d;
                if (ClearedStage >= 19) multiplier *= 1.25d;
                if (ClearedStage >= 20) multiplier *= 1.50d;
                return multiplier;
            }
        }

        public static float FishPurchaseDiscount
        {
            get { return ClearedStage >= 8 ? .5f : ClearedStage >= 4 ? .35f : ClearedStage >= 1 ? .2f : 0f; }
        }

        public static int FishStartLevel { get { return ClearedStage >= 16 ? 4 : ClearedStage >= 6 ? 3 : ClearedStage >= 2 ? 2 : 1; } }

        public static float FishSurvivalBonus
        {
            get
            {
                float earlyBonus = Mathf.Min(FishDeaths, 3) * .00001f;
                float milestoneBonus = Mathf.Floor(FishDeaths / 10f) * .01f;
                return Mathf.Min(.10f, earlyBonus + milestoneBonus);
            }
        }


public static float FishFailureRate(int level)
        {
            float stageReduction = ClearedStage >= 18 ? .10f : ClearedStage >= 11 ? .05f : 0f;
            return Mathf.Max(0f, FishFailureRates[level] - stageReduction - FishSurvivalBonus);
        }

        public static double FishEconomyScale
        {
            get { return Math.Max(1d, PassiveIncome / Math.Max(1d, BalanceDatabase.Current.fishIncomeReference)); }
        }

        public static double FishSaleValue(int level)
        {
            int startLevel = FishStartLevel;
            double value = FishPurchaseCost;
            float[] growth = { .10f, .20f, .20f, .30f, .30f, .40f, .40f, .50f, .50f };
            for (int nextLevel = startLevel + 1; nextLevel <= level; nextLevel++)
                value *= 1d + growth[Mathf.Clamp(nextLevel - 2, 0, growth.Length - 1)];
            return Math.Round(value);
        }

        public static int FishUpgradeCost(int level)
        {
            int configuredCost = Mathf.Max(1, (int)Math.Round(FishUpgradeCosts[level] * FishEconomyScale));
            int valueGain = Mathf.Max(1, (int)(FishSaleValue(level + 1) - FishSaleValue(level)));
            return Mathf.Min(configuredCost, valueGain);
        }

        public static double FishPurchaseCost
        {
            get { return Math.Round(BalanceDatabase.Current.fishPurchaseCost * FishEconomyScale * (1d - FishPurchaseDiscount)); }
        }

        public static string RegisterFishDeathCause()
        {
            int index = UnityEngine.Random.Range(0, FishDeathCauses.Length);
            FishDeathCollection |= 1 << index;
            return FishDeathCauses[index];
        }

        public static int EstatePurchaseLimit(int index)
        {
            int limit = 1;
            if (index == 0 && ClearedStage >= 3) limit++;
            if (index == 1 && ClearedStage >= 5) limit++;
            if (index == 2 && ClearedStage >= 7) limit++;
            if (index == 3 && ClearedStage >= 9) limit++;
            if (ClearedStage >= 10) limit++;
            if (ClearedStage >= 14) limit++;
            if (ClearedStage >= 17) limit++;
            return limit;
        }

public static int EstateNextLimitUnlockStage(int index)
        {
            if (index == 0 && ClearedStage < 3) return 3;
            if (index == 1 && ClearedStage < 5) return 5;
            if (index == 2 && ClearedStage < 7) return 7;
            if (index == 3 && ClearedStage < 9) return 9;
            if (ClearedStage < 10) return 10;
            if (ClearedStage < 14) return 14;
            if (ClearedStage < 17) return 17;
            return 0;
        }


        public static double EstatePurchaseCost(int index)
        {
            return EstateCosts[index] * Math.Pow(1d + BalanceDatabase.Current.estateRepeatCostRate, Estates[index]);
        }

public static void Save()
        {
            var savedParty = new HeroData[Party.Length];
            for (int i = 0; i < Party.Length; i++)
                savedParty[i] = Party[i] == null ? null : Party[i].Clone();

            SaveService.Save(new GameProgress
            {
                money = Money,
                totalEarned = TotalEarned,
                highestStage = HighestStage,
                clearedStage = ClearedStage,
                fishLevel = FishLevel,
                fishDeaths = FishDeaths,
                fishDeathCollection = FishDeathCollection,
                currentLocation = CurrentLocation,
                estates = (int[])Estates.Clone(),
                party = savedParty
            });
        }

public static void Load()
        {
            var progress = SaveService.Load(BalanceDatabase.Current, Estates.Length);
            Money = progress.money;
            TotalEarned = progress.totalEarned;
            HighestStage = Mathf.Clamp(progress.highestStage, 1, MaxBattleStage);
            ClearedStage = Mathf.Clamp(progress.clearedStage, 0, MaxBattleStage);
            FishLevel = progress.fishLevel;
            FishDeaths = Mathf.Max(0, progress.fishDeaths);
            FishDeathCollection = Mathf.Max(0, progress.fishDeathCollection);
            CurrentLocation = Mathf.Clamp(progress.currentLocation, 0, LocationNames.Length - 1);
            Array.Copy(progress.estates, Estates, Estates.Length);

            Array.Clear(Party, 0, Party.Length);
            if (progress.party != null)
            {
                int count = Mathf.Min(progress.party.Length, Party.Length);
                for (int i = 0; i < count; i++)
                {
                    if (progress.party[i] == null) continue;
                    Party[i] = progress.party[i].Clone();
                    Party[i].hp = Party[i].maxHp;
                }
            }
        }

public static void ResetData()
        {
            SaveService.Clear(Estates.Length);
            Money = BalanceDatabase.Current.startingMoney;
            TotalEarned = BalanceDatabase.Current.startingTotalEarned;
            FishLevel = 0;
            FishDeaths = 0;
            FishDeathCollection = 0;
            CurrentLocation = 0;
            HighestStage = 1;
            ClearedStage = 0;
            Array.Clear(Estates, 0, Estates.Length);
            Array.Clear(Party, 0, Party.Length);
            BattleController.ClearSessionState();
            Save();
        }
    }

    public sealed class BeggarEstatePrototype : MonoBehaviour
    {
        static Font font;
                // Set to false to restore the former square UI without reverting the implementation.
        public const bool RoundedUiEnabled = true;
        static Sprite roundedRectSprite;
static Sprite circleSprite;
        static readonly Color32 Navy = new Color32(27, 37, 59, 255);
        static readonly Color32 Panel = new Color32(42, 55, 82, 255);
        static readonly Color32 Cream = new Color32(51, 44, 36, 255);
        static readonly Color32 Gold = new Color32(255, 193, 71, 255);
        static readonly Color32 Mint = new Color32(83, 205, 166, 255);
        static readonly Color32 Coral = new Color32(241, 101, 94, 255);
        static readonly Color32 Blue = new Color32(75, 139, 221, 255);
        Text moneyText, incomeText, clickText, fishText, fishRiskText, fishActionText, estateSummary, estateDetailText, noticeText;
        Text estateHudText, fishHudText, stageHudText;
        GameObject streetPanel, streetZoneBadge, streetClickHint, fishHomePanel, estateHomePanel, fishRiskBar;
        readonly Text[] estateTexts = new Text[6];
        readonly Text[] estateBuyTexts = new Text[6];
        readonly Button[] estateButtons = new Button[6];
        readonly CanvasGroup[] estateCardGroups = new CanvasGroup[6];
        Button fishActionButton, fishSellButton, estateDetailButton, homeNavButton, fishNavButton, estateNavButton;
        Image fishImage, streetFishImage, estateMapImage, beggarImage, homeBackgroundImage, outfitIconImage, outfitProgressFill, fishRiskFill;
        Text outfitProgressText;
        float resetConfirmUntil;
        float uiRefreshTimer;
        float reactionUntil;
        int reactionIndex = -1;
        int loadedReactionStage;
        bool fishScreen, homeSummary;
        Sprite[] reactionSprites;
        readonly CoinPopEffect[] coinEffects = new CoinPopEffect[8];
        int nextCoinEffect;
        float nextExtortionAt;
        bool extortionPlaying;
        int selectedEstate;

        static readonly string[] BegLines = { "감사합니다! 오늘도 버텨볼게요!", "우와, 진짜 동전이다!", "한 푼이 건물 한 채가 되는 날까지!", "이 은혜 잊지 않을게요!", "좋았어, 부자까지 한 걸음!" };
        static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();
        static readonly string[] EstateArt = { "Estate_01_Studio", "Estate_02_House", "Estate_03_Shop", "Estate_04_Officetel", "Estate_05_Building", "Estate_06_Tower" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            Screen.SetResolution(390, 844, FullScreenMode.Windowed);
#endif
            if (FindAnyObjectByType<BeggarEstatePrototype>() == null)
                new GameObject("Game Bootstrap").AddComponent<BeggarEstatePrototype>();
        }

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneLoaded;
            GameData.Load();
            font = Resources.Load<Font>("Fonts/Jua-Regular");
            if (font == null) font = Font.CreateDynamicFontFromOSFont(new[] { "Jua", "Malgun Gothic", "Arial" }, 28);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureRenderingCamera();
            EnsureEventSystem();
            GameAudio.Ensure();
            if (SceneManager.GetActiveScene().name == "BattleScene") BuildBattleEntry();
            else BuildHomeV2();
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            EnsureRenderingCamera();
            EnsureEventSystem();
            if (scene.name == "BattleScene") BuildBattleEntry();
            else BuildHomeV2();
        }

        void Update()
        {
            if (SceneManager.GetActiveScene().name == "BattleScene") return;
            double passiveTick = GameData.PassiveIncome * Time.unscaledDeltaTime;
            GameData.Money += passiveTick;
            GameData.TotalEarned += passiveTick;
            uiRefreshTimer -= Time.unscaledDeltaTime;
            if (moneyText != null && uiRefreshTimer <= 0)
            {
                uiRefreshTimer = .12f;
                RefreshMoneyOnly();
            }
            if (reactionUntil > 0 && Time.unscaledTime >= reactionUntil) { reactionUntil = 0; RefreshBeggarSprite(); }
            if (!extortionPlaying && Time.unscaledTime >= nextExtortionAt && GameData.Money >= 100)
                StartCoroutine(PlayExtortion());
        }

        void OnApplicationPause(bool pause) { if (pause) GameData.Save(); }
        void OnApplicationQuit() { GameData.Save(); }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        }

        static void EnsureRenderingCamera()
        {
            Camera camera = FindAnyObjectByType<Camera>();
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camera = cameraObject.GetComponent<Camera>();
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0, 0, -10);
                camera.orthographic = true;
                camera.orthographicSize = 5;
            }
            camera.enabled = true;
            camera.targetDisplay = 0;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Navy;
            if (!camera.CompareTag("MainCamera")) camera.tag = "MainCamera";
        }

        Canvas MakeCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(540, 1170);
            scaler.matchWidthOrHeight = .5f;
            return canvas;
        }

        void BuildHome()
        {
            var canvas = MakeCanvas("거지 부동산 본부");
            Box(canvas.transform, "배경", new Color32(255, 248, 231, 255), Vector2.zero, Vector2.one);
            SpriteImage(canvas.transform, "Alley Background", "Backgrounds/Background_Alley", Vector2.zero, Vector2.one, new Color(1, 1, 1, .28f), false);
            Transform content = CreateSafeArea(canvas.transform);
            var header = Box(content, "상단", new Color32(35, 47, 72, 255), new Vector2(0, .90f), Vector2.one);
            Label(header.transform, "거지에서 건물주까지", 38, Cream, TextAnchor.MiddleLeft, new Vector2(.04f, .48f), new Vector2(.62f, .96f), FontStyle.Bold);
            moneyText = Label(header.transform, "", 30, Gold, TextAnchor.MiddleRight, new Vector2(.62f, .48f), new Vector2(.96f, .96f), FontStyle.Bold);
            incomeText = Label(header.transform, "", 20, Mint, TextAnchor.MiddleLeft, new Vector2(.04f, .04f), new Vector2(.35f, .48f), FontStyle.Bold);
            Button(header.transform, "전투 준비소", new Vector2(.62f, .07f), new Vector2(.96f, .44f), Coral, Cream, LoadBattleScene);

            var clicker = Box(content, "구걸", Panel, new Vector2(.035f, .625f), new Vector2(.965f, .88f));
            Label(clicker.transform, "거리 구걸", 30, Cream, TextAnchor.UpperCenter, new Vector2(.05f, .86f), new Vector2(.95f, .97f), FontStyle.Bold);
            beggarImage = SpriteImage(clicker.transform, "Beggar Growth", "Beggar/Beggar_Stage_01", new Vector2(.06f, .15f), new Vector2(.38f, .84f));
            clickText = Label(clicker.transform, "", 21, Cream, TextAnchor.MiddleCenter, new Vector2(.39f, .46f), new Vector2(.94f, .68f));
            Button(clicker.transform, "구걸하기  +10원", new Vector2(.40f, .16f), new Vector2(.92f, .42f), Gold, Navy, Beg);
            Label(clicker.transform, "즉시 돈을 벌지만 효율은 가장 낮습니다", 16, new Color32(160, 174, 198, 255), TextAnchor.MiddleCenter, new Vector2(.05f, .02f), new Vector2(.95f, .13f));

            var fish = Box(content, "개복치 도박", Panel, new Vector2(.035f, .345f), new Vector2(.965f, .605f));
            Label(fish.transform, "개복치 강화 도박", 30, Cream, TextAnchor.UpperCenter, new Vector2(.05f, .86f), new Vector2(.95f, .97f), FontStyle.Bold);
            SpriteImage(fish.transform, "Aquarium Background", "Backgrounds/Background_Aquarium", Vector2.zero, Vector2.one, new Color(1, 1, 1, .20f), false);
            fishImage = MakeFishSprite(fish.transform, new Vector2(.24f, .56f), new Vector2(.34f, .42f));
            fishText = Label(fish.transform, "", 22, Cream, TextAnchor.MiddleCenter, new Vector2(.39f, .57f), new Vector2(.95f, .72f), FontStyle.Bold);
            fishRiskText = Label(fish.transform, "", 17, new Color32(185, 196, 214, 255), TextAnchor.MiddleCenter, new Vector2(.39f, .39f), new Vector2(.95f, .58f));
            fishActionButton = Button(fish.transform, "", new Vector2(.07f, .13f), new Vector2(.62f, .31f), Coral, Cream, UpgradeFish);
            fishActionText = fishActionButton.GetComponentInChildren<Text>();
            fishSellButton = Button(fish.transform, "매각", new Vector2(.65f, .13f), new Vector2(.93f, .31f), Gold, Navy, SellFish);
            Label(fish.transform, "강화 실패 시 개복치와 투자금이 모두 사라집니다", 15, new Color32(255, 155, 149, 255), TextAnchor.MiddleCenter, new Vector2(.04f, .03f), new Vector2(.96f, .10f));

            var estate = Box(content, "부동산", Panel, new Vector2(.035f, .045f), new Vector2(.965f, .325f));
            Label(estate.transform, "안정적인 부동산", 30, Cream, TextAnchor.UpperCenter, new Vector2(.05f, .88f), new Vector2(.95f, .98f), FontStyle.Bold);
            estateSummary = Label(estate.transform, "", 18, Mint, TextAnchor.MiddleCenter, new Vector2(.05f, .81f), new Vector2(.95f, .88f), FontStyle.Bold);
            for (int i = 0; i < 6; i++)
            {
                int index = i;
                float top = .79f - i * .112f;
                var button = Button(estate.transform, "", new Vector2(.06f, top - .085f), new Vector2(.94f, top), i < 2 ? Mint : new Color32(69, 83, 111, 255), i < 2 ? Navy : Cream, delegate { BuyEstate(index); });
                estateTexts[i] = button.GetComponentInChildren<Text>();
                estateTexts[i].rectTransform.anchorMin = new Vector2(.22f, 0);
                estateTexts[i].alignment = TextAnchor.MiddleLeft;
                SpriteImage(button.transform, "Estate Art", "Estate/" + EstateArt[i], new Vector2(.015f, .05f), new Vector2(.21f, .95f));
            }

            noticeText = Label(content, "", 17, new Color32(177, 189, 209, 255), TextAnchor.MiddleCenter, new Vector2(.05f, .005f), new Vector2(.95f, .04f), FontStyle.Bold);
            RefreshEconomyText();
        }

        void BuildHomeV2()
        {
            nextExtortionAt = Time.unscaledTime + 25f;
            var canvas = MakeCanvas("거지 키우기 모바일 UI");
            Box(canvas.transform, "배경", new Color32(207, 196, 171, 255), Vector2.zero, Vector2.one);
            SpriteImage(canvas.transform, "전체 도시 배경", "Backgrounds/Background_Home_Cartoon", Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, .30f), false);
            Transform content = CreateSafeArea(canvas.transform);
            var title = Label(content, "♛  거지 키우기", 34, new Color32(94, 58, 31, 255), TextAnchor.MiddleCenter, new Vector2(.12f, .925f), new Vector2(.78f, .995f), FontStyle.Normal);
            var titleOutline = title.gameObject.GetComponent<Outline>() ?? title.gameObject.AddComponent<Outline>();
            titleOutline.effectColor = new Color32(255, 239, 190, 166); titleOutline.effectDistance = new Vector2(1f, -1f);
            var resetButton = Button(content, "기록 초기화", new Vector2(.79f, .945f), new Vector2(.965f, .987f), new Color32(220, 156, 142, 255), new Color32(51, 44, 36, 255), RequestDataReset);
            var resetLabel = resetButton.GetComponentInChildren<Text>();
            resetLabel.fontStyle = FontStyle.Normal;
            resetLabel.GetComponent<ResponsiveTypography>().Configure(12, TypographyRole.Caption);


            // Top HUD: compact, readable and always visible like the reviewed prototype.
            var header = CreamPanelBox(content, "상단 HUD", new Vector2(.055f, .835f), new Vector2(.945f, .925f));
            moneyText = Label(header.transform, "", 18, new Color32(51, 44, 36, 255), TextAnchor.MiddleCenter, new Vector2(.02f, .30f), new Vector2(.49f, .92f), FontStyle.Normal);
            incomeText = Label(header.transform, "", 18, new Color32(51, 44, 36, 255), TextAnchor.MiddleCenter, new Vector2(.51f, .30f), new Vector2(.98f, .92f), FontStyle.Normal);
            Box(header.transform, "HUD 구분선", new Color32(205, 194, 166, 255), new Vector2(.498f, .15f), new Vector2(.502f, .85f));
            estateHudText = Label(header.transform, "", 17, Cream, TextAnchor.MiddleLeft, new Vector2(.18f, .41f), new Vector2(.36f, .66f), FontStyle.Bold);
            fishHudText = Label(header.transform, "", 17, Cream, TextAnchor.MiddleLeft, new Vector2(.50f, .41f), new Vector2(.67f, .66f), FontStyle.Bold);
            stageHudText = Label(header.transform, "", 17, Cream, TextAnchor.MiddleRight, new Vector2(.87f, .41f), new Vector2(.96f, .66f), FontStyle.Bold);
            estateHudText.gameObject.SetActive(false); fishHudText.gameObject.SetActive(false); stageHudText.gameObject.SetActive(false);
            outfitIconImage = SpriteImage(header.transform, "옷차림 아이콘", "Beggar/Beggar_Stage_01", new Vector2(.03f, .015f), new Vector2(.18f, .40f));
            outfitIconImage.gameObject.SetActive(false);
            outfitProgressText = Label(header.transform, "", 14, Cream, TextAnchor.MiddleLeft, new Vector2(.18f, .06f), new Vector2(.57f, .36f), FontStyle.Bold);
            outfitProgressText.gameObject.SetActive(false);
            var outfitProgressBg = RoundedBox(header.transform, "옷차림 달성도 배경", new Color32(9, 15, 28, 255), new Vector2(.58f, .12f), new Vector2(.96f, .30f), true);
            outfitProgressFill = Box(outfitProgressBg.transform, "옷차림 달성도", Gold, Vector2.zero, Vector2.one).GetComponent<Image>();
            outfitProgressBg.SetActive(false);

            // Main street view keeps the original clicker direction while giving the character room.
            var street = CreamPanelBox(content, "메인 거리", new Vector2(.055f, .47f), new Vector2(.945f, .825f));
            streetPanel = street;
            street.AddComponent<BegClickSurface>().Configure(street.GetComponent<RectTransform>(), BegAt);
            homeBackgroundImage = SpriteImage(street.transform, "성장 배경", "Backgrounds/Background_Home_Cartoon", new Vector2(.012f, .025f), new Vector2(.988f, .975f), Color.white, false);
            var zoneBadge = SoftPanelBox(street.transform, "지역 배지", new Vector2(.035f, .84f), new Vector2(.30f, .965f));
            streetZoneBadge = zoneBadge;
            var zoneText = Label(zoneBadge.transform, "거리 구걸", 19, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, FontStyle.Bold);
            var zoneOutline = zoneText.gameObject.AddComponent<Outline>(); zoneOutline.effectColor = new Color32(35, 24, 18, 255); zoneOutline.effectDistance = new Vector2(1.4f, -1.4f);
            clickText = Label(street.transform, "", 17, Color.white, TextAnchor.UpperRight, new Vector2(.54f, .74f), new Vector2(.95f, .95f), FontStyle.Normal);
            var clickOutline = clickText.gameObject.AddComponent<Outline>(); clickOutline.effectColor = new Color32(35, 24, 18, 255); clickOutline.effectDistance = new Vector2(1.5f, -1.5f);
            beggarImage = SpriteImage(street.transform, "거지 성장", "Beggar/Beggar_Stage_01_Cartoon", new Vector2(.20f, .12f), new Vector2(.80f, .88f));
            beggarImage.gameObject.AddComponent<UIFloatMotion>().Configure(0, 9, 2.2f, .018f, 0f);
            beggarImage.raycastTarget = true;
            streetFishImage = MakeFishSprite(street.transform, new Vector2(.30f, .48f), new Vector2(.25f, .32f));
            streetFishImage.gameObject.AddComponent<UIFloatMotion>().Configure(8, 5, 1.6f, .02f, .4f);
            streetFishImage.gameObject.SetActive(false);
            EnsureReactionSprites(BeggarLifestyleLevel());
            for (int i = 0; i < coinEffects.Length; i++)
            {
                var coin = SpriteImage(street.transform, "Coin FX " + i, "Icons/Icon_Coin", new Vector2(.45f, .20f), new Vector2(.55f, .30f));
                coinEffects[i] = coin.gameObject.AddComponent<CoinPopEffect>(); coin.gameObject.SetActive(false);
            }
            var hintText = Label(street.transform, "거지를 눌러 구걸", 16, Color.white, TextAnchor.MiddleCenter, new Vector2(.27f, .025f), new Vector2(.73f, .12f), FontStyle.Bold);
            var hintOutline = hintText.gameObject.AddComponent<Outline>(); hintOutline.effectColor = new Color32(35, 24, 18, 255); hintOutline.effectDistance = new Vector2(1.5f, -1.5f);
            streetClickHint = hintText.gameObject;

            // Bottom-sheet content area: risky fish and stable estate remain intact as separate tabs.
            fishHomePanel = CreamPanelBox(content, "개복치 탭", new Vector2(.055f, .115f), new Vector2(.945f, .455f));
            Label(fishHomePanel.transform, "개복치 강화", 23, Cream, TextAnchor.MiddleLeft, new Vector2(.05f, .84f), new Vector2(.62f, .97f), FontStyle.Bold);
            Button(fishHomePanel.transform, "전투 보상 i", new Vector2(.64f, .85f), new Vector2(.94f, .96f), Gold, Navy, ShowNextBattleReward);
            Button(fishHomePanel.transform, "도감", new Vector2(.05f, .68f), new Vector2(.22f, .82f), Mint, Navy, ShowFishCollection);
            fishImage = MakeFishSprite(fishHomePanel.transform, new Vector2(.235f, .56f), new Vector2(.42f, .58f));
            fishImage.gameObject.AddComponent<UIFloatMotion>().Configure(10, 7, 1.8f, .025f, 1.1f);
            fishText = Label(fishHomePanel.transform, "", 18, Cream, TextAnchor.MiddleLeft, new Vector2(.46f, .61f), new Vector2(.94f, .81f), FontStyle.Bold);
            fishRiskText = Label(fishHomePanel.transform, "", 16, Cream, TextAnchor.MiddleLeft, new Vector2(.46f, .43f), new Vector2(.94f, .62f), FontStyle.Normal);
            fishRiskBar = RoundedBox(fishHomePanel.transform, "강화 위험도 배경", new Color32(11, 20, 34, 230), new Vector2(.47f, .39f), new Vector2(.93f, .425f), true);
            fishRiskFill = Box(fishRiskBar.transform, "강화 위험도", Coral, Vector2.zero, Vector2.one).GetComponent<Image>();
            fishActionButton = Button(fishHomePanel.transform, "", new Vector2(.18f, .13f), new Vector2(.82f, .31f), new Color32(233, 201, 154, 255), new Color32(51, 44, 36, 255), UpgradeFish);
            fishActionText = fishActionButton.GetComponentInChildren<Text>();
            fishSellButton = Button(fishHomePanel.transform, "매각", new Vector2(.66f, .12f), new Vector2(.94f, .31f), Gold, Navy, SellFish);
            Label(fishHomePanel.transform, "실패 시 개복치와 투자금이 사라집니다", 14, new Color32(255, 161, 154, 255), TextAnchor.MiddleCenter, new Vector2(.05f, .015f), new Vector2(.95f, .10f));

            estateHomePanel = CreamPanelBox(content, "부동산 탭", new Vector2(.055f, .115f), new Vector2(.945f, .825f));
            Label(estateHomePanel.transform, "부동산 지도", 20, new Color32(51, 44, 36, 255), TextAnchor.MiddleLeft, new Vector2(.04f, .91f), new Vector2(.34f, .985f), FontStyle.Bold);
            estateSummary = Label(estateHomePanel.transform, "", 14, new Color32(0, 92, 67, 255), TextAnchor.MiddleRight, new Vector2(.34f, .91f), new Vector2(.96f, .985f), FontStyle.Normal);
            estateMapImage = SpriteImage(estateHomePanel.transform, "3x2 도시 블록", "EstateMap/EstateMap_Base_3x2", new Vector2(.025f, .19f), new Vector2(.975f, .81f), Color.white, true);
            estateMapImage.transform.SetAsFirstSibling();
            for (int i = 0; i < GameData.LocationNames.Length; i++)
            {
                int location = i;
                float left = .04f + i * .32f;
                var locationButton = Button(estateHomePanel.transform, GameData.LocationNames[i] + " ×" + GameData.LocationIncomeMultipliers[i].ToString("0.00"), new Vector2(left, .82f), new Vector2(left + .28f, .89f), Mint, Navy, delegate { MoveLocation(location); });
                locationButton.GetComponentInChildren<ResponsiveTypography>().Configure(12, TypographyRole.Caption);
            }
            for (int i = 0; i < 6; i++)
            {
                int index = i;
                int row = i / 3;
                int column = i % 3;
                float left = .045f + column * .315f;
                float top = .83f - row * .37f;
                var card = RoundedBox(estateHomePanel.transform, "부동산 타일 " + i, new Color32(255, 248, 231, 255), new Vector2(left, top - .31f), new Vector2(left + .28f, top), true);
                var cardOutline = card.AddComponent<Outline>(); cardOutline.effectColor = new Color32(51, 44, 36, 255); cardOutline.effectDistance = new Vector2(1.2f, -1.2f);
                estateCardGroups[i] = card.AddComponent<CanvasGroup>();
                SpriteImage(card.transform, "건물 아이콘", "Estate/" + EstateArt[i], new Vector2(.08f, .08f), new Vector2(.92f, .88f));
                estateTexts[i] = Label(card.transform, "", 15, Cream, TextAnchor.MiddleCenter, new Vector2(.56f, .68f), new Vector2(.98f, .98f), FontStyle.Bold);
                estateBuyTexts[i] = Label(card.transform, "", 12, Cream, TextAnchor.MiddleCenter, new Vector2(.03f, .00f), new Vector2(.97f, .22f), FontStyle.Normal);
                estateButtons[i] = card.AddComponent<Button>();
                estateButtons[i].targetGraphic = card.GetComponent<Image>();
                estateButtons[i].transition = Selectable.Transition.None;
                estateButtons[i].onClick.AddListener(delegate { SelectEstate(index); });
            }
            var detail = RoundedBox(estateHomePanel.transform, "선택 부동산 상세", new Color(1f, .97f, .88f, .98f), new Vector2(.025f, .02f), new Vector2(.975f, .175f), true);
            estateDetailText = Label(detail.transform, "", 14, Cream, TextAnchor.MiddleLeft, new Vector2(.04f, .08f), new Vector2(.72f, .92f), FontStyle.Normal);
            estateDetailButton = Button(detail.transform, "구매", new Vector2(.75f, .18f), new Vector2(.97f, .82f), Gold, Navy, delegate { BuyEstate(selectedEstate); });

            var nav = CreamPanelBox(content, "하단 메뉴", new Vector2(0, 0), new Vector2(1, .105f));
            fishNavButton = IconNavButton(nav.transform, "Icons/Icon_Sunfish", "개복치", new Vector2(.01f, .04f), new Vector2(.325f, .96f), new Color32(255, 190, 24, 255), delegate { ShowHomePanel(true); });
            estateNavButton = IconNavButton(nav.transform, "Icons/Icon_Estate", "부동산", new Vector2(.342f, .04f), new Vector2(.658f, .96f), new Color32(244, 232, 203, 255), delegate { ShowHomePanel(false); });
            IconNavButton(nav.transform, "Icons/Icon_Dealer", "전투", new Vector2(.675f, .04f), new Vector2(.99f, .96f), new Color32(244, 232, 203, 255), LoadBattleScene);
            noticeText = Label(content, "", 15, new Color32(51, 44, 36, 255), TextAnchor.MiddleCenter, new Vector2(.04f, .455f), new Vector2(.96f, .475f), FontStyle.Normal);

            ShowHomePanel(true);
            RefreshEconomyText();
        }

        void ShowHomePanel(bool showFish)
        {
            fishScreen = false;
            homeSummary = !showFish;
            if (streetPanel != null) streetPanel.SetActive(true);
            if (fishHomePanel != null) fishHomePanel.SetActive(showFish);
            if (estateHomePanel != null) estateHomePanel.SetActive(!showFish);
            if (!showFish && estateHomePanel != null)
            {
                var rect = estateHomePanel.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(.055f, .115f); rect.anchorMax = new Vector2(.945f, .825f);
            }
            if (streetPanel != null) streetPanel.SetActive(showFish);
            if (estateMapImage != null) estateMapImage.gameObject.SetActive(!showFish);
            LayoutEstateCards(showFish);
            SetHomeNav(showFish ? fishNavButton : estateNavButton);
            if (streetFishImage != null) streetFishImage.gameObject.SetActive(false);
            if (streetZoneBadge != null) streetZoneBadge.SetActive(true);
            if (streetClickHint != null) streetClickHint.SetActive(true);
            if (clickText != null) clickText.gameObject.SetActive(true);
            if (beggarImage != null) { beggarImage.rectTransform.anchorMin = new Vector2(.20f, .12f); beggarImage.rectTransform.anchorMax = new Vector2(.80f, .88f); }
            RefreshHomeBackground();
            RefreshEconomyText();
        }

        void ShowHomeScreen(bool showFishScreen)
        {
            fishScreen = showFishScreen;
            homeSummary = !showFishScreen;
            if (streetPanel != null) streetPanel.SetActive(true);
            if (fishHomePanel != null) fishHomePanel.SetActive(showFishScreen);
            if (estateHomePanel != null) estateHomePanel.SetActive(!showFishScreen);
            if (!showFishScreen && estateHomePanel != null)
            {
                var rect = estateHomePanel.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(.055f, .115f); rect.anchorMax = new Vector2(.945f, .455f);
            }
            if (estateMapImage != null) estateMapImage.gameObject.SetActive(false);
            LayoutEstateCards(!showFishScreen);
            SetHomeNav(showFishScreen ? fishNavButton : homeNavButton);
            if (streetFishImage != null) streetFishImage.gameObject.SetActive(showFishScreen);
            if (streetZoneBadge != null) streetZoneBadge.SetActive(!showFishScreen);
            if (streetClickHint != null) streetClickHint.SetActive(!showFishScreen);
            if (clickText != null) clickText.gameObject.SetActive(!showFishScreen);
            if (streetFishImage != null && showFishScreen)
            {
                streetFishImage.sprite = Resources.Load<Sprite>("SunfishGrowth/Sunfish_Stage_01");
                streetFishImage.color = Color.white;
                streetFishImage.rectTransform.anchorMin = new Vector2(.14f, .30f);
                streetFishImage.rectTransform.anchorMax = new Vector2(.43f, .68f);
                streetFishImage.transform.SetAsLastSibling();
            }
            if (beggarImage != null)
            {
                beggarImage.rectTransform.anchorMin = showFishScreen ? new Vector2(.50f, .14f) : new Vector2(.20f, .12f);
                beggarImage.rectTransform.anchorMax = showFishScreen ? new Vector2(.84f, .84f) : new Vector2(.80f, .88f);
            }
            RefreshHomeBackground();
            RefreshEconomyText();
        }

        void SetHomeNav(Button selected)
        {
            var normal = new Color32(244, 232, 203, 255); var active = new Color32(255, 190, 24, 255);
            foreach (var button in new[] { fishNavButton, estateNavButton })
                if (button != null && button.targetGraphic != null) button.targetGraphic.color = button == selected ? active : normal;
        }

        void LayoutEstateCards(bool compactHome)
        {
            for (int i = 0; i < estateCardGroups.Length; i++)
            {
                if (estateCardGroups[i] == null) continue;
                int columns = compactHome ? 2 : 3;
                int row = i / columns;
                int column = i % columns;
                float left = compactHome ? .055f + column * .46f : .075f + column * .30f;
                float top = compactHome ? .80f - row * .26f : .75f - row * .28f;
                float width = compactHome ? .42f : .22f;
                float height = compactHome ? .22f : .22f;
                var rect = estateCardGroups[i].GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(left, top - height); rect.anchorMax = new Vector2(left + width, top);
            }
        }

        void SelectEstate(int index)
        {
            selectedEstate = Mathf.Clamp(index, 0, GameData.Estates.Length - 1);
            RefreshEconomyText();
        }

        void MoveLocation(int location)
        {
            GameData.CurrentLocation = Mathf.Clamp(location, 0, GameData.LocationNames.Length - 1);
            GameData.Save();
            noticeText.text = GameData.LocationNames[GameData.CurrentLocation] + "로 이동! 부동산 수익 ×" + GameData.LocationIncomeMultipliers[GameData.CurrentLocation].ToString("0.00");
            RefreshEconomyText();
        }

        IEnumerator PlayExtortion()
        {
            extortionPlaying = true;
            var overlay = new GameObject("동네 양아치 이벤트", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup), typeof(GraphicRaycaster));
            overlay.transform.SetParent(transform, false);
            var eventCanvas = overlay.GetComponent<Canvas>(); eventCanvas.renderMode = RenderMode.ScreenSpaceOverlay; eventCanvas.sortingOrder = 190;
            var scaler = overlay.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(540, 1170); scaler.matchWidthOrHeight = .5f;
            var shade = Box(overlay.transform, "긴장 효과", new Color(0f, 0f, 0f, .22f), Vector2.zero, Vector2.one);
            var punk = SpriteImage(shade.transform, "임시 동네 양아치", "Bosses/Boss_01_Final", new Vector2(.32f, .34f), new Vector2(.68f, .72f));
            var punkRect = punk.rectTransform;
            var message = Label(shade.transform, "동네 양아치가 나타났다!", 28, Gold, TextAnchor.MiddleCenter, new Vector2(.06f, .72f), new Vector2(.94f, .84f), FontStyle.Bold);
            Vector2 start = new Vector2(620f, 0f); Vector2 center = Vector2.zero; Vector2 end = new Vector2(-620f, 0f);
            float elapsed = 0f;
            while (elapsed < .7f && punk != null) { elapsed += Time.unscaledDeltaTime; punkRect.anchoredPosition = Vector2.Lerp(start, center, Mathf.SmoothStep(0f, 1f, elapsed / .7f)); yield return null; }

            double stolen = Math.Max(10d, Math.Round(GameData.Money * .08d));
            stolen = Math.Min(stolen, GameData.Money);
            GameData.Money -= stolen;
            GameData.Save();
            message.text = "삥 뜯김  -" + stolen.ToString("N0") + "원";
            message.color = Coral;
            GameAudio.PlayFailure();
            if (beggarImage != null) { EnsureReactionSprites(BeggarLifestyleLevel()); if (reactionSprites != null && reactionSprites.Length > 1) beggarImage.sprite = reactionSprites[1]; }
            yield return new WaitForSecondsRealtime(.65f);

            elapsed = 0f;
            while (elapsed < .7f && punk != null) { elapsed += Time.unscaledDeltaTime; punkRect.anchoredPosition = Vector2.Lerp(center, end, Mathf.SmoothStep(0f, 1f, elapsed / .7f)); yield return null; }
            if (overlay != null) Destroy(overlay);
            extortionPlaying = false;
            nextExtortionAt = Time.unscaledTime + UnityEngine.Random.Range(45f, 80f);
            RefreshEconomyText();
        }

        void LoadBattleScene()
        {
            GameData.Save();
            if (Application.CanStreamedLevelBeLoaded("BattleScene")) SceneManager.LoadScene("BattleScene");
            else if (Application.CanStreamedLevelBeLoaded(1)) SceneManager.LoadScene(1);
            else noticeText.text = "전투 씬이 빌드 목록에 없습니다. BattleScene 등록을 확인하세요.";
        }

        void Beg() { BegAt(new Vector2(Screen.width * .5f, Screen.height * .65f)); }

        void BegAt(Vector2 screenPosition)
        {
            bool jackpot = UnityEngine.Random.value < .03f;
            double amount = BegIncome() * (jackpot ? 15d : 1d);
            GameData.Money += amount;
            GameData.TotalEarned += amount;
            int lifestyleStage = BeggarLifestyleLevel();
            EnsureReactionSprites(lifestyleStage);
            reactionIndex = jackpot ? 4 : (reactionIndex + 1) % 4;
            noticeText.text = jackpot ? "대박 후원!  +" + amount.ToString("N0") + "원" : BegLines[reactionIndex] + "  +" + amount.ToString("N0") + "원";
            if (reactionSprites != null && reactionSprites[reactionIndex] != null) beggarImage.sprite = reactionSprites[reactionIndex];
            reactionUntil = Time.unscaledTime + .75f;
            Vector2 localPosition;
            RectTransform streetRect = beggarImage.transform.parent as RectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(streetRect, screenPosition, null, out localPosition)) localPosition = Vector2.zero;
            PlayCoinBurst(localPosition, jackpot); GameAudio.PlayCoin();
            RefreshMoneyOnly();
        }

void PlayCoinBurst(Vector2 origin, bool jackpot)
        {
            int effectCount = coinEffects.Length;
            if (effectCount == 0) return;

            int count = jackpot ? effectCount : 1;
            for (int i = 0; i < count; i++)
            {
                int index = (nextCoinEffect + i) % effectCount;
                CoinPopEffect effect = coinEffects[index];
                if (effect == null) continue;
                effect.Play(origin + new Vector2(UnityEngine.Random.Range(-45f, 45f), UnityEngine.Random.Range(-10f, 20f)), i * .025f);
            }
            nextCoinEffect = (nextCoinEffect + count) % effectCount;
        }

        void EnsureReactionSprites(int stage)
        {
            stage = Mathf.Clamp(stage, 1, 5);
            if (reactionSprites != null && loadedReactionStage == stage) return;
            string root = "Beggar/Reactions/Stage_" + stage.ToString("00") + "/";
            reactionSprites = new[]
            {
                ArtSprite(root + "01_Happy"), ArtSprite(root + "02_Surprised"),
                ArtSprite(root + "03_Pleading"), ArtSprite(root + "04_Touched"),
                ArtSprite(root + "05_Cheeky")
            };
            loadedReactionStage = stage;
            reactionIndex = -1;
        }

        static int BeggarLifestyleLevel()
        {
            double[] thresholds = BalanceDatabase.Current.lifestyleThresholds;
            for (int i = 1; i < thresholds.Length; i++) if (GameData.TotalEarned < thresholds[i]) return i;
            return thresholds.Length;
        }

        static double IncomeReference() { return Math.Max(BalanceDatabase.Current.begIncomePerLifestyleLevel, GameData.PassiveIncome); }
        static double BegIncome() { return IncomeReference(); }
        static double FishPurchaseCost() { return GameData.FishPurchaseCost; }

        void ShowNextBattleReward()
        {
            int stage = Mathf.Clamp(GameData.ClearedStage + 1, 1, GameData.MaxBattleStage);
            ShowSimplePopup("STAGE " + stage + " 완료 혜택", BattleController.RewardPreview(stage));
        }

        void ShowFishCollection()
        {
            var lines = new List<string>();
            for (int i = 0; i < GameData.FishDeathCauses.Length; i++)
                lines.Add((GameData.FishDeathCollection & (1 << i)) != 0 ? "✓ " + GameData.FishDeathCauses[i] : "? 미발견 원인");
            ShowSimplePopup("개복치 사망 도감", string.Join("\n", lines.ToArray()));
        }

        void ShowSimplePopup(string title, string body)
        {
            var popup = new GameObject(title, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup), typeof(GraphicRaycaster));
            popup.transform.SetParent(transform, false);
            var popupCanvas = popup.GetComponent<Canvas>(); popupCanvas.renderMode = RenderMode.ScreenSpaceOverlay; popupCanvas.sortingOrder = 210;
            var scaler = popup.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(540, 1170); scaler.matchWidthOrHeight = .5f;
            var dim = Box(popup.transform, "터치하여 닫기", new Color(0f, 0f, 0f, .52f), Vector2.zero, Vector2.one);
            var dismiss = dim.AddComponent<Button>(); dismiss.targetGraphic = dim.GetComponent<Image>(); dismiss.transition = Selectable.Transition.None; dismiss.onClick.AddListener(delegate { Destroy(popup); });
            var panel = CreamPanelBox(popup.transform, title, new Vector2(.08f, .27f), new Vector2(.92f, .73f)); panel.GetComponent<Image>().raycastTarget = false;
            Label(panel.transform, title, 26, Coral, TextAnchor.MiddleCenter, new Vector2(.06f, .80f), new Vector2(.94f, .95f), FontStyle.Bold);
            Label(panel.transform, body, 17, Cream, TextAnchor.MiddleCenter, new Vector2(.06f, .13f), new Vector2(.94f, .80f), FontStyle.Normal);
            Label(panel.transform, "화면을 눌러 닫기", 13, new Color32(112, 82, 38, 255), TextAnchor.MiddleCenter, new Vector2(.06f, .02f), new Vector2(.94f, .12f));
        }

        void RequestDataReset()
        {
            if (Time.unscaledTime > resetConfirmUntil)
            {
                resetConfirmUntil = Time.unscaledTime + 3f;
                noticeText.text = "초기화하려면 3초 안에 초기화 버튼을 다시 누르세요.";
                return;
            }

            resetConfirmUntil = 0;
            GameData.ResetData();
            noticeText.text = "모든 진행 데이터가 초기화되었습니다.";
            RefreshEconomyText();
        }

void UpgradeFish()
        {
            if (GameData.FishLevel == 0)
            {
                double purchaseCost = FishPurchaseCost();
                if (!Spend(purchaseCost)) return;
                GameData.FishLevel = GameData.FishStartLevel;
                noticeText.text = "개복치를 " + purchaseCost.ToString("N0") + "원에 매입했습니다. · 시작 Lv." + GameData.FishStartLevel;
            }
            else if (GameData.FishLevel < 10)
            {
                int cost = GameData.FishUpgradeCost(GameData.FishLevel);
                if (!Spend(cost)) return;
                float failure = GameData.FishFailureRate(GameData.FishLevel);
                if (UnityEngine.Random.value < failure)
                {
                    float bonusBefore = GameData.FishSurvivalBonus;
                    GameData.FishDeaths++;
                    string deathCause = GameData.RegisterFishDeathCause();
                    float bonusAfter = GameData.FishSurvivalBonus;
                    GameData.FishLevel = 0;
                    noticeText.text = "강화 실패! " + deathCause + " · 누적 사망 " + GameData.FishDeaths + "회";
                    GameAudio.PlayFailure();
                    ShowFishSurvivalPopup(deathCause, bonusAfter - bonusBefore, bonusAfter);
                }
                else
                {
                    GameData.FishLevel++;
                    noticeText.text = "강화 성공! 개복치 Lv." + GameData.FishLevel;
                    GameAudio.PlaySuccess();
                }
            }
            GameData.Save();
            RefreshEconomyText();
        }

void ShowFishSurvivalPopup(string deathCause, float gainedBonus, float totalBonus)
        {
            var popup = new GameObject("개복치 생존 보너스 팝업", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup), typeof(GraphicRaycaster));
            popup.transform.SetParent(transform, false);

            var popupCanvas = popup.GetComponent<Canvas>();
            popupCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            popupCanvas.sortingOrder = 200;

            var scaler = popup.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(540, 1170);
            scaler.matchWidthOrHeight = .5f;

            var group = popup.GetComponent<CanvasGroup>();
            group.blocksRaycasts = true;
            group.interactable = true;

            var dim = Box(popup.transform, "터치하여 닫기", new Color(0f, 0f, 0f, .38f), Vector2.zero, Vector2.one);
            var dismissButton = dim.AddComponent<Button>();
            dismissButton.targetGraphic = dim.GetComponent<Image>();
            dismissButton.transition = Selectable.Transition.None;
            dismissButton.onClick.AddListener(delegate { if (popup != null) Destroy(popup); });

            var panel = CreamPanelBox(popup.transform, "생존 보너스 카드", new Vector2(.10f, .37f), new Vector2(.90f, .63f));
            panel.GetComponent<Image>().raycastTarget = false;
            Label(panel.transform, "개복치 사망 원인 발견!", 25, Coral, TextAnchor.MiddleCenter, new Vector2(.08f, .62f), new Vector2(.92f, .90f), FontStyle.Bold);
            string body = deathCause + "\n누적 사망 " + GameData.FishDeaths + "회\n생존 보정 +" + (gainedBonus * 100f).ToString("0.###") + "%p · 누적 +" + (totalBonus * 100f).ToString("0.###") + "%p";
            Label(panel.transform, body, 19, Cream, TextAnchor.MiddleCenter, new Vector2(.08f, .20f), new Vector2(.92f, .64f), FontStyle.Normal);
            Label(panel.transform, "화면을 눌러 닫기", 14, new Color32(112, 82, 38, 255), TextAnchor.MiddleCenter, new Vector2(.08f, .05f), new Vector2(.92f, .20f), FontStyle.Normal);
        }





        void SellFish()
        {
            if (GameData.FishLevel <= 0) return;
            double value = GameData.FishSaleValue(GameData.FishLevel);
            GameData.Money += value;
            GameData.TotalEarned += value;
            noticeText.text = "개복치 매각  +" + value.ToString("N0") + "원";
            GameData.FishLevel = 0;
            GameData.Save();
            RefreshEconomyText();
        }

        void BuyEstate(int index)
        {
            int limit = GameData.EstatePurchaseLimit(index);
            if (GameData.Estates[index] >= limit)
            {
                noticeText.text = GameData.EstateNames[index] + " 보유 한도 " + limit + "채에 도달했습니다.";
                return;
            }
            double cost = GameData.EstatePurchaseCost(index);
            if (!Spend(cost)) return;
            GameData.Estates[index]++;
            noticeText.text = GameData.EstateNames[index] + " 매입 완료 · 안정 수익 +" + GameData.EstateIncome[index].ToString("N0") + "원/초";
            GameData.Save();
            GameAudio.PlayPurchase();
            RefreshEconomyText();
        }

        bool Spend(double value)
        {
            if (GameData.Money >= value) { GameData.Money -= value; return true; }
            noticeText.text = "자금 부족 · " + value.ToString("N0") + "원이 필요합니다.";
            return false;
        }

        void RefreshEconomyText()
        {
            if (moneyText == null) return;
            RefreshMoneyOnly();
            double begIncome = BegIncome();
            clickText.text = "생활복 Lv." + BeggarLifestyleLevel() + "\n터치  +" + begIncome.ToString("N0") + "원";

            if (GameData.FishLevel == 0)
            {
                fishText.text = "개복치 미보유";
                fishRiskText.text = "첫 매입가 · " + FishPurchaseCost().ToString("N0") + "원";
                fishActionText.text = "개복치 매입  " + FishPurchaseCost().ToString("N0") + "원";
                fishSellButton.interactable = false;
            }
            else if (GameData.FishLevel >= 10)
            {
                fishText.text = "개복치 Lv.10 · 매각가 " + GameData.FishSaleValue(10).ToString("N0") + "원";
                fishRiskText.text = "최대 강화 단계 · 매각해 수익을 확정하세요.";
                fishActionText.text = "최대 강화";
                fishSellButton.interactable = true;
            }
            else
            {
                fishText.text = "개복치 Lv." + GameData.FishLevel + " · 매각가 " + GameData.FishSaleValue(GameData.FishLevel).ToString("N0") + "원";
                fishRiskText.text = "다음 강화 성공 확률 " + ((1f - GameData.FishFailureRate(GameData.FishLevel)) * 100).ToString("0.###") + "%\n사망 보정 +" + (GameData.FishSurvivalBonus * 100f).ToString("0.###") + "%p · 누적 " + GameData.FishDeaths + "회";
                fishActionText.text = "강화  " + GameData.FishUpgradeCost(GameData.FishLevel).ToString("N0") + "원";
                fishSellButton.interactable = true;
            }
            fishActionButton.interactable = GameData.FishLevel < 10;
            fishSellButton.gameObject.SetActive(GameData.FishLevel > 0);
            if (fishRiskBar != null) fishRiskBar.SetActive(GameData.FishLevel > 0 && GameData.FishLevel < 10);
            fishActionButton.GetComponent<RectTransform>().anchorMin = GameData.FishLevel <= 0 ? new Vector2(.18f, .12f) : new Vector2(.06f, .12f);
            fishActionButton.GetComponent<RectTransform>().anchorMax = GameData.FishLevel <= 0 ? new Vector2(.82f, .31f) : new Vector2(.63f, .31f);
            if (fishRiskFill != null)
            {
                float risk = GameData.FishLevel > 0 && GameData.FishLevel < 10 ? GameData.FishFailureRate(GameData.FishLevel) / .58f : 0;
                fishRiskFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(risk), 1);
            }
            RefreshFishSprite();
            RefreshBeggarSprite();

            int total = 0;
            for (int i = 0; i < GameData.Estates.Length; i++)
            {
                total += GameData.Estates[i];
                bool owned = GameData.Estates[i] > 0;
                int limit = GameData.EstatePurchaseLimit(i);
                estateTexts[i].text = GameData.Estates[i] + "채";
                estateBuyTexts[i].text = i == selectedEstate ? "선택됨" : owned ? "보유" : "미구매";
                estateButtons[i].interactable = true;
                if (estateCardGroups[i] != null) estateCardGroups[i].alpha = i == selectedEstate ? 1f : homeSummary ? (owned ? .88f : .32f) : owned ? 1f : .48f;
            }
            estateSummary.text = GameData.LocationNames[GameData.CurrentLocation] + " · " + total + "채 · +" + GameData.PassiveIncome.ToString("N0") + "원/초";
            if (estateDetailText != null)
            {
                int index = Mathf.Clamp(selectedEstate, 0, GameData.Estates.Length - 1);
                int limit = GameData.EstatePurchaseLimit(index);
                bool maxed = GameData.Estates[index] >= limit;
                double income = GameData.EstateIncome[index] * GameData.PassiveIncomeMultiplier * GameData.LocationIncomeMultipliers[GameData.CurrentLocation];
                estateDetailText.text = GameData.EstateNames[index] + " · 보유 " + GameData.Estates[index] + "/" + limit + "채\n+" + income.ToString("N0") + "원/초 · " + (maxed ? "현재 구매 한도" : "다음 " + GameData.EstatePurchaseCost(index).ToString("N0") + "원");
                estateDetailButton.interactable = !maxed;
                estateDetailButton.GetComponentInChildren<Text>().text = maxed ? "MAX" : "구매";
            }
            if (noticeText != null && string.IsNullOrEmpty(noticeText.text))
            {
                int next = -1;
                for (int i = 0; i < GameData.Estates.Length; i++) if (GameData.Estates[i] < GameData.EstatePurchaseLimit(i)) { next = i; break; }
                noticeText.text = next < 0 ? "목표 달성: 모든 부동산을 소유했습니다!" : "다음 목표 · " + GameData.EstateNames[next] + " " + GameData.EstateCosts[next].ToString("N0") + "원";
            }
            if (estateHudText != null) estateHudText.text = total + "채";
            if (fishHudText != null) fishHudText.text = GameData.FishLevel <= 0 ? "없음" : "Lv." + GameData.FishLevel;
            if (stageHudText != null) stageHudText.text = GameData.HighestStage.ToString();
            RefreshHomeBackground();
        }

        void RefreshMoneyOnly()
        {
            if (moneyText == null) return;
            moneyText.text = "현재 자산\n" + GameData.Money.ToString("N0") + "G";
            incomeText.text = "수익\n+" + GameData.PassiveIncome.ToString("N0") + "/sec";
        }

        void RefreshFishSprite()
        {
            if (fishImage == null) return;
            int stage = Mathf.Clamp(GameData.FishLevel <= 0 ? 1 : GameData.FishLevel, 1, 10);
            string spriteName = "Sunfish_Stage_" + stage.ToString("00") + "_Final";
            Sprite sprite = Resources.Load<Sprite>("SunfishGrowth/" + spriteName);
            if (sprite != null) fishImage.sprite = sprite;
            fishImage.color = Color.white;
        }

        void RefreshBeggarSprite()
        {
            if (beggarImage == null) return;
            int stage = BeggarLifestyleLevel();
            string spriteName = stage == 4 ? "Beggar_Stage_04_Fixed" : "Beggar_Stage_" + stage.ToString("00");
            Sprite sprite = ArtSprite("Beggar/" + spriteName);
            if (sprite != null) beggarImage.sprite = sprite;
            if (outfitIconImage != null && sprite != null) outfitIconImage.sprite = sprite;

            double[] thresholds = BalanceDatabase.Current.lifestyleThresholds;
            string[] names = { "누더기", "거리 생활복", "단정한 외출복", "건물주 정장", "랜드마크 오너" };
            double progress = stage >= 5 ? 1 : (GameData.TotalEarned - thresholds[stage - 1]) / (thresholds[stage] - thresholds[stage - 1]);
            if (outfitProgressFill != null) outfitProgressFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01((float)progress), 1);
            if (outfitProgressText != null)
                outfitProgressText.text = stage >= 5 ? names[stage - 1] + "  MAX" : names[stage - 1] + "  " + GameData.TotalEarned.ToString("N0") + " / " + thresholds[stage].ToString("N0");
        }

void RefreshHomeBackground()
        {
            if (homeBackgroundImage == null) return;

            int highestOwnedTier = 0;
            for (int i = GameData.Estates.Length - 1; i >= 0; i--)
            {
                if (GameData.Estates[i] <= 0) continue;
                highestOwnedTier = i + 1;
                break;
            }

            string path = "Backgrounds/HomeProgress/Background_Home_" + highestOwnedTier.ToString("00");
            Sprite sprite = ArtSprite(path);
            if (sprite != null) homeBackgroundImage.sprite = sprite;
            homeBackgroundImage.color = Color.white;
        }

        void BuildBattleEntry()
        {
            var host = new GameObject("Battle Controller").AddComponent<BattleController>();
            host.transform.SetParent(transform, false);
            host.Build(font);
        }

        public static GameObject Box(Transform parent, string name, Color color, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            return go;
        }

                static Sprite RoundedRectSprite()
        {
            if (roundedRectSprite != null) return roundedRectSprite;

            const int size = 64;
            const int radius = 14;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Runtime Rounded UI";
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + .5f;
                    float py = y + .5f;
                    float nearestX = Mathf.Clamp(px, radius, size - radius);
                    float nearestY = Mathf.Clamp(py, radius, size - radius);
                    float distance = Vector2.Distance(new Vector2(px, py), new Vector2(nearestX, nearestY));
                    byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(radius + .5f - distance) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            roundedRectSprite = Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(.5f, .5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            roundedRectSprite.name = "Runtime Rounded UI Sprite";
            return roundedRectSprite;
        }

        static void ApplyRoundedSurface(Image image)
        {
            if (!RoundedUiEnabled || image == null) return;
            image.sprite = RoundedRectSprite();
            image.type = Image.Type.Sliced;
        }

        public static GameObject RoundedBox(Transform parent, string name, Color color, Vector2 min, Vector2 max, bool clipChildren = false)
        {
            var go = Box(parent, name, color, min, max);
            if (!RoundedUiEnabled) return go;

            ApplyRoundedSurface(go.GetComponent<Image>());
            if (clipChildren)
            {
                var mask = go.AddComponent<Mask>();
                mask.showMaskGraphic = true;
            }
            return go;
        }

public static GameObject PanelBox(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var go = Box(parent, name, Color.white, min, max);
            var image = go.GetComponent<Image>();
            if (RoundedUiEnabled)
                ApplyRoundedSurface(image);
            else
            {
                image.sprite = ArtSprite("UI/UI_Panel_NavyGold");
                image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            }
            return go;
        }

public static GameObject SoftPanelBox(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var go = Box(parent, name, Color.white, min, max);
            var image = go.GetComponent<Image>();
            if (RoundedUiEnabled)
                ApplyRoundedSurface(image);
            else
            {
                image.sprite = ArtSprite("UI/UI_Panel_SoftNavy");
                image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            }
            return go;
        }

public static GameObject CreamPanelBox(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var go = Box(parent, name, new Color32(244, 232, 203, 255), min, max);
            if (RoundedUiEnabled)
            {
                ApplyRoundedSurface(go.GetComponent<Image>());
                var mask = go.AddComponent<Mask>();
                mask.showMaskGraphic = true;
            }

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color32(51, 44, 36, 255);
            outline.effectDistance = new Vector2(2f, -2f);
            return go;
        }




        public static Sprite ArtSprite(string path)
        {
            Sprite sprite;
            if (SpriteCache.TryGetValue(path, out sprite)) return sprite;
            sprite = Resources.Load<Sprite>("GameArt/" + path); SpriteCache[path] = sprite; return sprite;
        }




        public static Image SpriteImage(Transform parent, string name, string path, Vector2 min, Vector2 max, Color? tint = null, bool preserveAspect = true)
        {
            var go = Box(parent, name, tint ?? Color.white, min, max);
            var image = go.GetComponent<Image>();
            image.sprite = ArtSprite(path);
            image.type = Image.Type.Simple;
            image.preserveAspect = preserveAspect;
            image.useSpriteMesh = false;
            image.raycastTarget = false;
            return image;
        }

        public static Transform CreateSafeArea(Transform parent)
        {
            var go = new GameObject("Mobile Safe Area", typeof(RectTransform), typeof(MobileSafeArea));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go.transform;
        }

public static Text Label(Transform parent, string text, int size, Color color, TextAnchor anchor, Vector2 min, Vector2 max, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Shadow));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = new Vector2(8, 4);
            rect.offsetMax = new Vector2(-8, -4);

            var label = go.GetComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = anchor;
            label.fontStyle = style;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;

            Color warmInk = new Color(.19f, .13f, .07f, 1f);
            if (size >= 24)
            {
                var outline = go.AddComponent<Outline>();
                outline.effectColor = new Color(warmInk.r, warmInk.g, warmInk.b, .22f);
                outline.effectDistance = new Vector2(.65f, -.65f);
            }
            var shadow = go.GetComponent<Shadow>();
            shadow.effectColor = new Color(warmInk.r, warmInk.g, warmInk.b, size >= 24 ? .12f : .055f);
            shadow.effectDistance = new Vector2(.35f, -.35f);

            TypographyRole role = size >= 24 ? TypographyRole.Hud : size >= 20 ? TypographyRole.Title : size <= 14 ? TypographyRole.Caption : TypographyRole.Body;
            go.AddComponent<ResponsiveTypography>().Configure(size, role);
            return label;
        }

public static Button Button(Transform parent, string text, Vector2 min, Vector2 max, Color bg, Color fg, UnityEngine.Events.UnityAction action)
        {
            var go = Box(parent, "Button", bg, min, max);
            var image = go.GetComponent<Image>();
            if (RoundedUiEnabled)
                ApplyRoundedSurface(image);
            else
            {
                image.sprite = null;
                image.type = Image.Type.Simple;
            }

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color32(51, 44, 36, 255);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, .92f);
            colors.pressedColor = new Color(.72f, .72f, .72f, 1f);
            colors.disabledColor = new Color(.82f, .76f, .64f, .88f);
            button.colors = colors;
            var buttonLabel = Label(go.transform, text, 18, fg, TextAnchor.MiddleCenter, new Vector2(.04f, .08f), new Vector2(.96f, .92f), FontStyle.Bold);
            buttonLabel.GetComponent<ResponsiveTypography>().Configure(18, TypographyRole.Button);
            button.onClick.AddListener(action);
            return button;
        }

public static Button IconNavButton(Transform parent, string iconPath, string text, Vector2 min, Vector2 max, Color bg, UnityEngine.Events.UnityAction action)
        {
            var go = Box(parent, "Nav " + text, bg, min, max);
            var image = go.GetComponent<Image>();
            if (RoundedUiEnabled)
                ApplyRoundedSurface(image);
            else
            {
                image.sprite = null;
                image.type = Image.Type.Simple;
            }

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color32(51, 44, 36, 255);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, .96f, .82f, 1f);
            colors.pressedColor = new Color(.82f, .72f, .52f, 1f);
            colors.disabledColor = new Color(.55f, .55f, .55f, .72f);
            button.colors = colors;
            var icon = SpriteImage(go.transform, text + " 아이콘", iconPath, new Vector2(.22f, .32f), new Vector2(.78f, .94f));
            icon.raycastTarget = false;
            var label = Label(go.transform, text, 16, new Color32(51, 44, 36, 255), TextAnchor.MiddleCenter, new Vector2(.02f, .02f), new Vector2(.98f, .34f), FontStyle.Normal);
            label.GetComponent<ResponsiveTypography>().Configure(16, TypographyRole.Body);
            button.onClick.AddListener(action);
            return button;
        }

        static void Circle(Transform parent, Color color, Vector2 center, Vector2 size)
        {
            var item = Box(parent, "Shape", color, center - size * .5f, center + size * .5f);
            if (circleSprite == null)
            {
                const int resolution = 64;
                var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
                texture.name = "Runtime Circle";
                texture.hideFlags = HideFlags.HideAndDontSave;
                var pixels = new Color32[resolution * resolution];
                var clear = new Color32(255, 255, 255, 0);
                var white = new Color32(255, 255, 255, 255);
                for (int y = 0; y < resolution; y++)
                    for (int x = 0; x < resolution; x++)
                    {
                        float dx = x + .5f - resolution * .5f;
                        float dy = y + .5f - resolution * .5f;
                        pixels[y * resolution + x] = dx * dx + dy * dy <= resolution * resolution * .25f ? white : clear;
                    }
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                circleSprite = Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(.5f, .5f), 100);
                circleSprite.name = "Runtime Circle Sprite";
            }
            item.GetComponent<Image>().sprite = circleSprite;
        }

        public static void MakeFish(Transform parent, Vector2 center, Vector2 size)
        {
            Circle(parent, new Color32(91, 190, 223, 255), center, size);
            var tail = Box(parent, "Tail", new Color32(66, 158, 204, 255), center + new Vector2(-size.x * .66f, -.05f), center + new Vector2(-size.x * .25f, .12f));
            tail.transform.localRotation = Quaternion.Euler(0, 0, 18);
            Circle(parent, Color.white, center + new Vector2(size.x * .20f, size.y * .12f), size * .15f);
            Circle(parent, Navy, center + new Vector2(size.x * .22f, size.y * .13f), size * .065f);
        }

        static Image MakeFishSprite(Transform parent, Vector2 center, Vector2 size)
        {
            var go = Box(parent, "Sunfish Growth Sprite", Color.clear, center - size * .5f, center + size * .5f);
            var image = go.GetComponent<Image>();
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.useSpriteMesh = false;
            image.raycastTarget = false;
            image.sprite = Resources.Load<Sprite>("SunfishGrowth/Sunfish_Stage_01");
            return image;
        }

        static void MakeBuilding(Transform parent, Vector2 center, Vector2 size, int tier)
        {
            var colors = new[] { new Color32(100, 180, 188, 255), new Color32(244, 174, 88, 255), new Color32(126, 142, 210, 255) };
            var b = Box(parent, "Building", colors[tier], center - size * .5f, center + size * .5f);
            for (int y = 0; y < 3; y++) for (int x = 0; x < 2; x++) Box(b.transform, "Window", Cream, new Vector2(.16f + x * .42f, .20f + y * .24f), new Vector2(.42f + x * .42f, .34f + y * .24f));
        }

        public static void MakeMonster(Transform parent, Vector2 center, Vector2 size, Color color, string mark)
        {
            Circle(parent, color, center, size);
            Circle(parent, Cream, center + new Vector2(-size.x * .18f, size.y * .10f), size * .16f);
            Circle(parent, Cream, center + new Vector2(size.x * .18f, size.y * .10f), size * .16f);
            Label(parent, mark, 26, Navy, TextAnchor.MiddleCenter, center - size * .16f, center + size * .16f, FontStyle.Bold);
        }
    }

    public sealed class BattleController : MonoBehaviour
    {

public static void ClearSessionState()
        {
            savedRerollCost = -1;
            for (int i = 0; i < SavedOffers.Length; i++) SavedOffers[i] = null;
        }

        static readonly HeroData[] SavedOffers = new HeroData[3];
        static int savedRerollCost = -1;
        readonly HeroData[] offers = new HeroData[3];
        readonly List<Text> partyTexts = new List<Text>();
        readonly List<Image> partyHp = new List<Image>();
        readonly List<Text> partyHpTexts = new List<Text>();
        readonly Image[] offerImages = new Image[3];
        
        readonly Button[] offerHireButtons = new Button[3];
readonly Text[] offerHireTexts = new Text[3];
        readonly Image[] partyImages = new Image[3];
        Text money, stage, bossName, bossHpText, status, synergy, rewardPreview, rerollText, rerollButtonText;
        Image bossHp, bossImage, battleEffect;
        Button startButton;
        Text startButtonText;
        Transform canvas, offerRoot, battleRoot;
        GameObject resultPopup;

        int selectedStage = 1;
        bool fighting;
        float bossCurrent, bossMax, bossCooldown, heroTick, healerCooldown;
        CombatElement bossElement;
        int rerollCost;
        float abandonConfirmUntil;
        bool enraged;
        static readonly Color32 Navy = new Color32(255, 248, 231, 255), Panel = new Color32(244, 232, 203, 255), Cream = new Color32(51, 44, 36, 255);
        static readonly Color32 Gold = new Color32(255, 193, 71, 255), Mint = new Color32(83, 205, 166, 255), Coral = new Color32(241, 101, 94, 255), Blue = new Color32(75, 139, 221, 255);

public void Build(Font unused)
        {
            int maxSelectableStage = Mathf.Max(1, Mathf.Min(GameData.HighestStage, GameData.MaxBattleStage));
            selectedStage = Mathf.Clamp(GameData.ClearedStage + 1, 1, maxSelectableStage);
            if (rerollCost <= 0) rerollCost = BalanceDatabase.Current.rerollStartCost;

            var go = new GameObject("전투 UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = go.transform;
            go.transform.SetParent(transform, false);
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(540, 1170);
            sc.matchWidthOrHeight = .5f;
            BeggarEstatePrototype.Box(canvas, "BG", Navy, Vector2.zero, Vector2.one);
            BeggarEstatePrototype.SpriteImage(canvas, "Battle Arena Background", "Backgrounds/Background_BattleArena_Cartoon", Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, .18f), false);
            canvas = BeggarEstatePrototype.CreateSafeArea(canvas);
            BuildHeader();
            BuildArena();
            BuildRecruit();
            RestoreOffersOrRoll();
            Refresh();
        }

void BuildHeader()
        {
            var h = BeggarEstatePrototype.CreamPanelBox(canvas, "Header", new Vector2(.035f, .91f), new Vector2(.965f, .99f));
            BeggarEstatePrototype.Button(h.transform, "← 본부", new Vector2(.03f, .18f), new Vector2(.22f, .82f), Panel, Cream, LeaveBattleScene);
            stage = BeggarEstatePrototype.Label(h.transform, "", 22, Cream, TextAnchor.MiddleCenter, new Vector2(.24f, .05f), new Vector2(.63f, .95f), FontStyle.Bold);
            money = BeggarEstatePrototype.Label(h.transform, "", 20, Gold, TextAnchor.MiddleRight, new Vector2(.63f, .05f), new Vector2(.96f, .95f), FontStyle.Bold);
        }

void LeaveBattleScene()
        {
            if (fighting) return;
            GameData.Save();
            SceneManager.LoadScene("SampleScene");
        }

void EndBattleSession(bool failed)
        {
            if (failed)
            {
                Array.Clear(GameData.Party, 0, GameData.Party.Length);
            }
            else
            {
                foreach (HeroData hero in GameData.Party)
                    if (hero != null) hero.hp = hero.maxHp;
            }
            ResetReroll();
            GameData.Save();
        }



void BuildRecruit()
        {
            BeggarEstatePrototype.Label(battleRoot, "영웅 고용", 19, Cream, TextAnchor.MiddleLeft, new Vector2(.055f, .525f), new Vector2(.37f, .57f), FontStyle.Bold);
            rerollText = BeggarEstatePrototype.Label(battleRoot, "", 14, new Color32(112, 82, 38, 255), TextAnchor.MiddleRight, new Vector2(.42f, .527f), new Vector2(.69f, .568f), FontStyle.Normal);
            var rerollButton = BeggarEstatePrototype.Button(battleRoot, "리롤", new Vector2(.715f, .525f), new Vector2(.945f, .57f), new Color32(233, 201, 154, 255), Cream, delegate { RollOffers(true); });
            rerollButton.GetComponentInChildren<ResponsiveTypography>().Configure(14, TypographyRole.Button);
            rerollButtonText = rerollButton.GetComponentInChildren<Text>();

            offerRoot = BeggarEstatePrototype.Box(battleRoot, "직군별 영입 후보", Color.clear, new Vector2(.045f, .28f), new Vector2(.955f, .525f)).transform;
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                float left = i * .337f;
                var card = BeggarEstatePrototype.CreamPanelBox(offerRoot, "Offer " + i, new Vector2(left, 0), new Vector2(left + .315f, 1));
                BeggarEstatePrototype.Label(card.transform, i == 0 ? "탱커" : i == 1 ? "딜러" : "힐러", 16, i == 0 ? Blue : i == 1 ? new Color32(196, 77, 61, 255) : new Color32(24, 133, 99, 255), TextAnchor.UpperCenter, new Vector2(.03f, .79f), new Vector2(.97f, .98f), FontStyle.Bold);
                offerImages[i] = BeggarEstatePrototype.SpriteImage(card.transform, "직군 아이콘", "Icons/Icon_" + (i == 0 ? "Tank" : i == 1 ? "Dealer" : "Healer"), new Vector2(i == 0 ? .18f : .10f, .41f), new Vector2(i == 0 ? .82f : .90f, .84f));
                var hireButton = BeggarEstatePrototype.Button(card.transform, "고용", new Vector2(.07f, .025f), new Vector2(.93f, .205f), new Color32(54, 145, 111, 255), new Color32(255, 248, 231, 255), delegate { Hire(index); });
                hireButton.GetComponentInChildren<ResponsiveTypography>().Configure(14, TypographyRole.Button);
                
                offerHireButtons[i] = hireButton;
offerHireTexts[i] = hireButton.GetComponentInChildren<Text>();
            }
        }

        void BuildArena()
        {
            battleRoot = BeggarEstatePrototype.CreamPanelBox(canvas, "전투 스테이지 V2", new Vector2(.035f, .025f), new Vector2(.965f, .895f)).transform;
            var arenaBackground = BeggarEstatePrototype.SpriteImage(battleRoot, "관객석 원형 경기장", "Backgrounds/Background_BattleArena_Audience_V2", new Vector2(.045f, .57f), new Vector2(.955f, .875f), Color.white, false);
            var arenaOutline = arenaBackground.gameObject.AddComponent<Outline>(); arenaOutline.effectColor = new Color32(51, 44, 36, 255); arenaOutline.effectDistance = new Vector2(1.5f, -1.5f);
            arenaBackground.transform.SetAsFirstSibling();
            bossName = BeggarEstatePrototype.Label(battleRoot, "", 23, Cream, TextAnchor.MiddleCenter, new Vector2(.17f, .94f), new Vector2(.83f, .985f), FontStyle.Bold);
            BeggarEstatePrototype.Button(battleRoot, "◀", new Vector2(.045f, .932f), new Vector2(.145f, .982f), Panel, Cream, delegate { if (!fighting) selectedStage = Mathf.Max(1, selectedStage - 1); Refresh(); });
            BeggarEstatePrototype.Button(battleRoot, "▶", new Vector2(.855f, .932f), new Vector2(.955f, .982f), Panel, Cream, delegate { if (!fighting) selectedStage = Mathf.Min(Mathf.Min(GameData.HighestStage, GameData.MaxBattleStage), selectedStage + 1); Refresh(); });
            var rewardBg = BeggarEstatePrototype.RoundedBox(battleRoot, "완료 보상 강조 배경", new Color32(24, 76, 68, 245), new Vector2(.16f, .892f), new Vector2(.84f, .938f), true);
            rewardPreview = BeggarEstatePrototype.Label(rewardBg.transform, "", 14, Color.white, TextAnchor.MiddleCenter, new Vector2(.03f, .04f), new Vector2(.97f, .96f), FontStyle.Normal);
            var rewardOutline = rewardPreview.gameObject.AddComponent<Outline>(); rewardOutline.effectColor = new Color32(0, 0, 0, 180); rewardOutline.effectDistance = new Vector2(.8f, -.8f);
            var hpBg = BeggarEstatePrototype.RoundedBox(battleRoot, "Boss HP BG", new Color32(80, 50, 45, 255), new Vector2(.12f, .845f), new Vector2(.88f, .885f), true);
            bossHp = BeggarEstatePrototype.Box(hpBg.transform, "Boss HP", Coral, Vector2.zero, Vector2.one).GetComponent<Image>();
            bossHpText = BeggarEstatePrototype.Label(hpBg.transform, "", 13, new Color32(255, 248, 231, 255), TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, FontStyle.Normal);
            bossImage = BeggarEstatePrototype.SpriteImage(battleRoot, "Boss Art", "Bosses/Boss_01_Final", new Vector2(.25f, .57f), new Vector2(.75f, .835f));
            bossImage.gameObject.AddComponent<UIFloatMotion>().Configure(0, 8, 1.35f, .035f, .5f);

            BeggarEstatePrototype.Label(battleRoot, "현재 파티", 17, Cream, TextAnchor.MiddleLeft, new Vector2(.055f, .25f), new Vector2(.42f, .28f), FontStyle.Bold);
            for (int i = 0; i < 3; i++)
            {
                float top = .248f - i * .031f;
                var unit = BeggarEstatePrototype.Box(battleRoot, "Party Row " + i, Color.clear, new Vector2(.055f, top - .028f), new Vector2(.945f, top));
                partyImages[i] = BeggarEstatePrototype.SpriteImage(unit.transform, "Party Art " + i, "Icons/Icon_" + (i == 0 ? "Tank" : i == 1 ? "Dealer" : "Healer"), new Vector2(.00f, .02f), new Vector2(.07f, .98f));
                partyTexts.Add(BeggarEstatePrototype.Label(unit.transform, "", 14, Cream, TextAnchor.MiddleLeft, new Vector2(.07f, .00f), new Vector2(.45f, 1f), FontStyle.Normal));
                var heroHpBg = BeggarEstatePrototype.RoundedBox(unit.transform, "HP BG", new Color32(80, 50, 45, 255), new Vector2(.48f, .32f), new Vector2(.96f, .68f), true);
                partyHp.Add(BeggarEstatePrototype.Box(heroHpBg.transform, "HP Fill", i == 0 ? Blue : Mint, Vector2.zero, Vector2.one).GetComponent<Image>());
                partyHpTexts.Add(BeggarEstatePrototype.Label(unit.transform, "", 11, Cream, TextAnchor.MiddleCenter, new Vector2(.48f, .00f), new Vector2(.96f, 1f), FontStyle.Bold));
            }
            bossImage.transform.SetAsLastSibling();
            battleEffect = BeggarEstatePrototype.SpriteImage(battleRoot, "Battle Effect", "Effects/Effect_NeutralImpact", new Vector2(.25f, .59f), new Vector2(.75f, .88f));
            battleEffect.gameObject.SetActive(false);
            synergy = BeggarEstatePrototype.Label(battleRoot, "", 14, new Color32(24, 133, 99, 255), TextAnchor.MiddleCenter, new Vector2(.055f, .13f), new Vector2(.945f, .158f), FontStyle.Normal);
            status = BeggarEstatePrototype.Label(battleRoot, "", 15, Cream, TextAnchor.MiddleCenter, new Vector2(.055f, .083f), new Vector2(.945f, .13f), FontStyle.Normal);
            startButton = BeggarEstatePrototype.Button(battleRoot, "자동 전투 시작", new Vector2(.18f, .015f), new Vector2(.82f, .078f), new Color32(233, 201, 154, 255), Cream, StartBattle);
            startButtonText = startButton.GetComponentInChildren<Text>();
            startButton.GetComponentInChildren<ResponsiveTypography>().Configure(18, TypographyRole.Button);
        }

        HeroData RandomHero(CombatRole? forcedRole = null)
        {
            var role = forcedRole ?? (CombatRole)UnityEngine.Random.Range(0, 3); var element = (CombatElement)UnityEngine.Random.Range(0, 4);
            var balance = BalanceDatabase.Current;
            int stars = RollStars(balance, GameData.HighestStage);
            float hp = role == CombatRole.Tank ? balance.tankHpPerStar : balance.otherHpPerStar;
            float statMultiplier = Mathf.Pow(balance.heroStatGrowth, stars - 1);
            float attackBase = role == CombatRole.Dealer ? balance.dealerAttackPerStar : role == CombatRole.Tank ? balance.tankAttackPerStar : balance.healerAttackPerStar;
            int cost = Mathf.RoundToInt(balance.heroCostPerStar * Mathf.Pow(2f, stars - 1));
            return new HeroData { name = HeroName(role, element), role = role, element = element, stars = stars, cost = cost, maxHp = hp * statMultiplier, hp = hp * statMultiplier, attack = attackBase * statMultiplier, heal = (role == CombatRole.Healer ? balance.healerHealPerStar : 0) * statMultiplier };
        }

        static int RollStars(BalanceConfig balance, int highestStage)
        {
            if (balance.starChanceStageBreakpoints == null || balance.starChanceStageBreakpoints.Length == 0)
            {
                int legacyRoll = UnityEngine.Random.Range(0, 100);
                return legacyRoll < balance.threeStarChance ? 3 : legacyRoll < balance.twoStarCumulativeChance ? 2 : 1;
            }
            int tier = 0;
            for (int i = 1; i < balance.starChanceStageBreakpoints.Length; i++) if (highestStage >= balance.starChanceStageBreakpoints[i]) tier = i;
            int roll = UnityEngine.Random.Range(0, 100);
            int cumulative = balance.fiveStarChances[tier]; if (roll < cumulative) return 5;
            cumulative += balance.fourStarChances[tier]; if (roll < cumulative) return 4;
            cumulative += balance.threeStarChances[tier]; if (roll < cumulative) return 3;
            cumulative += balance.twoStarChances[tier]; return roll < cumulative ? 2 : 1;
        }

        void RollOffers(bool paid)
        {
            if (fighting) return;
            if (paid) { if (GameData.Money < rerollCost) { status.text = "자금이 부족합니다."; return; } GameData.Money -= rerollCost; rerollCost = Mathf.CeilToInt(rerollCost * BalanceDatabase.Current.rerollCostGrowth / 10f) * 10; }
            for (int i = 0; i < offers.Length; i++) offers[i] = RandomHero((CombatRole)i);
            for (int i = 0; i < offers.Length; i++)
            {
                var card = offerRoot.GetChild(i); var old = card.Find("Info"); if (old != null) Destroy(old.gameObject);
                var h = offers[i]; var text = BeggarEstatePrototype.Label(card, Stars(h.stars) + "  " + Element(h.element) + "\nHP " + h.maxHp.ToString("0") + " · ATK " + h.attack.ToString("0") + (h.heal > 0 ? " · HEAL " + h.heal.ToString("0") : ""), 13, Cream, TextAnchor.MiddleCenter, new Vector2(.04f, .205f), new Vector2(.96f, .45f), FontStyle.Normal); text.gameObject.name = "Info";
                if (offerHireTexts[i] != null) offerHireTexts[i].text = "고용  " + h.cost.ToString("N0") + "원";
                if (offerImages[i] != null) { offerImages[i].sprite = BeggarEstatePrototype.ArtSprite("Icons/Icon_" + (i == 0 ? "Tank" : i == 1 ? "Dealer" : "Healer")); offerImages[i].color = Color.white; }
                SavedOffers[i] = h.Clone();
            }
            savedRerollCost = rerollCost;
            Refresh();
        }

        void RestoreOffersOrRoll()
        {
            if (Array.Exists(SavedOffers, h => h == null))
            {
                RollOffers(false);
                return;
            }
            rerollCost = savedRerollCost > 0 ? savedRerollCost : BalanceDatabase.Current.rerollStartCost;
            for (int i = 0; i < offers.Length; i++) offers[i] = SavedOffers[i].Clone();
            RenderOffers();
        }

        void RenderOffers()
        {
            for (int i = 0; i < offers.Length; i++)
            {
                var card = offerRoot.GetChild(i); var old = card.Find("Info"); if (old != null) Destroy(old.gameObject);
                var h = offers[i]; var text = BeggarEstatePrototype.Label(card, Stars(h.stars) + "  " + Element(h.element) + "\nHP " + h.maxHp.ToString("0") + " · ATK " + h.attack.ToString("0") + (h.heal > 0 ? " · HEAL " + h.heal.ToString("0") : ""), 13, Cream, TextAnchor.MiddleCenter, new Vector2(.04f, .205f), new Vector2(.96f, .45f), FontStyle.Normal); text.gameObject.name = "Info";
                if (offerHireTexts[i] != null) offerHireTexts[i].text = "고용  " + h.cost.ToString("N0") + "원";
                if (offerImages[i] != null) { offerImages[i].sprite = BeggarEstatePrototype.ArtSprite("Icons/Icon_" + (i == 0 ? "Tank" : i == 1 ? "Dealer" : "Healer")); offerImages[i].color = Color.white; }
            }
        }

void RefreshOfferAvailability()
        {
            for (int i = 0; i < offers.Length; i++)
            {
                HeroData offer = offers[i];
                if (offer == null) continue;

                bool roleFilled = GameData.Party[(int)offer.role] != null;
                if (offerHireButtons[i] != null)
                    offerHireButtons[i].interactable = !fighting && !roleFilled;
                if (offerHireTexts[i] != null)
                    offerHireTexts[i].text = roleFilled ? "고용 완료" : "고용  " + offer.cost.ToString("N0") + "원";
            }
        }


void Hire(int index)
        {
            if (fighting || index < 0 || index >= offers.Length) return;

            HeroData hero = offers[index];
            if (hero == null) return;

            int partyIndex = (int)hero.role;
            if (GameData.Party[partyIndex] != null)
            {
                status.text = Role(hero.role) + "는 이미 고용했습니다.";
                RefreshOfferAvailability();
                return;
            }

            if (GameData.Money < hero.cost)
            {
                status.text = "고용 자금이 부족합니다.";
                return;
            }

            GameData.Money -= hero.cost;
            GameData.Party[partyIndex] = hero.Clone();
            status.text = Role(hero.role) + " 고용 완료 · 남은 후보를 계속 선택하세요.";
            GameAudio.PlayRecruit();
            GameData.Save();
            Refresh();
        }

void StartBattle()
        {
            if (fighting)
            {
                if (Time.unscaledTime > abandonConfirmUntil)
                {
                    abandonConfirmUntil = Time.unscaledTime + 3f;
                    status.text = "전투 포기 시 보상이 없습니다. 3초 안에 다시 누르세요.";
                    return;
                }

                fighting = false;
                abandonConfirmUntil = 0;
                string abandonMessage = "전투를 포기했습니다.\n보상 없음 · 고용 영웅 소멸";
                EndBattleSession(true);
                status.text = abandonMessage;
                Refresh();
                ShowBattleResultPopup(false, abandonMessage);
                return;
            }

            if (Array.Exists(GameData.Party, h => h == null)) return;
            fighting = true;
            enraged = false;
            bossElement = selectedStage % 4 == 0 ? CombatElement.Neutral : (CombatElement)((selectedStage - 1) % 3);
            var balance = BalanceDatabase.Current;
            bossMax = BossHp(selectedStage);
            bossCurrent = bossMax;
            bossCooldown = 1.2f;
            heroTick = .2f;
            healerCooldown = .3f;
            foreach (var h in GameData.Party) h.hp = h.maxHp;
            status.text = "자동 전투 진행 중 · 본부 이동 불가";
            startButton.interactable = true;
            startButtonText.text = "전투 포기";
            GameAudio.PlayBattleStart();
        }

        void Update()
        {
            if (!fighting) return; float dt = Time.deltaTime; bossCooldown -= dt; heroTick -= dt; healerCooldown -= dt;
            if (heroTick <= 0)
            {
                heroTick = BalanceDatabase.Current.heroAttackInterval;
                float synergyMult = SynergyMultiplier();
                bool playedHit = false;
                foreach (var h in GameData.Party) if (h != null && h.hp > 0)
                {
                    if (h.attack > 0) { bossCurrent -= h.attack * ElementMultiplier(h.element, bossElement) * synergyMult; FlashEffect(EffectFor(h)); if (!playedHit) { GameAudio.PlayHit(); playedHit = true; } }
                    if (h.heal > 0 && healerCooldown <= 0) { HealFirst(h.heal * synergyMult); healerCooldown = BalanceDatabase.Current.heroAttackInterval * BalanceDatabase.Current.healerActionIntervalMultiplier; FlashEffect("Effects/Effect_Heal"); }
                }
                if (bossCurrent <= 0) { Win(); return; }
            }
            if (bossCooldown <= 0)
            {
                if (!enraged && bossCurrent / bossMax <= BalanceDatabase.Current.bossEnrageHpRatio) { enraged = true; status.text = "⚠ 보스 광폭화 · 공격력/공격속도 증가"; GameAudio.PlayEnrage(); }
                bossCooldown = BalanceDatabase.Current.bossAttackInterval * (enraged ? BalanceDatabase.Current.bossEnrageAttackSpeedMultiplier : 1f);
                var target = FirstAlive(); if (target == null) { Lose(); return; }
                float damage = BossDamage(selectedStage) * ElementMultiplier(bossElement, target.element) * (enraged ? BalanceDatabase.Current.bossEnrageDamageMultiplier : 1f);
                target.hp = Mathf.Max(0, target.hp - damage);
                GameAudio.PlayBossHit();
                FlashEffect("Effects/Effect_Explosion");
                if (FirstAlive() == null) { Lose(); return; }
            }
            RefreshBattleBars();
        }

        HeroData FirstAlive() { foreach (var h in GameData.Party) if (h != null && h.hp > 0) return h; return null; }
        static float BossHp(int stage) { var b = BalanceDatabase.Current; return b.bossBaseHp * Mathf.Pow(b.bossHpGrowth, stage - 1); }
        static float BossDamage(int stage) { var b = BalanceDatabase.Current; return b.bossBaseDamage * Mathf.Pow(b.bossDamageGrowth, stage - 1); }
        void HealFirst(float value) { foreach (var h in GameData.Party) if (h != null && h.hp > 0 && h.hp < h.maxHp) { h.hp = Mathf.Min(h.maxHp, h.hp + value); return; } }
        float SynergyMultiplier() { int best = 1; for (int e = 0; e < 3; e++) { int count = 0; foreach (var h in GameData.Party) if (h != null && (int)h.element == e) count++; best = Mathf.Max(best, count); } return best == 3 ? BalanceDatabase.Current.threeElementSynergy : best == 2 ? BalanceDatabase.Current.twoElementSynergy : 1f; }
        static float ElementMultiplier(CombatElement attack, CombatElement defend)
        {
            if (attack == CombatElement.Neutral || defend == CombatElement.Neutral || attack == defend) return 1f;
            bool strong = (attack == CombatElement.Fire && defend == CombatElement.Grass) || (attack == CombatElement.Grass && defend == CombatElement.Water) || (attack == CombatElement.Water && defend == CombatElement.Fire);
            return strong ? BalanceDatabase.Current.strongElementMultiplier : BalanceDatabase.Current.weakElementMultiplier;
        }

void Win()
        {
            fighting = false;
            bool firstClear = selectedStage > GameData.ClearedStage;
            string rewardMessage;
            int cashReward = 0;
            if (firstClear)
            {
                int firstRewardStage = GameData.ClearedStage + 1;
                int rewardCount = selectedStage - GameData.ClearedStage;
                cashReward = TotalStageReward(firstRewardStage, selectedStage);
                GameData.Money += cashReward;
                GameData.TotalEarned += cashReward;
                GameData.ClearedStage = selectedStage;
                rewardMessage = rewardCount == 1
                    ? "최초 격파 보상  +" + cashReward.ToString("N0") + "원\n영구 특전 · " + StageReward(selectedStage)
                    : "스테이지 " + firstRewardStage + "~" + selectedStage + " 보상 합계  +" + cashReward.ToString("N0") + "원\n영구 특전 " + rewardCount + "개 소급 획득";
            }
            else
            {
                int repeatReward = RepeatReward(selectedStage);
                GameData.Money += repeatReward;
                GameData.TotalEarned += repeatReward;
                rewardMessage = "반복 격파 보상  +" + repeatReward.ToString("N0") + "원";
            }

            if (selectedStage < GameData.MaxBattleStage)
                GameData.HighestStage = Mathf.Max(GameData.HighestStage, selectedStage + 1);

            EndBattleSession(false);
            status.text = rewardMessage;
            startButton.interactable = true;
            GameAudio.PlayVictory();
            Refresh();
            ShowBattleResultPopup(true, rewardMessage);
            int nextStage = Mathf.Max(1, Mathf.Min(GameData.HighestStage, GameData.MaxBattleStage));
            selectedStage = Mathf.Clamp(GameData.ClearedStage + 1, 1, nextStage);
            Refresh();
        }

void Lose()
        {
            fighting = false;
            string failureMessage = "파티가 전멸했습니다.\n보상 없음 · 고용 영웅 소멸";
            EndBattleSession(true);
            status.text = failureMessage;
            startButton.interactable = true;
            GameAudio.PlayFailure();
            Refresh();
            ShowBattleResultPopup(false, failureMessage);
        }

void ShowBattleResultPopup(bool victory, string detail)
        {
            if (resultPopup != null) Destroy(resultPopup);

            resultPopup = BeggarEstatePrototype.Box(canvas, "전투 결과 팝업", new Color(0f, 0f, 0f, .52f), Vector2.zero, Vector2.one);
            resultPopup.transform.SetAsLastSibling();
            var dismissButton = resultPopup.AddComponent<Button>();
            dismissButton.targetGraphic = resultPopup.GetComponent<Image>();
            dismissButton.transition = Selectable.Transition.None;

            var panel = BeggarEstatePrototype.CreamPanelBox(resultPopup.transform, victory ? "승리 결과" : "패배 결과", new Vector2(.10f, .35f), new Vector2(.90f, .65f));
            panel.GetComponent<Image>().raycastTarget = false;
            BeggarEstatePrototype.Label(panel.transform, victory ? "전투 승리!" : "전투 실패", 31, victory ? new Color32(24, 133, 99, 255) : Coral, TextAnchor.MiddleCenter, new Vector2(.08f, .66f), new Vector2(.92f, .91f), FontStyle.Bold);
            BeggarEstatePrototype.Label(panel.transform, "STAGE " + selectedStage + "\n" + detail, 19, Cream, TextAnchor.MiddleCenter, new Vector2(.08f, .22f), new Vector2(.92f, .68f), FontStyle.Normal);
            BeggarEstatePrototype.Label(panel.transform, "화면을 눌러 닫기", 14, new Color32(112, 82, 38, 255), TextAnchor.MiddleCenter, new Vector2(.08f, .05f), new Vector2(.92f, .20f), FontStyle.Normal);

            dismissButton.onClick.AddListener(delegate
            {
                if (resultPopup == null) return;
                Destroy(resultPopup);
                resultPopup = null;
            });
        }

        static string StageReward(int value)
        {
            switch (value)
            {
                case 1: return "개복치 20% 할인";
                case 2: return "개복치 시작 Lv.2";
                case 3: return "원룸 +1채";
                case 4: return "개복치 35% 할인";
                case 5: return "투룸 +1채";
                case 6: return "개복치 시작 Lv.3";
                case 7: return "소형 상가 +1채";
                case 8: return "개복치 50% 할인";
                case 9: return "오피스텔 +1채";
                case 10: return "모든 부동산 +1채";
                case 11: return "개복치 실패율 -5%p";
                case 12: return "패시브 수익 +15%";
                case 13: return "개복치 매각가 +20%";
                case 14: return "모든 부동산 +1채";
                case 15: return "패시브 수익 +20%";
                case 16: return "개복치 시작 Lv.4";
                case 17: return "모든 부동산 +1채";
                case 18: return "개복치 실패율 추가 -5%p";
                case 19: return "패시브 수익 +25%";
                case 20: return "패시브 수익 +50%";
                default: return "";
            }
        }

        public static string RewardPreview(int stageValue)
        {
            if (stageValue > GameData.ClearedStage)
                return "최초 보상 " + TotalStageReward(GameData.ClearedStage + 1, stageValue).ToString("N0") + "원\n영구 특전 · " + StageReward(stageValue);
            return "반복 격파 보상 " + RepeatReward(stageValue).ToString("N0") + "원";
        }
        static int RepeatReward(int stageValue)
        {
            var balance = BalanceDatabase.Current;
            return Mathf.RoundToInt(balance.replayRewardBase * Mathf.Pow(balance.replayRewardGrowth, stageValue - 1));
        }
        static int TotalStageReward(int firstStage, int lastStage)
        {
            int total = 0;
            for (int rewardStage = firstStage; rewardStage <= lastStage; rewardStage++) total += RepeatReward(rewardStage);
            return total;
        }
        void ResetReroll() { rerollCost = BalanceDatabase.Current.rerollStartCost; savedRerollCost = rerollCost; for (int i = 0; i < SavedOffers.Length; i++) SavedOffers[i] = null; if (offerRoot != null) RollOffers(false); }
        void RefreshBattleBars()
        {
            bossHp.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(bossCurrent / bossMax), 1);
            if (bossHpText != null) bossHpText.text = "HP " + Mathf.CeilToInt(bossCurrent).ToString("N0") + " / " + Mathf.CeilToInt(bossMax).ToString("N0");
            for (int i = 0; i < 3; i++)
            {
                var h = GameData.Party[i];
                partyHp[i].rectTransform.anchorMax = new Vector2(h == null ? 0 : Mathf.Clamp01(h.hp / h.maxHp), 1);
                partyHpTexts[i].text = h == null ? "HP —" : "HP " + Mathf.CeilToInt(h.hp) + " / " + Mathf.CeilToInt(h.maxHp);
            }
            bossName.text = "STAGE " + selectedStage + " : " + Element(bossElement) + " 보스";
        }

        void Refresh()
        {
            money.text = "보유 자금  " + GameData.Money.ToString("N0") + "원"; stage.text = "STAGE " + selectedStage + " / " + GameData.MaxBattleStage;
            if (rewardPreview != null) rewardPreview.text = "완료 보상 · " + RewardPreview(selectedStage).Replace("\n", " / ");
            rerollText.text = "직군별 후보 3명";
            
            RefreshOfferAvailability();
if (rerollButtonText != null) rerollButtonText.text = "리롤  " + rerollCost.ToString("N0") + "원";
            for (int i = 0; i < 3; i++) { var h = GameData.Party[i]; partyTexts[i].text = h == null ? Role((CombatRole)i) + "\n미고용" : Role(h.role) + " " + Stars(h.stars) + "\n" + Element(h.element); }
            for (int i = 0; i < 3; i++) { var hero = GameData.Party[i]; if (partyImages[i] != null) partyImages[i].sprite = hero == null ? BeggarEstatePrototype.ArtSprite("Icons/Icon_" + (i == 0 ? "Tank" : i == 1 ? "Dealer" : "Healer")) : HeroSprite(hero); }
            if (bossImage != null) { int artStage = ((selectedStage - 1) % 10) + 1; bossImage.sprite = BeggarEstatePrototype.ArtSprite("Bosses/Boss_" + artStage.ToString("00") + "_Final"); }
            float mult = SynergyMultiplier(); synergy.text = mult > 1 ? "속성 시너지 적용  ×" + mult.ToString("0.0") : "같은 속성 2명: ×1.5 · 3명: ×2.0";
            bool ready = !Array.Exists(GameData.Party, h => h == null); startButton.interactable = ready && !fighting;
            if (startButtonText != null) startButtonText.text = fighting ? "전투 포기" : "자동 전투 시작";
            if (fighting) startButton.interactable = true;
            if (!fighting) { bossElement = selectedStage % 4 == 0 ? CombatElement.Neutral : (CombatElement)((selectedStage - 1) % 3); bossCurrent = bossMax = BossHp(selectedStage); RefreshBattleBars(); status.text = !ready ? "탱커·딜러·힐러를 모두 고용하면 전투를 시작할 수 있습니다." : selectedStage > GameData.ClearedStage ? "최초 보상 " + TotalStageReward(GameData.ClearedStage + 1, selectedStage).ToString("N0") + "원 · " + StageReward(selectedStage) : "반복 격파 보상 · " + RepeatReward(selectedStage).ToString("N0") + "원"; }
        }

        static Sprite HeroSprite(HeroData h) { return BeggarEstatePrototype.ArtSprite("Heroes/Hero_" + h.role + "_" + h.element); }
        static string EffectFor(HeroData h) { return h.element == CombatElement.Fire ? "Effects/Effect_FireSlash" : h.element == CombatElement.Water ? "Effects/Effect_WaterWave" : h.element == CombatElement.Grass ? "Effects/Effect_Thorns" : h.role == CombatRole.Tank ? "Effects/Effect_ShieldImpact" : "Effects/Effect_NeutralImpact"; }
        void FlashEffect(string path)
        {
            if (battleEffect == null) return;
            battleEffect.sprite = BeggarEstatePrototype.ArtSprite(path);
            battleEffect.gameObject.SetActive(true);
            StopCoroutine("HideEffect");
            StartCoroutine("HideEffect");
        }
        IEnumerator HideEffect() { yield return new WaitForSeconds(.22f); if (battleEffect != null) battleEffect.gameObject.SetActive(false); }

        static string HeroName(CombatRole role, CombatElement element) { return Element(element) + " " + (role == CombatRole.Tank ? "골목 방패" : role == CombatRole.Dealer ? "동전 검사" : "붕대 천사"); }
        static string Role(CombatRole r) { return r == CombatRole.Tank ? "탱커" : r == CombatRole.Dealer ? "딜러" : "힐러"; }
        static string Element(CombatElement e) { return e == CombatElement.Fire ? "불" : e == CombatElement.Water ? "물" : e == CombatElement.Grass ? "풀" : "무"; }
        static string Stars(int n) { return new string('★', Mathf.Clamp(n, 1, 5)); }
    }

    public sealed class GameAudio : MonoBehaviour
    {
        static GameAudio instance;
        AudioSource music, effects;
        AudioClip mainMusic, battleMusic;
        AudioClip coin, purchase, success, failure, battle, enrage, victory, hit, bossHit, recruit;
        float lastCoinAt = -10f;


        public static void Ensure()
        {
            if (instance != null) return;
            var go = new GameObject("Game Audio"); DontDestroyOnLoad(go); instance = go.AddComponent<GameAudio>(); instance.Setup();
        }

        void Setup()
        {
            music = gameObject.AddComponent<AudioSource>(); effects = gameObject.AddComponent<AudioSource>();
            music.loop = true; music.playOnAwake = false; music.spatialBlend = 0f; effects.volume = .38f;
            mainMusic = Resources.Load<AudioClip>("Audio/MainBGM");
            battleMusic = Resources.Load<AudioClip>("Audio/BattleBGM");
            if (mainMusic == null) mainMusic = Tone("BGM_LoFi_Fallback", 16f, 110f, .055f, true);
            if (battleMusic == null) battleMusic = Tone("BGM_Battle_Fallback", 8f, 165f, .06f, true);
            SceneManager.sceneLoaded += HandleSceneLoaded;
            PlayMusicForScene(SceneManager.GetActiveScene());
            coin = Tone("SFX_Coin_Ting", .16f, 980f, .24f); purchase = Tone("SFX_Purchase", .22f, 440f, .20f);
            success = Tone("SFX_Success", .30f, 660f, .18f); failure = Tone("SFX_Failure", .35f, 150f, .22f);
            battle = Tone("SFX_BattleStart", .30f, 260f, .22f); enrage = Tone("SFX_Enrage", .45f, 95f, .24f);
            victory = Tone("SFX_Victory", .55f, 520f, .20f);
            hit = Impact("SFX_Hit", .11f, 190f, .42f, .24f, 17);
            bossHit = Impact("SFX_BossHit", .18f, 95f, .58f, .30f, 29);
            recruit = Tone("SFX_Recruit", .34f, 740f, .22f);
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        void HandleSceneLoaded(Scene scene, LoadSceneMode mode) { PlayMusicForScene(scene); }

        void PlayMusicForScene(Scene scene)
        {
            bool isBattle = scene.name == "BattleScene";
            AudioClip next = isBattle ? battleMusic : mainMusic;
            music.volume = isBattle ? .14f : .16f;
            if (music.clip == next && music.isPlaying) return;
            music.Stop(); music.clip = next; music.Play();
        }

        static AudioClip Tone(string name, float seconds, float frequency, float volume, bool chord = false)
        {
            const int rate = 22050; int samples = Mathf.CeilToInt(seconds * rate); float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / rate; float envelope = chord ? .65f + .35f * Mathf.Sin(t * Mathf.PI * 2f / 8f) : Mathf.Clamp01(1f - t / seconds);
                float value = Mathf.Sin(2f * Mathf.PI * frequency * t);
                if (chord) value = value * .55f + Mathf.Sin(2f * Mathf.PI * frequency * 1.25f * t) * .25f + Mathf.Sin(2f * Mathf.PI * frequency * 1.5f * t) * .20f;
                data[i] = value * volume * envelope;
            }
            var clip = AudioClip.Create(name, samples, 1, rate, false); clip.SetData(data, 0); return clip;
        }

        static AudioClip Impact(string name, float seconds, float frequency, float noiseAmount, float volume, int seed)
        {
            const int rate = 22050; int samples = Mathf.CeilToInt(seconds * rate); float[] data = new float[samples];
            var random = new System.Random(seed);
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / rate; float envelope = Mathf.Pow(Mathf.Clamp01(1f - t / seconds), 2f);
                float noise = (float)(random.NextDouble() * 2d - 1d);
                float body = Mathf.Sin(2f * Mathf.PI * frequency * t) * (1f - noiseAmount) + noise * noiseAmount;
                data[i] = body * envelope * volume;
            }
            var clip = AudioClip.Create(name, samples, 1, rate, false); clip.SetData(data, 0); return clip;
        }

        void Play(AudioClip clip) { if (instance != null && clip != null) effects.PlayOneShot(clip); }
public static void PlayCoin()
        {
            if (instance == null || Time.unscaledTime - instance.lastCoinAt < .035f) return;
            instance.lastCoinAt = Time.unscaledTime;
            instance.Play(instance.coin);
        }
        public static void PlayPurchase() { if (instance != null) instance.Play(instance.purchase); }
        public static void PlaySuccess() { if (instance != null) instance.Play(instance.success); }
        public static void PlayFailure() { if (instance != null) instance.Play(instance.failure); }
        public static void PlayBattleStart() { if (instance != null) instance.Play(instance.battle); }
        public static void PlayEnrage() { if (instance != null) instance.Play(instance.enrage); }
        public static void PlayVictory() { if (instance != null) instance.Play(instance.victory); }
        public static void PlayHit() { if (instance != null) instance.Play(instance.hit); }
        public static void PlayBossHit() { if (instance != null) instance.Play(instance.bossHit); }
        public static void PlayRecruit() { if (instance != null) instance.Play(instance.recruit); }
    }

    public sealed class BegClickSurface : MonoBehaviour, IPointerDownHandler
    {
        Action<Vector2> clickAction;
        RectTransform targetRect;

        public void Configure(RectTransform rect, Action<Vector2> action) { targetRect = rect; clickAction = action; }
public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (targetRect != null && RectTransformUtility.RectangleContainsScreenPoint(targetRect, eventData.position, eventData.pressEventCamera))
                clickAction?.Invoke(eventData.position);
        }
    }

    public sealed class CoinPopEffect : MonoBehaviour
    {
        RectTransform rect;
        Image image;
        Vector2 start;
        float beganAt, delay;

        void Awake() { rect = transform as RectTransform; image = GetComponent<Image>(); }

        public void Play(Vector2 offset, float startDelay)
        {
            if (rect == null) { rect = transform as RectTransform; image = GetComponent<Image>(); }
            start = offset; delay = startDelay; beganAt = Time.unscaledTime; rect.anchoredPosition = start;
            rect.localScale = Vector3.one * .55f; image.color = Color.white; gameObject.SetActive(true);
        }

        void Update()
        {
            float t = (Time.unscaledTime - beganAt - delay) / .55f;
            if (t < 0) { image.color = new Color(1, 1, 1, 0); return; }
            if (t >= 1) { gameObject.SetActive(false); return; }
            float arc = Mathf.Sin(t * Mathf.PI) * 65f;
            rect.anchoredPosition = start + new Vector2((t - .5f) * start.x * .35f, t * 145f + arc);
            rect.localScale = Vector3.one * Mathf.Lerp(.55f, 1.05f, Mathf.Sin(t * Mathf.PI));
            image.color = new Color(1, 1, 1, 1f - t);
        }
    }

    public sealed class UIFloatMotion : MonoBehaviour
    {
        RectTransform rect;
        Vector2 origin;
        Vector3 baseScale;
        float amplitudeX, amplitudeY, speed, scaleAmount, phase;

        public void Configure(float x, float y, float motionSpeed, float scalePulse, float motionPhase)
        {
            amplitudeX = x;
            amplitudeY = y;
            speed = motionSpeed;
            scaleAmount = scalePulse;
            phase = motionPhase;
        }

        void Awake()
        {
            rect = transform as RectTransform;
            if (rect != null) origin = rect.anchoredPosition;
            baseScale = transform.localScale;
        }

        void Update()
        {
            float wave = Mathf.Sin(Time.unscaledTime * speed + phase);
            if (rect != null) rect.anchoredPosition = origin + new Vector2(wave * amplitudeX, wave * amplitudeY);
            transform.localScale = baseScale * (1f + wave * scaleAmount);
        }
    }

    public sealed class MobileSafeArea : MonoBehaviour
    {
        Rect lastSafeArea;
        Vector2Int lastScreen;

        void OnEnable() { Apply(); }

        void Update()
        {
            if (lastSafeArea != Screen.safeArea || lastScreen.x != Screen.width || lastScreen.y != Screen.height) Apply();
        }

        void Apply()
        {
            Rect safe = Screen.safeArea;
            lastSafeArea = safe;
            lastScreen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
