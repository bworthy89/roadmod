using System.Collections.Generic;
using UnityEngine;

namespace Game.Rendering;

public class WindPivotHelper : MonoBehaviour
{
	public enum PivotBakeMode
	{
		SingleDecompose,
		HierarchyDecompose
	}

	public PivotBakeMode m_BakedMode;

	public bool m_ShowBasePivot = true;

	public bool m_ShowLevel0Pivot = true;

	public bool m_ShowLevel0Guide = true;

	public bool m_ShowLevel1Pivot = true;

	public bool m_ShowLevel1Guide = true;

	public List<Vector3> m_PivotsP0 = new List<Vector3>();

	public List<Vector3> m_PivotsN0 = new List<Vector3>();

	public List<float> m_PivotsH0 = new List<float>();

	public List<Vector3> m_PivotsR1 = new List<Vector3>();

	public List<Vector3> m_PivotsP1 = new List<Vector3>();

	public List<Vector3> m_PivotsN1 = new List<Vector3>();

	public List<float> m_PivotsH1 = new List<float>();

	public void Clear()
	{
		m_PivotsP0.Clear();
		m_PivotsN0.Clear();
		m_PivotsH0.Clear();
		m_PivotsR1.Clear();
		m_PivotsP1.Clear();
		m_PivotsN1.Clear();
		m_PivotsH1.Clear();
	}

	private void OnDrawGizmosSelected()
	{
		Matrix4x4 localToWorldMatrix = base.transform.localToWorldMatrix;
		float num = Mathf.Max(base.transform.lossyScale.x, Mathf.Max(base.transform.lossyScale.y, base.transform.lossyScale.z));
		if (m_ShowLevel1Pivot || m_ShowLevel1Guide)
		{
			Color color = new Color(0f, 1f, 1f, 0.1f);
			for (int i = 0; i < m_PivotsP1.Count; i++)
			{
				Vector3 to = localToWorldMatrix.MultiplyPoint(m_PivotsR1[i]);
				Vector3 vector = localToWorldMatrix.MultiplyPoint(m_PivotsP1[i]);
				Vector3 vector2 = localToWorldMatrix.MultiplyVector(m_PivotsN1[i] * m_PivotsH1[i]);
				if (m_ShowLevel1Guide)
				{
					Gizmos.color = color;
					Gizmos.DrawLine(vector, to);
				}
				if (m_ShowLevel1Pivot)
				{
					Gizmos.color = Color.blue;
					Gizmos.DrawLine(vector, vector + vector2);
					Gizmos.DrawSphere(vector, 0.01f * num);
				}
			}
		}
		if (m_ShowLevel0Pivot || m_ShowLevel0Guide)
		{
			Color color2 = new Color(1f, 1f, 0f, 0.1f);
			for (int j = 0; j < m_PivotsP0.Count; j++)
			{
				Vector3 vector3 = localToWorldMatrix.MultiplyPoint(m_PivotsP0[j]);
				Vector3 vector4 = localToWorldMatrix.MultiplyVector(m_PivotsN0[j] * m_PivotsH0[j]);
				if (m_ShowLevel0Guide)
				{
					Gizmos.color = color2;
					Gizmos.DrawLine(vector3, base.transform.position);
				}
				if (m_ShowLevel0Pivot)
				{
					Gizmos.color = Color.green;
					Gizmos.DrawLine(vector3, vector3 + vector4);
					Gizmos.DrawSphere(vector3, 0.01f * num);
				}
			}
		}
		if (m_ShowBasePivot)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawSphere(base.transform.position, 0.1f * num);
		}
	}
}
