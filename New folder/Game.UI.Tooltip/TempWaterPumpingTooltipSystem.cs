using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.UI.Localization;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.Tooltip;

[CompilerGenerated]
public class TempWaterPumpingTooltipSystem : TooltipSystemBase
{
	private struct TempResult
	{
		public AllowedWaterTypes m_Types;

		public int m_Production;

		public int m_MaxCapacity;
	}

	[BurstCompile]
	private struct TempJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public BufferTypeHandle<Game.Objects.SubObject> m_SubObjectType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<WaterPumpingStationData> m_PumpDatas;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_Transforms;

		[ReadOnly]
		public ComponentLookup<Game.Simulation.WaterSourceData> m_WaterSources;

		[ReadOnly]
		public NativeArray<GroundWater> m_GroundWaterMap;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		public NativeReference<TempResult> m_Result;

		public WaterPipeParameterData m_Parameters;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			ref TempResult reference = ref m_Result.ValueAsRef();
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<Game.Objects.Transform> nativeArray3 = chunk.GetNativeArray(ref m_TransformType);
			BufferAccessor<Game.Objects.SubObject> bufferAccessor = chunk.GetBufferAccessor(ref m_SubObjectType);
			BufferAccessor<InstalledUpgrade> bufferAccessor2 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			for (int i = 0; i < chunk.Count; i++)
			{
				if ((nativeArray2[i].m_Flags & (TempFlags.Create | TempFlags.Modify | TempFlags.Upgrade)) == 0)
				{
					continue;
				}
				m_PumpDatas.TryGetComponent(nativeArray[i].m_Prefab, out var componentData);
				if (bufferAccessor2.Length != 0)
				{
					UpgradeUtils.CombineStats(ref componentData, bufferAccessor2[i], ref m_Prefabs, ref m_PumpDatas);
				}
				int num = 0;
				if (componentData.m_Types != AllowedWaterTypes.None)
				{
					if ((componentData.m_Types & AllowedWaterTypes.Groundwater) != AllowedWaterTypes.None)
					{
						int num2 = Mathf.RoundToInt(math.clamp((float)GroundWaterSystem.GetGroundWater(nativeArray3[i].m_Position, m_GroundWaterMap).m_Max / m_Parameters.m_GroundwaterPumpEffectiveAmount, 0f, 1f) * (float)componentData.m_Capacity);
						num += num2;
					}
					if ((componentData.m_Types & AllowedWaterTypes.SurfaceWater) != AllowedWaterTypes.None && bufferAccessor.Length != 0)
					{
						DynamicBuffer<Game.Objects.SubObject> dynamicBuffer = bufferAccessor[i];
						for (int j = 0; j < dynamicBuffer.Length; j++)
						{
							Entity subObject = dynamicBuffer[j].m_SubObject;
							if (m_WaterSources.HasComponent(subObject) && m_Transforms.TryGetComponent(subObject, out var componentData2))
							{
								float surfaceWaterAvailability = WaterPumpingStationAISystem.GetSurfaceWaterAvailability(componentData2.m_Position, componentData.m_Types, m_WaterSurfaceData, m_Parameters.m_SurfaceWaterPumpEffectiveDepth);
								num += Mathf.RoundToInt(surfaceWaterAvailability * (float)componentData.m_Capacity);
							}
						}
					}
				}
				else
				{
					num = componentData.m_Capacity;
				}
				reference.m_Types |= componentData.m_Types;
				reference.m_Production += math.min(num, componentData.m_Capacity);
				reference.m_MaxCapacity += componentData.m_Capacity;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct GroundWaterPumpJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<WaterPumpingStationData> m_PumpDatas;

		[ReadOnly]
		public NativeArray<GroundWater> m_GroundWaterMap;

		public NativeParallelHashMap<int2, int> m_PumpCapacityMap;

		public NativeList<int2> m_TempGroundWaterPumpCells;

		public WaterPipeParameterData m_Parameters;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<Game.Objects.Transform> nativeArray3 = chunk.GetNativeArray(ref m_TransformType);
			BufferAccessor<InstalledUpgrade> bufferAccessor = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			bool flag = nativeArray2.Length != 0;
			for (int i = 0; i < chunk.Count; i++)
			{
				if (flag && (nativeArray2[i].m_Flags & (TempFlags.Create | TempFlags.Modify | TempFlags.Upgrade)) == 0)
				{
					continue;
				}
				m_PumpDatas.TryGetComponent(nativeArray[i].m_Prefab, out var componentData);
				if (bufferAccessor.Length != 0)
				{
					UpgradeUtils.CombineStats(ref componentData, bufferAccessor[i], ref m_Prefabs, ref m_PumpDatas);
				}
				if ((componentData.m_Types & AllowedWaterTypes.Groundwater) != AllowedWaterTypes.None && GroundWaterSystem.TryGetCell(nativeArray3[i].m_Position, out var cell))
				{
					int num = Mathf.CeilToInt(math.clamp((float)GroundWaterSystem.GetGroundWater(nativeArray3[i].m_Position, m_GroundWaterMap).m_Max / m_Parameters.m_GroundwaterPumpEffectiveAmount, 0f, 1f) * (float)componentData.m_Capacity);
					if (!m_PumpCapacityMap.ContainsKey(cell))
					{
						m_PumpCapacityMap.Add(cell, num);
					}
					else
					{
						m_PumpCapacityMap[cell] += num;
					}
					if (flag)
					{
						m_TempGroundWaterPumpCells.Add(in cell);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	public struct GroundWaterReservoirResult
	{
		public int m_PumpCapacity;

		public int m_Volume;
	}

	[BurstCompile]
	public struct GroundWaterReservoirJob : IJob
	{
		[ReadOnly]
		public NativeArray<GroundWater> m_GroundWaterMap;

		[ReadOnly]
		public NativeParallelHashMap<int2, int> m_PumpCapacityMap;

		[ReadOnly]
		public NativeList<int2> m_TempGroundWaterPumpCells;

		public NativeQueue<int2> m_Queue;

		public NativeReference<GroundWaterReservoirResult> m_Result;

		public void Execute()
		{
			NativeParallelHashSet<int2> processedCells = new NativeParallelHashSet<int2>(128, Allocator.Temp);
			ref GroundWaterReservoirResult reference = ref m_Result.ValueAsRef();
			foreach (int2 item3 in m_TempGroundWaterPumpCells)
			{
				EnqueueIfUnprocessed(item3, processedCells);
			}
			int2 item;
			while (m_Queue.TryDequeue(out item))
			{
				int index = item.x + item.y * GroundWaterSystem.kTextureSize;
				GroundWater groundWater = m_GroundWaterMap[index];
				if (m_PumpCapacityMap.TryGetValue(item, out var item2))
				{
					reference.m_PumpCapacity += item2;
				}
				if (groundWater.m_Max > 500)
				{
					reference.m_Volume += groundWater.m_Max;
					EnqueueIfUnprocessed(new int2(item.x - 1, item.y), processedCells);
					EnqueueIfUnprocessed(new int2(item.x + 1, item.y), processedCells);
					EnqueueIfUnprocessed(new int2(item.x, item.y - 1), processedCells);
					EnqueueIfUnprocessed(new int2(item.x, item.y + 2), processedCells);
				}
				else if (reference.m_Volume > 0)
				{
					reference.m_Volume += groundWater.m_Max;
				}
			}
		}

		private void EnqueueIfUnprocessed(int2 cell, NativeParallelHashSet<int2> processedCells)
		{
			if (GroundWaterSystem.IsValidCell(cell) && processedCells.Add(cell))
			{
				m_Queue.Enqueue(cell);
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterPumpingStationData> __Game_Prefabs_WaterPumpingStationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Simulation.WaterSourceData> __Game_Simulation_WaterSourceData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_WaterPumpingStationData_RO_ComponentLookup = state.GetComponentLookup<WaterPumpingStationData>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Simulation_WaterSourceData_RO_ComponentLookup = state.GetComponentLookup<Game.Simulation.WaterSourceData>(isReadOnly: true);
		}
	}

	private GroundWaterSystem m_GroundWaterSystem;

	private WaterSystem m_WaterSystem;

	private TerrainSystem m_TerrainSystem;

	private EntityQuery m_ErrorQuery;

	private EntityQuery m_TempQuery;

	private EntityQuery m_PumpQuery;

	private EntityQuery m_ParameterQuery;

	private ProgressTooltip m_Capacity;

	private IntTooltip m_ReservoirUsage;

	private StringTooltip m_OverRefreshCapacityWarning;

	private StringTooltip m_AvailabilityWarning;

	private LocalizedString m_GroundWarning;

	private LocalizedString m_SurfaceWarning;

	private NativeReference<TempResult> m_TempResult;

	private NativeReference<GroundWaterReservoirResult> m_ReservoirResult;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GroundWaterSystem = base.World.GetOrCreateSystemManaged<GroundWaterSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_ErrorQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Error>());
		m_TempQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.WaterPumpingStation>(), ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<Game.Objects.Transform>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Temp>(), ComponentType.Exclude<Hidden>(), ComponentType.Exclude<Error>(), ComponentType.Exclude<Deleted>());
		m_PumpQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.WaterPumpingStation>(), ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<Game.Objects.Transform>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Hidden>(), ComponentType.Exclude<Deleted>());
		m_ParameterQuery = GetEntityQuery(ComponentType.ReadOnly<WaterPipeParameterData>());
		m_Capacity = new ProgressTooltip
		{
			path = "groundWaterCapacity",
			icon = "Media/Game/Icons/Water.svg",
			label = LocalizedString.Id("Tools.WATER_OUTPUT_LABEL"),
			unit = "volume",
			omitMax = true
		};
		m_ReservoirUsage = new IntTooltip
		{
			path = "groundWaterReservoirUsage",
			label = LocalizedString.Id("Tools.GROUND_WATER_RESERVOIR_USAGE"),
			unit = "percentage"
		};
		m_OverRefreshCapacityWarning = new StringTooltip
		{
			path = "groundWaterOverRefreshCapacityWarning",
			value = LocalizedString.Id("Tools.WARNING[OverRefreshCapacity]"),
			color = TooltipColor.Warning
		};
		m_AvailabilityWarning = new StringTooltip
		{
			path = "waterAvailabilityWarning",
			color = TooltipColor.Warning
		};
		m_GroundWarning = LocalizedString.Id("Tools.WARNING[NotEnoughGroundWater]");
		m_SurfaceWarning = LocalizedString.Id("Tools.WARNING[NotEnoughFreshWater]");
		m_TempResult = new NativeReference<TempResult>(Allocator.Persistent);
		m_ReservoirResult = new NativeReference<GroundWaterReservoirResult>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_TempResult.Dispose();
		m_ReservoirResult.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_ErrorQuery.IsEmptyIgnoreFilter || m_TempQuery.IsEmptyIgnoreFilter)
		{
			m_TempResult.Value = default(TempResult);
			m_ReservoirResult.Value = default(GroundWaterReservoirResult);
			return;
		}
		ProcessResults();
		m_TempResult.Value = default(TempResult);
		m_ReservoirResult.Value = default(GroundWaterReservoirResult);
		JobHandle dependencies;
		NativeArray<GroundWater> map = m_GroundWaterSystem.GetMap(readOnly: true, out dependencies);
		WaterPipeParameterData singleton = m_ParameterQuery.GetSingleton<WaterPipeParameterData>();
		JobHandle deps;
		JobHandle jobHandle = JobChunkExtensions.Schedule(new TempJob
		{
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SubObjectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PumpDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WaterPumpingStationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Transforms = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WaterSources = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterSourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GroundWaterMap = map,
			m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_Result = m_TempResult,
			m_Parameters = singleton
		}, m_TempQuery, JobHandle.CombineDependencies(base.Dependency, dependencies, deps));
		m_WaterSystem.AddSurfaceReader(jobHandle);
		m_TerrainSystem.AddCPUHeightReader(jobHandle);
		NativeParallelHashMap<int2, int> pumpCapacityMap = new NativeParallelHashMap<int2, int>(8, Allocator.TempJob);
		NativeList<int2> tempGroundWaterPumpCells = new NativeList<int2>(Allocator.TempJob);
		JobHandle dependsOn = JobChunkExtensions.Schedule(new GroundWaterPumpJob
		{
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PumpDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WaterPumpingStationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GroundWaterMap = map,
			m_PumpCapacityMap = pumpCapacityMap,
			m_TempGroundWaterPumpCells = tempGroundWaterPumpCells,
			m_Parameters = singleton
		}, m_PumpQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
		GroundWaterReservoirJob jobData = new GroundWaterReservoirJob
		{
			m_GroundWaterMap = map,
			m_PumpCapacityMap = pumpCapacityMap,
			m_TempGroundWaterPumpCells = tempGroundWaterPumpCells,
			m_Queue = new NativeQueue<int2>(Allocator.TempJob),
			m_Result = m_ReservoirResult
		};
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData, dependsOn);
		jobData.m_Queue.Dispose(jobHandle2);
		pumpCapacityMap.Dispose(jobHandle2);
		tempGroundWaterPumpCells.Dispose(jobHandle2);
		base.Dependency = JobHandle.CombineDependencies(jobHandle, jobHandle2);
		m_GroundWaterSystem.AddReader(base.Dependency);
	}

