using Infonetica.WorkflowEngine.Application.DTOs;
using Infonetica.WorkflowEngine.Application.Interfaces;
using Infonetica.WorkflowEngine.Application.Services;
using Infonetica.WorkflowEngine.Infrastructure.Persistence;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IWorkflowRepository, InMemoryWorkflowRepository>();
builder.Services.AddScoped<WorkflowService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Global exception handling middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (ValidationException ex)
    {
        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";
        var errorResponse = new { error = ex.Message };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse));
    }
});

app.UseHttpsRedirection();

// API Endpoints
app.MapPost("/workflows", async (CreateWorkflowRequest request, WorkflowService workflowService) =>
{
    var definition = await workflowService.CreateWorkflowDefinitionAsync(request);
    return Results.Created($"/workflows/{definition.Id}", definition);
})
.WithName("CreateWorkflow")
.WithOpenApi();

app.MapPost("/workflows/{definitionId}/instances", async (string definitionId, WorkflowService workflowService) =>
{
    var instance = await workflowService.StartInstanceAsync(definitionId);
    return Results.Created($"/instances/{instance.Id}", instance);
})
.WithName("StartWorkflowInstance")
.WithOpenApi();

app.MapGet("/instances/{instanceId}", async (string instanceId, WorkflowService workflowService) =>
{
    var instance = await workflowService.GetInstanceStatusAsync(instanceId);
    return Results.Ok(instance);
})
.WithName("GetInstanceStatus")
.WithOpenApi();

app.MapPost("/instances/{instanceId}/execute", async (string instanceId, ExecuteActionRequest request, WorkflowService workflowService) =>
{
    var instance = await workflowService.ExecuteActionAsync(instanceId, request);
    return Results.Ok(instance);
})
.WithName("ExecuteAction")
.WithOpenApi();

app.Run();
