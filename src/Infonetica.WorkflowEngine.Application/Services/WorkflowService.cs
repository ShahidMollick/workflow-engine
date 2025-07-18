using Infonetica.WorkflowEngine.Application.DTOs;
using Infonetica.WorkflowEngine.Application.Interfaces;
using Infonetica.WorkflowEngine.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace Infonetica.WorkflowEngine.Application.Services;

public class WorkflowService
{
    private readonly IWorkflowRepository _repository;

    public WorkflowService(IWorkflowRepository repository)
    {
        _repository = repository;
    }

    public async Task<WorkflowDefinition> CreateWorkflowDefinitionAsync(CreateWorkflowRequest request)
    {
        // Validation
        var initialStates = request.States.Where(s => s.IsInitial).ToList();
        if (initialStates.Count != 1)
        {
            throw new ValidationException("Workflow must have exactly one initial state.");
        }

        if (request.States.Count == 0)
        {
            throw new ValidationException("Workflow must have at least one state.");
        }

        if (request.Actions.Count == 0)
        {
            throw new ValidationException("Workflow must have at least one action.");
        }

        // Check for duplicate state IDs
        var stateIds = request.States.Select(s => s.Id).ToList();
        if (stateIds.Count != stateIds.Distinct().Count())
        {
            throw new ValidationException("State IDs must be unique.");
        }

        // Check for duplicate action IDs
        var actionIds = request.Actions.Select(a => a.Id).ToList();
        if (actionIds.Count != actionIds.Distinct().Count())
        {
            throw new ValidationException("Action IDs must be unique.");
        }

        // Validate that all action FromStates and ToState reference existing states
        var validStateIds = new HashSet<string>(stateIds);
        foreach (var action in request.Actions)
        {
            if (!validStateIds.Contains(action.ToState))
            {
                throw new ValidationException($"Action '{action.Id}' references invalid ToState '{action.ToState}'.");
            }

            foreach (var fromState in action.FromStates)
            {
                if (!validStateIds.Contains(fromState))
                {
                    throw new ValidationException($"Action '{action.Id}' references invalid FromState '{fromState}'.");
                }
            }
        }

        // Map DTOs to domain entities
        var states = request.States.Select(s => new State(s.Id, s.IsInitial, s.IsFinal, true)).ToList();
        var actions = request.Actions.Select(a => new Domain.Entities.Action(a.Id, true, a.FromStates, a.ToState)).ToList();
        var definition = new WorkflowDefinition(request.Id, states, actions);

        // Save definition
        await _repository.SaveDefinitionAsync(definition);

        return definition;
    }

    public async Task<WorkflowInstanceResponse> StartInstanceAsync(string definitionId)
    {
        var definition = await _repository.GetDefinitionAsync(definitionId);
        if (definition == null)
        {
            throw new ValidationException($"Workflow definition '{definitionId}' not found.");
        }

        var initialState = definition.States.FirstOrDefault(s => s.IsInitial);
        if (initialState == null)
        {
            throw new ValidationException($"No initial state found in workflow definition '{definitionId}'.");
        }

        var instanceId = Guid.NewGuid().ToString();
        var instance = new WorkflowInstance
        {
            Id = instanceId,
            DefinitionId = definitionId,
            CurrentState = initialState.Id,
            History = new List<HistoryEntry>
            {
                new HistoryEntry("WORKFLOW_STARTED", DateTime.UtcNow)
            }
        };

        await _repository.SaveInstanceAsync(instance);

        return new WorkflowInstanceResponse(instance.Id, instance.DefinitionId, instance.CurrentState, instance.History);
    }

    public async Task<WorkflowInstanceResponse> ExecuteActionAsync(string instanceId, ExecuteActionRequest request)
    {
        var instance = await _repository.GetInstanceAsync(instanceId);
        if (instance == null)
        {
            throw new ValidationException($"Workflow instance '{instanceId}' not found.");
        }

        var definition = await _repository.GetDefinitionAsync(instance.DefinitionId);
        if (definition == null)
        {
            throw new ValidationException($"Workflow definition '{instance.DefinitionId}' not found.");
        }

        var action = definition.Actions.FirstOrDefault(a => a.Id == request.ActionId);
        if (action == null)
        {
            throw new ValidationException($"Action '{request.ActionId}' not found in workflow definition.");
        }

        // Validation checks
        if (!action.Enabled)
        {
            throw new ValidationException($"Action '{request.ActionId}' is disabled.");
        }

        if (!action.FromStates.Contains(instance.CurrentState))
        {
            throw new ValidationException($"Action '{request.ActionId}' cannot be executed from current state '{instance.CurrentState}'.");
        }

        var currentState = definition.States.FirstOrDefault(s => s.Id == instance.CurrentState);
        if (currentState != null && currentState.IsFinal)
        {
            throw new ValidationException($"Cannot execute action from final state '{instance.CurrentState}'.");
        }

        // Execute the action
        instance.CurrentState = action.ToState;
        instance.History.Add(new HistoryEntry(request.ActionId, DateTime.UtcNow));

        await _repository.SaveInstanceAsync(instance);

        return new WorkflowInstanceResponse(instance.Id, instance.DefinitionId, instance.CurrentState, instance.History);
    }

    public async Task<WorkflowInstanceResponse> GetInstanceStatusAsync(string instanceId)
    {
        var instance = await _repository.GetInstanceAsync(instanceId);
        if (instance == null)
        {
            throw new ValidationException($"Workflow instance '{instanceId}' not found.");
        }

        return new WorkflowInstanceResponse(instance.Id, instance.DefinitionId, instance.CurrentState, instance.History);
    }
}
