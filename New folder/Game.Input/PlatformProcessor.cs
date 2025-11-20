using Colossal;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Input;

public abstract class PlatformProcessor<TValue> : InputProcessor<TValue> where TValue : struct
{
	public Platform m_Platform = Platform.All;

	public ProcessorDeviceType m_DeviceType;

	private bool? m_NeedProcess;

	protected bool needProcess
	{
		get
		{
			bool valueOrDefault = m_NeedProcess == true;
			if (!m_NeedProcess.HasValue)
			{
				valueOrDefault = m_Platform.IsPlatformSet(Application.platform);
				m_NeedProcess = valueOrDefault;
				return valueOrDefault;
			}
			return valueOrDefault;
		}
	}
}
