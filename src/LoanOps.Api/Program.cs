using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddAuthentication("Demo").AddScheme<AuthenticationSchemeOptions, DemoAuthenticationHandler>("Demo", _ => { });
builder.Services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddSingleton<LoanApplicationStore>();
builder.Services.AddSingleton<LoanApplicationService>();
builder.Services.AddSingleton<WorkflowFeatureStore>();

var app = builder.Build();
_ = app.Services.GetRequiredService<WorkflowFeatureStore>();
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
    context.Items["CorrelationId"] = correlationId;
    context.Response.Headers["X-Correlation-Id"] = correlationId;
    try
    {
        await next();
    }
    finally
    {
        app.Logger.LogInformation("HTTP {Method} {Path} returned {StatusCode} with correlation {CorrelationId}", context.Request.Method, context.Request.Path, context.Response.StatusCode, correlationId);
    }
});
app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await Results.Problem("The request could not be completed.", statusCode: StatusCodes.Status500InternalServerError, title: "Unexpected server error.", extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }).ExecuteAsync(context);
}));
app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health/live", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapGet("/api/v1/applications", (HttpContext context, LoanApplicationStore store) => Results.Ok(store.List().Where(item => item.TenantId == context.TenantId())));
app.MapGet("/api/v1/applications/{id:guid}", (HttpContext context, Guid id, LoanApplicationStore store) =>
{
    var application = store.Get(id);
    if (application is not null && application.TenantId != context.TenantId()) return Results.NotFound(new { error = "Application not found." });
    return application is null ? Results.NotFound(new { error = "Application not found." }) : Results.Ok(application);
});
app.MapPost("/api/v1/applications", (HttpContext context, CreateApplicationRequest request, LoanApplicationService service) =>
{
    if (!context.HasRole("RelationshipManager") || request.TenantId != context.TenantId() || request.OwnerId != context.UserId()) return Results.Forbid();
    var result = service.Create(request);
    return result.Error is null ? Results.Created($"/api/v1/applications/{result.Application!.Id}", result.Application) : Results.BadRequest(new { error = result.Error });
});
app.MapPost("/api/v1/applications/{id:guid}/transitions", (HttpContext context, Guid id, TransitionRequest request, LoanApplicationService service, LoanApplicationStore store) =>
{
    var application = store.Get(id);
    var requiredRole = request.TargetStatus.Equals("Analysis", StringComparison.OrdinalIgnoreCase) ? "RelationshipManager" : request.TargetStatus.Equals("PendingApproval", StringComparison.OrdinalIgnoreCase) ? "CreditAnalyst" : "CreditApprover";
    if (!context.HasRole(requiredRole) || application is null || application.TenantId != context.TenantId() || request.ActorId != context.UserId()) return Results.NotFound(new { error = "Application not found." });
    var result = service.Transition(id, request);
    return result.Error switch
    {
        null => Results.Ok(result.Application),
        "Application not found." => Results.NotFound(new { error = result.Error }),
        "Actor, target status, and reason are required and bounded." => Results.BadRequest(new { error = result.Error }),
        "Application was modified concurrently." => Results.Conflict(new { error = result.Error }),
        _ => Results.Conflict(new { error = result.Error })
    };
});
app.MapGet("/api/v1/applications/{id:guid}/workspace", (HttpContext context, Guid id, LoanApplicationStore applications, WorkflowFeatureStore features) =>
{
    if (applications.Get(id) is not { TenantId: var tenantId } || tenantId != context.TenantId()) return Results.NotFound(new { error = "Application not found." });
    return Results.Ok(new { recommendations = features.Recommendations(id), decisions = features.Decisions(id), verifications = features.Verifications(id), requiresSecondLevelVerification = features.RequiresSecondLevelVerification(id), collateral = features.Collateral(id), documents = features.Documents(id), tasks = features.Tasks(id) });
});
app.MapPost("/api/v1/applications/{id:guid}/credit-verification", (HttpContext context, Guid id, SecondLevelVerificationRequest request, LoanApplicationStore applications, WorkflowFeatureStore features) =>
{
    if (applications.Get(id) is not { TenantId: var tenantId } || tenantId != context.TenantId()) return Results.NotFound(new { error = "Application not found." });
    if (!context.HasRole("CreditVerifier") || request.VerifierId != context.UserId()) return Results.Forbid();
    var result = features.AddSecondLevelVerification(id, request.Outcome, request.Rationale, request.VerifierId);
    return result.Error is null ? Results.Created($"/api/v1/applications/{id}/credit-verification", result.Item) : Results.BadRequest(new { error = result.Error });
});
app.MapPost("/api/v1/applications/{id:guid}/recommendations", (HttpContext context, Guid id, RecommendationRequest request, LoanApplicationStore applications, WorkflowFeatureStore features) =>
{
    if (applications.Get(id) is not { TenantId: var tenantId } || tenantId != context.TenantId()) return Results.NotFound(new { error = "Application not found." });
    if (!context.HasRole("CreditAnalyst") || request.AnalystId != context.UserId()) return Results.Forbid();
    var result = features.AddRecommendation(id, request.Recommendation, request.Rationale, request.Factors, request.AnalystId);
    return result.Error is null ? Results.Created($"/api/v1/applications/{id}/recommendations", result.Item) : Results.BadRequest(new { error = result.Error });
});
app.MapPost("/api/v1/applications/{id:guid}/decisions", (HttpContext context, Guid id, DecisionRequest request, LoanApplicationStore applications, WorkflowFeatureStore features) =>
{
    if (applications.Get(id) is not { TenantId: var tenantId } || tenantId != context.TenantId()) return Results.NotFound(new { error = "Application not found." });
    if (!context.HasRole("CreditApprover") || request.ApproverId != context.UserId()) return Results.Forbid();
    var result = features.AddDecision(id, request.Decision, request.Rationale, request.ApproverId);
    return result.Error is null ? Results.Created($"/api/v1/applications/{id}/decisions", result.Item) : Results.BadRequest(new { error = result.Error });
});
app.MapPost("/api/v1/applications/{id:guid}/collateral", (HttpContext context, Guid id, CollateralRequest request, LoanApplicationStore applications, WorkflowFeatureStore features) =>
{
    if (applications.Get(id) is not { TenantId: var tenantId } || tenantId != context.TenantId()) return Results.NotFound(new { error = "Application not found." });
    if (!context.HasRole("CollateralSpecialist")) return Results.Forbid();
    var result = features.AddCollateral(id, request.Type, request.Description, request.Value, request.Currency);
    return result.Error is null ? Results.Created($"/api/v1/applications/{id}/collateral", result.Item) : Results.BadRequest(new { error = result.Error });
});
app.MapPost("/api/v1/applications/{id:guid}/documents", (HttpContext context, Guid id, DocumentRequest request, LoanApplicationStore applications, WorkflowFeatureStore features) =>
{
    if (applications.Get(id) is not { TenantId: var tenantId } || tenantId != context.TenantId()) return Results.NotFound(new { error = "Application not found." });
    if (!context.HasRole("DocumentVerificationAnalyst")) return Results.Forbid();
    var result = features.AddDocument(id, request.Category, request.FileName);
    return result.Error is null ? Results.Created($"/api/v1/applications/{id}/documents", result.Item) : Results.BadRequest(new { error = result.Error });
});
app.MapPost("/api/v1/collateral/{id:guid}/status", (HttpContext context, Guid id, CollateralStatusRequest request, WorkflowFeatureStore features) =>
{
    if (!features.IsCollateralInTenant(id, context.TenantId())) return Results.NotFound(new { error = "Collateral not found." });
    if (!context.HasRole("CollateralSpecialist")) return Results.Forbid();
    var result = features.UpdateCollateral(id, request.Status);
    return result.Error is null ? Results.Ok(result.Item) : Results.BadRequest(new { error = result.Error });
});
app.MapPost("/api/v1/documents/{id:guid}/verification", (HttpContext context, Guid id, DocumentVerificationRequest request, WorkflowFeatureStore features) =>
{
    if (!features.IsDocumentInTenant(id, context.TenantId())) return Results.NotFound(new { error = "Document not found." });
    if (!context.HasRole("DocumentVerificationAnalyst") || request.ReviewerId != context.UserId()) return Results.Forbid();
    var result = features.VerifyDocument(id, request.VerificationStatus, request.ReviewerId, request.Findings);
    return result.Error is null ? Results.Ok(result.Item) : Results.BadRequest(new { error = result.Error });
});

app.Run();
