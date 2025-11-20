using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Prefabs;
using Game.Serialization;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Policies;

[CompilerGenerated]
public class DefaultPoliciesSystem : GameSystemBase, IPostDeserialize
{
	[BurstCompile]
	private struct AddDefaultPoliciesJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public BufferTypeHandle<Policy> m_PolicyType;

		[ReadOnly]
		public ComponentLookup<PolicySliderData> m_PolicySliderData;

		[ReadOnly]
		public BufferLookup<DefaultPolicyData> m_DefaultPolicyData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<Policy> bufferAccessor = chunk.GetBufferAccessor(ref m_PolicyType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				PrefabRef prefabRef = nativeArray[i];
				if (!m_DefaultPolicyData.HasBuffer(prefabRef.m_Prefab))
				{
					continue;
				}
				DynamicBuffer<DefaultPolicyData> dynamicBuffer = m_DefaultPolicyData[prefabRef.m_Prefab];
				DynamicBuffer<Policy> dynamicBuffer2 = bufferAccessor[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					DefaultPolicyData defaultPolicyData = dynamicBuffer[j];
					if (m_PolicySliderData.HasComponent(defaultPolicyData.m_Policy))
					{
						PolicySliderData policySliderData = m_PolicySliderData[defaultPolicyData.m_Policy];
						dynamicBuffer2.Add(new Policy(defaultPolicyData.m_Policy, PolicyFlags.Active, policySliderData.m_Default));
					}
					else
					{
						dynamicBuffer2.Add(new Policy(defaultPolicyData.m_Policy, PolicyFlags.Active, 0f));
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
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public BufferTypeHandle<Policy> __Game_Policies_Policy_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PolicySliderData> __Game_Prefabs_PolicySliderData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<DefaultPolicyData> __Game_Prefabs_DefaultPolicyData_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Policies_Policy_RW_BufferTypeHandle = state.GetBufferTypeHandle<Policy>();
			__Game_Prefabs_PolicySliderData_RO_ComponentLookup = state.GetComponentLookup<PolicySliderData>(isReadOnly: true);
			__Game_Prefabs_DefaultPolicyData_RO_BufferLookup = state.GetBufferLookup<DefaultPolicyData>(isReadOnly: true);
		}
	}

	private CitySystem m_CitySystem;

	private EntityQuery m_CreatedQuery;

	private EntityQuery m_CityConfigurationQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_CreatedQuery = GetEntityQuery(ComponentType.ReadOnly<Created>(), ComponentType.ReadWrite<Policy>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>());
		m_CityConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<ServiceFeeParameterData>());
		RequireForUpdate(m_CreatedQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		AddDefaultPoliciesJob jobData = new AddDefaultPoliciesJob
		{
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PolicyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Policies_Policy_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_PolicySliderData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PolicySliderData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DefaultPolicyData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_DefaultPolicyData_RO_BufferLookup, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_CreatedQuery, base.Dependency);
	}

	public void PostDeserialize(Context context)
	{
		if (context.purpose != Purpose.NewGame && (context.purpose != Purpose.LoadGame || !(context.version < Version.taxiFee)))
		{
			return;
		}
		Entity singletonEntity = m_CityConfigurationQuery.GetSingletonEntity();
		DynamicBuffer<Policy> buffer = base.EntityManager.GetBuffer<Policy>(m_CitySystem.City);
		if (!base.EntityManager.TryGetBuffer(singletonEntity, isReadOnly: true, out DynamicBuffer<DefaultPolicyData> buffer2))
		{
			return;
		}
		for (int i = 0; i < buffer2.Length; i++)
		{
			DefaultPolicyData defaultPolicyData = buffer2[i];
			if (base.EntityManager.HasComponent<PolicySliderData>(defaultPolicyData.m_Policy))
			{
				PolicySliderData componentData = base.EntityManager.GetComponentData<PolicySliderData>(defaultPolicyData.m_Policy);
				buffer.Add(new Policy(defaultPolicyData.m_Policy, PolicyFlags.Active, componentData.m_Default));
			}
			else
			{
				buffer.Add(new Policy(defaultPolicyData.m_Policy, PolicyFlags.Active, 0f));
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
	public DefaultPoliciesSystem()
	{
	}
}
