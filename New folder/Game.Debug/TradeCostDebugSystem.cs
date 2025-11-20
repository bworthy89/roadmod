using System.Runtime.CompilerServices;
using Colossal;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Economy;
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
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace Game.Debug;

[CompilerGenerated]
public class TradeCostDebugSystem : BaseDebugSystem
{
	private struct TradeCostGizmoJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> m_RenterType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public BufferTypeHandle<TradeCost> m_TradeCostType;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_Transforms;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> m_StorageCompanyDatas;

		public GizmoBatcher m_GizmoBatcher;

		public Resource m_Resource;

		public bool m_StorageOption;

		public bool m_CompanyOption;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<TradeCost> bufferAccessor = chunk.GetBufferAccessor(ref m_TradeCostType);
			NativeArray<PropertyRenter> nativeArray = chunk.GetNativeArray(ref m_RenterType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabType);
			NativeArray<Entity> nativeArray3 = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<TradeCost> dynamicBuffer = bufferAccessor[i];
				Entity entity = nativeArray3[i];
				if (nativeArray.Length > 0)
				{
					entity = nativeArray[i].m_Property;
					if (!m_Transforms.HasComponent(entity))
					{
						UnityEngine.Debug.Log($"{nativeArray3[i].Index} renting {entity.Index} without transform");
					}
				}
				Entity prefab = nativeArray2[i].m_Prefab;
				if (!m_Transforms.HasComponent(entity) || ((!m_StorageOption || !m_StorageCompanyDatas.HasComponent(prefab)) && (!m_CompanyOption || m_StorageCompanyDatas.HasComponent(prefab))))
				{
					continue;
				}
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					if (dynamicBuffer[j].m_Resource == m_Resource)
					{
						TradeCost tradeCost = dynamicBuffer[j];
						Game.Objects.Transform transform = m_Transforms[entity];
						float3 position = transform.m_Position;
						float3 center = position + 5f * math.rotate(transform.m_Rotation.value, new float3(1f, 0f, 0f));
						position.y += 10f * tradeCost.m_BuyCost / 2f;
						center.y += 10f * tradeCost.m_SellCost / 2f;
						UnityEngine.Color color = UnityEngine.Color.Lerp(UnityEngine.Color.green, UnityEngine.Color.red, 0.1f * tradeCost.m_BuyCost);
						UnityEngine.Color color2 = UnityEngine.Color.Lerp(UnityEngine.Color.green, UnityEngine.Color.red, 0.1f * tradeCost.m_SellCost);
						m_GizmoBatcher.DrawWireCube(position, new float3(5f, 10f * tradeCost.m_BuyCost, 5f), color);
						m_GizmoBatcher.DrawWireCube(center, new float3(5f, 10f * tradeCost.m_SellCost, 5f), color2);
						break;
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
		public BufferTypeHandle<TradeCost> __Game_Companies_TradeCost_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> __Game_Prefabs_StorageCompanyData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Companies_TradeCost_RO_BufferTypeHandle = state.GetBufferTypeHandle<TradeCost>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PropertyRenter>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Prefabs_StorageCompanyData_RO_ComponentLookup = state.GetComponentLookup<StorageCompanyData>(isReadOnly: true);
		}
	}

	private EntityQuery m_StorageGroup;

	private GizmosSystem m_GizmosSystem;

	private Resource m_SelectedResource;

	private Option m_StorageOption;

	private Option m_CompanyOption;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_StorageOption = AddOption("Warehouses", defaultEnabled: true);
		m_CompanyOption = AddOption("Companies", defaultEnabled: false);
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_StorageGroup = GetEntityQuery(ComponentType.ReadOnly<TradeCost>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Hidden>());
		base.Enabled = false;
		m_SelectedResource = Resource.Grain;
	}

	public override void OnEnabled(DebugUI.Container container)
	{
		container.children.Add(new DebugUI.EnumField
		{
			displayName = "Resource",
			getter = () => EconomyUtils.GetResourceIndex(m_SelectedResource) + 1,
			setter = delegate(int value)
			{
				m_SelectedResource = EconomyUtils.GetResource(value - 1);
			},
			autoEnum = typeof(ResourceInEditor),
			getIndex = () => EconomyUtils.GetResourceIndex(m_SelectedResource) + 1,
			setIndex = delegate(int value)
			{
				m_SelectedResource = EconomyUtils.GetResource(value - 1);
			}
		});
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		if (m_StorageGroup.IsEmptyIgnoreFilter)
		{
			return inputDeps;
		}
		JobHandle dependencies;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new TradeCostGizmoJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TradeCostType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Companies_TradeCost_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_RenterType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Transforms = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StorageCompanyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StorageCompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies),
			m_Resource = m_SelectedResource,
			m_StorageOption = m_StorageOption.enabled,
			m_CompanyOption = m_CompanyOption.enabled
		}, m_StorageGroup, JobHandle.CombineDependencies(inputDeps, dependencies));
		m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
		return jobHandle;
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
	public TradeCostDebugSystem()
	{
	}
}
