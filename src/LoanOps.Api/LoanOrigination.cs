using System.Collections.Concurrent;

public sealed record CreateApplicationRequest(string TenantId, string BorrowerLegalName, decimal RequestedAmount, string Currency, string Purpose, string OwnerId);
public sealed record TransitionRequest(string TargetStatus, string Reason, string ActorId);
public sealed record LoanApplication(Guid Id, string TenantId, string ApplicationNumber, string BorrowerLegalName, decimal RequestedAmount, string Currency, string Purpose, string Status, string OwnerId, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record AuditEvent(Guid Id, string TenantId, string ActorId, string Action, Guid EntityId, DateTimeOffset OccurredAt);

public sealed class LoanApplicationStore
{
    private readonly ConcurrentDictionary<Guid, LoanApplication> applications = new();
    private readonly ConcurrentQueue<AuditEvent> auditEvents = new();

    public IReadOnlyCollection<LoanApplication> List() => applications.Values.OrderByDescending(item => item.CreatedAt).ToArray();
    public LoanApplication? Get(Guid id) => applications.GetValueOrDefault(id);

    public LoanApplication Add(CreateApplicationRequest request)
    {
        var now = DateTimeOffset.UtcNow;
        var application = new LoanApplication(Guid.NewGuid(), request.TenantId, $"LOAN-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..21], request.BorrowerLegalName, request.RequestedAmount, request.Currency.ToUpperInvariant(), request.Purpose, "Intake", request.OwnerId, now, now);
        applications[application.Id] = application;
        auditEvents.Enqueue(new AuditEvent(Guid.NewGuid(), application.TenantId, request.OwnerId, "ApplicationCreated", application.Id, now));
        return application;
    }

    public LoanApplication? Update(LoanApplication application, LoanApplication expected, string actorId)
    {
        if (!applications.TryUpdate(application.Id, application, expected)) return null;
        auditEvents.Enqueue(new AuditEvent(Guid.NewGuid(), application.TenantId, actorId, "ApplicationTransitioned", application.Id, application.UpdatedAt));
        return application;
    }
}

public sealed class LoanApplicationService(LoanApplicationStore store, WorkflowFeatureStore features)
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedTransitions = new Dictionary<string, string[]>
    {
        ["Intake"] = ["Analysis", "Declined"], ["Analysis"] = ["PendingApproval", "Declined"], ["PendingApproval"] = ["Approved", "Declined"], ["Approved"] = ["Closing"]
    };

    public (LoanApplication? Application, string? Error) Create(CreateApplicationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId) || string.IsNullOrWhiteSpace(request.BorrowerLegalName) || string.IsNullOrWhiteSpace(request.OwnerId)) return (null, "Required fields are missing.");
        if (request.RequestedAmount <= 0 || request.RequestedAmount > 1_000_000_000_000m) return (null, "Requested amount must be positive and within the supported range.");
        if (request.Currency.Length != 3 || request.Currency.Any(character => !char.IsLetter(character))) return (null, "Currency must be a three-letter ISO code.");
        return (store.Add(request), null);
    }

    public (LoanApplication? Application, string? Error) Transition(Guid id, TransitionRequest request)
    {
        var current = store.Get(id);
        if (current is null) return (null, "Application not found.");
        if (string.IsNullOrWhiteSpace(request.ActorId) || string.IsNullOrWhiteSpace(request.Reason)) return (null, "Actor and reason are required.");
        if (!AllowedTransitions.TryGetValue(current.Status, out var targets) || !targets.Contains(request.TargetStatus, StringComparer.OrdinalIgnoreCase)) return (null, "The requested lifecycle transition is not allowed.");
        if (request.TargetStatus.Equals("PendingApproval", StringComparison.OrdinalIgnoreCase) && features.RequiresSecondLevelVerification(id) && !features.HasSecondLevelVerification(id)) return (null, "Second-level credit verification is required before approval submission.");
        if ((request.TargetStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase) || request.TargetStatus.Equals("Declined", StringComparison.OrdinalIgnoreCase)) && !features.HasDecision(id, request.TargetStatus)) return (null, "A recorded human credit decision is required before this transition.");
        var updated = current with { Status = request.TargetStatus, UpdatedAt = DateTimeOffset.UtcNow };
        var saved = store.Update(updated, current, request.ActorId);
        return saved is null ? (null, "Application was modified concurrently.") : (saved, null);
    }
}
