namespace LoanOps.Api.Tests;

public class UnitTest1
{
    [Fact]
    public void NewApplicationStartsInIntake()
    {
        var store = new LoanApplicationStore();
        var service = new LoanApplicationService(store);
        var result = service.Create(new CreateApplicationRequest("tenant-demo", "Synthetic Borrower", 125000m, "usd", "Equipment", "rm-demo"));

        Assert.Null(result.Error);
        Assert.Equal("Intake", result.Application!.Status);
        Assert.Equal(125000m, result.Application.RequestedAmount);
    }

    [Fact]
    public void InvalidLifecycleTransitionIsRejected()
    {
        var store = new LoanApplicationStore();
        var service = new LoanApplicationService(store);
        var application = service.Create(new CreateApplicationRequest("tenant-demo", "Synthetic Borrower", 125000m, "USD", "Equipment", "rm-demo")).Application!;

        var result = service.Transition(application.Id, new TransitionRequest("Approved", "skip", "approver-demo"));

        Assert.Equal("The requested lifecycle transition is not allowed.", result.Error);
    }
}
