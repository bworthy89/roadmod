using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.City;
using Game.Common;
using Game.Serialization;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class VehicleCapacitySystem : GameSystemBase, IPostDeserialize
{
	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<DeliveryTruckData> __Game_Prefabs_DeliveryTruckData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CarTrailerData> __Game_Prefabs_CarTrailerData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CarTractorData> __Game_Prefabs_CarTractorData_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_DeliveryTruckData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<DeliveryTruckData>(isReadOnly: true);
			__Game_Prefabs_CarTrailerData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CarTrailerData>(isReadOnly: true);
			__Game_Prefabs_CarTractorData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CarTractorData>(isReadOnly: true);
		}
	}

	private CityConfigurationSystem m_CityConfigurationSystem;

	private EntityQuery m_UpdatedQuery;

	private EntityQuery m_DeliveryTruckQuery;

	private NativeList<DeliveryTruckSelectItem> m_DeliveryTruckItems;

	private JobHandle m_WriteDependency;

	private VehicleSelectRequirementData m_VehicleSelectRequirementData;

	private bool m_RequireUpdate;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_VehicleSelectRequirementData = new VehicleSelectRequirementData(this);
		m_UpdatedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<VehicleData>(),
				ComponentType.ReadOnly<PrefabData>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_DeliveryTruckQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[4]
			{
				ComponentType.ReadOnly<DeliveryTruckData>(),
				ComponentType.ReadOnly<CarData>(),
				ComponentType.ReadOnly<ObjectData>(),
				ComponentType.ReadOnly<PrefabData>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Locked>() }
		});
		m_DeliveryTruckItems = new NativeList<DeliveryTruckSelectItem>(10, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_DeliveryTruckItems.Dispose();
		base.OnDestroy();
	}

	public void PostDeserialize(Context context)
	{
		if (context.purpose == Purpose.NewGame || context.purpose == Purpose.LoadGame)
		{
			m_RequireUpdate = true;
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_RequireUpdate || !m_UpdatedQuery.IsEmptyIgnoreFilter)
		{
			m_RequireUpdate = false;
			JobHandle outJobHandle;
			NativeList<ArchetypeChunk> prefabChunks = m_DeliveryTruckQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
			m_VehicleSelectRequirementData.Update(this, m_CityConfigurationSystem);
			JobHandle jobHandle = IJobExtensions.Schedule(new UpdateDeliveryTruckSelectJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_DeliveryTruckDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_DeliveryTruckData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CarTrailerDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_CarTrailerData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CarTractorDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_CarTractorData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabChunks = prefabChunks,
				m_RequirementData = m_VehicleSelectRequirementData,
				m_DeliveryTruckItems = m_DeliveryTruckItems
			}, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
			prefabChunks.Dispose(jobHandle);
			m_WriteDependency = jobHandle;
			base.Dependency = jobHandle;
		}
	}

	public DeliveryTruckSelectData GetDeliveryTruckSelectData()
	{
		m_WriteDependency.Complete();
		return new DeliveryTruckSelectData(m_DeliveryTruckItems.AsArray());
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
	public VehicleCapacitySystem()
	{
	}
}
