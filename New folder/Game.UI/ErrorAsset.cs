using System;
using Colossal;
using Colossal.IO.AssetDatabase;

namespace Game.UI;

public readonly struct ErrorAsset : IEquatable<ErrorAsset>
{
	public readonly Hash128 guid;

	public readonly IDataSourceProvider dataSource;

	public readonly string path;

	public readonly string uri;

	public ErrorAsset(IAssetException asset)
	{
		guid = asset.guid;
		dataSource = asset.dataSource;
		path = asset.path;
		uri = asset.uri;
	}

	public bool Equals(ErrorAsset other)
	{
		if (guid.Equals(other.guid) && dataSource == other.dataSource)
		{
			return string.Equals(path, other.path, StringComparison.Ordinal);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is ErrorAsset other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((guid.GetHashCode() * 397) ^ ((dataSource != null) ? dataSource.GetHashCode() : 0)) * 397) ^ ((path != null) ? path.GetHashCode() : 0);
	}
}
