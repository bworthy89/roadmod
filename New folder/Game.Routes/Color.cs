using Colossal.Serialization.Entities;
using Unity.Entities;
using UnityEngine;

namespace Game.Routes;

public struct Color : IComponentData, IQueryTypeParameter, ISerializable
{
	public Color32 m_Color;

	public Color(Color32 color)
	{
		m_Color = color;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Color);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Color);
	}
}
