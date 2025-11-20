using System;
using System.Runtime.CompilerServices;
using Game.Common;
using Game.Prefabs;
using Game.Routes;
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
public class RouteModifierInitializeSystem : GameSystemBase
{
	[BurstCompile]
	private struct InitializeRouteModifiersJob : IJobChunk
	{
		[ReadOnly]
		public RouteModifierRefreshData m_RouteModifierRefreshData;

		[ReadOnly]
		public BufferTypeHandle<Policy> m_PolicyType;

		public ComponentTypeHandle<Route> m_RouteType;

		public BufferTypeHandle<RouteModifier> m_RouteModifierType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Route> nativeArray = chunk.GetNativeArray(ref m_RouteType);
			BufferAccessor<RouteModifier> bufferAccessor = chunk.GetBufferAccessor(ref m_RouteModifierType);
			BufferAccessor<Policy> bufferAccessor2 = chunk.GetBufferAccessor(ref m_PolicyType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				DynamicBuffer<Policy> policies = bufferAccessor2[i];
				if (policies.Length != 0)
				{
					Route route = nativeArray[i];
					m_RouteModifierRefreshData.RefreshRouteOptions(ref route, policies);
					nativeArray[i] = route;
					if (bufferAccessor.Length != 0)
					{
						DynamicBuffer<RouteModifier> modifiers = bufferAccessor[i];
						m_RouteModifierRefreshData.RefreshRouteModifiers(modifiers, policies);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	public struct RouteModifierRefreshData
	{
		public ComponentLookup<PolicySliderData> m_PolicySliderData;

		public ComponentLookup<RouteOptionData> m_RouteOptionData;

		public BufferLookup<RouteModifierData> m_RouteModifierData;

		public RouteModifierRefreshData(SystemBase system)
		{
			m_PolicySliderData = system.GetComponentLookup<PolicySliderData>(isReadOnly: true);
			m_RouteOptionData = system.GetComponentLookup<RouteOptionData>(isReadOnly: true);
			m_RouteModifierData = system.GetBufferLookup<RouteModifierData>(isReadOnly: true);
		}

		public void Update(SystemBase system)
		{
			m_PolicySliderData.Update(system);
			m_RouteOptionData.Update(system);
			m_RouteModifierData.Update(system);
		}

		public void RefreshRouteOptions(ref Route route, DynamicBuffer<Policy> policies)
		{
			route.m_OptionMask = 0u;
			for (int i = 0; i < policies.Length; i++)
			{
				Policy policy = policies[i];
				if ((policy.m_Flags & PolicyFlags.Active) != 0 && m_RouteOptionData.HasComponent(policy.m_Policy))
				{
					RouteOptionData routeOptionData = m_RouteOptionData[policy.m_Policy];
					route.m_OptionMask |= routeOptionData.m_OptionMask;
				}
			}
		}

		public void RefreshRouteModifiers(DynamicBuffer<RouteModifier> modifiers, DynamicBuffer<Policy> policies)
		{
			modifiers.Clear();
			for (int i = 0; i < policies.Length; i++)
			{
				Policy policy = policies[i];
				if ((policy.m_Flags & PolicyFlags.Active) != 0 && m_RouteModifierData.HasBuffer(policy.m_Policy))
				{
					DynamicBuffer<RouteModifierData> dynamicBuffer = m_RouteModifierData[policy.m_Policy];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						RouteModifierData modifierData = dynamicBuffer[j];
						float modifierDelta = GetModifierDelta(modifierData, policy.m_Adjustment, policy.m_Policy, m_PolicySliderData);
						AddModifier(modifiers, modifierData, modifierDelta);
					}
				}
			}
		}

		public static float GetModifierDelta(RouteModifierData modifierData, float policyAdjustment, Entity policy, ComponentLookup<PolicySliderData> policySliderData)
		{
			if (policySliderData.HasComponent(policy))
			{
				PolicySliderData policySliderData2 = policySliderData[policy];
				float falseValue = (policyAdjustment - policySliderData2.m_Range.min) / (policySliderData2.m_Range.max - policySliderData2.m_Range.min);
				falseValue = math.select(falseValue, 0f, policySliderData2.m_Range.min == policySliderData2.m_Range.max);
				falseValue = math.saturate(falseValue);
				return math.lerp(modifierData.m_Range.min, modifierData.m_Range.max, falseValue);
			}
			return modifierData.m_Range.min;
		}

		public static float GetPolicyAdjustmentFromModifierDelta(RouteModifierData modifierData, float modifierDelta, PolicySliderData sliderData)
		{
			return math.clamp(math.remap(modifierData.m_Range.min, modifierData.m_Range.max, sliderData.m_Range.min, sliderData.m_Range.max, modifierDelta), sliderData.m_Range.min, sliderData.m_Range.max);
		}

		public static void AddModifierData(ref RouteModifier modifier, RouteModifierData modifierData, float delta)
		{
			switch (modifierData.m_Mode)
			{
			case ModifierValueMode.Relative:
				modifier.m_Delta.y = modifier.m_Delta.y * (1f + delta) + delta;
				break;
			case ModifierValueMode.Absolute:
				modifier.m_Delta.x += delta;
				break;
			case ModifierValueMode.InverseRelative:
				delta = 1f / math.max(0.001f, 1f + delta) - 1f;
				modifier.m_Delta.y = modifier.m_Delta.y * (1f + delta) + delta;
				break;
			}
		}

		public static float GetDeltaFromModifier(RouteModifier modifier, RouteModifierData modifierData)
		{
			return modifierData.m_Mode switch
			{
				ModifierValueMode.Relative => modifier.m_Delta.y, 
				ModifierValueMode.Absolute => modifier.m_Delta.x, 
				ModifierValueMode.InverseRelative => (0f - modifier.m_Delta.y) / (1f + modifier.m_Delta.y), 
				_ => throw new ArgumentException(), 
			};
		}

		private static void AddModifier(DynamicBuffer<RouteModifier> modifiers, RouteModifierData modifierData, float delta)
		{
			while (modifiers.Length <= (int)modifierData.m_Type)
			{
				modifiers.Add(default(RouteModifier));
			}
			RouteModifier modifier = modifiers[(int)modifierData.m_Type];
			AddModifierData(ref modifier, modifierData, delta);
			modifiers[(int)modifierData.m_Type] = modifier;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public BufferTypeHandle<Policy> __Game_Policies_Policy_RO_BufferTypeHandle;

		public ComponentTypeHandle<Route> __Game_Routes_Route_RW_ComponentTypeHandle;

		public BufferTypeHandle<RouteModifier> __Game_Routes_RouteModifier_RW_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Policies_Policy_RO_BufferTypeHandle = state.GetBufferTypeHandle<Policy>(isReadOnly: true);
			__Game_Routes_Route_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Route>();
			__Game_Routes_RouteModifier_RW_BufferTypeHandle = state.GetBufferTypeHandle<RouteModifier>();
		}
	}

	private EntityQuery m_CreatedQuery;

	private RouteModifierRefreshData m_RouteModifierRefreshData;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_RouteModifierRefreshData = new RouteModifierRefreshData(this);
		m_CreatedQuery = GetEntityQuery(ComponentType.ReadOnly<Created>(), ComponentType.ReadWrite<Route>(), ComponentType.ReadOnly<Policy>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_CreatedQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_RouteModifierRefreshData.Update(this);
		InitializeRouteModifiersJob jobData = new InitializeRouteModifiersJob
		{
			m_RouteModifierRefreshData = m_RouteModifierRefreshData,
			m_PolicyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Policies_Policy_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_RouteType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_Route_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RouteModifierType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_RouteModifier_RW_BufferTypeHandle, ref base.CheckedStateRef)
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
	public RouteModifierInitializeSystem()
	{
	}
}
