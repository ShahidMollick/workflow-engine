namespace Infonetica.WorkflowEngine.Domain.Entities;

public record WorkflowDefinition(string Id, List<State> States, List<Action> Actions);
