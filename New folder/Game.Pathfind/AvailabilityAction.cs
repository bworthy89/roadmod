using System;
using Colossal.Collections;
using Unity.Collections;

namespace Game.Pathfind;

public struct AvailabilityAction : IDisposable
{
	public NativeReference<AvailabilityActionData> m_Data;

	public ref AvailabilityActionData data => ref m_Data.ValueAsRef();

	public AvailabilityAction(Allocator allocator, AvailabilityParameters parameters)
	{
		m_Data = new NativeReference<AvailabilityActionData>(new AvailabilityActionData(allocator, parameters), allocator);
	}

	public void Dispose()
	{
		data.Dispose();
		m_Data.Dispose();
	}
}
