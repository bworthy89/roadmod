using Cinemachine;
using Game.Input;
using UnityEngine;

namespace Game;

public class CinemachineGameAxisProvider : MonoBehaviour, AxisState.IInputAxisProvider
{
	private ProxyAction m_RotateAction;

	private ProxyAction m_ZoomAction;

	private void Awake()
	{
		m_RotateAction = InputManager.instance.FindAction("Camera", "Rotate");
		m_ZoomAction = InputManager.instance.FindAction("Camera", "Zoom");
	}

	public float GetAxisValue(int axis)
	{
		return axis switch
		{
			0 => m_RotateAction.ReadRawValue<Vector2>(disableAll: false).x, 
			1 => m_RotateAction.ReadRawValue<Vector2>(disableAll: false).y, 
			2 => m_ZoomAction.ReadRawValue<float>(disableAll: false), 
			_ => 0f, 
		};
	}
}
