using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Tools;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

public class LoanUISystem : UISystemBase
{
	public class LoanWriter : IWriter<LoanInfo>
	{
		public void Write(IJsonWriter writer, LoanInfo value)
		{
			writer.TypeBegin("loan.Loan");
			writer.PropertyName("amount");
			writer.Write(value.m_Amount);
			writer.PropertyName("dailyInterestRate");
			writer.Write(value.m_DailyInterestRate);
			writer.PropertyName("dailyPayment");
			writer.Write(value.m_DailyPayment);
			writer.TypeEnd();
		}
	}

	private const string kGroup = "loan";

	private GetterValueBinding<int> m_LoanLimitBinding;

	private GetterValueBinding<LoanInfo> m_CurrentLoanBinding;

	private GetterValueBinding<LoanInfo> m_LoanOfferBinding;

	private ILoanSystem m_LoanSystem;

	private int m_RequestedOfferDifference;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_LoanSystem = base.World.GetOrCreateSystemManaged<LoanSystem>();
		AddBinding(m_LoanLimitBinding = new GetterValueBinding<int>("loan", "loanLimit", () => m_LoanSystem.Creditworthiness));
		AddBinding(m_CurrentLoanBinding = new GetterValueBinding<LoanInfo>("loan", "currentLoan", () => m_LoanSystem.CurrentLoan, new LoanWriter()));
		AddBinding(m_LoanOfferBinding = new GetterValueBinding<LoanInfo>("loan", "loanOffer", () => m_LoanSystem.RequestLoanOffer(m_LoanSystem.CurrentLoan.m_Amount + m_RequestedOfferDifference), new LoanWriter()));
		AddBinding(new TriggerBinding<int>("loan", "requestLoanOffer", RequestLoanOffer));
		AddBinding(new TriggerBinding("loan", "acceptLoanOffer", AcceptLoanOffer));
		AddBinding(new TriggerBinding("loan", "resetLoanOffer", ResetLoanOffer));
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		m_RequestedOfferDifference = 0;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_LoanLimitBinding.Update();
		m_CurrentLoanBinding.Update();
		m_LoanOfferBinding.Update();
	}

	private void RequestLoanOffer(int amount)
	{
		LoanInfo loanInfo = m_LoanSystem.RequestLoanOffer(amount);
		LoanInfo currentLoan = m_LoanSystem.CurrentLoan;
		m_RequestedOfferDifference = loanInfo.m_Amount - currentLoan.m_Amount;
	}

	private void AcceptLoanOffer()
	{
		m_LoanSystem.ChangeLoan(m_LoanSystem.CurrentLoan.m_Amount + m_RequestedOfferDifference);
		m_RequestedOfferDifference = 0;
	}

	private void ResetLoanOffer()
	{
		m_RequestedOfferDifference = 0;
	}

	[Preserve]
	public LoanUISystem()
	{
	}
}
