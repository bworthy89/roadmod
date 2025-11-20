using Colossal.Serialization.Entities;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

public struct LotData : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_MaxRadius;

	public Color32 m_RangeColor;

	public bool m_OnWater;

	public bool m_AllowOverlap;

	public bool m_AllowEditing;

	public LotData(float maxRadius, Color32 rangeColor, bool onWater, bool allowOverlap, bool allowEditing)
	{
		m_MaxRadius = maxRadius;
		m_RangeColor = rangeColor;
		m_OnWater = onWater;
		m_AllowOverlap = allowOverlap;
		m_AllowEditing = allowEditing;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float maxRadius = m_MaxRadius;
		writer.Write(maxRadius);
		bool onWater = m_OnWater;
		writer.Write(onWater);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float maxRadius = ref m_MaxRadius;
		reader.Read(out maxRadius);
		ref bool onWater = ref m_OnWater;
		reader.Read(out onWater);
	}
}
