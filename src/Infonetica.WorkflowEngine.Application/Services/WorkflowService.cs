using Infonetica.WorkflowEngine.Application.DTOs;
using Infonetica.WorkflowEngine.Application.Interfaces;
using Infonetica.WorkflowEngine.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Infonetica.WorkflowEngine.Application.Services;

/// <summary>
/// Core service for managing workflows - think of this as the brain of our workflow engine.
/// I handle creating workflow templates, starting instances, and executing actions safely.
/// My job is to make sure workflows follow all the rules and don't break.
/// </summary>
public class WorkflowService
{
    private readonly IWorkflowRepository _repository;
    private readonly ILogger<WorkflowService> _logger;

    // I keep track of maximum limits to prevent system overload
    private const int MAX_STATES_PER_WORKFLOW = 100;
    private const int MAX_ACTIONS_PER_WORKFLOW = 500;
    private const int MAX_ID_LENGTH = 50;
    private const int MAX_FROM_STATES_PER_ACTION = 20;

    public WorkflowService(IWorkflowRepository repository, ILogger<WorkflowService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new workflow definition with comprehensive validation.
    /// I'm like a quality inspector - I check everything before letting workflows through!
    /// I validate: basic rules, input format, circular loops, unreachable states, and dead-ends.
    /// </summary>
    public async Task<WorkflowDefinition> CreateWorkflowDefinitionAsync(CreateWorkflowRequest request)
    {
        _logger.LogInformation("Starting workflow creation for ID: {WorkflowId}", request.Id);

        try
        {
            // Step 1: Basic validations (the foundation checks)
            ValidateBasicRequirements(request);
            
            // Step 2: Input format validation (prevent malicious data)
            ValidateInputFormat(request);
            
            // Step 3: Advanced algorithm validations (the smart checks)
            ValidateCircularDependencies(request);
            ValidateStateReachability(request);
            ValidateDeadEndStates(request);

            // Step 4: Create the workflow if everything passes
            var states = request.States.Select(s => new State(s.Id, s.IsInitial, s.IsFinal, true)).ToList();
            var actions = request.Actions.Select(a => new Domain.Entities.Action(a.Id, true, a.FromStates, a.ToState)).ToList();
            var definition = new WorkflowDefinition(request.Id, states, actions);

            await _repository.SaveDefinitionAsync(definition);
            
            _logger.LogInformation("Successfully created workflow: {WorkflowId} with {StateCount} states and {ActionCount} actions", 
                request.Id, states.Count, actions.Count);
            
            return definition;
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Workflow creation failed for {WorkflowId}: {Error}", request.Id, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating workflow {WorkflowId}", request.Id);
            throw;
        }
    }

    /// <summary>
    /// Basic requirements that every workflow must meet.
    /// I check the fundamental rules that make a workflow valid.
    /// </summary>
    private void ValidateBasicRequirements(CreateWorkflowRequest request)
    {
        // Must have exactly one starting point
        var initialStates = request.States.Where(s => s.IsInitial).ToList();
        if (initialStates.Count != 1)
        {
            throw new ValidationException("Every workflow needs exactly one starting state - I found " + initialStates.Count);
        }

        // Must have at least one state and action
        if (request.States.Count == 0)
        {
            throw new ValidationException("A workflow without states is like a car without wheels - it won't work!");
        }

        if (request.Actions.Count == 0)
        {
            throw new ValidationException("A workflow without actions is like having doors but no keys - nothing can move!");
        }

        // Check for duplicate IDs (states must be unique)
        var stateIds = request.States.Select(s => s.Id).ToList();
        if (stateIds.Count != stateIds.Distinct().Count())
        {
            throw new ValidationException("I found duplicate state IDs - each state needs a unique name!");
        }

        // Check for duplicate action IDs
        var actionIds = request.Actions.Select(a => a.Id).ToList();
        if (actionIds.Count != actionIds.Distinct().Count())
        {
            throw new ValidationException("I found duplicate action IDs - each action needs a unique name!");
        }

        // Make sure all actions reference real states
        var validStateIds = new HashSet<string>(stateIds);
        foreach (var action in request.Actions)
        {
            if (!validStateIds.Contains(action.ToState))
            {
                throw new ValidationException($"Action '{action.Id}' tries to go to state '{action.ToState}' which doesn't exist!");
            }

            foreach (var fromState in action.FromStates)
            {
                if (!validStateIds.Contains(fromState))
                {
                    throw new ValidationException($"Action '{action.Id}' tries to start from state '{fromState}' which doesn't exist!");
                }
            }
        }
    }

