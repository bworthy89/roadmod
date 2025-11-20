using System;
using Colossal.IO.AssetDatabase;
using Colossal.PSI.Common;

namespace Game.Assets;

public class MapMetadata : Metadata<MapInfo>
{
	public const string kExtension = ".MapMetadata";

	public static readonly Func<string> kPersistentLocation = () => "Maps/" + PlatformManager.instance.userSpecificPath;

	protected override void OnPostLoad()
	{
		if (state == LoadState.Full && base.database.dataSource.Contains(base.id))
		{
			base.target.id = base.identifier;
			SourceMeta meta = GetMeta();
			base.target.metaData = this;
			base.target.isReadonly = !meta.belongsToCurrentUser;
			base.target.cloudTarget = meta.remoteStorageSourceName;
		}
	}
}
