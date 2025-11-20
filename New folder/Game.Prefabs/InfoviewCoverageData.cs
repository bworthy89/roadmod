using Colossal.Mathematics;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

public struct InfoviewCoverageData : IComponentData, IQueryTypeParameter
{
	public CoverageService m_Service;

	public Bounds1 m_Range;
}
