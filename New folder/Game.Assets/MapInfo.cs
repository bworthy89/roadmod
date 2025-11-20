using System;
using Colossal.IO.AssetDatabase;
using Colossal.Json;
using Colossal.Mathematics;
using Colossal.UI;
using Colossal.UI.Binding;
using Game.UI;
using Game.UI.Menu;

namespace Game.Assets;

public class MapInfo : IJsonWritable, IContentPrerequisite, IComparable<MapInfo>
{
	[Exclude]
	public string id { get; set; }

	public string displayName { get; set; }

	public TextureAsset thumbnail { get; set; }

	public TextureAsset preview { get; set; }

	public string theme { get; set; }

	public Bounds1 temperatureRange { get; set; }

	public float cloudiness { get; set; }

	public float precipitation { get; set; }

	public float latitude { get; set; }

	public float longitude { get; set; }

	public float buildableLand { get; set; }

	public float area { get; set; }

	[DecodeAlias(new string[] { "waterAvailability" })]
	public float surfaceWaterAvailability { get; set; }

	public float groundWaterAvailability { get; set; }

	public MapMetadataSystem.Resources resources { get; set; }

	public MapMetadataSystem.Connections connections { get; set; }

	[DecodeAlias(new string[] { "contentPrerequisite" })]
	public string[] contentPrerequisites { get; set; }

	public bool nameAsCityName { get; set; }

	public int startingYear { get; set; } = -1;

	public MapData mapData { get; set; }

	[Exclude]
	public MapMetadata metaData { get; set; }

	public Guid sessionGuid { get; set; }

	public LocaleAsset[] localeAssets { get; set; }

	public PrefabAsset climate { get; set; }

	[Exclude]
	public bool isReadonly { get; set; }

	[Exclude]
	public string cloudTarget { get; set; }

	[Exclude]
	public bool locked { get; set; }

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(GetType().FullName);
		writer.PropertyName("id");
		writer.Write(id);
		writer.PropertyName("displayName");
		writer.Write(displayName);
		writer.PropertyName("thumbnail");
		writer.Write(thumbnail.ToUri(MenuHelpers.defaultThumbnail));
		writer.PropertyName("preview");
		writer.Write(preview.ToUri(MenuHelpers.defaultPreview));
		writer.PropertyName("theme");
		writer.Write(theme);
		writer.PropertyName("temperatureRange");
		writer.Write(temperatureRange);
		writer.PropertyName("cloudiness");
		writer.Write(cloudiness);
		writer.PropertyName("precipitation");
		writer.Write(precipitation);
		writer.PropertyName("latitude");
		writer.Write(latitude);
		writer.PropertyName("longitude");
		writer.Write(longitude);
		writer.PropertyName("area");
		writer.Write(area);
		writer.PropertyName("buildableLand");
		writer.Write(buildableLand);
		writer.PropertyName("surfaceWaterAvailability");
		writer.Write(surfaceWaterAvailability);
		writer.PropertyName("groundWaterAvailability");
		writer.Write(groundWaterAvailability);
		writer.PropertyName("resources");
		writer.Write(resources);
		writer.PropertyName("connections");
		writer.Write(connections);
		writer.PropertyName("contentPrerequisites");
		writer.Write(contentPrerequisites);
		writer.PropertyName("locked");
		writer.Write(locked);
		writer.PropertyName("nameAsCityName");
		writer.Write(nameAsCityName);
		writer.PropertyName("startingYear");
		writer.Write(startingYear);
		writer.PropertyName("isReadonly");
		writer.Write(isReadonly);
		writer.PropertyName("cloudTarget");
		writer.Write(cloudTarget);
		writer.TypeEnd();
	}

	public int CompareTo(MapInfo other)
	{
		return string.Compare(id, other.id, StringComparison.OrdinalIgnoreCase);
	}

	public MapInfo Copy()
	{
		return new MapInfo
		{
			id = id,
			displayName = displayName,
			thumbnail = thumbnail,
			preview = preview,
			theme = theme,
			temperatureRange = temperatureRange,
			cloudiness = cloudiness,
			precipitation = precipitation,
			latitude = latitude,
			longitude = longitude,
			buildableLand = buildableLand,
			area = area,
			surfaceWaterAvailability = surfaceWaterAvailability,
			resources = resources,
			connections = connections,
			contentPrerequisites = contentPrerequisites,
			nameAsCityName = nameAsCityName,
			startingYear = startingYear,
			mapData = mapData,
			metaData = metaData,
			sessionGuid = sessionGuid,
			localeAssets = localeAssets,
			climate = climate,
			locked = locked
		};
	}
}
