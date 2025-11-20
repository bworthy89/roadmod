using System;
using Colossal.Collections;
using Unity.Collections;

namespace Game.Pathfind;

public struct CoverageAction : IDisposable
{
	public NativeReference<CoverageActionData> m_Data;

	public ref CoverageActionData data => ref m_Data.ValueAsRef();

	public CoverageAction(Allocator allocator)
	{
		m_Data = new NativeReference<CoverageActionData>(new CoverageActionData(allocator), allocator);
	}

	public void Dispose()
	{
		data.Dispose();
		m_Data.Dispose();
	}
}
