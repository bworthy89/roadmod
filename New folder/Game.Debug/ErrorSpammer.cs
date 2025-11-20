using System;
using Colossal.Logging;

namespace Game.Debug;

public class ErrorSpammer
{
	private readonly TimeSpan m_IntervalBetweenEmissions;

	private readonly int m_MessagesPerEmission;

	private readonly int m_MaxEmissions;

	private bool m_IsRunning = true;

	private DateTime m_LastEmissionUtc = DateTime.MinValue;

	private int m_EmissionsEmitted;

	public bool isRunning => m_IsRunning;

	public DateTime lastEmissionUtc => m_LastEmissionUtc;

	public int emissionsEmitted => m_EmissionsEmitted;

	public ErrorSpammer(TimeSpan intervalBetweenEmissions, int messagesPerEmission = 1, int maxEmissions = -1, bool autoStart = true)
	{
		m_IntervalBetweenEmissions = intervalBetweenEmissions;
		m_MessagesPerEmission = Math.Max(1, messagesPerEmission);
		m_MaxEmissions = maxEmissions;
		if (autoStart)
		{
			Start();
		}
	}

	public void Update()
	{
		if (m_IsRunning)
		{
			DateTime utcNow = DateTime.UtcNow;
			bool flag = m_LastEmissionUtc == DateTime.MinValue;
			if (m_IntervalBetweenEmissions <= TimeSpan.Zero || flag || utcNow - m_LastEmissionUtc >= m_IntervalBetweenEmissions)
			{
				EmitErrors(utcNow);
			}
		}
	}

	public void EmitNow()
	{
		if (m_IsRunning)
		{
			EmitErrors(DateTime.UtcNow);
		}
	}

	public void Start(bool reset = false)
	{
		if (reset)
		{
			Reset();
		}
		m_IsRunning = true;
	}

	public void Stop()
	{
		m_IsRunning = false;
	}

	public void Reset()
	{
		m_LastEmissionUtc = DateTime.MinValue;
		m_EmissionsEmitted = 0;
	}

	private void EmitErrors(DateTime nowUtc)
	{
		for (int i = 0; i < m_MessagesPerEmission; i++)
		{
			ILog fileSystem = LogManager.FileSystem;
			TimeSpan intervalBetweenEmissions = m_IntervalBetweenEmissions;
			fileSystem.Error($"[ErrorLogTimer] interval={intervalBetweenEmissions.TotalSeconds:0.###}s " + $"messagesPerEmission={m_MessagesPerEmission} maxEmissions={m_MaxEmissions} ");
		}
		m_EmissionsEmitted++;
		m_LastEmissionUtc = nowUtc;
		if (m_MaxEmissions >= 0 && m_EmissionsEmitted >= m_MaxEmissions)
		{
			m_IsRunning = false;
		}
	}
}
