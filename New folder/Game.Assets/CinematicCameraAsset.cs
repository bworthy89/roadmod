using System;
using Colossal.IO.AssetDatabase;
using Colossal.PSI.Common;
using Colossal.UI.Binding;
using Game.CinematicCamera;
using Game.UI.Menu;

namespace Game.Assets;

public class CinematicCameraAsset : Metadata<CinematicCameraSequence>, IJsonWritable
{
	public const string kExtension = ".CinematicCamera";

	public static readonly Func<string> kPersistentLocation = () => "CinematicCamera/" + PlatformManager.instance.userSpecificPath;

	private static readonly string kCloudTargetProperty = "cloudTarget";

	private static readonly string kReadOnlyProperty = "isReadOnly";

	public void Write(IJsonWriter writer)
	{
		SourceMeta meta = GetMeta();
		writer.TypeBegin("CinematicCameraAsset");
		writer.PropertyName("name");
		writer.Write(name);
		writer.PropertyName("guid");
		writer.Write(base.id.guid.ToString());
		writer.PropertyName("identifier");
		writer.Write(base.identifier);
		writer.PropertyName(kCloudTargetProperty);
		writer.Write(MenuHelpers.GetSanitizedCloudTarget(meta.remoteStorageSourceName).name);
		writer.PropertyName(kReadOnlyProperty);
		writer.Write(!meta.belongsToCurrentUser);
		writer.TypeEnd();
	}
}
