using Infonetica.WorkflowEngine.Domain.Entities;

namespace Infonetica.WorkflowEngine.Application.DTOs;

public record CreateStateDto(string Id, bool IsInitial, bool IsFinal);

public record CreateActionDto(string Id, List<string> FromStates, string ToState);

public record CreateWorkflowRequest(string Id, List<CreateStateDto> States, List<CreateActionDto> Actions);

public record ExecuteActionRequest(string ActionId);

public record WorkflowInstanceResponse(string Id, string DefinitionId, string CurrentState, List<HistoryEntry> History);