    /// <summary>
    /// Validates input format to prevent security issues and system crashes.
    /// I'm like a security guard checking IDs at the door.
    /// </summary>
    private void ValidateInputFormat(CreateWorkflowRequest request)
    {
        // Workflow ID validation
        if (string.IsNullOrWhiteSpace(request.Id))
            throw new ValidationException("Workflow ID can't be empty - I need something to call it!");

        if (request.Id.Length > MAX_ID_LENGTH)
            throw new ValidationException($"Workflow ID is too long! Keep it under {MAX_ID_LENGTH} characters.");

        if (!Regex.IsMatch(request.Id, @"^[a-zA-Z0-9_-]+$"))
            throw new ValidationException("Workflow ID can only contain letters, numbers, underscores, and hyphens - keep it simple!");

        // Size limits to prevent system overload
        if (request.States.Count > MAX_STATES_PER_WORKFLOW)
            throw new ValidationException($"Too many states! I can handle max {MAX_STATES_PER_WORKFLOW} states per workflow.");

        if (request.Actions.Count > MAX_ACTIONS_PER_WORKFLOW)
            throw new ValidationException($"Too many actions! I can handle max {MAX_ACTIONS_PER_WORKFLOW} actions per workflow.");

        // Validate each state
        foreach (var state in request.States)
        {
            if (string.IsNullOrWhiteSpace(state.Id))
                throw new ValidationException("Found a state with no ID - every state needs a name!");

            if (state.Id.Length > MAX_ID_LENGTH)
                throw new ValidationException($"State '{state.Id}' name is too long! Keep it under {MAX_ID_LENGTH} characters.");

            if (!Regex.IsMatch(state.Id, @"^[a-zA-Z0-9_-]+$"))
                throw new ValidationException($"State ID '{state.Id}' has invalid characters - stick to letters, numbers, underscores, and hyphens!");
        }

        // Validate each action
        foreach (var action in request.Actions)
        {
            if (string.IsNullOrWhiteSpace(action.Id))
                throw new ValidationException("Found an action with no ID - every action needs a name!");

            if (action.Id.Length > MAX_ID_LENGTH)
                throw new ValidationException($"Action '{action.Id}' name is too long! Keep it under {MAX_ID_LENGTH} characters.");

            if (!Regex.IsMatch(action.Id, @"^[a-zA-Z0-9_-]+$"))
                throw new ValidationException($"Action ID '{action.Id}' has invalid characters - stick to letters, numbers, underscores, and hyphens!");

            if (action.FromStates.Count == 0)
                throw new ValidationException($"Action '{action.Id}' has no starting states - where can I use it?");

            if (action.FromStates.Count > MAX_FROM_STATES_PER_ACTION)
                throw new ValidationException($"Action '{action.Id}' has too many starting states! Max {MAX_FROM_STATES_PER_ACTION} allowed.");
        }
    }

    /// <summary>
    /// Detects circular dependencies that could create infinite loops.
    /// I use graph theory (DFS with colors) to spot cycles - like finding loops in a maze.
    /// </summary>
    private void ValidateCircularDependencies(CreateWorkflowRequest request)
    {
        _logger.LogDebug("Checking for circular dependencies in workflow {WorkflowId}", request.Id);

        // Build a graph of state transitions
        var graph = new Dictionary<string, HashSet<string>>();
        
        foreach (var action in request.Actions)
        {
            foreach (var fromState in action.FromStates)
            {
                if (!graph.ContainsKey(fromState))
                    graph[fromState] = new HashSet<string>();
                graph[fromState].Add(action.ToState);
            }
        }

        // Use DFS with three colors to detect cycles
        // White = haven't visited, Gray = currently visiting, Black = done visiting
        var colors = request.States.ToDictionary(s => s.Id, _ => NodeColor.White);

        foreach (var state in request.States.Where(s => s.IsInitial))
        {
            if (HasCycleDFS(graph, state.Id, colors))
            {
                throw new ValidationException($"I found a circular loop starting from '{state.Id}' - this could make workflows run forever!");
            }
        }

        _logger.LogDebug("No circular dependencies found in workflow {WorkflowId}", request.Id);
    }

    /// <summary>
    /// Recursive function to detect cycles using Depth-First Search.
    /// If I'm visiting a state I'm already in the middle of visiting, that's a loop!
    /// </summary>
    private bool HasCycleDFS(Dictionary<string, HashSet<string>> graph, string node, Dictionary<string, NodeColor> colors)
    {
        colors[node] = NodeColor.Gray; // Mark as "currently visiting"

        if (graph.ContainsKey(node))
        {
            foreach (var neighbor in graph[node])
            {
                if (colors[neighbor] == NodeColor.Gray) // Found a back edge = cycle!
                    return true;

                if (colors[neighbor] == NodeColor.White && HasCycleDFS(graph, neighbor, colors))
                    return true;
            }
        }

        colors[node] = NodeColor.Black; // Mark as "finished visiting"
        return false;
    }

