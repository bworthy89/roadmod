using System.Runtime.InteropServices;

namespace Game.Simulation.Flow;

[StructLayout(LayoutKind.Explicit)]
public struct Node
{
	[FieldOffset(0)]
	public int m_FirstConnection;

	[FieldOffset(4)]
	public int m_LastConnection;

	[FieldOffset(8)]
	public int m_Height;

	[FieldOffset(12)]
	public int m_Excess;

	[FieldOffset(16)]
	public int m_Version;

	[FieldOffset(20)]
	public Identifier m_CutElementId;

	[FieldOffset(28)]
	public bool m_Retreat;

	[FieldOffset(20)]
	public int m_Distance;

	[FieldOffset(24)]
	public int m_Predecessor;

	[FieldOffset(28)]
	public bool m_Enqueued;

	public int connectionCount => m_LastConnection - m_FirstConnection;
}
