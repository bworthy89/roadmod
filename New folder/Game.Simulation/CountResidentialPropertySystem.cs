using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Debug;
using Game.Prefabs;
using Game.Tools;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CountResidentialPropertySystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	public struct ResidentialPropertyData : IAccumulable<ResidentialPropertyData>, ISerializable
	{
		public int3 m_FreeProperties;

		public int3 m_TotalProperties;

		public int m_FreeShelterCapacity;

		public int m_TotalShelterCapacity;

		public void Accumulate(ResidentialPropertyData other)
		{
			m_FreeProperties += other.m_FreeProperties;
			m_TotalProperties += other.m_TotalProperties;
			m_FreeShelterCapacity += other.m_FreeShelterCapacity;
			m_TotalShelterCapacity += other.m_TotalShelterCapacity;
		}

		public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
		{
			int3 value = m_FreeProperties;
			writer.Write(value);
			int3 value2 = m_TotalProperties;
			writer.Write(value2);
			int value3 = m_FreeShelterCapacity;
			writer.Write(value3);
			int value4 = m_TotalShelterCapacity;
			writer.Write(value4);
		}

		public void Deserialize<TReader>(TReader reader) where TReader : IReader
		{
			ref int3 value = ref m_FreeProperties;
			reader.Read(out value);
			ref int3 value2 = ref m_TotalProperties;
			reader.Read(out value2);
			if (reader.context.format.Has(FormatTags.HomelessAndWorkerFix))
			{
				ref int value3 = ref m_FreeShelterCapacity;
				reader.Read(out value3);
				ref int value4 = ref m_TotalShelterCapacity;
				reader.Read(out value4);
			}
		}
	}

	[BurstCompile]
	private struct CountResidentialPropertyJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<Renter> m_RenterType;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDatas;

		[ReadOnly]
		public ComponentLookup<ZoneData> m_ZoneDatas;

		[ReadOnly]
		public ComponentLookup<ZonePropertiesData> m_ZonePropertiesDatas;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingDatas;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

		[ReadOnly]
		public ComponentLookup<Household> m_Households;

		public NativeAccumulator<ResidentialPropertyData>.ParallelWriter m_ResidentialPropertyData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			ResidentialPropertyData value = default(ResidentialPropertyData);
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<Renter> bufferAccessor = chunk.GetBufferAccessor(ref m_RenterType);
			if (chunk.Has<Abandoned>() || chunk.Has<Game.Buildings.Park>())
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					int shelterHomelessCapacity = BuildingUtils.GetShelterHomelessCapacity(nativeArray[i].m_Prefab, ref m_BuildingDatas, ref m_BuildingPropertyDatas);
					value.m_TotalShelterCapacity += shelterHomelessCapacity;
					DynamicBuffer<Renter> dynamicBuffer = bufferAccessor[i];
					int num = 0;
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						if (m_Households.HasComponent(dynamicBuffer[j].m_Renter))
						{
							num++;
						}
					}
					value.m_FreeShelterCapacity = math.max(0, shelterHomelessCapacity - num);
				}
			}
			else
			{
				for (int k = 0; k < nativeArray.Length; k++)
				{
					Entity prefab = nativeArray[k].m_Prefab;
					if (!m_BuildingPropertyDatas.HasComponent(prefab))
					{
						continue;
					}
					SpawnableBuildingData spawnableBuildingData = m_SpawnableBuildingDatas[prefab];
					if ((!m_ZoneDatas.TryGetComponent(spawnableBuildingData.m_ZonePrefab, out var componentData) && componentData.m_AreaType != AreaType.Residential) || !m_ZonePropertiesDatas.TryGetComponent(spawnableBuildingData.m_ZonePrefab, out var componentData2))
					{
						continue;
					}
					ZoneDensity zoneDensity = PropertyUtils.GetZoneDensity(componentData, componentData2);
					BuildingPropertyData propertyData = m_BuildingPropertyDatas[prefab];
					DynamicBuffer<Renter> dynamicBuffer2 = bufferAccessor[k];
					int residentialProperties = PropertyUtils.GetResidentialProperties(propertyData);
					int num2 = 0;
					for (int l = 0; l < dynamicBuffer2.Length; l++)
					{
						if (m_Households.HasComponent(dynamicBuffer2[l].m_Renter))
						{
							num2++;
						}
					}
					switch (zoneDensity)
					{
					case ZoneDensity.Low:
						value.m_TotalProperties.x += residentialProperties;
						if (chunk.Has<PropertyOnMarket>() || chunk.Has<PropertyToBeOnMarket>())
						{
							value.m_FreeProperties.x += residentialProperties - num2;
						}
						break;
					case ZoneDensity.Medium:
						value.m_TotalProperties.y += residentialProperties;
						if (chunk.Has<PropertyOnMarket>() || chunk.Has<PropertyToBeOnMarket>())
						{
							value.m_FreeProperties.y += residentialProperties - num2;
						}
						break;
					default:
						value.m_TotalProperties.z += residentialProperties;
						if (chunk.Has<PropertyOnMarket>() || chunk.Has<PropertyToBeOnMarket>())
						{
							value.m_FreeProperties.z += residentialProperties - num2;
						}
						break;
					}
				}
			}
			m_ResidentialPropertyData.Accumulate(value);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Renter> __Game_Buildings_Renter_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZonePropertiesData> __Game_Prefabs_ZonePropertiesData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_ZonePropertiesData_RO_ComponentLookup = state.GetComponentLookup<ZonePropertiesData>(isReadOnly: true);
			__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
			__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
		}
	}

	private NativeAccumulator<ResidentialPropertyData> m_ResidentialPropertyData;

	private ResidentialPropertyData m_LastResidentialPropertyData;

	private EntityQuery m_ResidentialPropertyQuery;

	private bool m_WasReset;

	private TypeHandle __TypeHandle;

	[DebugWatchValue]
	public int3 FreeProperties => m_LastResidentialPropertyData.m_FreeProperties;

	[DebugWatchValue]
	public int3 TotalProperties => m_LastResidentialPropertyData.m_TotalProperties;

	[DebugWatchValue]
	public int FreeShelterCapacity => m_LastResidentialPropertyData.m_FreeShelterCapacity;

	[DebugWatchValue]
	public int TotalShelterCapacity => m_LastResidentialPropertyData.m_TotalShelterCapacity;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public ResidentialPropertyData GetResidentialPropertyData()
	{
		return m_LastResidentialPropertyData;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ResidentialPropertyQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Building>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Abandoned>(),
				ComponentType.ReadOnly<Game.Buildings.Park>()
			},
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Condemned>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<ResidentialProperty>() },
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Condemned>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_ResidentialPropertyData = new NativeAccumulator<ResidentialPropertyData>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_ResidentialPropertyData.Dispose();
		base.OnDestroy();
	}

	private void Reset()
	{
		if (!m_WasReset)
		{
			m_LastResidentialPropertyData = default(ResidentialPropertyData);
			m_WasReset = true;
		}
	}

	public void SetDefaults(Context context)
	{
		Reset();
		m_ResidentialPropertyData.Clear();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_ResidentialPropertyQuery.IsEmptyIgnoreFilter)
		{
			Reset();
			return;
		}
		m_WasReset = false;
		m_LastResidentialPropertyData = m_ResidentialPropertyData.GetResult();
		m_ResidentialPropertyData.Clear();
		CountResidentialPropertyJob jobData = new CountResidentialPropertyJob
		{
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RenterType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_SpawnableBuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ZonePropertiesDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZonePropertiesData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ZoneDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingPropertyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Households = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResidentialPropertyData = m_ResidentialPropertyData.AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_ResidentialPropertyQuery, base.Dependency);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_LastResidentialPropertyData);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.homelessFix)
		{
			ref ResidentialPropertyData value = ref m_LastResidentialPropertyData;
			reader.Read(out value);
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
	public CountResidentialPropertySystem()
	{
	}
}
