using Colossal.Mathematics;
using Unity.Entities;

namespace Game.Tools;

public struct Zoning : IComponentData, IQueryTypeParameter
{
	public Quad3 m_Position;

	public ZoningFlags m_Flags;
}