    /// <summary>
    /// Checks if all states can be reached from the initial state.
    /// I use BFS (like ripples in water) to see what states are reachable.
    /// </summary>
    private void ValidateStateReachability(CreateWorkflowRequest request)
    {
        _logger.LogDebug("Checking state reachability in workflow {WorkflowId}", request.Id);

        var initialStates = request.States.Where(s => s.IsInitial).Select(s => s.Id).ToHashSet();
        var reachableStates = new HashSet<string>(initialStates);
        var queue = new Queue<string>(initialStates);

        // BFS to find all reachable states
        while (queue.Count > 0)
        {
            var currentState = queue.Dequeue();

            foreach (var action in request.Actions.Where(a => a.FromStates.Contains(currentState)))
            {
                if (!reachableStates.Contains(action.ToState))
                {
                    reachableStates.Add(action.ToState);
                    queue.Enqueue(action.ToState);
                }
            }
        }

        // Find unreachable states
        var allStates = request.States.Select(s => s.Id).ToHashSet();
        var unreachableStates = allStates.Except(reachableStates).ToList();

        if (unreachableStates.Any())
        {
            throw new ValidationException($"I found unreachable states: {string.Join(", ", unreachableStates)}. These states can never be reached from the starting point!");
        }

        _logger.LogDebug("All states are reachable in workflow {WorkflowId}", request.Id);
    }

    /// <summary>
    /// Checks for dead-end states (non-final states with no way out).
    /// These states would trap workflows with no way to continue.
    /// </summary>
    private void ValidateDeadEndStates(CreateWorkflowRequest request)
    {
        _logger.LogDebug("Checking for dead-end states in workflow {WorkflowId}", request.Id);

        var statesWithOutgoingActions = request.Actions.SelectMany(a => a.FromStates).ToHashSet();
        var finalStateIds = request.States.Where(s => s.IsFinal).Select(s => s.Id).ToHashSet();

        var deadEndStates = request.States
            .Where(s => !s.IsFinal && !statesWithOutgoingActions.Contains(s.Id))
            .Select(s => s.Id)
            .ToList();

        if (deadEndStates.Any())
        {
            throw new ValidationException($"I found dead-end states: {string.Join(", ", deadEndStates)}. These non-final states have no way out - workflows would get stuck!");
        }

        _logger.LogDebug("No dead-end states found in workflow {WorkflowId}", request.Id);
    }

    /// <summary>
    /// Colors for DFS cycle detection algorithm
    /// </summary>
    private enum NodeColor
    {
        White, // Not visited yet
        Gray,  // Currently being visited
        Black  // Finished visiting
    }

