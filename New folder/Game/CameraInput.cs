using Colossal.Mathematics;
using Game.Input;
using UnityEngine;

namespace Game;

public class CameraInput : MonoBehaviour
{
	public float m_MoveSmoothing = 1E-06f;

	public float m_RotateSmoothing = 1E-06f;

	public float m_ZoomSmoothing = 1E-06f;

	private ProxyAction m_MoveAction;

	private ProxyAction m_FastMoveAction;

	private ProxyAction m_RotateAction;

	private ProxyAction m_ZoomAction;

	public Vector2 move { get; private set; }

	public Vector2 rotate { get; private set; }

	public float zoom { get; private set; }

	public bool isMoving => m_MoveAction.IsInProgress();

	public bool any
	{
		get
		{
			if (!m_MoveAction.IsInProgress() && !m_FastMoveAction.IsInProgress() && !m_RotateAction.IsInProgress())
			{
				return m_ZoomAction.IsInProgress();
			}
			return true;
		}
	}

	public void Initialize()
	{
		m_MoveAction = InputManager.instance.FindAction("Camera", "Move");
		m_FastMoveAction = InputManager.instance.FindAction("Camera", "Move Fast");
		m_RotateAction = InputManager.instance.FindAction("Camera", "Rotate");
		m_ZoomAction = InputManager.instance.FindAction("Camera", "Zoom");
	}

	public void Refresh()
	{
		move = MathUtils.MaxAbs(m_MoveAction.ReadValue<Vector2>(), m_FastMoveAction.ReadValue<Vector2>());
		rotate = m_RotateAction.ReadValue<Vector2>();
		zoom = m_ZoomAction.ReadValue<float>();
	}
}
