using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace Game.Input;

public class DeviceListener : IInputStateChangeMonitor, IDisposable
{
	public class DeviceEvent : UnityEvent<InputDevice>
	{
	}

	private List<InputControl> m_Controls;

	private bool m_Listening;

	private float m_RequiredDelta;

	private float m_Delta;

	private bool m_Activated;

	public DeviceEvent EventDeviceActivated;

	public InputDevice device { get; private set; }

	public DeviceListener(InputDevice device, float requiredDelta)
	{
		EventDeviceActivated = new DeviceEvent();
		this.device = device;
		m_Controls = new List<InputControl>();
		m_RequiredDelta = requiredDelta;
		foreach (InputControl allControl in device.allControls)
		{
			if (ValidateControl(allControl))
			{
				m_Controls.Add(allControl);
			}
		}
	}

	public void Tick()
	{
		m_Delta = Math.Max(m_Delta - Time.deltaTime, 0f);
		if (m_Activated)
		{
			m_Activated = false;
			EventDeviceActivated?.Invoke(device);
		}
	}

	public void StartListening()
	{
		if (m_Listening)
		{
			return;
		}
		m_Activated = false;
		m_Listening = true;
		m_Delta = 0f;
		foreach (InputControl control in m_Controls)
		{
			InputState.AddChangeMonitor(control, this, -1L);
		}
	}

	public void StopListening()
	{
		if (!m_Listening)
		{
			return;
		}
		m_Activated = false;
		m_Listening = false;
		m_Delta = 0f;
		foreach (InputControl control in m_Controls)
		{
			InputState.RemoveChangeMonitor(control, this, -1L);
		}
	}

	private bool ValidateControl(InputControl control)
	{
		if (control is ButtonControl)
		{
			return true;
		}
		return false;
	}

	public void NotifyControlStateChanged(InputControl control, double time, InputEventPtr eventPtr, long monitorIndex)
	{
		if (control is ButtonControl && ((ButtonControl)control).wasPressedThisFrame)
		{
			m_Activated = true;
		}
	}

	public void NotifyTimerExpired(InputControl control, double time, long monitorIndex, int timerIndex)
	{
	}

	public void Dispose()
	{
		StopListening();
	}
}
