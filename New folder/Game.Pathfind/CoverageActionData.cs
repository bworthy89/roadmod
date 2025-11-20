using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Game.Pathfind;

public struct CoverageActionData : IDisposable
{
	public UnsafeQueue<PathTarget> m_Sources;

	public UnsafeList<CoverageResult> m_Results;

	public CoverageParameters m_Parameters;

	public PathfindActionState m_State;

	public CoverageActionData(Allocator allocator)
	{
		m_Sources = new UnsafeQueue<PathTarget>(allocator);
		m_Results = new UnsafeList<CoverageResult>(100, allocator);
		m_Parameters = default(CoverageParameters);
		m_State = PathfindActionState.Pending;
	}

	public void Dispose()
	{
		m_Sources.Dispose();
		m_Results.Dispose();
	}
}
