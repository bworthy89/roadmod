using Game.Rendering;
using Game.Simulation;
using UnityEngine;

internal static class _0024BurstDirectCallInitializer
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
	private static void Initialize()
	{
		WaterRenderSystem.DequeueAndSort_000045A6_0024BurstDirectCall.Initialize();
		HeightDataReader.CopyWaterValuesInternal_00005E87_0024BurstDirectCall.Initialize();
		SurfaceDataReader.CopyWaterValuesInternal_00005E8E_0024BurstDirectCall.Initialize();
	}
}
