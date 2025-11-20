using Unity.Entities;
using Unity.Mathematics;

namespace Game.Tools;

public struct ErrorData
{
	public Entity m_TempEntity;

	public Entity m_PermanentEntity;

	public float3 m_Position;

	public ErrorType m_ErrorType;

	public ErrorSeverity m_ErrorSeverity;
}
