using System;
using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Policies;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CityModifierUpdateSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateCityModifiersJob : IJobChunk
	{
		[ReadOnly]
		public CityModifierRefreshData m_CityModifierRefreshData;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_EffectProviderChunks;

		[ReadOnly]
		public BufferTypeHandle<Policy> m_PolicyType;

		public ComponentTypeHandle<Game.City.City> m_CityType;

		public BufferTypeHandle<CityModifier> m_CityModifierType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeList<CityModifierData> tempModifierList = new NativeList<CityModifierData>(10, Allocator.Temp);
			NativeArray<Game.City.City> nativeArray = chunk.GetNativeArray(ref m_CityType);
			BufferAccessor<CityModifier> bufferAccessor = chunk.GetBufferAccessor(ref m_CityModifierType);
			BufferAccessor<Policy> bufferAccessor2 = chunk.GetBufferAccessor(ref m_PolicyType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Game.City.City city = nativeArray[i];
				DynamicBuffer<CityModifier> modifiers = bufferAccessor[i];
				DynamicBuffer<Policy> policies = bufferAccessor2[i];
				m_CityModifierRefreshData.RefreshCityOptions(ref city, policies);
				m_CityModifierRefreshData.RefreshCityModifiers(modifiers, policies, m_EffectProviderChunks, tempModifierList);
				nativeArray[i] = city;
			}
			tempModifierList.Dispose();
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	public struct CityModifierRefreshData
	{
		public BufferTypeHandle<Efficiency> m_BuildingEfficiencyType;

		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		public ComponentTypeHandle<Signature> m_SignatureType;

		public ComponentLookup<PrefabRef> m_PrefabRefData;

		public ComponentLookup<PolicySliderData> m_PolicySliderData;

		public ComponentLookup<CityOptionData> m_CityOptionData;

		public BufferLookup<CityModifierData> m_CityModifierData;

		public CityModifierRefreshData(SystemBase system)
		{
			m_BuildingEfficiencyType = system.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
			m_PrefabRefType = system.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			m_InstalledUpgradeType = system.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			m_SignatureType = system.GetComponentTypeHandle<Signature>(isReadOnly: true);
			m_PrefabRefData = system.GetComponentLookup<PrefabRef>(isReadOnly: true);
			m_PolicySliderData = system.GetComponentLookup<PolicySliderData>(isReadOnly: true);
			m_CityOptionData = system.GetComponentLookup<CityOptionData>(isReadOnly: true);
			m_CityModifierData = system.GetBufferLookup<CityModifierData>(isReadOnly: true);
		}

		public void Update(SystemBase system)
		{
			m_BuildingEfficiencyType.Update(system);
			m_PrefabRefType.Update(system);
			m_InstalledUpgradeType.Update(system);
			m_SignatureType.Update(system);
			m_PrefabRefData.Update(system);
			m_PolicySliderData.Update(system);
			m_CityOptionData.Update(system);
			m_CityModifierData.Update(system);
		}

		public void RefreshCityOptions(ref Game.City.City city, DynamicBuffer<Policy> policies)
		{
			city.m_OptionMask = 0u;
			for (int i = 0; i < policies.Length; i++)
			{
				Policy policy = policies[i];
				if ((policy.m_Flags & PolicyFlags.Active) != 0 && m_CityOptionData.HasComponent(policy.m_Policy))
				{
					CityOptionData cityOptionData = m_CityOptionData[policy.m_Policy];
					city.m_OptionMask |= cityOptionData.m_OptionMask;
				}
			}
		}

		public void RefreshCityModifiers(DynamicBuffer<CityModifier> modifiers, DynamicBuffer<Policy> policies, NativeList<ArchetypeChunk> effectProviderChunks, NativeList<CityModifierData> tempModifierList)
		{
			modifiers.Clear();
			for (int i = 0; i < policies.Length; i++)
			{
				Policy policy = policies[i];
				if ((policy.m_Flags & PolicyFlags.Active) == 0 || !m_CityModifierData.HasBuffer(policy.m_Policy))
				{
					continue;
				}
				DynamicBuffer<CityModifierData> dynamicBuffer = m_CityModifierData[policy.m_Policy];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					CityModifierData modifierData = dynamicBuffer[j];
					float delta;
					if (m_PolicySliderData.HasComponent(policy.m_Policy))
					{
						PolicySliderData policySliderData = m_PolicySliderData[policy.m_Policy];
						float falseValue = (policy.m_Adjustment - policySliderData.m_Range.min) / (policySliderData.m_Range.max - policySliderData.m_Range.min);
						falseValue = math.select(falseValue, 0f, policySliderData.m_Range.min == policySliderData.m_Range.max);
						falseValue = math.saturate(falseValue);
						delta = math.lerp(modifierData.m_Range.min, modifierData.m_Range.max, falseValue);
					}
					else
					{
						delta = modifierData.m_Range.min;
					}
					AddModifier(modifiers, modifierData, delta);
				}
			}
			for (int k = 0; k < effectProviderChunks.Length; k++)
			{
				ArchetypeChunk archetypeChunk = effectProviderChunks[k];
				bool num = archetypeChunk.Has(ref m_SignatureType);
				BufferAccessor<Efficiency> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_BuildingEfficiencyType);
				NativeArray<PrefabRef> nativeArray = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
				BufferAccessor<InstalledUpgrade> bufferAccessor2 = archetypeChunk.GetBufferAccessor(ref m_InstalledUpgradeType);
				if (!num && bufferAccessor.Length != 0)
				{
					for (int l = 0; l < nativeArray.Length; l++)
					{
						PrefabRef prefabRef = nativeArray[l];
						float efficiency = BuildingUtils.GetEfficiency(bufferAccessor[l]);
						if (m_CityModifierData.HasBuffer(prefabRef.m_Prefab))
						{
							InitializeTempList(tempModifierList, m_CityModifierData[prefabRef.m_Prefab]);
						}
						else
						{
							InitializeTempList(tempModifierList);
						}
						if (bufferAccessor2.Length != 0)
						{
							AddToTempList(tempModifierList, bufferAccessor2[l]);
						}
						for (int m = 0; m < tempModifierList.Length; m++)
						{
							CityModifierData modifierData2 = tempModifierList[m];
							float delta2 = math.lerp(modifierData2.m_Range.min, modifierData2.m_Range.max, efficiency);
							AddModifier(modifiers, modifierData2, delta2);
						}
					}
					continue;
				}
				for (int n = 0; n < nativeArray.Length; n++)
				{
					PrefabRef prefabRef2 = nativeArray[n];
					if (m_CityModifierData.HasBuffer(prefabRef2.m_Prefab))
					{
						InitializeTempList(tempModifierList, m_CityModifierData[prefabRef2.m_Prefab]);
					}
					else
					{
						InitializeTempList(tempModifierList);
					}
					if (bufferAccessor2.Length != 0)
					{
						AddToTempList(tempModifierList, bufferAccessor2[n]);
					}
					for (int num2 = 0; num2 < tempModifierList.Length; num2++)
					{
						CityModifierData modifierData3 = tempModifierList[num2];
						AddModifier(modifiers, modifierData3, modifierData3.m_Range.max);
					}
				}
			}
		}

		private void AddToTempList(NativeList<CityModifierData> tempModifierList, DynamicBuffer<InstalledUpgrade> upgrades)
		{
			for (int i = 0; i < upgrades.Length; i++)
			{
				InstalledUpgrade installedUpgrade = upgrades[i];
				if (!BuildingUtils.CheckOption(installedUpgrade, BuildingOption.Inactive) && m_CityModifierData.TryGetBuffer(m_PrefabRefData[installedUpgrade.m_Upgrade].m_Prefab, out var bufferData))
				{
					CityModifierUpdateSystem.AddToTempList(tempModifierList, bufferData);
				}
			}
		}

		private static void AddModifier(DynamicBuffer<CityModifier> modifiers, CityModifierData modifierData, float delta)
		{
			while (modifiers.Length <= (int)modifierData.m_Type)
			{
				modifiers.Add(default(CityModifier));
			}
			CityModifier value = modifiers[(int)modifierData.m_Type];
			switch (modifierData.m_Mode)
			{
			case ModifierValueMode.Relative:
				value.m_Delta.y = value.m_Delta.y * (1f + delta) + delta;
				break;
			case ModifierValueMode.Absolute:
				value.m_Delta.x += delta;
				break;
			case ModifierValueMode.InverseRelative:
				delta = 1f / math.max(0.001f, 1f + delta) - 1f;
				value.m_Delta.y = value.m_Delta.y * (1f + delta) + delta;
				break;
			}
			modifiers[(int)modifierData.m_Type] = value;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public BufferTypeHandle<Policy> __Game_Policies_Policy_RO_BufferTypeHandle;

		public ComponentTypeHandle<Game.City.City> __Game_City_City_RW_ComponentTypeHandle;

		public BufferTypeHandle<CityModifier> __Game_City_CityModifier_RW_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Policies_Policy_RO_BufferTypeHandle = state.GetBufferTypeHandle<Policy>(isReadOnly: true);
			__Game_City_City_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.City.City>();
			__Game_City_CityModifier_RW_BufferTypeHandle = state.GetBufferTypeHandle<CityModifier>();
		}
	}

	private EntityQuery m_CityQuery;

	private EntityQuery m_EffectProviderQuery;

	private CityModifierRefreshData m_CityModifierRefreshData;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 256;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CityModifierRefreshData = new CityModifierRefreshData(this);
		m_CityQuery = GetEntityQuery(ComponentType.ReadWrite<Game.City.City>());
		m_EffectProviderQuery = GetEntityQuery(ComponentType.ReadOnly<CityEffectProvider>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_CityQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> effectProviderChunks = m_EffectProviderQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		m_CityModifierRefreshData.Update(this);
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new UpdateCityModifiersJob
		{
			m_CityModifierRefreshData = m_CityModifierRefreshData,
			m_EffectProviderChunks = effectProviderChunks,
			m_PolicyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Policies_Policy_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_CityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_City_City_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CityModifierType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_City_CityModifier_RW_BufferTypeHandle, ref base.CheckedStateRef)
		}, m_CityQuery, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
		effectProviderChunks.Dispose(jobHandle);
		base.Dependency = jobHandle;
	}

	public static void InitializeTempList(NativeList<CityModifierData> tempModifierList)
	{
		tempModifierList.Clear();
	}

	public static void InitializeTempList(NativeList<CityModifierData> tempModifierList, DynamicBuffer<CityModifierData> cityModifiers)
	{
		tempModifierList.Clear();
		tempModifierList.AddRange(cityModifiers.AsNativeArray());
	}

	public static void AddToTempList(NativeList<CityModifierData> tempModifierList, DynamicBuffer<CityModifierData> cityModifiers)
	{
		for (int i = 0; i < cityModifiers.Length; i++)
		{
			CityModifierData value = cityModifiers[i];
			int num = 0;
			while (true)
			{
				if (num < tempModifierList.Length)
				{
					CityModifierData value2 = tempModifierList[num];
					if (value2.m_Type == value.m_Type)
					{
						if (value2.m_Mode != value.m_Mode)
						{
							throw new Exception($"Modifier mode mismatch (type: {value.m_Type})");
						}
						value2.m_Range.min += value.m_Range.min;
						value2.m_Range.max += value.m_Range.max;
						tempModifierList[num] = value2;
						break;
					}
					num++;
					continue;
				}
				tempModifierList.Add(in value);
				break;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public CityModifierUpdateSystem()
	{
	}
}
