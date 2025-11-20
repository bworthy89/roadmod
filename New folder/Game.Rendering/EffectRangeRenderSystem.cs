using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class EffectRangeRenderSystem : GameSystemBase
{
	[BurstCompile]
	private struct EffectRangeRenderJob : IJobChunk
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_InfomodeChunks;

		public OverlayRenderSystem.Buffer m_OverlayBuffer;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_BuildingEfficiencyType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.FirewatchTower> m_FirewatchTowerType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewLocalEffectData> m_InfoviewLocalEffectType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.FirewatchTower> m_FirewatchTowerData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public BufferLookup<Efficiency> m_Efficiencies;

		[ReadOnly]
		public BufferLookup<LocalModifierData> m_LocalModifierData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeList<LocalModifierData> tempModifierList = new NativeList<LocalModifierData>(10, Allocator.Temp);
			uint modifierTypes = GetModifierTypes();
			NativeArray<Game.Objects.Transform> nativeArray = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<Game.Buildings.FirewatchTower> nativeArray2 = chunk.GetNativeArray(ref m_FirewatchTowerType);
			NativeArray<Temp> nativeArray3 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<InstalledUpgrade> bufferAccessor = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			BufferAccessor<Efficiency> bufferAccessor2 = chunk.GetBufferAccessor(ref m_BuildingEfficiencyType);
			for (int i = 0; i < nativeArray4.Length; i++)
			{
				PrefabRef prefabRef = nativeArray4[i];
				InitializeTempList(tempModifierList, prefabRef.m_Prefab);
				if (bufferAccessor.Length != 0)
				{
					AddToTempList(tempModifierList, bufferAccessor[i]);
				}
				for (int j = 0; j < tempModifierList.Length; j++)
				{
					LocalModifierData localModifier = tempModifierList[j];
					if (((uint)(1 << (int)localModifier.m_Type) & modifierTypes) == 0)
					{
						continue;
					}
					Game.Objects.Transform transform = nativeArray[i];
					Game.Buildings.FirewatchTower value2;
					DynamicBuffer<Efficiency> value3;
					if (CollectionUtils.TryGet(nativeArray3, i, out var value))
					{
						if ((m_FirewatchTowerData.TryGetComponent(value.m_Original, out var componentData) && (componentData.m_Flags & FirewatchTowerFlags.HasCoverage) == 0) || !m_Efficiencies.TryGetBuffer(value.m_Original, out var bufferData))
						{
							CheckModifier(localModifier, transform);
						}
						else
						{
							CheckModifier(localModifier, BuildingUtils.GetEfficiency(bufferData), transform);
						}
					}
					else if ((CollectionUtils.TryGet(nativeArray2, i, out value2) && (value2.m_Flags & FirewatchTowerFlags.HasCoverage) == 0) || !CollectionUtils.TryGet(bufferAccessor2, i, out value3))
					{
						CheckModifier(localModifier, transform);
					}
					else
					{
						CheckModifier(localModifier, BuildingUtils.GetEfficiency(value3), transform);
					}
				}
			}
			tempModifierList.Dispose();
		}

		private void InitializeTempList(NativeList<LocalModifierData> tempModifierList, Entity prefab)
		{
			if (m_LocalModifierData.TryGetBuffer(prefab, out var bufferData))
			{
				LocalEffectSystem.InitializeTempList(tempModifierList, bufferData);
			}
			else
			{
				tempModifierList.Clear();
			}
		}

		private void AddToTempList(NativeList<LocalModifierData> tempModifierList, DynamicBuffer<InstalledUpgrade> upgrades)
		{
			for (int i = 0; i < upgrades.Length; i++)
			{
				InstalledUpgrade installedUpgrade = upgrades[i];
				if (m_LocalModifierData.TryGetBuffer(m_PrefabRefData[installedUpgrade.m_Upgrade].m_Prefab, out var bufferData))
				{
					LocalEffectSystem.AddToTempList(tempModifierList, bufferData, BuildingUtils.CheckOption(installedUpgrade, BuildingOption.Inactive));
				}
			}
		}

		private uint GetModifierTypes()
		{
			uint num = 0u;
			for (int i = 0; i < m_InfomodeChunks.Length; i++)
			{
				NativeArray<InfoviewLocalEffectData> nativeArray = m_InfomodeChunks[i].GetNativeArray(ref m_InfoviewLocalEffectType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					num |= (uint)(1 << (int)nativeArray[j].m_Type);
				}
			}
			return num;
		}

		private void CheckModifier(LocalModifierData localModifier, float efficiency, Game.Objects.Transform transform)
		{
			for (int i = 0; i < m_InfomodeChunks.Length; i++)
			{
				NativeArray<InfoviewLocalEffectData> nativeArray = m_InfomodeChunks[i].GetNativeArray(ref m_InfoviewLocalEffectType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					InfoviewLocalEffectData infoviewLocalEffectData = nativeArray[j];
					if (infoviewLocalEffectData.m_Type == localModifier.m_Type)
					{
						float3 @float = math.forward(transform.m_Rotation);
						float num = math.lerp(localModifier.m_Radius.min, localModifier.m_Radius.max, math.sqrt(efficiency));
						UnityEngine.Color color = RenderingUtils.ToColor(infoviewLocalEffectData.m_Color);
						UnityEngine.Color fillColor = color;
						fillColor.a = 0f;
						m_OverlayBuffer.DrawCircle(color, fillColor, num * 0.02f, OverlayRenderSystem.StyleFlags.Projected, @float.xz, transform.m_Position, num * 2f);
					}
				}
			}
		}

		private void CheckModifier(LocalModifierData localModifier, Game.Objects.Transform transform)
		{
			for (int i = 0; i < m_InfomodeChunks.Length; i++)
			{
				NativeArray<InfoviewLocalEffectData> nativeArray = m_InfomodeChunks[i].GetNativeArray(ref m_InfoviewLocalEffectType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					InfoviewLocalEffectData infoviewLocalEffectData = nativeArray[j];
					if (infoviewLocalEffectData.m_Type == localModifier.m_Type)
					{
						float3 @float = math.forward(transform.m_Rotation);
						float max = localModifier.m_Radius.max;
						UnityEngine.Color color = RenderingUtils.ToColor(infoviewLocalEffectData.m_Color);
						UnityEngine.Color fillColor = color;
						fillColor.a = 0f;
						m_OverlayBuffer.DrawCircle(color, fillColor, max * 0.02f, OverlayRenderSystem.StyleFlags.Projected, @float.xz, transform.m_Position, max * 2f);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.FirewatchTower> __Game_Buildings_FirewatchTower_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewLocalEffectData> __Game_Prefabs_InfoviewLocalEffectData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.FirewatchTower> __Game_Buildings_FirewatchTower_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Efficiency> __Game_Buildings_Efficiency_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LocalModifierData> __Game_Prefabs_LocalModifierData_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Buildings_FirewatchTower_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.FirewatchTower>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_InfoviewLocalEffectData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewLocalEffectData>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Buildings_FirewatchTower_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.FirewatchTower>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferLookup = state.GetBufferLookup<Efficiency>(isReadOnly: true);
			__Game_Prefabs_LocalModifierData_RO_BufferLookup = state.GetBufferLookup<LocalModifierData>(isReadOnly: true);
		}
	}

	private EntityQuery m_ProviderQuery;

	private EntityQuery m_InfomodeQuery;

	private OverlayRenderSystem m_OverlayRenderSystem;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_OverlayRenderSystem = base.World.GetOrCreateSystemManaged<OverlayRenderSystem>();
		m_ProviderQuery = GetEntityQuery(ComponentType.ReadOnly<LocalEffectProvider>(), ComponentType.Exclude<Hidden>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Destroyed>());
		m_InfomodeQuery = GetEntityQuery(ComponentType.ReadOnly<InfomodeActive>(), ComponentType.ReadOnly<InfoviewLocalEffectData>());
		RequireForUpdate(m_ProviderQuery);
		RequireForUpdate(m_InfomodeQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> infomodeChunks = m_InfomodeQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle dependencies;
		JobHandle jobHandle = JobChunkExtensions.Schedule(new EffectRangeRenderJob
		{
			m_InfomodeChunks = infomodeChunks,
			m_OverlayBuffer = m_OverlayRenderSystem.GetBuffer(out dependencies),
			m_BuildingEfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_FirewatchTowerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_FirewatchTower_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InfoviewLocalEffectType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewLocalEffectData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_FirewatchTowerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_FirewatchTower_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Efficiencies = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferLookup, ref base.CheckedStateRef),
			m_LocalModifierData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_LocalModifierData_RO_BufferLookup, ref base.CheckedStateRef)
		}, m_ProviderQuery, JobHandle.CombineDependencies(base.Dependency, outJobHandle, dependencies));
		infomodeChunks.Dispose(jobHandle);
		m_OverlayRenderSystem.AddBufferWriter(jobHandle);
		base.Dependency = jobHandle;
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
	public EffectRangeRenderSystem()
	{
	}
}
