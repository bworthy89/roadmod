using System;
using Colossal.Collections;
using Unity.Collections;

namespace Game.Pathfind;

public struct PathfindAction : IDisposable
{
	public NativeReference<PathfindActionData> m_Data;

	public ref PathfindActionData data => ref m_Data.ValueAsRef();

	public PathfindActionData readOnlyData => m_Data.Value;

	public PathfindAction(int startCount, int endCount, Allocator allocator, PathfindParameters parameters, SetupTargetType originType, SetupTargetType destinationType)
	{
		m_Data = new NativeReference<PathfindActionData>(new PathfindActionData(startCount, endCount, allocator, parameters, originType, destinationType), allocator);
	}

	public void Dispose()
	{
		data.Dispose();
		m_Data.Dispose();
	}
}
