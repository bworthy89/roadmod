using Game.Tools;
using Unity.Entities;

namespace Game.Prefabs;

public struct ToolErrorData : IComponentData, IQueryTypeParameter
{
	public ErrorType m_Error;

	public ToolErrorFlags m_Flags;
}
