using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Simulation;

public struct TransformerData
{
	[ReadOnly]
	public ComponentLookup<Deleted> m_Deleted;

	[ReadOnly]
	public ComponentLookup<PrefabRef> m_PrefabRefs;

	[ReadOnly]
	public ComponentLookup<ElectricityConnectionData> m_ElectricityConnectionDatas;

	[ReadOnly]
	public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

	[ReadOnly]
	public BufferLookup<Game.Net.SubNet> m_SubNets;

	[ReadOnly]
	public ComponentLookup<Node> m_NetNodes;

	[ReadOnly]
	public ComponentLookup<ElectricityNodeConnection> m_ElectricityNodeConnections;

	[ReadOnly]
	public ComponentLookup<ElectricityValveConnection> m_ElectricityValveConnections;

	[ReadOnly]
	public BufferLookup<ConnectedFlowEdge> m_FlowConnections;

	[ReadOnly]
	public ComponentLookup<ElectricityFlowEdge> m_FlowEdges;

	public void GetTransformerData(Entity entity, out int capacity, out int flow)
	{
		int lowVoltageCapacity = 0;
		int highVoltageCapacity = 0;
		flow = 0;
		if (m_SubNets.TryGetBuffer(entity, out var bufferData))
		{
			ProcessMarkerNodes(bufferData, ref lowVoltageCapacity, ref highVoltageCapacity, ref flow);
		}
		if (m_InstalledUpgrades.TryGetBuffer(entity, out var bufferData2))
		{
			ProcessMarkerNodes(bufferData2, ref lowVoltageCapacity, ref highVoltageCapacity, ref flow);
		}
		capacity = math.min(lowVoltageCapacity, highVoltageCapacity);
	}

	private void ProcessMarkerNodes(DynamicBuffer<InstalledUpgrade> upgrades, ref int lowVoltageCapacity, ref int highVoltageCapacity, ref int flow)
	{
		for (int i = 0; i < upgrades.Length; i++)
		{
			InstalledUpgrade installedUpgrade = upgrades[i];
			if (!BuildingUtils.CheckOption(installedUpgrade, BuildingOption.Inactive) && m_SubNets.TryGetBuffer(installedUpgrade.m_Upgrade, out var bufferData))
			{
				ProcessMarkerNodes(bufferData, ref lowVoltageCapacity, ref highVoltageCapacity, ref flow);
			}
		}
	}

	private void ProcessMarkerNodes(DynamicBuffer<Game.Net.SubNet> subNets, ref int lowVoltageCapacity, ref int highVoltageCapacity, ref int flow)
	{
		for (int i = 0; i < subNets.Length; i++)
		{
			Entity subNet = subNets[i].m_SubNet;
			if (!m_NetNodes.HasComponent(subNet) || m_Deleted.HasComponent(subNet) || !m_ElectricityNodeConnections.TryGetComponent(subNet, out var componentData) || !m_ElectricityValveConnections.TryGetComponent(subNet, out var componentData2) || !m_PrefabRefs.TryGetComponent(subNet, out var componentData3) || !m_ElectricityConnectionDatas.TryGetComponent(componentData3.m_Prefab, out var componentData4))
			{
				continue;
			}
			if (componentData4.m_Voltage == Game.Prefabs.ElectricityConnection.Voltage.Low)
			{
				lowVoltageCapacity += componentData4.m_Capacity;
				if (ElectricityGraphUtils.TryGetFlowEdge(componentData2.m_ValveNode, componentData.m_ElectricityNode, ref m_FlowConnections, ref m_FlowEdges, out ElectricityFlowEdge edge))
				{
					flow += edge.m_Flow;
				}
			}
			else
			{
				highVoltageCapacity += componentData4.m_Capacity;
			}
		}
	}
}
