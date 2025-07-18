namespace Infonetica.WorkflowEngine.Domain.Entities;

public record State(string Id, bool IsInitial, bool IsFinal, bool Enabled);
