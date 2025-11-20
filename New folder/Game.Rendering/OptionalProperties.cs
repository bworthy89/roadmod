using System;
using Colossal.Rendering;
using Game.Prefabs;

namespace Game.Rendering;

public struct OptionalProperties : IOptionalProperties<OptionalProperties>, IEquatable<OptionalProperties>
{
	private BatchFlags m_Flags;

	private MeshType m_MeshTypes;

	public OptionalProperties(BatchFlags flags, MeshType meshTypes)
	{
		m_Flags = flags;
		m_MeshTypes = meshTypes;
	}

	public bool EnableProperty(OptionalProperties required)
	{
		if ((m_Flags & required.m_Flags) == required.m_Flags)
		{
			if ((m_MeshTypes & required.m_MeshTypes) == 0)
			{
				return required.m_MeshTypes == (MeshType)0;
			}
			return true;
		}
		return false;
	}

	public bool MergeRequirements(OptionalProperties other)
	{
		m_MeshTypes |= other.m_MeshTypes;
		return m_Flags == other.m_Flags;
	}

	public bool Equals(OptionalProperties other)
	{
		if (m_Flags == other.m_Flags)
		{
			return m_MeshTypes == other.m_MeshTypes;
		}
		return false;
	}
}
