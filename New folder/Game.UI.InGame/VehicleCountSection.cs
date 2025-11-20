using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Pathfind;
using Game.Policies;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class VehicleCountSection : InfoSectionBase
{
	private enum Result
	{
		VehicleCount,
		ActiveVehicles,
		VehicleCountMin,
		VehicleCountMax,
		Count
	}

	[BurstCompile]
	private struct CalculateVehicleCountJob : IJob
	{
		[ReadOnly]
		public Entity m_SelectedEntity;

		[ReadOnly]
		public Entity m_SelectedPrefab;

		[ReadOnly]
		public Entity m_Policy;

		[ReadOnly]
		public ComponentLookup<VehicleTiming> m_VehicleTimings;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformations;

		[ReadOnly]
		public ComponentLookup<TransportLineData> m_TransportLineDatas;

		[ReadOnly]
		public ComponentLookup<PolicySliderData> m_PolicySliderDatas;

		[ReadOnly]
		public BufferLookup<RouteVehicle> m_RouteVehicles;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> m_RouteWaypoints;

		[ReadOnly]
		public BufferLookup<RouteSegment> m_RouteSegments;

		[ReadOnly]
		public BufferLookup<RouteModifier> m_RouteModifiers;

		[ReadOnly]
		public BufferLookup<RouteModifierData> m_RouteModifierDatas;

		public NativeArray<int> m_IntResults;

		public NativeReference<float> m_Duration;

		public void Execute()
		{
			TransportLineData transportLineData = m_TransportLineDatas[m_SelectedPrefab];
			DynamicBuffer<RouteVehicle> dynamicBuffer = m_RouteVehicles[m_SelectedEntity];
			DynamicBuffer<RouteModifier> modifiers = m_RouteModifiers[m_SelectedEntity];
			PolicySliderData policySliderData = m_PolicySliderDatas[m_Policy];
			float defaultVehicleInterval = transportLineData.m_DefaultVehicleInterval;
			float value = defaultVehicleInterval;
			RouteUtils.ApplyModifier(ref value, modifiers, RouteModifierType.VehicleInterval);
			float num = CalculateStableDuration(transportLineData);
			m_Duration.Value = num;
			m_IntResults[0] = TransportLineSystem.CalculateVehicleCount(value, num);
			m_IntResults[1] = dynamicBuffer.Length;
			m_IntResults[2] = CalculateVehicleCountFromAdjustment(policySliderData.m_Range.min, defaultVehicleInterval, num);
			m_IntResults[3] = CalculateVehicleCountFromAdjustment(policySliderData.m_Range.max, defaultVehicleInterval, num);
		}

		private int CalculateVehicleCountFromAdjustment(float policyAdjustment, float interval, float duration)
		{
			RouteModifier modifier = default(RouteModifier);
			foreach (RouteModifierData item in m_RouteModifierDatas[m_Policy])
			{
				if (item.m_Type == RouteModifierType.VehicleInterval)
				{
					float modifierDelta = RouteModifierInitializeSystem.RouteModifierRefreshData.GetModifierDelta(item, policyAdjustment, m_Policy, m_PolicySliderDatas);
					RouteModifierInitializeSystem.RouteModifierRefreshData.AddModifierData(ref modifier, item, modifierDelta);
					break;
				}
			}
			interval += modifier.m_Delta.x;
			interval += interval * modifier.m_Delta.y;
			return TransportLineSystem.CalculateVehicleCount(interval, duration);
		}

		public static float CalculateAdjustmentFromVehicleCount(int vehicleCount, float originalInterval, float duration, DynamicBuffer<RouteModifierData> modifierDatas, PolicySliderData sliderData)
		{
			float num = TransportLineSystem.CalculateVehicleInterval(duration, vehicleCount);
			RouteModifier modifier = default(RouteModifier);
			foreach (RouteModifierData item in modifierDatas)
			{
				if (item.m_Type == RouteModifierType.VehicleInterval)
				{
					if (item.m_Mode == ModifierValueMode.Absolute)
					{
						modifier.m_Delta.x = num - originalInterval;
					}
					else
					{
						modifier.m_Delta.y = (0f - originalInterval + num) / originalInterval;
					}
					float deltaFromModifier = RouteModifierInitializeSystem.RouteModifierRefreshData.GetDeltaFromModifier(modifier, item);
					return RouteModifierInitializeSystem.RouteModifierRefreshData.GetPolicyAdjustmentFromModifierDelta(item, deltaFromModifier, sliderData);
				}
			}
			return -1f;
		}

		public float CalculateStableDuration(TransportLineData transportLineData)
		{
			DynamicBuffer<RouteWaypoint> dynamicBuffer = m_RouteWaypoints[m_SelectedEntity];
			DynamicBuffer<RouteSegment> dynamicBuffer2 = m_RouteSegments[m_SelectedEntity];
			int num = 0;
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				if (m_VehicleTimings.HasComponent(dynamicBuffer[i].m_Waypoint))
				{
					num = i;
					break;
				}
			}
			float num2 = 0f;
			for (int j = 0; j < dynamicBuffer.Length; j++)
			{
				int2 @int = num + j;
				@int.y++;
				@int = math.select(@int, @int - dynamicBuffer.Length, @int >= dynamicBuffer.Length);
				Entity waypoint = dynamicBuffer[@int.y].m_Waypoint;
				Entity segment = dynamicBuffer2[@int.x].m_Segment;
				if (m_PathInformations.TryGetComponent(segment, out var componentData))
				{
					num2 += componentData.m_Duration;
				}
				if (m_VehicleTimings.HasComponent(waypoint))
				{
					num2 += transportLineData.m_StopDuration;
				}
			}
			return num2;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<TransportLineData> __Game_Prefabs_TransportLineData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PolicySliderData> __Game_Prefabs_PolicySliderData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<VehicleTiming> __Game_Routes_VehicleTiming_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<RouteVehicle> __Game_Routes_RouteVehicle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> __Game_Routes_RouteWaypoint_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<RouteSegment> __Game_Routes_RouteSegment_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<RouteModifier> __Game_Routes_RouteModifier_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<RouteModifierData> __Game_Prefabs_RouteModifierData_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_TransportLineData_RO_ComponentLookup = state.GetComponentLookup<TransportLineData>(isReadOnly: true);
			__Game_Prefabs_PolicySliderData_RO_ComponentLookup = state.GetComponentLookup<PolicySliderData>(isReadOnly: true);
			__Game_Routes_VehicleTiming_RO_ComponentLookup = state.GetComponentLookup<VehicleTiming>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Routes_RouteVehicle_RO_BufferLookup = state.GetBufferLookup<RouteVehicle>(isReadOnly: true);
			__Game_Routes_RouteWaypoint_RO_BufferLookup = state.GetBufferLookup<RouteWaypoint>(isReadOnly: true);
			__Game_Routes_RouteSegment_RO_BufferLookup = state.GetBufferLookup<RouteSegment>(isReadOnly: true);
			__Game_Routes_RouteModifier_RO_BufferLookup = state.GetBufferLookup<RouteModifier>(isReadOnly: true);
			__Game_Prefabs_RouteModifierData_RO_BufferLookup = state.GetBufferLookup<RouteModifierData>(isReadOnly: true);
		}
	}

	private PoliciesUISystem m_PoliciesUISystem;

	private Entity m_VehicleCountPolicy;

	private EntityQuery m_ConfigQuery;

	private NativeArray<int> m_IntResults;

	private NativeReference<float> m_DurationResult;

	private TypeHandle __TypeHandle;

	protected override string group => "VehicleCountSection";

	private int vehicleCountMin { get; set; }

	private int vehicleCountMax { get; set; }

	private int vehicleCount { get; set; }

	private int activeVehicles { get; set; }

	private float stableDuration { get; set; }

	protected override void Reset()
	{
		vehicleCountMin = 0;
		vehicleCountMax = 0;
		vehicleCount = 0;
		activeVehicles = 0;
		stableDuration = 0f;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PoliciesUISystem = base.World.GetOrCreateSystemManaged<PoliciesUISystem>();
		m_ConfigQuery = GetEntityQuery(ComponentType.ReadOnly<UITransportConfigurationData>());
		AddBinding(new TriggerBinding<float>(group, "setVehicleCount", OnSetVehicleCount));
		m_IntResults = new NativeArray<int>(4, Allocator.Persistent);
		m_DurationResult = new NativeReference<float>(0f, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_IntResults.Dispose();
		m_DurationResult.Dispose();
		base.OnDestroy();
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		if (!m_ConfigQuery.IsEmptyIgnoreFilter)
		{
			UITransportConfigurationPrefab singletonPrefab = m_PrefabSystem.GetSingletonPrefab<UITransportConfigurationPrefab>(m_ConfigQuery);
			m_VehicleCountPolicy = m_PrefabSystem.GetEntity(singletonPrefab.m_VehicleCountPolicy);
		}
	}

	private void OnSetVehicleCount(float newVehicleCount)
	{
		DynamicBuffer<RouteModifierData> buffer = base.EntityManager.GetBuffer<RouteModifierData>(m_VehicleCountPolicy, isReadOnly: true);
		PolicySliderData componentData = base.EntityManager.GetComponentData<PolicySliderData>(m_VehicleCountPolicy);
		float adjustment = CalculateVehicleCountJob.CalculateAdjustmentFromVehicleCount((int)newVehicleCount, base.EntityManager.GetComponentData<TransportLineData>(selectedPrefab).m_DefaultVehicleInterval, stableDuration, buffer, componentData);
		m_PoliciesUISystem.SetPolicy(selectedEntity, m_VehicleCountPolicy, active: true, adjustment);
	}

	private bool Visible()
	{
		if (base.EntityManager.HasComponent<Route>(selectedEntity) && base.EntityManager.HasComponent<TransportLine>(selectedEntity) && base.EntityManager.HasComponent<RouteWaypoint>(selectedEntity))
		{
			return base.EntityManager.HasComponent<Policy>(selectedEntity);
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
		if (base.visible)
		{
			IJobExtensions.Schedule(new CalculateVehicleCountJob
			{
				m_SelectedEntity = selectedEntity,
				m_SelectedPrefab = selectedPrefab,
				m_Policy = m_VehicleCountPolicy,
				m_TransportLineDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportLineData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PolicySliderDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PolicySliderData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_VehicleTimings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_VehicleTiming_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PathInformations = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_RouteVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteVehicle_RO_BufferLookup, ref base.CheckedStateRef),
				m_RouteWaypoints = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferLookup, ref base.CheckedStateRef),
				m_RouteSegments = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteSegment_RO_BufferLookup, ref base.CheckedStateRef),
				m_RouteModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteModifier_RO_BufferLookup, ref base.CheckedStateRef),
				m_RouteModifierDatas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_RouteModifierData_RO_BufferLookup, ref base.CheckedStateRef),
				m_IntResults = m_IntResults,
				m_Duration = m_DurationResult
			}, base.Dependency).Complete();
		}
	}

	protected override void OnProcess()
	{
		vehicleCountMin = m_IntResults[2];
		vehicleCountMax = m_IntResults[3];
		vehicleCount = m_IntResults[0];
		activeVehicles = m_IntResults[1];
		stableDuration = m_DurationResult.Value;
		base.tooltipTags.Add("TransportLine");
		base.tooltipTags.Add("CargoRoute");
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("vehicleCountMin");
		writer.Write(vehicleCountMin);
		writer.PropertyName("vehicleCountMax");
		writer.Write(vehicleCountMax);
		writer.PropertyName("vehicleCount");
		writer.Write(vehicleCount);
		writer.PropertyName("activeVehicles");
		writer.Write(activeVehicles);
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
	public VehicleCountSection()
	{
	}
}
