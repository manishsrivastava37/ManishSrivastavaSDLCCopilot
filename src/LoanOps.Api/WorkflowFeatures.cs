using System.Collections.Concurrent;

public sealed record CreditRecommendation(Guid Id, Guid ApplicationId, string Recommendation, string Rationale, string AnalystId, DateTimeOffset CreatedAt);
public sealed record CreditDecision(Guid Id, Guid ApplicationId, string Decision, string Rationale, string ApproverId, DateTimeOffset DecidedAt);
public sealed record Collateral(Guid Id, Guid ApplicationId, string Type, string Description, decimal Value, string Currency, string Status, DateTimeOffset CreatedAt);
public sealed record DocumentRecord(Guid Id, Guid ApplicationId, string Category, string FileName, string VerificationStatus, string? Findings, DateTimeOffset UploadedAt);
public sealed record WorkflowTask(Guid Id, Guid ApplicationId, string TaskType, string AssigneeId, string Status, DateTimeOffset DueAt);

public sealed class WorkflowFeatureStore
{
    private readonly ConcurrentDictionary<Guid, CreditRecommendation> recommendations = new();
    private readonly ConcurrentDictionary<Guid, CreditDecision> decisions = new();
    private readonly ConcurrentDictionary<Guid, Collateral> collateral = new();
    private readonly ConcurrentDictionary<Guid, DocumentRecord> documents = new();
    private readonly ConcurrentDictionary<Guid, WorkflowTask> tasks = new();

    public WorkflowFeatureStore(LoanApplicationStore applications)
    {
        var seeded = applications.Add(new CreateApplicationRequest("tenant-demo", "Northstar Industrial Supply (Demo)", 875000m, "USD", "Warehouse expansion", "rm-demo"));
        tasks[Guid.NewGuid()] = new WorkflowTask(Guid.NewGuid(), seeded.Id, "Initial Review", "ops-demo", "Open", DateTimeOffset.UtcNow.AddDays(2));
        documents[Guid.NewGuid()] = new DocumentRecord(Guid.NewGuid(), seeded.Id, "Financial Statements", "northstar-fy2025.pdf", "Verified", null, DateTimeOffset.UtcNow.AddDays(-2));
        collateral[Guid.NewGuid()] = new Collateral(Guid.NewGuid(), seeded.Id, "Real Estate", "Demo warehouse property", 1250000m, "USD", "Under Review", DateTimeOffset.UtcNow.AddDays(-1));
    }

    public IReadOnlyCollection<CreditRecommendation> Recommendations(Guid applicationId) => recommendations.Values.Where(item => item.ApplicationId == applicationId).ToArray();
    public IReadOnlyCollection<CreditDecision> Decisions(Guid applicationId) => decisions.Values.Where(item => item.ApplicationId == applicationId).ToArray();
    public IReadOnlyCollection<Collateral> Collateral(Guid applicationId) => collateral.Values.Where(item => item.ApplicationId == applicationId).ToArray();
    public IReadOnlyCollection<DocumentRecord> Documents(Guid applicationId) => documents.Values.Where(item => item.ApplicationId == applicationId).ToArray();
    public IReadOnlyCollection<WorkflowTask> Tasks(Guid applicationId) => tasks.Values.Where(item => item.ApplicationId == applicationId).ToArray();

    public CreditRecommendation AddRecommendation(Guid applicationId, string recommendation, string rationale, string analystId)
    {
        var item = new CreditRecommendation(Guid.NewGuid(), applicationId, recommendation, rationale, analystId, DateTimeOffset.UtcNow);
        recommendations[item.Id] = item;
        return item;
    }

    public CreditDecision AddDecision(Guid applicationId, string decision, string rationale, string approverId)
    {
        var item = new CreditDecision(Guid.NewGuid(), applicationId, decision, rationale, approverId, DateTimeOffset.UtcNow);
        decisions[item.Id] = item;
        return item;
    }

    public Collateral AddCollateral(Guid applicationId, string type, string description, decimal value, string currency)
    {
        var item = new Collateral(Guid.NewGuid(), applicationId, type, description, value, currency.ToUpperInvariant(), "Proposed", DateTimeOffset.UtcNow);
        collateral[item.Id] = item;
        return item;
    }

    public DocumentRecord AddDocument(Guid applicationId, string category, string fileName)
    {
        var item = new DocumentRecord(Guid.NewGuid(), applicationId, category, fileName, "Ready for Verification", null, DateTimeOffset.UtcNow);
        documents[item.Id] = item;
        return item;
    }
}

public sealed record RecommendationRequest(string Recommendation, string Rationale, string AnalystId);
public sealed record DecisionRequest(string Decision, string Rationale, string ApproverId);
public sealed record CollateralRequest(string Type, string Description, decimal Value, string Currency);
public sealed record DocumentRequest(string Category, string FileName);
