using Infonetica.WorkflowEngine.Application.DTOs;
using Infonetica.WorkflowEngine.Application.Interfaces;
using Infonetica.WorkflowEngine.Application.Services;
using Infonetica.WorkflowEngine.Infrastructure.Persistence;
using System.ComponentModel.DataAnnotations;

namespace Infonetica.WorkflowEngine.Tests;

public class WorkflowEngineTests
{
    private readonly IWorkflowRepository _repository;
    private readonly WorkflowService _workflowService;

    public WorkflowEngineTests()
    {
        _repository = new InMemoryWorkflowRepository();
        _workflowService = new WorkflowService(_repository);
    }

    [Fact]
    public async Task CreateWorkflowDefinition_ShouldCreateValidWorkflow()
    {
        // Arrange
        var request = new CreateWorkflowRequest(
            "order-workflow",
            new List<CreateStateDto>
            {
                new("draft", true, false),
                new("submitted", false, false),
                new("approved", false, true)
            },
            new List<CreateActionDto>
            {
                new("submit", new List<string> { "draft" }, "submitted"),
                new("approve", new List<string> { "submitted" }, "approved")
            }
        );

        // Act
        var result = await _workflowService.CreateWorkflowDefinitionAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("order-workflow", result.Id);
        Assert.Equal(3, result.States.Count);
        Assert.Equal(2, result.Actions.Count);
    }

    [Fact]
    public async Task StartInstance_ShouldCreateNewInstance()
    {
        // Arrange
        var request = new CreateWorkflowRequest(
            "test-workflow",
            new List<CreateStateDto>
            {
                new("initial", true, false),
                new("final", false, true)
            },
            new List<CreateActionDto>
            {
                new("complete", new List<string> { "initial" }, "final")
            }
        );

        await _workflowService.CreateWorkflowDefinitionAsync(request);

        // Act
        var instance = await _workflowService.StartInstanceAsync("test-workflow");

        // Assert
        Assert.NotNull(instance);
        Assert.Equal("test-workflow", instance.DefinitionId);
        Assert.Equal("initial", instance.CurrentState);
        Assert.Single(instance.History);
        Assert.Equal("WORKFLOW_STARTED", instance.History[0].Action);
    }

    [Fact]
    public async Task ExecuteAction_ShouldUpdateInstanceState()
    {
        // Arrange
        var request = new CreateWorkflowRequest(
            "execution-test",
            new List<CreateStateDto>
            {
                new("start", true, false),
                new("end", false, true)
            },
            new List<CreateActionDto>
            {
                new("finish", new List<string> { "start" }, "end")
            }
        );

        await _workflowService.CreateWorkflowDefinitionAsync(request);
        var instance = await _workflowService.StartInstanceAsync("execution-test");

        // Act
        var updatedInstance = await _workflowService.ExecuteActionAsync(
            instance.Id, 
            new ExecuteActionRequest("finish")
        );

        // Assert
        Assert.Equal("end", updatedInstance.CurrentState);
        Assert.Equal(2, updatedInstance.History.Count);
        Assert.Equal("finish", updatedInstance.History[1].Action);
    }

    [Fact]
    public async Task ExecuteAction_FromFinalState_ShouldThrowValidationException()
    {
        // Arrange
        var request = new CreateWorkflowRequest(
            "final-test",
            new List<CreateStateDto>
            {
                new("start", true, false),
                new("end", false, true)
            },
            new List<CreateActionDto>
            {
                new("finish", new List<string> { "start" }, "end"),
                new("restart", new List<string> { "end" }, "start")
            }
        );

        await _workflowService.CreateWorkflowDefinitionAsync(request);
        var instance = await _workflowService.StartInstanceAsync("final-test");
        await _workflowService.ExecuteActionAsync(instance.Id, new ExecuteActionRequest("finish"));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(async () =>
            await _workflowService.ExecuteActionAsync(instance.Id, new ExecuteActionRequest("restart"))
        );
    }

    [Fact]
    public async Task CreateWorkflow_WithoutInitialState_ShouldThrowValidationException()
    {
        // Arrange
        var request = new CreateWorkflowRequest(
            "invalid-workflow",
            new List<CreateStateDto>
            {
                new("state1", false, false),
                new("state2", false, true)
            },
            new List<CreateActionDto>
            {
                new("action1", new List<string> { "state1" }, "state2")
            }
        );

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(async () =>
            await _workflowService.CreateWorkflowDefinitionAsync(request)
        );
    }
}
