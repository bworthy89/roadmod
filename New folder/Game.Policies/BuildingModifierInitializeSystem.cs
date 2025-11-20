using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Policies;

[CompilerGenerated]
public class BuildingModifierInitializeSystem : GameSystemBase
{
	[BurstCompile]
	private struct InitializeBuildingModifiersJob : IJobChunk
	{
		[ReadOnly]
		public BuildingModifierRefreshData m_BuildingModifierRefreshData;

		[ReadOnly]
		public BufferTypeHandle<Policy> m_PolicyType;

		public ComponentTypeHandle<Building> m_BuildingType;

		public BufferTypeHandle<BuildingModifier> m_BuildingModifierType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Building> nativeArray = chunk.GetNativeArray(ref m_BuildingType);
			BufferAccessor<BuildingModifier> bufferAccessor = chunk.GetBufferAccessor(ref m_BuildingModifierType);
			BufferAccessor<Policy> bufferAccessor2 = chunk.GetBufferAccessor(ref m_PolicyType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				DynamicBuffer<Policy> policies = bufferAccessor2[i];
				if (policies.Length != 0)
				{
					Building building = nativeArray[i];
					m_BuildingModifierRefreshData.RefreshBuildingOptions(ref building, policies);
					nativeArray[i] = building;
					if (bufferAccessor.Length != 0)
					{
						DynamicBuffer<BuildingModifier> modifiers = bufferAccessor[i];
						m_BuildingModifierRefreshData.RefreshBuildingModifiers(modifiers, policies);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	public struct BuildingModifierRefreshData
	{
		public ComponentLookup<PolicySliderData> m_PolicySliderData;

		public ComponentLookup<BuildingOptionData> m_BuildingOptionData;

		public BufferLookup<BuildingModifierData> m_BuildingModifierData;

		public BuildingModifierRefreshData(SystemBase system)
		{
			m_PolicySliderData = system.GetComponentLookup<PolicySliderData>(isReadOnly: true);
			m_BuildingOptionData = system.GetComponentLookup<BuildingOptionData>(isReadOnly: true);
			m_BuildingModifierData = system.GetBufferLookup<BuildingModifierData>(isReadOnly: true);
		}

		public void Update(SystemBase system)
		{
			m_PolicySliderData.Update(system);
			m_BuildingOptionData.Update(system);
			m_BuildingModifierData.Update(system);
		}

		public void RefreshBuildingOptions(ref Building building, DynamicBuffer<Policy> policies)
		{
			building.m_OptionMask = 0u;
			for (int i = 0; i < policies.Length; i++)
			{
				Policy policy = policies[i];
				if ((policy.m_Flags & PolicyFlags.Active) != 0 && m_BuildingOptionData.HasComponent(policy.m_Policy))
				{
					BuildingOptionData buildingOptionData = m_BuildingOptionData[policy.m_Policy];
					building.m_OptionMask |= buildingOptionData.m_OptionMask;
				}
			}
		}

		public void RefreshBuildingModifiers(DynamicBuffer<BuildingModifier> modifiers, DynamicBuffer<Policy> policies)
		{
			modifiers.Clear();
			for (int i = 0; i < policies.Length; i++)
			{
				Policy policy = policies[i];
				if ((policy.m_Flags & PolicyFlags.Active) == 0 || !m_BuildingModifierData.HasBuffer(policy.m_Policy))
				{
					continue;
				}
				DynamicBuffer<BuildingModifierData> dynamicBuffer = m_BuildingModifierData[policy.m_Policy];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					BuildingModifierData modifierData = dynamicBuffer[j];
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
		}

		private static void AddModifier(DynamicBuffer<BuildingModifier> modifiers, BuildingModifierData modifierData, float delta)
		{
			while (modifiers.Length <= (int)modifierData.m_Type)
			{
				modifiers.Add(default(BuildingModifier));
			}
			BuildingModifier value = modifiers[(int)modifierData.m_Type];
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

		public ComponentTypeHandle<Building> __Game_Buildings_Building_RW_ComponentTypeHandle;

		public BufferTypeHandle<BuildingModifier> __Game_Buildings_BuildingModifier_RW_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Policies_Policy_RO_BufferTypeHandle = state.GetBufferTypeHandle<Policy>(isReadOnly: true);
			__Game_Buildings_Building_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Building>();
			__Game_Buildings_BuildingModifier_RW_BufferTypeHandle = state.GetBufferTypeHandle<BuildingModifier>();
		}
	}

	private EntityQuery m_CreatedQuery;

	private BuildingModifierRefreshData m_BuildingModifierRefreshData;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_BuildingModifierRefreshData = new BuildingModifierRefreshData(this);
		m_CreatedQuery = GetEntityQuery(ComponentType.ReadOnly<Created>(), ComponentType.ReadWrite<Building>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_CreatedQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_BuildingModifierRefreshData.Update(this);
		InitializeBuildingModifiersJob jobData = new InitializeBuildingModifiersJob
		{
			m_BuildingModifierRefreshData = m_BuildingModifierRefreshData,
			m_PolicyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Policies_Policy_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingModifierType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_BuildingModifier_RW_BufferTypeHandle, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_CreatedQuery, base.Dependency);
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
	public BuildingModifierInitializeSystem()
	{
	}
}
