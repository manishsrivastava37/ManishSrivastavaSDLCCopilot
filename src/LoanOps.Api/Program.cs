var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<LoanApplicationStore>();
builder.Services.AddSingleton<LoanApplicationService>();
builder.Services.AddSingleton<WorkflowFeatureStore>();

var app = builder.Build();
_ = app.Services.GetRequiredService<WorkflowFeatureStore>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));
app.MapGet("/api/v1/applications", (LoanApplicationStore store) => Results.Ok(store.List()));
app.MapGet("/api/v1/applications/{id:guid}", (Guid id, LoanApplicationStore store) =>
{
    var application = store.Get(id);
    return application is null ? Results.NotFound(new { error = "Application not found." }) : Results.Ok(application);
});
app.MapPost("/api/v1/applications", (CreateApplicationRequest request, LoanApplicationService service) =>
{
    var result = service.Create(request);
    return result.Error is null ? Results.Created($"/api/v1/applications/{result.Application!.Id}", result.Application) : Results.BadRequest(new { error = result.Error });
});
app.MapPost("/api/v1/applications/{id:guid}/transitions", (Guid id, TransitionRequest request, LoanApplicationService service) =>
{
    var result = service.Transition(id, request);
    return result.Error switch
    {
        null => Results.Ok(result.Application),
        "Application not found." => Results.NotFound(new { error = result.Error }),
        _ => Results.Conflict(new { error = result.Error })
    };
});
app.MapGet("/api/v1/applications/{id:guid}/workspace", (Guid id, LoanApplicationStore applications, WorkflowFeatureStore features) =>
{
    if (applications.Get(id) is null) return Results.NotFound(new { error = "Application not found." });
    return Results.Ok(new { recommendations = features.Recommendations(id), decisions = features.Decisions(id), collateral = features.Collateral(id), documents = features.Documents(id), tasks = features.Tasks(id) });
});
app.MapPost("/api/v1/applications/{id:guid}/recommendations", (Guid id, RecommendationRequest request, LoanApplicationStore applications, WorkflowFeatureStore features) =>
    applications.Get(id) is null ? Results.NotFound(new { error = "Application not found." }) : Results.Created($"/api/v1/applications/{id}/recommendations", features.AddRecommendation(id, request.Recommendation, request.Rationale, request.AnalystId)));
app.MapPost("/api/v1/applications/{id:guid}/decisions", (Guid id, DecisionRequest request, LoanApplicationStore applications, WorkflowFeatureStore features) =>
    applications.Get(id) is null ? Results.NotFound(new { error = "Application not found." }) : Results.Created($"/api/v1/applications/{id}/decisions", features.AddDecision(id, request.Decision, request.Rationale, request.ApproverId)));
app.MapPost("/api/v1/applications/{id:guid}/collateral", (Guid id, CollateralRequest request, LoanApplicationStore applications, WorkflowFeatureStore features) =>
    applications.Get(id) is null ? Results.NotFound(new { error = "Application not found." }) : Results.Created($"/api/v1/applications/{id}/collateral", features.AddCollateral(id, request.Type, request.Description, request.Value, request.Currency)));
app.MapPost("/api/v1/applications/{id:guid}/documents", (Guid id, DocumentRequest request, LoanApplicationStore applications, WorkflowFeatureStore features) =>
    applications.Get(id) is null ? Results.NotFound(new { error = "Application not found." }) : Results.Created($"/api/v1/applications/{id}/documents", features.AddDocument(id, request.Category, request.FileName)));

app.Run();
