using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class RenterSystem : GameSystemBase
{
	[BurstCompile]
	private struct RenterJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> m_PropertyRenterType;

		[ReadOnly]
		public ComponentTypeHandle<TouristHousehold> m_TouristHouseholdType;

		[ReadOnly]
		public ComponentTypeHandle<HomelessHousehold> m_HomelessHouseholdType;

		[ReadOnly]
		public ComponentLookup<CityServiceUpkeep> m_ServiceBuildings;

		[ReadOnly]
		public ComponentLookup<CompanyData> m_CompanyDatas;

		[ReadOnly]
		public ComponentLookup<Park> m_Parks;

		[ReadOnly]
		public ComponentLookup<Abandoned> m_Abandoneds;

		public BufferLookup<Renter> m_Renters;

		public EntityCommandBuffer m_CommandBuffer;

		private void AddRenter(DynamicBuffer<Renter> renterBuf, Entity renterEntity)
		{
			bool flag = false;
			for (int i = 0; i < renterBuf.Length; i++)
			{
				if (renterBuf[i].m_Renter == renterEntity)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				renterBuf.Add(new Renter
				{
					m_Renter = renterEntity
				});
			}
		}

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PropertyRenter> nativeArray2 = chunk.GetNativeArray(ref m_PropertyRenterType);
			NativeArray<TouristHousehold> nativeArray3 = chunk.GetNativeArray(ref m_TouristHouseholdType);
			NativeArray<HomelessHousehold> nativeArray4 = chunk.GetNativeArray(ref m_HomelessHouseholdType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity entity = nativeArray[i];
				PropertyRenter propertyRenter = nativeArray2[i];
				if (m_Renters.TryGetBuffer(propertyRenter.m_Property, out var bufferData))
				{
					if (m_CompanyDatas.HasComponent(entity))
					{
						bool flag = false;
						for (int j = 0; j < bufferData.Length; j++)
						{
							if (m_CompanyDatas.HasComponent(bufferData[j].m_Renter))
							{
								UnityEngine.Debug.LogWarning($"delete duplicate company:{entity.Index}");
								m_CommandBuffer.AddComponent<Deleted>(entity);
								flag = true;
							}
						}
						if (!flag)
						{
							AddRenter(bufferData, entity);
						}
					}
					else
					{
						if (!chunk.Has<HomelessHousehold>() && BuildingUtils.IsHomelessShelterBuilding(propertyRenter.m_Property, ref m_Parks, ref m_Abandoneds))
						{
							m_CommandBuffer.AddComponent(entity, new HomelessHousehold
							{
								m_TempHome = propertyRenter.m_Property
							});
						}
						AddRenter(bufferData, entity);
					}
				}
				else if (!m_ServiceBuildings.HasComponent(entity))
				{
					m_CommandBuffer.RemoveComponent<PropertyRenter>(entity);
				}
			}
			for (int k = 0; k < nativeArray3.Length; k++)
			{
				Entity renterEntity = nativeArray[k];
				TouristHousehold touristHousehold = nativeArray3[k];
				if (m_Renters.TryGetBuffer(touristHousehold.m_Hotel, out var bufferData2))
				{
					AddRenter(bufferData2, renterEntity);
				}
			}
			for (int l = 0; l < nativeArray4.Length; l++)
			{
				Entity renterEntity2 = nativeArray[l];
				HomelessHousehold homelessHousehold = nativeArray4[l];
				if (m_Renters.TryGetBuffer(homelessHousehold.m_TempHome, out var bufferData3))
				{
					AddRenter(bufferData3, renterEntity2);
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
		public ComponentTypeHandle<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TouristHousehold> __Game_Citizens_TouristHousehold_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HomelessHousehold> __Game_Citizens_HomelessHousehold_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<CityServiceUpkeep> __Game_City_CityServiceUpkeep_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CompanyData> __Game_Companies_CompanyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Park> __Game_Buildings_Park_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Abandoned> __Game_Buildings_Abandoned_RO_ComponentLookup;

		public BufferLookup<Renter> __Game_Buildings_Renter_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PropertyRenter>(isReadOnly: true);
			__Game_Citizens_TouristHousehold_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TouristHousehold>(isReadOnly: true);
			__Game_Citizens_HomelessHousehold_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HomelessHousehold>(isReadOnly: true);
			__Game_City_CityServiceUpkeep_RO_ComponentLookup = state.GetComponentLookup<CityServiceUpkeep>(isReadOnly: true);
			__Game_Companies_CompanyData_RO_ComponentLookup = state.GetComponentLookup<CompanyData>(isReadOnly: true);
			__Game_Buildings_Park_RO_ComponentLookup = state.GetComponentLookup<Park>(isReadOnly: true);
			__Game_Buildings_Abandoned_RO_ComponentLookup = state.GetComponentLookup<Abandoned>(isReadOnly: true);
			__Game_Buildings_Renter_RW_BufferLookup = state.GetBufferLookup<Renter>();
		}
	}

	private DeserializationBarrier m_DeserializationBarrier;

	private EntityQuery m_Query;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_DeserializationBarrier = base.World.GetOrCreateSystemManaged<DeserializationBarrier>();
		m_Query = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<PropertyRenter>(),
				ComponentType.ReadOnly<TouristHousehold>(),
				ComponentType.ReadOnly<HomelessHousehold>()
			}
		});
		RequireForUpdate(m_Query);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		RenterJob jobData = new RenterJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PropertyRenterType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TouristHouseholdType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HomelessHouseholdType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_HomelessHousehold_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceBuildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_CityServiceUpkeep_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CompanyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_CompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Parks = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Park_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Abandoneds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Renters = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RW_BufferLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_DeserializationBarrier.CreateCommandBuffer()
		};
		base.Dependency = JobChunkExtensions.Schedule(jobData, m_Query, base.Dependency);
		m_DeserializationBarrier.AddJobHandleForProducer(base.Dependency);
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
	public RenterSystem()
	{
	}
}
