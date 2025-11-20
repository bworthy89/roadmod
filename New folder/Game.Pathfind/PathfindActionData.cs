using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Game.Pathfind;

public struct PathfindActionData : IDisposable
{
	public UnsafeList<PathTarget> m_StartTargets;

	public UnsafeList<PathTarget> m_EndTargets;

	public UnsafeList<PathfindResult> m_Result;

	public UnsafeList<PathfindPath> m_Path;

	public PathfindParameters m_Parameters;

	public SetupTargetType m_OriginType;

	public SetupTargetType m_DestinationType;

	public PathfindActionState m_State;

	public PathfindActionData(int startCount, int endCount, Allocator allocator, PathfindParameters parameters, SetupTargetType originType, SetupTargetType destinationType)
	{
		if (startCount != 0)
		{
			m_StartTargets = new UnsafeList<PathTarget>(startCount, allocator);
			m_StartTargets.Length = startCount;
		}
		else
		{
			m_StartTargets = default(UnsafeList<PathTarget>);
		}
		if (endCount != 0)
		{
			m_EndTargets = new UnsafeList<PathTarget>(endCount, allocator);
			m_EndTargets.Length = endCount;
		}
		else
		{
			m_EndTargets = default(UnsafeList<PathTarget>);
		}
		m_Result = new UnsafeList<PathfindResult>(1, allocator);
		if ((parameters.m_PathfindFlags & PathfindFlags.IgnorePath) == 0)
		{
			m_Path = new UnsafeList<PathfindPath>(100, allocator);
		}
		else
		{
			m_Path = default(UnsafeList<PathfindPath>);
		}
		m_Parameters = parameters;
		m_OriginType = originType;
		m_DestinationType = destinationType;
		m_State = PathfindActionState.Pending;
	}

	public void Dispose()
	{
		if (m_StartTargets.IsCreated)
		{
			m_StartTargets.Dispose();
		}
		if (m_EndTargets.IsCreated)
		{
			m_EndTargets.Dispose();
		}
		m_Result.Dispose();
		if (m_Path.IsCreated)
		{
			m_Path.Dispose();
		}
	}
}
