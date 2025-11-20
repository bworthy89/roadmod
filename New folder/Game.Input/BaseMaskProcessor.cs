using UnityEngine.InputSystem;

namespace Game.Input;

public abstract class BaseMaskProcessor<TValue> : InputProcessor<TValue> where TValue : struct
{
	public InputManager.DeviceType m_Mask;

	public int m_Index;

	private ProxyAction m_Action;

	public override TValue Process(TValue value, InputControl control)
	{
		if (m_Index == -1)
		{
			return value;
		}
		if ((m_Action == null || m_Index != m_Action.m_GlobalIndex) && !InputManager.instance.TryFindAction(m_Index, out m_Action))
		{
			m_Index = -1;
			return value;
		}
		if ((m_Action.mask & m_Mask) != InputManager.DeviceType.None)
		{
			return value;
		}
		return default(TValue);
	}
}
