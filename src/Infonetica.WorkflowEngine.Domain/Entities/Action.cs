namespace Infonetica.WorkflowEngine.Domain.Entities;

public record Action(string Id, bool Enabled, List<string> FromStates, string ToState);
