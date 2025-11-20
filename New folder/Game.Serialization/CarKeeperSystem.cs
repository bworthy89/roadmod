using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Citizens;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class CarKeeperSystem : GameSystemBase
{
	[BurstCompile]
	private struct CarKeeperJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PersonalCar> m_PersonalCarType;

		[ReadOnly]
		public ComponentTypeHandle<Bicycle> m_BicycleType;

		public ComponentLookup<CarKeeper> m_CarKeeperData;

		public ComponentLookup<BicycleOwner> m_BicycleOwnerData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PersonalCar> nativeArray2 = chunk.GetNativeArray(ref m_PersonalCarType);
			bool flag = chunk.Has(ref m_BicycleType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity entity = nativeArray[i];
				PersonalCar personalCar = nativeArray2[i];
				CarKeeper component2;
				if (flag)
				{
					if (m_BicycleOwnerData.TryGetEnabledComponent(personalCar.m_Keeper, out var component))
					{
						component.m_Bicycle = entity;
						m_BicycleOwnerData[personalCar.m_Keeper] = component;
					}
				}
				else if (m_CarKeeperData.TryGetEnabledComponent(personalCar.m_Keeper, out component2))
				{
					component2.m_Car = entity;
					m_CarKeeperData[personalCar.m_Keeper] = component2;
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct NoCarJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public ComponentTypeHandle<CarKeeper> m_CarKeeperType;

		[ReadOnly]
		public ComponentLookup<PersonalCar> m_PersonalCars;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<CarKeeper> nativeArray2 = chunk.GetNativeArray(ref m_CarKeeperType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				_ = nativeArray[i];
				if (chunk.IsComponentEnabled(ref m_CarKeeperType, i))
				{
					CarKeeper carKeeper = nativeArray2[i];
					if (!m_PersonalCars.HasComponent(carKeeper.m_Car))
					{
						chunk.SetComponentEnabled(ref m_CarKeeperType, i, value: false);
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
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PersonalCar> __Game_Vehicles_PersonalCar_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Bicycle> __Game_Vehicles_Bicycle_RO_ComponentTypeHandle;

		public ComponentLookup<CarKeeper> __Game_Citizens_CarKeeper_RW_ComponentLookup;

		public ComponentLookup<BicycleOwner> __Game_Citizens_BicycleOwner_RW_ComponentLookup;

		public ComponentTypeHandle<CarKeeper> __Game_Citizens_CarKeeper_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PersonalCar> __Game_Vehicles_PersonalCar_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Vehicles_PersonalCar_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PersonalCar>(isReadOnly: true);
			__Game_Vehicles_Bicycle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Bicycle>(isReadOnly: true);
			__Game_Citizens_CarKeeper_RW_ComponentLookup = state.GetComponentLookup<CarKeeper>();
			__Game_Citizens_BicycleOwner_RW_ComponentLookup = state.GetComponentLookup<BicycleOwner>();
			__Game_Citizens_CarKeeper_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CarKeeper>();
			__Game_Vehicles_PersonalCar_RO_ComponentLookup = state.GetComponentLookup<PersonalCar>(isReadOnly: true);
		}
	}

	private EntityQuery m_Query;

	private EntityQuery m_KeeperQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_Query = GetEntityQuery(ComponentType.ReadOnly<PersonalCar>());
		m_KeeperQuery = GetEntityQuery(ComponentType.ReadOnly<CarKeeper>());
		RequireForUpdate(m_Query);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependsOn = JobChunkExtensions.Schedule(new CarKeeperJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PersonalCarType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_PersonalCar_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BicycleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Bicycle_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarKeeperData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CarKeeper_RW_ComponentLookup, ref base.CheckedStateRef),
			m_BicycleOwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_BicycleOwner_RW_ComponentLookup, ref base.CheckedStateRef)
		}, m_Query, base.Dependency);
		NoCarJob jobData = new NoCarJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CarKeeperType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CarKeeper_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PersonalCars = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PersonalCar_RO_ComponentLookup, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.Schedule(jobData, m_KeeperQuery, dependsOn);
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
	public CarKeeperSystem()
	{
	}
}