    /// <summary>
    /// Starts a new workflow instance from a workflow definition.
    /// I create a fresh instance and put it in the initial state, ready to begin its journey.
    /// </summary>
    public async Task<WorkflowInstanceResponse> StartInstanceAsync(string definitionId)
    {
        _logger.LogInformation("Starting new instance for workflow definition: {DefinitionId}", definitionId);

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
            },
            Version = 1, // Start with version 1
            LastModified = DateTime.UtcNow
        };

        await _repository.SaveInstanceAsync(instance);

        _logger.LogInformation("Successfully started workflow instance {InstanceId} for definition {DefinitionId}", 
            instanceId, definitionId);

        return new WorkflowInstanceResponse(instance.Id, instance.DefinitionId, instance.CurrentState, instance.History);
    }

    /// <summary>
    /// Executes an action on a workflow instance with full validation and race condition protection.
    /// I make sure only valid actions can run and prevent conflicts when multiple users act simultaneously.
    /// </summary>
    public async Task<WorkflowInstanceResponse> ExecuteActionAsync(string instanceId, ExecuteActionRequest request)
    {
        _logger.LogInformation("Executing action {ActionId} on instance {InstanceId}", request.ActionId, instanceId);

        const int maxRetries = 3;
        var retryDelay = TimeSpan.FromMilliseconds(100);

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
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

                // Enhanced validation with detailed state checking
                ValidateActionExecution(instance, action, definition);

                // Store original version for race condition detection
                var originalVersion = instance.Version;
                var previousState = instance.CurrentState;

                // Execute the action
                instance.CurrentState = action.ToState;
                instance.History.Add(new HistoryEntry(request.ActionId, DateTime.UtcNow));
                instance.Version++; // Increment version to detect concurrent modifications

                // Save with optimistic concurrency check
                var success = await _repository.SaveInstanceWithVersionCheckAsync(instance, originalVersion);

                if (success)
                {
                    _logger.LogInformation("Successfully executed action {ActionId} on instance {InstanceId}: {PreviousState} → {NewState}", 
                        request.ActionId, instanceId, previousState, instance.CurrentState);
                    
                    return new WorkflowInstanceResponse(instance.Id, instance.DefinitionId, instance.CurrentState, instance.History);
                }
                else if (attempt < maxRetries - 1)
                {
                    _logger.LogWarning("Concurrent modification detected on instance {InstanceId}, retrying attempt {Attempt}", 
                        instanceId, attempt + 1);
                    
                    // Exponential backoff
                    await Task.Delay(retryDelay * (int)Math.Pow(2, attempt));
                    continue;
                }
                else
                {
                    throw new ConcurrencyException($"Instance '{instanceId}' was modified by another process. Please retry the operation.");
                }
            }
            catch (ConcurrencyException)
            {
                if (attempt == maxRetries - 1)
                {
                    _logger.LogError("Maximum retry attempts exceeded for instance {InstanceId}", instanceId);
                    throw;
                }
                await Task.Delay(retryDelay * (int)Math.Pow(2, attempt));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning("Action execution validation failed for instance {InstanceId}: {Error}", instanceId, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error executing action {ActionId} on instance {InstanceId}", request.ActionId, instanceId);
                throw;
            }
        }

        throw new ConcurrencyException("Maximum retry attempts exceeded due to concurrent modifications.");
    }

    /// <summary>
    /// Validates whether an action can be executed on the current workflow instance.
    /// I check all the business rules to make sure the action makes sense.
    /// </summary>
    private void ValidateActionExecution(WorkflowInstance instance, Domain.Entities.Action action, WorkflowDefinition definition)
    {
        // Check if action is enabled
        if (!action.Enabled)
        {
            throw new ValidationException($"Action '{action.Id}' is currently disabled and cannot be executed.");
        }

        // Check if action can be executed from current state
        if (!action.FromStates.Contains(instance.CurrentState))
        {
            throw new ValidationException($"Action '{action.Id}' cannot be executed from current state '{instance.CurrentState}'. Valid states are: {string.Join(", ", action.FromStates)}");
        }

        // Get current and target state details
        var currentState = definition.States.FirstOrDefault(s => s.Id == instance.CurrentState);
        var targetState = definition.States.FirstOrDefault(s => s.Id == action.ToState);

        // Check if we're trying to execute from a final state
        if (currentState != null && currentState.IsFinal)
        {
            throw new ValidationException($"Cannot execute action from final state '{instance.CurrentState}'. The workflow has already completed.");
        }

        // Check if current state is enabled
        if (currentState != null && !currentState.Enabled)
        {
            throw new ValidationException($"Cannot execute action from disabled state '{instance.CurrentState}'.");
        }

        // Check if target state exists and is enabled
        if (targetState == null)
        {
            throw new ValidationException($"Target state '{action.ToState}' does not exist in workflow definition.");
        }

        if (!targetState.Enabled)
        {
            throw new ValidationException($"Cannot transition to disabled state '{action.ToState}'.");
        }
    }

    /// <summary>
    /// Gets all workflow definitions available in the system.
    /// I return a list of all workflow templates that can be used to create instances.
    /// </summary>
    public async Task<IEnumerable<WorkflowDefinition>> GetAllWorkflowDefinitionsAsync()
    {
        _logger.LogDebug("Retrieving all workflow definitions");
        return await _repository.GetAllDefinitionsAsync();
    }

    /// <summary>
    /// Gets the current status of a workflow instance.
    /// I return the current state and complete history of what happened.
    /// </summary>
    public async Task<WorkflowInstanceResponse> GetInstanceStatusAsync(string instanceId)
    {
        _logger.LogDebug("Getting status for instance {InstanceId}", instanceId);
        
        var instance = await _repository.GetInstanceAsync(instanceId);
        if (instance == null)
        {
            throw new ValidationException($"Workflow instance '{instanceId}' not found.");
        }

        return new WorkflowInstanceResponse(instance.Id, instance.DefinitionId, instance.CurrentState, instance.History);
    }
}

/// <summary>
/// Exception thrown when concurrent modifications are detected on a workflow instance.
/// This helps prevent data corruption when multiple users try to modify the same workflow simultaneously.
/// </summary>
public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message) { }
    public ConcurrencyException(string message, Exception innerException) : base(message, innerException) { }
}
