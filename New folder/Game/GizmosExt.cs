using Colossal;
using Game.Net;
using UnityEngine;

namespace Game;

public static class GizmosExt
{
	public static void DrawCurve(this GizmoBatcher batcher, Curve curve, Color color, int segmentsCount = -1)
	{
		batcher.DrawCurve(curve.m_Bezier, curve.m_Length, color, segmentsCount);
	}

	public static void DrawDirectionalCurve(this GizmoBatcher batcher, Curve curve, Color color, bool reverse = false, int segmentsCount = -1, float arrowHeadLength = 0.4f, float arrowHeadAngle = 25f, int circleSegmentsCount = 16)
	{
		batcher.DrawDirectionalCurve(curve.m_Bezier, curve.m_Length, color, reverse, segmentsCount, arrowHeadLength, arrowHeadAngle, circleSegmentsCount);
	}

	public static void DrawFlowCurve(this GizmoBatcher batcher, Curve curve, Color color, float timeOffset = 0f, bool reverse = false, int arrowCount = 2, int segmentsCount = 16, float arrowHeadLength = 0.4f, float arrowHeadAngle = 25f, int circleSegmentsCount = 16)
	{
		batcher.DrawFlowCurve(curve.m_Bezier, curve.m_Length, color, timeOffset, reverse, arrowCount, segmentsCount, arrowHeadLength, arrowHeadAngle, circleSegmentsCount);
	}
}