	private void ProcessResults()
	{
		TempResult value = m_TempResult.Value;
		GroundWaterReservoirResult value2 = m_ReservoirResult.Value;
		if (value.m_MaxCapacity <= 0)
		{
			return;
		}
		if ((value.m_Types & AllowedWaterTypes.Groundwater) != AllowedWaterTypes.None)
		{
			ProcessProduction(value);
			if (value2.m_Volume > 0)
			{
				ProcessReservoir(value2);
			}
			ProcessAvailabilityWarning(value, m_GroundWarning);
		}
		else if ((value.m_Types & AllowedWaterTypes.SurfaceWater) != AllowedWaterTypes.None)
		{
			ProcessProduction(value);
			ProcessAvailabilityWarning(value, m_SurfaceWarning);
		}
		else
		{
			ProcessProduction(value);
		}
	}

	private void ProcessReservoir(GroundWaterReservoirResult reservoir)
	{
		WaterPipeParameterData singleton = m_ParameterQuery.GetSingleton<WaterPipeParameterData>();
		float num = singleton.m_GroundwaterReplenish / singleton.m_GroundwaterUsageMultiplier * (float)reservoir.m_Volume;
		float num2 = ((num > 0f && reservoir.m_PumpCapacity > 0) ? math.clamp(100f * (float)reservoir.m_PumpCapacity / num, 1f, 999f) : 0f);
		m_ReservoirUsage.value = Mathf.RoundToInt(num2);
		m_ReservoirUsage.color = ((num2 > 100f) ? TooltipColor.Warning : TooltipColor.Info);
		AddMouseTooltip(m_ReservoirUsage);
		if (num2 > 100f)
		{
			AddMouseTooltip(m_OverRefreshCapacityWarning);
		}
	}

	private void ProcessProduction(TempResult temp)
	{
		if (temp.m_Production > 0)
		{
			m_Capacity.value = temp.m_Production;
			m_Capacity.max = temp.m_MaxCapacity;
			ProgressTooltip.SetCapacityColor(m_Capacity);
			AddMouseTooltip(m_Capacity);
		}
	}

	private void ProcessAvailabilityWarning(TempResult temp, LocalizedString warningText)
	{
		if (temp.m_Production > 0 && (float)temp.m_Production < (float)temp.m_MaxCapacity * 0.75f)
		{
			m_AvailabilityWarning.value = warningText;
			AddMouseTooltip(m_AvailabilityWarning);
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
	public TempWaterPumpingTooltipSystem()
	{
	}
}
