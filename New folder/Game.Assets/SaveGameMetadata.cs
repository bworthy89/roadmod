using System;
using System.Collections.Generic;
using System.IO;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.PSI.Common;

namespace Game.Assets;

public class SaveGameMetadata : Metadata<SaveInfo>
{
	public const string kExtension = ".SaveGameMetadata";

	public static readonly Func<string> kPersistentLocation = () => "Saves/" + PlatformManager.instance.userSpecificPath;

	public bool isValidSaveGame
	{
		get
		{
			if (base.isValid && base.target?.saveGameData != null)
			{
				if (!base.target.isReadonly)
				{
					return base.target.saveGameData.isValid;
				}
				return false;
			}
			return false;
		}
	}

	public override IEnumerable<string> modTags
	{
		get
		{
			foreach (string modTag in base.modTags)
			{
				yield return modTag;
			}
			yield return "Savegame";
		}
	}

	protected override void OnPostLoad()
	{
		if (state != LoadState.Full)
		{
			return;
		}
		if (!base.database.dataSource.Contains(base.id))
		{
			return;
		}
		base.target.id = base.identifier;
		SourceMeta meta = GetMeta();
		base.target.metaData = this;
		if (meta.packaged && base.database.TryGetAsset(meta.package, out PackageAsset assetData))
		{
			base.target.displayName = assetData.GetMeta().displayName;
		}
		else
		{
			base.target.displayName = meta.displayName;
		}
		base.target.path = base.id.uri;
		base.target.isReadonly = !meta.belongsToCurrentUser;
		base.target.lastModified = meta.lastWriteTime.ToLocalTime();
		base.target.cloudTarget = meta.remoteStorageSourceName;
		if (!(base.target.saveGameData == null))
		{
			return;
		}
		if (base.database.TryGetAsset(Hash128.CreateGuid(Path.ChangeExtension(meta.path, SaveGameData.kExtensions[1])), out SaveGameData assetData2))
		{
			base.target.saveGameData = assetData2;
		}
		else if (meta.packaged)
		{
			base.target.saveGameData = base.database.GetAsset(SearchFilter<SaveGameData>.ByCondition((SaveGameData a) => a.GetMeta().package == meta.package));
		}
	}
}
