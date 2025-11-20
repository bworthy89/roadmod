using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Game.Pathfind;

public struct AvailabilityActionData : IDisposable
{
	public UnsafeQueue<PathTarget> m_Sources;

	public UnsafeQueue<AvailabilityProvider> m_Providers;

	public UnsafeList<AvailabilityResult> m_Results;

	public AvailabilityParameters m_Parameters;

	public PathfindActionState m_State;

	public AvailabilityActionData(Allocator allocator, AvailabilityParameters parameters)
	{
		m_Sources = new UnsafeQueue<PathTarget>(allocator);
		m_Providers = new UnsafeQueue<AvailabilityProvider>(allocator);
		m_Results = new UnsafeList<AvailabilityResult>(100, allocator);
		m_Parameters = parameters;
		m_State = PathfindActionState.Pending;
	}

	public void Dispose()
	{
		m_Sources.Dispose();
		m_Providers.Dispose();
		m_Results.Dispose();
	}
}
