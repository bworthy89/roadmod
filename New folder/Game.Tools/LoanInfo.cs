using System;

namespace Game.Tools;

public struct LoanInfo : IEquatable<LoanInfo>
{
	public int m_Amount;

	public float m_DailyInterestRate;

	public int m_DailyPayment;

	public bool Equals(LoanInfo other)
	{
		if (m_Amount == other.m_Amount && m_DailyInterestRate.Equals(other.m_DailyInterestRate))
		{
			return m_DailyPayment == other.m_DailyPayment;
		}
		return false;
	}
}
