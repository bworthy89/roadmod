using System;
using Colossal.Serialization.Entities;

namespace Game.Prefabs;

public struct PrefabID : IEquatable<PrefabID>, ISerializable
{
	private string m_Type;

	private string m_Name;

	public PrefabID(PrefabBase prefab)
	{
		m_Type = prefab.GetType().Name;
		m_Name = prefab.name;
	}

	public PrefabID(string type, string name)
	{
		m_Type = type;
		m_Name = name;
	}

	public bool Equals(PrefabID other)
	{
		if (m_Type.Equals(other.m_Type))
		{
			return m_Name.Equals(other.m_Name);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return m_Name.GetHashCode();
	}

	public override string ToString()
	{
		return $"{m_Type}:{m_Name}";
	}

	public string GetName()
	{
		return m_Name;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		string type = m_Type;
		writer.Write(type);
		string name = m_Name;
		writer.Write(name);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version < Version.newPrefabID)
		{
			reader.Read(out string value);
			string[] array = value.Split(':');
			m_Type = array[0];
			m_Name = array[1];
		}
		else
		{
			ref string type = ref m_Type;
			reader.Read(out type);
			ref string name = ref m_Name;
			reader.Read(out name);
		}
		if (reader.context.version < Version.staticObjectPrefab && m_Type == "ObjectGeometryPrefab")
		{
			m_Type = "StaticObjectPrefab";
		}
	}
}
