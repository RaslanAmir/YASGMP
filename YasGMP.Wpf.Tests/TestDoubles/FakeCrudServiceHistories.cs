using System.Collections.Generic;
using YasGMP.Models;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.Tests.TestDoubles;

public sealed partial class FakeCapaCrudService
{
    public List<(string Operation, CapaCase Entity, CapaCrudContext Context)> TransitionHistory { get; } = new();

    private void RecordTransition(string operation, CapaCase entity, CapaCrudContext context)
        => TransitionHistory.Add((operation, Clone(entity), context));
}

public sealed partial class FakeChangeControlCrudService
{
    public List<(string Operation, ChangeControl Entity, ChangeControlCrudContext Context)> TransitionHistory { get; } = new();

    private void RecordTransition(string operation, ChangeControl entity, ChangeControlCrudContext context)
        => TransitionHistory.Add((operation, Clone(entity), context));
}

public sealed partial class FakeIncidentCrudService
{
    public List<(string Operation, Incident Entity, IncidentCrudContext Context)> TransitionHistory { get; } = new();

    private void RecordTransition(string operation, Incident entity, IncidentCrudContext context)
        => TransitionHistory.Add((operation, Clone(entity), context));
}
