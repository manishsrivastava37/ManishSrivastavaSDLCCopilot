using System.Collections.Concurrent;

public sealed record CreditRecommendation(Guid Id, Guid ApplicationId, string Recommendation, string Rationale, IReadOnlyCollection<string> Factors, string AnalystId, DateTimeOffset CreatedAt);
public sealed record CreditDecision(Guid Id, Guid ApplicationId, string Decision, string Rationale, string ApproverId, DateTimeOffset DecidedAt);
public sealed record Collateral(Guid Id, Guid ApplicationId, string Type, string Description, decimal Value, string Currency, string Status, DateTimeOffset CreatedAt);
public sealed record DocumentRecord(Guid Id, Guid ApplicationId, string Category, string FileName, string VerificationStatus, string? Findings, DateTimeOffset UploadedAt);
public sealed record WorkflowTask(Guid Id, Guid ApplicationId, string TaskType, string AssigneeId, string Status, DateTimeOffset DueAt);

public sealed class WorkflowFeatureStore
{
    private readonly LoanApplicationStore applications;
    private readonly ConcurrentDictionary<Guid, CreditRecommendation> recommendations = new();
    private readonly ConcurrentDictionary<Guid, CreditDecision> decisions = new();
    private readonly ConcurrentDictionary<Guid, Collateral> collateral = new();
    private readonly ConcurrentDictionary<Guid, DocumentRecord> documents = new();
    private readonly ConcurrentDictionary<Guid, WorkflowTask> tasks = new();

    public WorkflowFeatureStore(LoanApplicationStore applications)
    {
        this.applications = applications;
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
    public bool HasDecision(Guid applicationId, string decision) => decisions.Values.Any(item => item.ApplicationId == applicationId && item.Decision.Equals(decision, StringComparison.OrdinalIgnoreCase));

    public (CreditRecommendation? Item, string? Error) AddRecommendation(Guid applicationId, string recommendation, string rationale, IReadOnlyCollection<string> factors, string analystId)
    {
        if (string.IsNullOrWhiteSpace(recommendation) || string.IsNullOrWhiteSpace(rationale) || string.IsNullOrWhiteSpace(analystId)) return (null, "Recommendation, rationale, and analyst are required.");
        if (recommendation.Length > 100 || rationale.Length > 4000 || factors.Count == 0 || factors.Any(string.IsNullOrWhiteSpace) || factors.Any(factor => factor.Length > 500)) return (null, "Recommendation rationale and factors are invalid.");
        var item = new CreditRecommendation(Guid.NewGuid(), applicationId, recommendation, rationale, factors, analystId, DateTimeOffset.UtcNow);
        recommendations[item.Id] = item;
        return (item, null);
    }

    public (CreditDecision? Item, string? Error) AddDecision(Guid applicationId, string decision, string rationale, string approverId)
    {
        if (recommendations.Values.All(item => item.ApplicationId != applicationId)) return (null, "A current credit recommendation is required before a decision.");
        var application = applications.Get(applicationId);
        if (application is null) return (null, "Application not found.");
        if (!application.Status.Equals("PendingApproval", StringComparison.OrdinalIgnoreCase)) return (null, "The application must be pending approval before a decision can be recorded.");
        if (string.IsNullOrWhiteSpace(decision) || string.IsNullOrWhiteSpace(rationale) || string.IsNullOrWhiteSpace(approverId)) return (null, "Decision, rationale, and approver are required.");
        if (!new[] { "Approved", "Declined" }.Contains(decision, StringComparer.OrdinalIgnoreCase)) return (null, "Decision must be Approved or Declined.");
        if (rationale.Length > 4000) return (null, "Decision rationale is too long.");
        var recommendation = recommendations.Values.Where(item => item.ApplicationId == applicationId).OrderByDescending(item => item.CreatedAt).First();
        if (recommendation.AnalystId.Equals(approverId, StringComparison.OrdinalIgnoreCase)) return (null, "The recommendation author cannot make the approval decision.");
        var item = new CreditDecision(Guid.NewGuid(), applicationId, decision, rationale, approverId, DateTimeOffset.UtcNow);
        decisions[item.Id] = item;
        return (item, null);
    }

    public (Collateral? Item, string? Error) AddCollateral(Guid applicationId, string type, string description, decimal value, string currency)
    {
        if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(currency)) return (null, "Collateral type, description, and currency are required.");
        if (value <= 0 || value > 1_000_000_000_000m || currency.Length != 3 || currency.Any(character => !char.IsLetter(character))) return (null, "Collateral value or currency is invalid.");
        var item = new Collateral(Guid.NewGuid(), applicationId, type, description, value, currency.ToUpperInvariant(), "Proposed", DateTimeOffset.UtcNow);
        collateral[item.Id] = item;
        return (item, null);
    }

    public (DocumentRecord? Item, string? Error) AddDocument(Guid applicationId, string category, string fileName)
    {
        if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(fileName) || fileName.Length > 255 || fileName.Contains("..", StringComparison.Ordinal) || fileName.Contains('/') || fileName.Contains('\\')) return (null, "Document category or file name is invalid.");
        var item = new DocumentRecord(Guid.NewGuid(), applicationId, category, fileName, "Ready for Verification", null, DateTimeOffset.UtcNow);
        documents[item.Id] = item;
        return (item, null);
    }

    public (Collateral? Item, string? Error) UpdateCollateral(Guid id, string status)
    {
        var current = collateral.GetValueOrDefault(id);
        if (current is null) return (null, "Collateral not found.");
        var allowed = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Proposed"] = ["Under Review", "Rejected"], ["Under Review"] = ["Perfected", "Rejected"], ["Perfected"] = ["Released"]
        };
        if (!allowed.TryGetValue(current.Status, out var targets) || !targets.Contains(status, StringComparer.OrdinalIgnoreCase)) return (null, "The requested collateral transition is not allowed.");
        var updated = current with { Status = targets.First(target => target.Equals(status, StringComparison.OrdinalIgnoreCase)) };
        collateral[id] = updated;
        return (updated, null);
    }

    public (DocumentRecord? Item, string? Error) VerifyDocument(Guid id, string verificationStatus, string reviewerId, string? findings)
    {
        var current = documents.GetValueOrDefault(id);
        if (current is null) return (null, "Document not found.");
        if (string.IsNullOrWhiteSpace(reviewerId) || !new[] { "Verified", "Rejected", "Needs Review" }.Contains(verificationStatus, StringComparer.OrdinalIgnoreCase)) return (null, "Verification status and reviewer are required.");
        if (verificationStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(findings)) return (null, "Findings are required when a document is rejected.");
        var statuses = new[] { "Verified", "Rejected", "Needs Review" };
        var updated = current with { VerificationStatus = statuses.First(status => status.Equals(verificationStatus, StringComparison.OrdinalIgnoreCase)), Findings = findings };
        documents[id] = updated;
        return (updated, null);
    }
}

public sealed record RecommendationRequest(string Recommendation, string Rationale, string[] Factors, string AnalystId);
public sealed record DecisionRequest(string Decision, string Rationale, string ApproverId);
public sealed record CollateralRequest(string Type, string Description, decimal Value, string Currency);
public sealed record DocumentRequest(string Category, string FileName);
public sealed record CollateralStatusRequest(string Status);
public sealed record DocumentVerificationRequest(string VerificationStatus, string ReviewerId, string? Findings);
