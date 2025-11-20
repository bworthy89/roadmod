using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct ElectricityBuildingConnection : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_TransformerNode;

	public Entity m_ProducerEdge;

	public Entity m_ConsumerEdge;

	public Entity m_ChargeEdge;

	public Entity m_DischargeEdge;

	public Entity GetProducerNode(ref ComponentLookup<ElectricityFlowEdge> flowEdges)
	{
		return flowEdges[m_ProducerEdge].m_End;
	}

	public Entity GetConsumerNode(ref ComponentLookup<ElectricityFlowEdge> flowEdges)
	{
		return flowEdges[m_ConsumerEdge].m_Start;
	}

	public Entity GetChargeNode(ref ComponentLookup<ElectricityFlowEdge> flowEdges)
	{
		return flowEdges[m_ChargeEdge].m_Start;
	}

	public Entity GetDischargeNode(ref ComponentLookup<ElectricityFlowEdge> flowEdges)
	{
		return flowEdges[m_DischargeEdge].m_End;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity transformerNode = m_TransformerNode;
		writer.Write(transformerNode);
		Entity producerEdge = m_ProducerEdge;
		writer.Write(producerEdge);
		Entity consumerEdge = m_ConsumerEdge;
		writer.Write(consumerEdge);
		Entity chargeEdge = m_ChargeEdge;
		writer.Write(chargeEdge);
		Entity dischargeEdge = m_DischargeEdge;
		writer.Write(dischargeEdge);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity transformerNode = ref m_TransformerNode;
		reader.Read(out transformerNode);
		ref Entity producerEdge = ref m_ProducerEdge;
		reader.Read(out producerEdge);
		ref Entity consumerEdge = ref m_ConsumerEdge;
		reader.Read(out consumerEdge);
		ref Entity chargeEdge = ref m_ChargeEdge;
		reader.Read(out chargeEdge);
		ref Entity dischargeEdge = ref m_DischargeEdge;
		reader.Read(out dischargeEdge);
	}
}
