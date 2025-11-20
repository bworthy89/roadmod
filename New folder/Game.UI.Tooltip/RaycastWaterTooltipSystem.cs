using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.UI.Localization;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.Tooltip;

[CompilerGenerated]
public class RaycastWaterTooltipSystem : TooltipSystemBase
{
	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<WaterPipeNodeConnection> __Game_Simulation_WaterPipeNodeConnection_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> __Game_Simulation_ConnectedFlowEdge_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<WaterPipeEdge> __Game_Simulation_WaterPipeEdge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterPipeBuildingConnection> __Game_Simulation_WaterPipeBuildingConnection_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Simulation_WaterPipeNodeConnection_RO_ComponentLookup = state.GetComponentLookup<WaterPipeNodeConnection>(isReadOnly: true);
			__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedFlowEdge>(isReadOnly: true);
			__Game_Simulation_WaterPipeEdge_RO_ComponentLookup = state.GetComponentLookup<WaterPipeEdge>(isReadOnly: true);
			__Game_Buildings_WaterConsumer_RO_ComponentLookup = state.GetComponentLookup<WaterConsumer>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Simulation_WaterPipeBuildingConnection_RO_ComponentLookup = state.GetComponentLookup<WaterPipeBuildingConnection>(isReadOnly: true);
		}
	}

	private ToolSystem m_ToolSystem;

	private DefaultToolSystem m_DefaultTool;

	private ToolRaycastSystem m_ToolRaycastSystem;

	private WaterPipeFlowSystem m_WaterPipeFlowSystem;

	private EntityQuery m_InfomodeQuery;

	private IntTooltip m_WaterCapacity;

	private IntTooltip m_WaterUsage;

	private IntTooltip m_SewageCapacity;

	private IntTooltip m_SewageUsage;

	private ProgressTooltip m_WaterConsumption;

	private ProgressTooltip m_SewageConsumption;

	private IntTooltip m_WaterFlow;

	private IntTooltip m_SewageFlow;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_DefaultTool = base.World.GetOrCreateSystemManaged<DefaultToolSystem>();
		m_ToolRaycastSystem = base.World.GetOrCreateSystemManaged<ToolRaycastSystem>();
		m_WaterPipeFlowSystem = base.World.GetOrCreateSystemManaged<WaterPipeFlowSystem>();
		m_InfomodeQuery = GetEntityQuery(ComponentType.ReadOnly<InfomodeActive>(), ComponentType.ReadOnly<InfoviewNetStatusData>());
		RequireForUpdate(m_InfomodeQuery);
		m_WaterCapacity = new IntTooltip
		{
			path = "waterCapacity",
			label = LocalizedString.Id("SelectedInfoPanel.WATER_OUTPUT"),
			unit = "volume"
		};
		m_WaterUsage = new IntTooltip
		{
			path = "waterUsage",
			label = LocalizedString.Id("SelectedInfoPanel.WATER_PUMP_USAGE"),
			unit = "percentage"
		};
		m_SewageCapacity = new IntTooltip
		{
			path = "sewageCapacity",
			label = LocalizedString.Id("SelectedInfoPanel.SEWAGE_PROCESSING_CAPACITY"),
			unit = "volume"
		};
		m_SewageUsage = new IntTooltip
		{
			path = "sewageUsage",
			label = LocalizedString.Id("SelectedInfoPanel.SEWAGE_OUTLET_USAGE"),
			unit = "percentage"
		};
		m_WaterConsumption = new ProgressTooltip
		{
			path = "waterConsumption",
			label = LocalizedString.Id("Tools.WATER_CONSUMPTION_LABEL"),
			unit = "volume",
			color = TooltipColor.Warning,
			omitMax = true
		};
		m_SewageConsumption = new ProgressTooltip
		{
			path = "sewageConsumption",
			label = LocalizedString.Id("Tools.SEWAGE_CONSUMPTION_LABEL"),
			unit = "volume",
			color = TooltipColor.Warning,
			omitMax = true
		};
		m_WaterFlow = new IntTooltip
		{
			path = "waterFlow",
			label = LocalizedString.Id("Tools.WATER_FLOW_LABEL"),
			unit = "volume"
		};
		m_SewageFlow = new IntTooltip
		{
			path = "sewageFlow",
			label = LocalizedString.Id("Tools.SEWAGE_FLOW_LABEL"),
			unit = "volume"
		};
	}

	[Preserve]
	protected override void OnUpdate()
	{
		CompleteDependency();
		if (!IsInfomodeActivated() || m_ToolSystem.activeTool != m_DefaultTool || !m_ToolRaycastSystem.GetRaycastResult(out var result) || base.EntityManager.HasComponent<Destroyed>(result.m_Owner))
		{
			return;
		}
		if (base.EntityManager.HasComponent<Game.Net.UtilityLane>(result.m_Owner))
		{
			PrefabRef componentData = base.EntityManager.GetComponentData<PrefabRef>(result.m_Owner);
			if (base.EntityManager.TryGetComponent<UtilityLaneData>(componentData.m_Prefab, out var component) && base.EntityManager.TryGetComponent<EdgeMapping>(result.m_Owner, out var component2) && (component.m_UtilityTypes & (UtilityTypes.WaterPipe | UtilityTypes.SewagePipe)) != UtilityTypes.None)
			{
				Owner component3;
				if (component2.m_Parent1 != Entity.Null)
				{
					if (base.EntityManager.HasComponent<Edge>(component2.m_Parent1))
					{
						if (component2.m_Parent2 != Entity.Null)
						{
							if (result.m_Hit.m_CurvePosition < 0.5f)
							{
								float curvePosition = math.lerp(component2.m_CurveDelta1.x, component2.m_CurveDelta1.y, result.m_Hit.m_CurvePosition * 2f);
								AddEdgeFlow(component2.m_Parent1, curvePosition);
							}
							else
							{
								float curvePosition2 = math.lerp(component2.m_CurveDelta2.x, component2.m_CurveDelta2.y, result.m_Hit.m_CurvePosition * 2f - 1f);
								AddEdgeFlow(component2.m_Parent2, curvePosition2);
							}
						}
						else
						{
							float curvePosition3 = math.lerp(component2.m_CurveDelta1.x, component2.m_CurveDelta1.y, result.m_Hit.m_CurvePosition);
							AddEdgeFlow(component2.m_Parent1, curvePosition3);
						}
					}
					else
					{
						AddNodeFlow(component2.m_Parent1, component2.m_Parent2);
					}
				}
				else if (base.EntityManager.HasComponent<Game.Net.SecondaryLane>(result.m_Owner) && base.EntityManager.TryGetComponent<Owner>(result.m_Owner, out component3))
				{
					result.m_Owner = component3.m_Owner;
				}
			}
		}
		if (base.EntityManager.TryGetComponent<Game.Buildings.SewageOutlet>(result.m_Owner, out var component4))
		{
			m_SewageCapacity.value = component4.m_Capacity;
			AddMouseTooltip(m_SewageCapacity);
			if (component4.m_Capacity > 0)
			{
				m_SewageUsage.value = ((component4.m_LastProcessed > 0) ? math.clamp(100 * component4.m_LastProcessed / component4.m_Capacity, 1, 100) : 0);
				m_SewageUsage.color = TooltipColor.Info;
				AddMouseTooltip(m_SewageUsage);
			}
		}
		if (base.EntityManager.TryGetComponent<Game.Buildings.WaterPumpingStation>(result.m_Owner, out var component5))
		{
			m_WaterCapacity.value = component5.m_Capacity;
			AddMouseTooltip(m_WaterCapacity);
			if (component5.m_Capacity > 0)
			{
				m_WaterUsage.value = ((component5.m_LastProduction > 0) ? math.clamp(100 * component5.m_LastProduction / component5.m_Capacity, 1, 100) : 0);
				m_WaterUsage.color = TooltipColor.Info;
				AddMouseTooltip(m_WaterUsage);
			}
		}
		if (base.EntityManager.TryGetComponent<WaterConsumer>(result.m_Owner, out var component6))
		{
			m_WaterConsumption.value = component6.m_FulfilledFresh;
			m_WaterConsumption.max = component6.m_WantedConsumption;
			m_WaterConsumption.color = ((component6.m_FulfilledFresh < component6.m_WantedConsumption) ? TooltipColor.Warning : TooltipColor.Info);
			AddMouseTooltip(m_WaterConsumption);
			if (component6.m_FulfilledFresh < component6.m_WantedConsumption || component6.m_FulfilledSewage < component6.m_WantedConsumption)
			{
				m_SewageConsumption.value = component6.m_FulfilledSewage;
				m_SewageConsumption.max = component6.m_WantedConsumption;
				m_SewageConsumption.color = ((component6.m_FulfilledSewage < component6.m_WantedConsumption) ? TooltipColor.Warning : TooltipColor.Info);
				AddMouseTooltip(m_SewageConsumption);
			}
		}
	}

	private void AddEdgeFlow(Entity edge, float curvePosition)
	{
		ComponentLookup<WaterPipeNodeConnection> componentLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterPipeNodeConnection_RO_ComponentLookup, ref base.CheckedStateRef);
		BufferLookup<ConnectedFlowEdge> flowConnections = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup, ref base.CheckedStateRef);
		ComponentLookup<WaterPipeEdge> flowEdges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterPipeEdge_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<WaterConsumer> componentLookup2 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<Building> componentLookup3 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<WaterPipeBuildingConnection> componentLookup4 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterPipeBuildingConnection_RO_ComponentLookup, ref base.CheckedStateRef);
		if (!base.EntityManager.TryGetComponent<Edge>(edge, out var component) || !componentLookup.TryGetComponent(edge, out var componentData) || !componentLookup.TryGetComponent(component.m_Start, out var componentData2) || !componentLookup.TryGetComponent(component.m_End, out var _) || !WaterPipeGraphUtils.TryGetFlowEdge(componentData2.m_WaterPipeNode, componentData.m_WaterPipeNode, ref flowConnections, ref flowEdges, out WaterPipeEdge edge2))
		{
			return;
		}
		int2 @int = math.max(1, edge2.capacity);
		int2 flow = edge2.flow;
		if (base.EntityManager.TryGetBuffer(edge, isReadOnly: true, out DynamicBuffer<ConnectedNode> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				ConnectedNode connectedNode = buffer[i];
				if (connectedNode.m_CurvePosition < curvePosition && componentLookup.TryGetComponent(connectedNode.m_Node, out var componentData4) && WaterPipeGraphUtils.TryGetFlowEdge(componentData4.m_WaterPipeNode, componentData.m_WaterPipeNode, ref flowConnections, ref flowEdges, out WaterPipeEdge edge3))
				{
					flow += edge3.flow;
				}
			}
		}
		if (WaterPipeGraphUtils.TryGetFlowEdge(componentData.m_WaterPipeNode, m_WaterPipeFlowSystem.sinkNode, ref flowConnections, ref flowEdges, out WaterPipeEdge edge4) && base.EntityManager.TryGetBuffer(edge, isReadOnly: true, out DynamicBuffer<ConnectedBuilding> buffer2))
		{
			int2 flow2 = edge4.flow;
			int2 int2 = 0;
			for (int j = 0; j < buffer2.Length; j++)
			{
				ConnectedBuilding connectedBuilding = buffer2[j];
				if (!componentLookup4.HasComponent(connectedBuilding.m_Building) && componentLookup2.TryGetComponent(connectedBuilding.m_Building, out var componentData5))
				{
					int2 += componentData5.m_WantedConsumption;
				}
			}
			for (int k = 0; k < buffer2.Length; k++)
			{
				ConnectedBuilding connectedBuilding2 = buffer2[k];
				if (!componentLookup4.HasComponent(connectedBuilding2.m_Building) && componentLookup2.TryGetComponent(connectedBuilding2.m_Building, out var componentData6))
				{
					int2 int3 = new int2(FlowUtils.ConsumeFromTotal(componentData6.m_WantedConsumption, ref flow2.x, ref int2.x), FlowUtils.ConsumeFromTotal(componentData6.m_WantedConsumption, ref flow2.y, ref int2.y));
					if (componentLookup3[connectedBuilding2.m_Building].m_CurvePosition < curvePosition)
					{
						flow -= int3;
					}
				}
			}
		}
		if (@int.x > 0)
		{
			m_WaterFlow.value = math.abs(flow.x);
			AddMouseTooltip(m_WaterFlow);
		}
		if (@int.y > 0)
		{
			m_SewageFlow.value = math.abs(flow.y);
			AddMouseTooltip(m_SewageFlow);
		}
	}

	private void AddNodeFlow(Entity node, Entity edge)
	{
		if (!base.EntityManager.TryGetComponent<WaterPipeNodeConnection>(node, out var component) || !base.EntityManager.TryGetComponent<WaterPipeNodeConnection>(edge, out var component2) || !base.EntityManager.TryGetBuffer(component.m_WaterPipeNode, isReadOnly: true, out DynamicBuffer<ConnectedFlowEdge> buffer))
		{
			return;
		}
		int2 x = 0;
		int2 x2 = 0;
		for (int i = 0; i < buffer.Length; i++)
		{
			WaterPipeEdge componentData = base.EntityManager.GetComponentData<WaterPipeEdge>(buffer[i].m_Edge);
			if (componentData.m_Start == component2.m_WaterPipeNode || componentData.m_End == component2.m_WaterPipeNode)
			{
				x = math.max(x, math.abs(componentData.flow));
				x2 = math.max(x2, componentData.capacity);
			}
		}
		if (x2.x > 0)
		{
			m_WaterFlow.value = x.x;
			AddMouseTooltip(m_WaterFlow);
		}
		if (x2.y > 0)
		{
			m_SewageFlow.value = x.y;
			AddMouseTooltip(m_SewageFlow);
		}
	}

	private bool IsInfomodeActivated()
	{
		NativeArray<InfoviewNetStatusData> nativeArray = m_InfomodeQuery.ToComponentDataArray<InfoviewNetStatusData>(Allocator.Temp);
		try
		{
			foreach (InfoviewNetStatusData item in nativeArray)
			{
				if (item.m_Type == NetStatusType.PipeWaterFlow || item.m_Type == NetStatusType.PipeSewageFlow)
				{
					return true;
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
		}
		return false;
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
	public RaycastWaterTooltipSystem()
	{
	}
}
