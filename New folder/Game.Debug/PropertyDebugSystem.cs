using System.Runtime.CompilerServices;
using Colossal;
using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Debug;

[CompilerGenerated]
public class PropertyDebugSystem : BaseDebugSystem
{
	private struct PropertyGizmoJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<CrimeProducer> m_CrimeProducerType;

		[ReadOnly]
		public BufferTypeHandle<Renter> m_RenterBufType;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_Transforms;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

		public GizmoBatcher m_GizmoBatcher;

		[ReadOnly]
		public bool m_ResidentialAvailableOption;

		[ReadOnly]
		public bool m_ResidentialCrimeOption;

		public bool m_CrimeOption;

		private void Draw(Entity building, float value, int offset)
		{
			if (m_Transforms.HasComponent(building))
			{
				Game.Objects.Transform transform = m_Transforms[building];
				float3 position = transform.m_Position;
				position.y += value / 2f + 10f;
				position += 5f * (float)offset * math.rotate(transform.m_Rotation.value, new float3(1f, 0f, 0f));
				UnityEngine.Color color = ((value > 0f) ? UnityEngine.Color.red : UnityEngine.Color.white);
				m_GizmoBatcher.DrawWireCube(position, new float3(5f, value, 5f), color);
			}
		}

		private void Draw(Entity building, float value, int offset, UnityEngine.Color color)
		{
			value /= 500f;
			if (m_Transforms.HasComponent(building))
			{
				Game.Objects.Transform transform = m_Transforms[building];
				float3 position = transform.m_Position;
				position.y += value / 2f + 10f;
				position += 5f * (float)offset * math.rotate(transform.m_Rotation.value, new float3(1f, 0f, 0f));
				m_GizmoBatcher.DrawWireCube(position, new float3(5f, value, 5f), color);
			}
		}

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			if (!m_CrimeOption)
			{
				return;
			}
			NativeArray<CrimeProducer> nativeArray2 = chunk.GetNativeArray(ref m_CrimeProducerType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<Renter> bufferAccessor = chunk.GetBufferAccessor(ref m_RenterBufType);
			if (nativeArray.Length <= 0)
			{
				return;
			}
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (m_ResidentialCrimeOption && nativeArray2.Length > 0)
				{
					CrimeProducer crimeProducer = nativeArray2[i];
					Draw(nativeArray[i], crimeProducer.m_Crime, 0);
				}
				else if (m_ResidentialAvailableOption && bufferAccessor.Length > 0 && m_BuildingPropertyDatas.HasComponent(nativeArray3[i].m_Prefab))
				{
					BuildingPropertyData buildingPropertyData = m_BuildingPropertyDatas[nativeArray3[i].m_Prefab];
					if (buildingPropertyData.m_ResidentialProperties > bufferAccessor[i].Length)
					{
						Draw(nativeArray[i], buildingPropertyData.m_ResidentialProperties - bufferAccessor[i].Length, 0, UnityEngine.Color.green);
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
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CrimeProducer> __Game_Buildings_CrimeProducer_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Renter> __Game_Buildings_Renter_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_CrimeProducer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CrimeProducer>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
		}
	}

	private EntityQuery m_PropertyQuery;

	private GizmosSystem m_GizmosSystem;

	private Option m_ResidentialAvailableOption;

	private Option m_ResidentialCrimeOption;

	private Option m_CrimeOption;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_PropertyQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Building>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<CrimeProducer>(),
				ComponentType.ReadOnly<Renter>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Hidden>()
			}
		});
		m_ResidentialCrimeOption = AddOption("Residential Crime", defaultEnabled: false);
		m_ResidentialAvailableOption = AddOption("Residential Available", defaultEnabled: false);
		m_CrimeOption = AddOption("Crime Accumulation", defaultEnabled: true);
		RequireForUpdate(m_PropertyQuery);
		base.Enabled = false;
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		JobHandle dependencies;
		PropertyGizmoJob jobData = new PropertyGizmoJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CrimeProducerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_CrimeProducer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RenterBufType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_Transforms = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingPropertyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CrimeOption = m_CrimeOption.enabled,
			m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies),
			m_ResidentialAvailableOption = m_ResidentialAvailableOption.enabled,
			m_ResidentialCrimeOption = m_ResidentialCrimeOption.enabled
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_PropertyQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
		m_GizmosSystem.AddGizmosBatcherWriter(base.Dependency);
		return base.Dependency;
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
	public PropertyDebugSystem()
	{
	}
}
