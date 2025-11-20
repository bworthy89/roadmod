#define UNITY_ASSERTIONS
using Game.Common;
using Game.Net;
using Unity.Assertions;
using Unity.Entities;

namespace Game.Simulation;

public static class ElectricityGraphUtils
{
	public static bool HasAnyFlowEdge(Entity node1, Entity node2, ref BufferLookup<ConnectedFlowEdge> flowConnections, ref ComponentLookup<ElectricityFlowEdge> flowEdges)
	{
		Assert.IsTrue(node1.Index > 0);
		Assert.IsTrue(node2.Index > 0);
		foreach (ConnectedFlowEdge item in flowConnections[node1])
		{
			ElectricityFlowEdge electricityFlowEdge = flowEdges[item.m_Edge];
			if ((electricityFlowEdge.m_Start == node1 && electricityFlowEdge.m_End == node2) || (electricityFlowEdge.m_Start == node2 && electricityFlowEdge.m_End == node1))
			{
				return true;
			}
		}
		return false;
	}

	public static bool TryGetFlowEdge(Entity startNode, Entity endNode, ref BufferLookup<ConnectedFlowEdge> flowConnections, ref ComponentLookup<ElectricityFlowEdge> flowEdges, out Entity entity)
	{
		ElectricityFlowEdge edge;
		return TryGetFlowEdge(startNode, endNode, ref flowConnections, ref flowEdges, out entity, out edge);
	}

	public static bool TryGetFlowEdge(Entity startNode, Entity endNode, ref BufferLookup<ConnectedFlowEdge> flowConnections, ref ComponentLookup<ElectricityFlowEdge> flowEdges, out ElectricityFlowEdge edge)
	{
		Entity entity;
		return TryGetFlowEdge(startNode, endNode, ref flowConnections, ref flowEdges, out entity, out edge);
	}

	public static bool TryGetFlowEdge(Entity startNode, Entity endNode, ref BufferLookup<ConnectedFlowEdge> flowConnections, ref ComponentLookup<ElectricityFlowEdge> flowEdges, out Entity entity, out ElectricityFlowEdge edge)
	{
		Assert.IsTrue(startNode.Index > 0);
		Assert.IsTrue(endNode.Index > 0);
		foreach (ConnectedFlowEdge item in flowConnections[startNode])
		{
			entity = item.m_Edge;
			edge = flowEdges[entity];
			if (edge.m_Start == startNode && edge.m_End == endNode)
			{
				return true;
			}
		}
		entity = default(Entity);
		edge = default(ElectricityFlowEdge);
		return false;
	}

	public static bool TrySetFlowEdge(Entity startNode, Entity endNode, FlowDirection direction, int capacity, ref BufferLookup<ConnectedFlowEdge> flowConnections, ref ComponentLookup<ElectricityFlowEdge> flowEdges)
	{
		if (TryGetFlowEdge(startNode, endNode, ref flowConnections, ref flowEdges, out var entity, out var edge))
		{
			edge.direction = direction;
			edge.m_Capacity = capacity;
			flowEdges[entity] = edge;
			return true;
		}
		return false;
	}

	public static Entity CreateFlowEdge(EntityCommandBuffer commandBuffer, EntityArchetype edgeArchetype, Entity startNode, Entity endNode, FlowDirection direction, int capacity)
	{
		Assert.AreNotEqual(startNode, Entity.Null);
		Assert.AreNotEqual(endNode, Entity.Null);
		Entity entity = commandBuffer.CreateEntity(edgeArchetype);
		commandBuffer.SetComponent(entity, new ElectricityFlowEdge
		{
			m_Start = startNode,
			m_End = endNode,
			direction = direction,
			m_Capacity = capacity
		});
		commandBuffer.AppendToBuffer(startNode, new ConnectedFlowEdge(entity));
		commandBuffer.AppendToBuffer(endNode, new ConnectedFlowEdge(entity));
		return entity;
	}

