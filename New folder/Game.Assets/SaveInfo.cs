using System;
using System.Collections.Generic;
using Colossal.IO.AssetDatabase;
using Colossal.Json;
using Colossal.UI;
using Colossal.UI.Binding;
using Game.UI.Menu;

namespace Game.Assets;

public class SaveInfo : IJsonWritable, IContentPrerequisite
{
	[DecodeAlias(new string[] { "previewAsset" })]
	public TextureAsset preview { get; set; }

	public string theme { get; set; }

	public string cityName { get; set; }

	public int population { get; set; }

	public int money { get; set; }

	public int xp { get; set; }

	public SimulationDateTime simulationDate { get; set; }

	public Dictionary<string, bool> options { get; set; }

	[DecodeAlias(new string[] { "contentPrerequisite" })]
	public string[] contentPrerequisites { get; set; }

	public string mapName { get; set; }

	public SaveGameData saveGameData { get; set; }

	public string[] modsEnabled { get; set; }

	[Exclude]
	public string id { get; set; }

	[Exclude]
	public string displayName { get; set; }

	[Exclude]
	public string path { get; set; }

	[Exclude]
	public bool isReadonly { get; set; }

	[Exclude]
	public string cloudTarget { get; set; }

	[Exclude]
	public DateTime lastModified { get; set; }

	public bool autoSave { get; set; }

	[Exclude]
	public SaveGameMetadata metaData { get; set; }

	public Guid sessionGuid { get; set; }

	[Exclude]
	public bool locked { get; set; }

	public string gameMode { get; set; }

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(GetType().FullName);
		writer.PropertyName("id");
		writer.Write(id);
		writer.PropertyName("displayName");
		writer.Write(displayName);
		writer.PropertyName("path");
		writer.Write(path);
		writer.PropertyName("preview");
		writer.Write(preview.ToUri(MenuHelpers.defaultPreview));
		writer.PropertyName("theme");
		writer.Write(theme);
		writer.PropertyName("cityName");
		writer.Write(cityName);
		writer.PropertyName("population");
		writer.Write(population);
		writer.PropertyName("money");
		writer.Write(money);
		writer.PropertyName("xp");
		writer.Write(xp);
		writer.PropertyName("simulationDate");
		writer.Write(simulationDate);
		writer.PropertyName("options");
		writer.Write((IReadOnlyDictionary<string, bool>)options);
		writer.PropertyName("locked");
		writer.Write(locked);
		writer.PropertyName("mapName");
		writer.Write(mapName);
		writer.PropertyName("lastModified");
		writer.Write(lastModified.ToString("o"));
		writer.PropertyName("isReadonly");
		writer.Write(isReadonly);
		writer.PropertyName("cloudTarget");
		writer.Write(cloudTarget);
		writer.PropertyName("autoSave");
		writer.Write(autoSave);
		writer.PropertyName("modsEnabled");
		writer.Write(modsEnabled ?? Array.Empty<string>());
		writer.PropertyName("gameMode");
		writer.Write(gameMode);
		writer.PropertyName("contentPrerequisites");
		writer.Write(contentPrerequisites);
		writer.TypeEnd();
	}

	public SaveInfo Copy()
	{
		return new SaveInfo
		{
			preview = preview,
			theme = theme,
			cityName = cityName,
			population = population,
			money = money,
			xp = xp,
			simulationDate = simulationDate,
			options = ((options != null) ? new Dictionary<string, bool>(options) : options),
			contentPrerequisites = ((contentPrerequisites != null) ? ((string[])contentPrerequisites.Clone()) : contentPrerequisites),
			mapName = mapName,
			saveGameData = saveGameData,
			id = id,
			displayName = displayName,
			path = path,
			isReadonly = isReadonly,
			cloudTarget = cloudTarget,
			lastModified = lastModified,
			autoSave = autoSave,
			metaData = metaData,
			sessionGuid = sessionGuid,
			locked = locked,
			modsEnabled = modsEnabled,
			gameMode = gameMode
		};
	}
}
