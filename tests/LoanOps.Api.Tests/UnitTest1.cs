namespace LoanOps.Api.Tests;

public class UnitTest1
{
    [Fact]
    public void NewApplicationStartsInIntake()
    {
        var store = new LoanApplicationStore();
        var service = new LoanApplicationService(store, new WorkflowFeatureStore(store));
        var result = service.Create(new CreateApplicationRequest("tenant-demo", "Synthetic Borrower", 125000m, "usd", "Equipment", "rm-demo"));

        Assert.Null(result.Error);
        Assert.Equal("Intake", result.Application!.Status);
        Assert.Equal(125000m, result.Application.RequestedAmount);
    }

    [Fact]
    public void InvalidLifecycleTransitionIsRejected()
    {
        var store = new LoanApplicationStore();
        var service = new LoanApplicationService(store, new WorkflowFeatureStore(store));
        var application = service.Create(new CreateApplicationRequest("tenant-demo", "Synthetic Borrower", 125000m, "USD", "Equipment", "rm-demo")).Application!;

        var result = service.Transition(application.Id, new TransitionRequest("Approved", "skip", "approver-demo"));

        Assert.Equal("The requested lifecycle transition is not allowed.", result.Error);
    }

    [Fact]
    public void RecommendationRequiresExplainabilityFactors()
    {
        var applications = new LoanApplicationStore();
        var application = applications.Add(new CreateApplicationRequest("tenant-demo", "Synthetic Borrower", 125000m, "USD", "Equipment", "rm-demo"));
        var features = new WorkflowFeatureStore(applications);

        var result = features.AddRecommendation(application.Id, "Approve", "Strong cash flow", [], "analyst-demo");

        Assert.Equal("Recommendation rationale and factors are invalid.", result.Error);
    }

    [Fact]
    public void DecisionRequiresRecommendationAndRejectedDocumentsRequireFindings()
    {
        var applications = new LoanApplicationStore();
        var application = applications.Add(new CreateApplicationRequest("tenant-demo", "Synthetic Borrower", 125000m, "USD", "Equipment", "rm-demo"));
        var features = new WorkflowFeatureStore(applications);
        var service = new LoanApplicationService(applications, features);
        service.Transition(application.Id, new TransitionRequest("Analysis", "Begin analysis", "analyst-demo"));
        service.Transition(application.Id, new TransitionRequest("PendingApproval", "Submit for approval", "analyst-demo"));

        var decision = features.AddDecision(application.Id, "Approved", "Looks good", "approver-demo");
        var document = features.AddDocument(application.Id, "Tax return", "tax-return.pdf");
        var verification = features.VerifyDocument(document.Item!.Id, "Rejected", "reviewer-demo", null);

        Assert.Equal("A current credit recommendation is required before a decision.", decision.Error);
        Assert.Equal("Findings are required when a document is rejected.", verification.Error);
    }

    [Fact]
    public void ApprovalRequiresPendingStatusAndSeparateHumanDecision()
    {
        var applications = new LoanApplicationStore();
        var application = applications.Add(new CreateApplicationRequest("tenant-demo", "Synthetic Borrower", 125000m, "USD", "Equipment", "rm-demo"));
        var features = new WorkflowFeatureStore(applications);
        var service = new LoanApplicationService(applications, features);

        var recommendation = features.AddRecommendation(application.Id, "Approve with conditions", "Strong repayment capacity", ["DSCR 1.8x", "Stable revenue"], "analyst-demo");
        var earlyDecision = features.AddDecision(application.Id, "Approved", "Not ready", "approver-demo");
        var pending = service.Transition(application.Id, new TransitionRequest("Analysis", "Begin analysis", "analyst-demo"));

        Assert.Null(recommendation.Error);
        Assert.Equal("The application must be pending approval before a decision can be recorded.", earlyDecision.Error);
        Assert.Null(pending.Error);
    }

    [Fact]
    public void CollateralLifecycleRequiresReviewBeforePerfection()
    {
        var applications = new LoanApplicationStore();
        var application = applications.Add(new CreateApplicationRequest("tenant-demo", "Synthetic Borrower", 125000m, "USD", "Equipment", "rm-demo"));
        var features = new WorkflowFeatureStore(applications);
        var collateral = features.AddCollateral(application.Id, "Equipment", "CNC machine", 50000m, "USD");

        var result = features.UpdateCollateral(collateral.Item!.Id, "Perfected");

        Assert.Equal("The requested collateral transition is not allowed.", result.Error);
    }
}
