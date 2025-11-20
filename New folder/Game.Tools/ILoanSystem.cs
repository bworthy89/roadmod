namespace Game.Tools;

public interface ILoanSystem
{
	LoanInfo CurrentLoan { get; }

	int Creditworthiness { get; }

	LoanInfo RequestLoanOffer(int amount);

	void ChangeLoan(int amount);
}
