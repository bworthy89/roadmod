using System;
using System.IO;
using Colossal.Json;
using Colossal.PSI.Environment;
using Game.Assets;
using Game.SceneFlow;
using Unity.Mathematics;
using UnityEngine;

namespace Game.PSI.PdxSdk;

public static class Launcher
{
	internal static class LocaleID
	{
		public const string kPopulation = "Population";

		public const string kPopulationID = "GameListScreen.POPULATION_LABEL";

		public const string kMoney = "Money";

		public const string kMoneyID = "GameListScreen.MONEY_LABEL";

		public const string kMoneyValue = "{0}¢{1}";

		public const string kMoneyValueID = "Common.VALUE_MONEY";

		public const string kUnlimitedMoney = "Unlimited";

		public const string kUnlimitedMoneyID = "Menu.UNLIMITED_MONEY_LABEL";
	}

	private class SaveInfoData
	{
		public string title;

		public string desc;

		public string date;

		public string rawGameVersion;
	}

	private const string kLastSaveInfoFileName = "continue_game.json";

	private static readonly string kLastSaveInfoPath = EnvPath.kUserDataPath + "/continue_game.json";

	private static string LocalizedString(string id, string def)
	{
		if (GameManager.instance.localizationManager.activeDictionary.TryGetValue(id, out var value))
		{
			return value;
		}
		return def;
	}

	private static string FormatMoney(int money, bool unlimitedMoney)
	{
		if (unlimitedMoney)
		{
			return LocalizedString("Menu.UNLIMITED_MONEY_LABEL", "Unlimited");
		}
		return string.Format(LocalizedString("Common.VALUE_MONEY", "{0}¢{1}").Replace("SIGN", "0").Replace("VALUE", "1"), ((float)math.sign(money) < 0f) ? "-" : "", money);
	}

	public static void SaveLastSaveMetadata(SaveInfo saveInfo)
	{
		try
		{
			SaveInfoData saveInfoData = new SaveInfoData();
			saveInfoData.title = saveInfo.cityName;
			saveInfoData.desc = string.Format("{0}: {1} {2}: {3}", LocalizedString("GameListScreen.POPULATION_LABEL", "Population"), saveInfo.population, LocalizedString("GameListScreen.MONEY_LABEL", "Money"), FormatMoney(saveInfo.money, saveInfo.options != null && saveInfo.options["unlimitedMoney"]));
			saveInfoData.date = saveInfo.lastModified.ToString("s");
			saveInfoData.rawGameVersion = Version.current.shortVersion;
			SaveInfoData data = saveInfoData;
			File.WriteAllText(kLastSaveInfoPath, JSON.Dump(data));
		}
		catch (Exception exception)
		{
			UnityEngine.Debug.LogException(exception);
		}
	}

	public static void DeleteLastSaveMetadata()
	{
		if (File.Exists(kLastSaveInfoPath))
		{
			File.Delete(kLastSaveInfoPath);
		}
	}
}
