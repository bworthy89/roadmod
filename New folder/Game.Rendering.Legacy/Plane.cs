using Unity.Mathematics;

namespace Game.Rendering.Legacy;

public struct Plane
{
	private float3 m_Normal;

	private float m_Distance;

	public float3 normal
	{
		get
		{
			return m_Normal;
		}
		set
		{
			m_Normal = value;
		}
	}

	public float distance
	{
		get
		{
			return m_Distance;
		}
		set
		{
			m_Distance = value;
		}
	}

	public Plane flipped => new Plane(-m_Normal, 0f - m_Distance);

	public Plane(float3 inNormal, float3 inPoint)
	{
		m_Normal = math.normalize(inNormal);
		m_Distance = 0f - math.dot(m_Normal, inPoint);
	}

	public Plane(float3 inNormal, float d)
	{
		m_Normal = math.normalize(inNormal);
		m_Distance = d;
	}

	public Plane(float3 a, float3 b, float3 c)
	{
		m_Normal = math.normalize(math.cross(b - a, c - a));
		m_Distance = 0f - math.dot(m_Normal, a);
	}

	public void SetNormalAndPosition(float3 inNormal, float3 inPoint)
	{
		m_Normal = math.normalize(inNormal);
		m_Distance = 0f - math.dot(inNormal, inPoint);
	}

	public void Set3Points(float3 a, float3 b, float3 c)
	{
		m_Normal = math.normalize(math.cross(b - a, c - a));
		m_Distance = 0f - math.dot(m_Normal, a);
	}

	public void Flip()
	{
		m_Normal = -m_Normal;
		m_Distance = 0f - m_Distance;
	}

	public void Translate(float3 translation)
	{
		m_Distance += math.dot(m_Normal, translation);
	}

	public static Plane Translate(Plane plane, float3 translation)
	{
		return new Plane(plane.m_Normal, plane.m_Distance += math.dot(plane.m_Normal, translation));
	}

	public float3 ClosestPointOnPlane(float3 point)
	{
		float num = math.dot(m_Normal, point) + m_Distance;
		return point - m_Normal * num;
	}

	public float GetDistanceToPoint(float3 point)
	{
		return math.dot(m_Normal, point) + m_Distance;
	}

	public bool GetSide(float3 point)
	{
		return math.dot(m_Normal, point) + m_Distance > 0f;
	}

	public bool SameSide(float3 inPt0, float3 inPt1)
	{
		float distanceToPoint = GetDistanceToPoint(inPt0);
		float distanceToPoint2 = GetDistanceToPoint(inPt1);
		if (!(distanceToPoint > 0f) || !(distanceToPoint2 > 0f))
		{
			if (distanceToPoint <= 0f)
			{
				return distanceToPoint2 <= 0f;
			}
			return false;
		}
		return true;
	}

	public override string ToString()
	{
		return $"(normal:({m_Normal.x:F1}, {m_Normal.y:F1}, {m_Normal.z:F1}), distance:{m_Distance:F1})";
	}
}
