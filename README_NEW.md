# 🔄 Infonetica Workflow Engine

A production-ready, configurable workflow engine built with .NET 8 that manages state machines with comprehensive validation and edge case handling.

## 📋 Table of Contents

- [🎯 What I Built](#-what-i-built)
- [🏗️ Architecture & Design](#️-architecture--design)
- [🔧 Key Features](#-key-features)
- [🛡️ Edge Cases I Handle](#️-edge-cases-i-handle)
- [🚀 Quick Start](#-quick-start)
- [📊 API Reference](#-api-reference)
- [🧪 Testing](#-testing)
- [📈 Design Decisions](#-design-decisions)
- [🔮 Future Enhancements](#-future-enhancements)

## 🎯 What I Built

This is a **configurable workflow engine** that lets you:

1. **Define workflows** as state machines with states and actions
2. **Start workflow instances** from any definition  
3. **Execute actions** to move instances between states with full validation
4. **Track history** of every action taken
5. **Handle edge cases** like circular dependencies, race conditions, and invalid transitions

### Real-World Example
Think of it like a **document approval process**:
- **States**: Draft → Review → Approved → Rejected
- **Actions**: Submit, Approve, Reject, Send Back for Changes
- **Validation**: Can't approve from Draft, Can't change Approved documents
- **Race Conditions**: Two managers can't approve/reject simultaneously

## 🏗️ Architecture & Design

I chose **Clean Architecture** to keep the code organized and testable:

```
src/
├── Domain/           # Core business entities (State, Action, WorkflowDefinition)
├── Application/      # Business logic and validation (WorkflowService)
├── Infrastructure/   # Data storage (InMemoryRepository)
└── Api/             # HTTP endpoints and middleware
```

### Why This Structure?
- **Domain**: Contains the core business rules that never change
- **Application**: Houses all the smart validation logic I implemented
- **Infrastructure**: Handles data storage (easily swappable)
- **Api**: Manages HTTP concerns and user interaction

## 🔧 Key Features

### ✅ **Core Requirements (All Implemented)**
- Create workflow definitions with states and actions
- Start workflow instances from definitions
- Execute actions with comprehensive validation
- Retrieve workflow status and complete history

### 🚀 **Advanced Features (My Additions)**
- **Circular Dependency Detection**: Prevents infinite loops using graph algorithms
- **Unreachable State Detection**: Ensures all states can be reached from initial state
- **Dead-End State Detection**: Prevents workflows from getting stuck
- **Race Condition Protection**: Handles concurrent users with optimistic locking
- **Enhanced Input Validation**: Prevents malicious data and system crashes
- **Structured Error Handling**: Clear, categorized error messages with correlation IDs
- **Comprehensive Logging**: Full audit trail for debugging and monitoring

## 🛡️ Edge Cases I Handle

### 1. **Circular Dependencies (Infinite Loops)**
**What breaks**: Workflows that can loop forever (A → B → A → B...)  
**My solution**: Graph theory cycle detection using DFS with three colors  
**Algorithm**: I treat states as nodes and actions as edges, then detect cycles

```csharp
// I use this approach to detect loops before they can break the system
private bool HasCycleDFS(Dictionary<string, HashSet<string>> graph, string node, Dictionary<string, Color> colors)
{
    colors[node] = Color.Gray; // Mark as "currently visiting"
    
    foreach (var neighbor in graph[node])
    {
        if (colors[neighbor] == Color.Gray) // Found a back edge = cycle!
            return true;
    }
    
    colors[node] = Color.Black; // Mark as "finished"
    return false;
}
```

### 2. **Race Conditions (Concurrent Users)**  
**What breaks**: Two users clicking buttons simultaneously, corrupting data  
**My solution**: Optimistic concurrency control with version numbers  
**How it works**: Each workflow instance has a version that increments on changes

```csharp
// Before saving, I check if someone else modified the instance
var originalVersion = instance.Version;
// ... make changes ...
instance.Version++; // Increment version
var success = await SaveInstanceWithVersionCheckAsync(instance, originalVersion);
if (!success) {
    // Someone else changed it - retry with backoff
}
```

### 3. **Unreachable States (Orphaned States)**  
**What breaks**: States that can never be reached from the starting point  
**My solution**: Breadth-first search to verify all states are reachable  
**Why important**: Prevents wasted workflow design and user confusion

### 4. **Dead-End States (Getting Stuck)**  
**What breaks**: Non-final states with no way out (workflow gets trapped)  
**My solution**: Validate that all non-final states have at least one outgoing action  
**Business impact**: Ensures workflows can always progress or complete

### 5. **Input Validation (Security & Stability)**
**What breaks**: Malicious input, overly long strings, invalid characters  
**My solution**: Multi-layer validation with security checks  
**Protects against**: DoS attacks, injection attempts, system crashes

## 🚀 Quick Start

### Prerequisites
- .NET 8 SDK
- Your favorite IDE (Visual Studio, VS Code, etc.)

### Running the Application

1. **Clone and navigate**:
   ```bash
   git clone <repository-url>
   cd Infonetica.WorkflowEngine
   ```

2. **Run the API**:
   ```bash
   dotnet run --project src/Infonetica.WorkflowEngine.Api
   ```

3. **Test the API**:
   - Open browser to `https://localhost:7147/swagger` for interactive docs
   - Or use the HTTP file: `src/Infonetica.WorkflowEngine.Api/Infonetica.WorkflowEngine.Api.http`

### Example: Document Approval Workflow

```json
POST /workflows
{
  "id": "document-approval",
  "states": [
    {"id": "draft", "isInitial": true, "isFinal": false},
    {"id": "review", "isInitial": false, "isFinal": false},
    {"id": "approved", "isInitial": false, "isFinal": true},
    {"id": "rejected", "isInitial": false, "isFinal": true}
  ],
  "actions": [
    {"id": "submit", "fromStates": ["draft"], "toState": "review"},
    {"id": "approve", "fromStates": ["review"], "toState": "approved"},
    {"id": "reject", "fromStates": ["review"], "toState": "rejected"}
  ]
}
```

Then start an instance and execute actions:
```bash
POST /workflows/document-approval/instances  # Start workflow
POST /instances/{instanceId}/execute {"actionId": "submit"}   # Move to review
POST /instances/{instanceId}/execute {"actionId": "approve"}  # Approve document
```

## 📊 API Reference

### Workflow Definitions
| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/workflows` | List all workflow definitions |
| `POST` | `/workflows` | Create a new workflow definition |

### Workflow Instances  
| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/workflows/{definitionId}/instances` | Start a new workflow instance |
| `GET` | `/instances/{instanceId}` | Get instance status and history |
| `POST` | `/instances/{instanceId}/execute` | Execute an action to change state |

### Error Responses
All endpoints return consistent error format with helpful messages:
```json
{
  "type": "ValidationError",
  "title": "Validation Failed", 
  "detail": "Action 'approve' cannot be executed from current state 'draft'. Valid states are: review",
  "status": 400,
  "correlationId": "unique-request-id",
  "timestamp": "2025-01-18T10:30:00Z"
}
```

**Error Types**:
- `ValidationError` (400) - Invalid request or business rule violation
- `NotFound` (404) - Resource doesn't exist
- `ConcurrencyError` (409) - Concurrent modification conflict
- `Timeout` (408) - Operation took too long
- `InternalServerError` (500) - Unexpected system error

## 🧪 Testing

### Running Tests
```bash
dotnet test  # Run all tests
dotnet test --logger trx --collect:"XPlat Code Coverage"  # With coverage
```

### Test Coverage
My test suite covers:
- ✅ All core workflow operations
- ✅ Edge case validations (circular dependencies, unreachable states)
- ✅ Race condition scenarios
- ✅ Input validation boundaries
- ✅ Error handling paths
- ✅ Concurrent access patterns

### Key Test Scenarios
```csharp
[Fact] public void CreateWorkflow_WithCircularDependency_ThrowsValidationException()
[Fact] public void CreateWorkflow_WithUnreachableStates_ThrowsValidationException()  
[Fact] public void ExecuteAction_ConcurrentModification_HandlesGracefully()
[Fact] public void ExecuteAction_FromFinalState_ThrowsValidationException()
[Fact] public void CreateWorkflow_WithDeadEndStates_ThrowsValidationException()
```

## 📈 Design Decisions

### My Approach to This Challenge

**1. Safety First Philosophy**  
I prioritized preventing problems over just handling them. That's why I implemented:
- Circular dependency detection (prevents infinite loops)
- Input validation (prevents crashes)
- Race condition protection (prevents corruption)

**2. Clean Architecture**  
I chose this structure because:
- **Separation of Concerns**: Each layer has one responsibility
- **Testability**: Easy to unit test business logic
- **Maintainability**: Changes in one layer don't affect others
- **Scalability**: Easy to swap implementations (e.g., database for in-memory storage)

**3. Algorithm Choices**

**For Circular Dependencies**: DFS with Three Colors
- **Why**: Mathematically proven, O(V+E) time complexity
- **How**: White = unvisited, Gray = visiting, Black = done
- **Detection**: If we visit a Gray node, we found a cycle

**For Unreachable States**: Breadth-First Search  
- **Why**: Natural "ripple effect" from initial states
- **How**: Start from initial states, follow all possible paths
- **Detection**: Any state not reached is unreachable

**For Race Conditions**: Optimistic Concurrency Control
- **Why**: Better performance than locks, works in distributed systems
- **How**: Version numbers increment with each change
- **Detection**: Save fails if version changed since read

### Trade-offs I Made

**In-Memory Storage**:
- ✅ **Pro**: Simple, fast, meets demo requirements
- ❌ **Con**: Data lost on restart
- **Decision**: Perfect for take-home exercise, easy to replace later

**Human-Readable Comments**:  
- ✅ **Pro**: Makes code approachable, explains intent
- ❌ **Con**: Less formal than traditional documentation
- **Decision**: Improves maintainability for small teams

**Comprehensive Validation**:
- ✅ **Pro**: Prevents runtime failures, better user experience
- ❌ **Con**: More complex code, longer development time
- **Decision**: Worth it for production-quality code

## 🔮 Future Enhancements

If I had more time, I would add:

### **Database Integration**
- Replace in-memory storage with Entity Framework
- Add workflow definition versioning
- Implement soft deletes and audit trails

### **Event-Driven Architecture**
- Publish events when workflow states change
- Enable integrations with external systems
- Add webhook support for notifications

### **Advanced Querying**
- Filter workflows by status, date, definition
- Pagination for large datasets
- Full-text search on workflow definitions

### **Security & Authorization**
- User authentication and role-based access
- Tenant isolation for multi-org scenarios
- API key management

### **Monitoring & Observability**
- Performance metrics and dashboards
- Health checks and readiness probes
- Distributed tracing with OpenTelemetry

---

## 🎯 What Makes This Stand Out

### **1. Production-Ready Edge Case Handling**
Most implementations handle happy paths. I added sophisticated validation that prevents real-world problems like infinite loops and data corruption.

### **2. Clear Error Communication**  
Instead of cryptic errors, I provide helpful messages that explain what went wrong and how to fix it.

### **3. Scalable Architecture**
Clean separation of concerns makes it easy to add features like authentication, databases, or event publishing.

### **4. Comprehensive Testing**
I test not just functionality, but edge cases, error scenarios, and concurrent access patterns.

### **5. Human-Readable Code**
My comments explain the "why" behind decisions, making the code maintainable for future developers.

---

**Built by**: Shahid Mollick  
**Time Investment**: ~3 hours (exceeded 2-hour suggestion to showcase advanced features)  
**Focus**: Production-ready code with comprehensive edge case handling and clear documentation

**Key Differentiator**: While most candidates implement basic requirements, I've added sophisticated algorithm-based validations that prevent real-world operational issues.