	public static Entity CreateFlowEdge(EntityCommandBuffer.ParallelWriter commandBuffer, int jobIndex, EntityArchetype edgeArchetype, Entity startNode, Entity endNode, FlowDirection direction, int capacity)
	{
		Assert.AreNotEqual(startNode, Entity.Null);
		Assert.AreNotEqual(endNode, Entity.Null);
		Entity entity = commandBuffer.CreateEntity(jobIndex, edgeArchetype);
		commandBuffer.SetComponent(jobIndex, entity, new ElectricityFlowEdge
		{
			m_Start = startNode,
			m_End = endNode,
			direction = direction,
			m_Capacity = capacity
		});
		commandBuffer.AppendToBuffer(jobIndex, startNode, new ConnectedFlowEdge(entity));
		commandBuffer.AppendToBuffer(jobIndex, endNode, new ConnectedFlowEdge(entity));
		return entity;
	}

	public static Entity CreateFlowEdge(EntityManager entityManager, EntityArchetype edgeArchetype, Entity startNode, Entity endNode, FlowDirection direction, int capacity)
	{
		Assert.AreNotEqual(startNode, Entity.Null);
		Assert.AreNotEqual(endNode, Entity.Null);
		Entity entity = entityManager.CreateEntity(edgeArchetype);
		entityManager.SetComponentData(entity, new ElectricityFlowEdge
		{
			m_Start = startNode,
			m_End = endNode,
			direction = direction,
			m_Capacity = capacity
		});
		entityManager.GetBuffer<ConnectedFlowEdge>(startNode).Add(new ConnectedFlowEdge(entity));
		entityManager.GetBuffer<ConnectedFlowEdge>(endNode).Add(new ConnectedFlowEdge(entity));
		return entity;
	}

	public static void DeleteFlowNode(EntityCommandBuffer.ParallelWriter commandBuffer, int jobIndex, Entity node, ref BufferLookup<ConnectedFlowEdge> flowConnections)
	{
		commandBuffer.AddComponent<Deleted>(jobIndex, node);
		foreach (ConnectedFlowEdge item in flowConnections[node])
		{
			commandBuffer.AddComponent<Deleted>(jobIndex, item.m_Edge);
		}
	}

	public static void DeleteFlowNode(EntityManager entityManager, Entity node)
	{
		entityManager.AddComponent<Deleted>(node);
		foreach (ConnectedFlowEdge item in entityManager.GetBuffer<ConnectedFlowEdge>(node, isReadOnly: true))
		{
			entityManager.AddComponent<Deleted>(item.m_Edge);
		}
	}

	public static void DeleteBuildingNodes(EntityCommandBuffer.ParallelWriter commandBuffer, int jobIndex, ElectricityBuildingConnection connection, ref BufferLookup<ConnectedFlowEdge> flowConnections, ref ComponentLookup<ElectricityFlowEdge> flowEdges)
	{
		if (connection.m_TransformerNode != Entity.Null)
		{
			DeleteFlowNode(commandBuffer, jobIndex, connection.m_TransformerNode, ref flowConnections);
		}
		if (connection.m_ProducerEdge != Entity.Null)
		{
			DeleteFlowNode(commandBuffer, jobIndex, connection.GetProducerNode(ref flowEdges), ref flowConnections);
		}
		if (connection.m_ConsumerEdge != Entity.Null)
		{
			DeleteFlowNode(commandBuffer, jobIndex, connection.GetConsumerNode(ref flowEdges), ref flowConnections);
		}
		if (connection.m_ChargeEdge != Entity.Null)
		{
			DeleteFlowNode(commandBuffer, jobIndex, connection.GetChargeNode(ref flowEdges), ref flowConnections);
		}
		if (connection.m_DischargeEdge != Entity.Null)
		{
			DeleteFlowNode(commandBuffer, jobIndex, connection.GetDischargeNode(ref flowEdges), ref flowConnections);
		}
	}
}
