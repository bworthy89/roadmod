using Colossal.Mathematics;

namespace Game.Tools;

public struct SnapLine
{
	public ControlPoint m_ControlPoint;

	public Bezier4x3 m_Curve;

	public SnapLineFlags m_Flags;

	public float m_HeightWeight;

	public SnapLine(ControlPoint position, Bezier4x3 curve, SnapLineFlags flags, float heightWeight)
	{
		m_ControlPoint = position;
		m_Curve = curve;
		m_Flags = flags;
		m_HeightWeight = heightWeight;
	}
}
