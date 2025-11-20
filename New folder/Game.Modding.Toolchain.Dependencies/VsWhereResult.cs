using Colossal.Json;

namespace Game.Modding.Toolchain.Dependencies;

internal class VsWhereResult
{
	public VsWhereEntry[] entries;

	public static VsWhereResult FromJson(string json)
	{
		return JSON.MakeInto<VsWhereResult>(JSON.Load(json));
	}
}
