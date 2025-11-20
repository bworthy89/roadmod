using Colossal.Entities;
using Game.Buildings;
using Unity.Collections;
using Unity.Entities;

namespace Game.Prefabs;

public static class UpgradeUtils
{
	public static void CombineStats<T>(ref T result, BufferAccessor<InstalledUpgrade> accessor, int i, ref ComponentLookup<PrefabRef> prefabs, ref ComponentLookup<T> combineDatas) where T : unmanaged, IComponentData, ICombineData<T>
	{
		if (accessor.Length != 0)
		{
			CombineStats(ref result, accessor[i], ref prefabs, ref combineDatas);
		}
	}

	public static bool CombineStats<T>(ref T data, DynamicBuffer<InstalledUpgrade> upgrades, ref ComponentLookup<PrefabRef> prefabs, ref ComponentLookup<T> combineDatas) where T : unmanaged, IComponentData, ICombineData<T>
	{
		bool result = false;
		for (int i = 0; i < upgrades.Length; i++)
		{
			InstalledUpgrade installedUpgrade = upgrades[i];
			if (!BuildingUtils.CheckOption(installedUpgrade, BuildingOption.Inactive) && combineDatas.TryGetComponent(prefabs[installedUpgrade.m_Upgrade].m_Prefab, out var componentData))
			{
				data.Combine(componentData);
				result = true;
			}
		}
		return result;
	}

	public static bool CombinePollutionStats(ref PollutionData data, DynamicBuffer<InstalledUpgrade> upgrades, ref ComponentLookup<PrefabRef> prefabs, ref ComponentLookup<PollutionData> combineDatas, ref ComponentLookup<PollutionEmitModifier> modifierDatas)
	{
		bool result = false;
		for (int i = 0; i < upgrades.Length; i++)
		{
			InstalledUpgrade installedUpgrade = upgrades[i];
			if (!BuildingUtils.CheckOption(installedUpgrade, BuildingOption.Inactive) && combineDatas.TryGetComponent(prefabs[installedUpgrade.m_Upgrade].m_Prefab, out var componentData))
			{
				if (modifierDatas.TryGetComponent(installedUpgrade, out var componentData2))
				{
					componentData2.UpdatePollutionData(ref componentData);
				}
				data.Combine(componentData);
				result = true;
			}
		}
		return result;
	}

	public static bool CombineStats<T>(EntityManager entityManager, ref T data, DynamicBuffer<InstalledUpgrade> upgrades) where T : unmanaged, IComponentData, ICombineData<T>
	{
		bool result = false;
		for (int i = 0; i < upgrades.Length; i++)
		{
			InstalledUpgrade installedUpgrade = upgrades[i];
			if (!BuildingUtils.CheckOption(installedUpgrade, BuildingOption.Inactive) && entityManager.TryGetComponent<PrefabRef>(installedUpgrade.m_Upgrade, out var component) && entityManager.TryGetComponent<T>(component.m_Prefab, out var component2))
			{
				data.Combine(component2);
				result = true;
			}
		}
		return result;
	}

	public static void CombineStats<T>(NativeList<T> result, DynamicBuffer<InstalledUpgrade> upgrades, ref ComponentLookup<PrefabRef> prefabs, ref BufferLookup<T> combineDatas) where T : unmanaged, IBufferElementData, ICombineBuffer<T>
	{
		for (int i = 0; i < upgrades.Length; i++)
		{
			InstalledUpgrade installedUpgrade = upgrades[i];
			if (!BuildingUtils.CheckOption(installedUpgrade, BuildingOption.Inactive))
			{
				CombineStats(result, prefabs[installedUpgrade.m_Upgrade].m_Prefab, ref combineDatas);
			}
		}
	}

	public static void CombineStats<T>(NativeList<T> result, Entity prefab, ref BufferLookup<T> combineDatas) where T : unmanaged, IBufferElementData, ICombineBuffer<T>
	{
		if (combineDatas.TryGetBuffer(prefab, out var bufferData))
		{
			CombineStats(result, bufferData);
		}
	}

	public static void CombineStats<T>(NativeList<T> result, DynamicBuffer<T> combineData) where T : unmanaged, IBufferElementData, ICombineBuffer<T>
	{
		for (int i = 0; i < combineData.Length; i++)
		{
			combineData[i].Combine(result);
		}
	}

	public static void CombineStats<T>(NativeList<T> result, T combineData) where T : unmanaged, IBufferElementData, ICombineBuffer<T>
	{
		combineData.Combine(result);
	}

	public static bool TryGetCombinedComponent<T>(EntityManager entityManager, Entity entity, Entity prefab, out T data) where T : unmanaged, IComponentData, ICombineData<T>
	{
		bool flag = entityManager.TryGetComponent<T>(prefab, out data);
		return TryCombineData(entityManager, entity, ref data) || flag;
	}

	public static bool TryCombineData<T>(EntityManager entityManager, Entity entity, ref T data) where T : unmanaged, IComponentData, ICombineData<T>
	{
		if (entityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<InstalledUpgrade> buffer))
		{
			return CombineStats(entityManager, ref data, buffer);
		}
		return false;
	}

	public static bool TryGetCombinedComponent<T>(Entity entity, out T data, ref ComponentLookup<PrefabRef> prefabRefLookup, ref ComponentLookup<T> combineDataLookup, ref BufferLookup<InstalledUpgrade> installedUpgradeLookup) where T : unmanaged, IComponentData, ICombineData<T>
	{
		data = default(T);
		PrefabRef componentData;
		bool flag = prefabRefLookup.TryGetComponent(entity, out componentData) && combineDataLookup.TryGetComponent(componentData.m_Prefab, out data);
		if (installedUpgradeLookup.TryGetBuffer(entity, out var bufferData))
		{
			flag |= CombineStats(ref data, bufferData, ref prefabRefLookup, ref combineDataLookup);
		}
		return flag;
	}
}
