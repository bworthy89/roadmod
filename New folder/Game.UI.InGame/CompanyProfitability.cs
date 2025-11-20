using System;
using Colossal.UI.Binding;

namespace Game.UI.InGame;

public readonly struct CompanyProfitability : IJsonWritable
{
	private static readonly string[] kHappinessPaths = new string[5] { "Media/Game/Icons/CompanyBankrupt.svg", "Media/Game/Icons/CompanyLosingMoney.svg", "Media/Game/Icons/CompanyBreakingEven.svg", "Media/Game/Icons/CompanyGettingBy.svg", "Media/Game/Icons/CompanyProfitable.svg" };

	private CompanyProfitabilityKey key { get; }

	public CompanyProfitability(int profit)
	{
		key = CompanyUIUtils.GetProfitabilityKey(profit);
	}

	public CompanyProfitability(CompanyProfitabilityKey key)
	{
		this.key = key;
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(typeof(CompanyProfitability).FullName);
		writer.PropertyName("key");
		writer.Write(Enum.GetName(typeof(CompanyProfitabilityKey), key));
		writer.PropertyName("iconPath");
		writer.Write(kHappinessPaths[(int)key]);
		writer.TypeEnd();
	}
}
