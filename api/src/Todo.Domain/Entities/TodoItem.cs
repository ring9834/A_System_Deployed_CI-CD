using Todo.Domain.Exceptions;

namespace Todo.Domain.Entities;

// Zero framework dependencies (ADR-0003) — no EF Core, no ASP.NET types here.
// State-transition METHODS are deliberately left as TODOs: Slice 2's plan is to write
// TodoStateMachineTests first (encoding every arrow in design.md's state diagram), then
// implement against those tests — not have this scaffold pre-decide the logic.
public class TodoItem
{
    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public string Title { get; private set; } = default!;
    public TodoState State { get; private set; }
    public string LocationAddress { get; private set; } = default!;
    public double? LocationLatitude { get; private set; }
    public double? LocationLongitude { get; private set; }
    public DateTimeOffset? ScheduledFor { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private TodoItem() { } // EF Core materialization

    public static TodoItem Create(
        Guid ownerId, string title, string locationAddress,
        double? lat, double? lng, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(locationAddress))
            throw new ArgumentException("Location address is required.", nameof(locationAddress));

        var now = clock.UtcNow;
        return new TodoItem
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Title = title,
            State = TodoState.Todo,
            LocationAddress = locationAddress,
            LocationLatitude = lat,
            LocationLongitude = lng,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    // --- Slice 2 (TDD): implement against TodoStateMachineTests, not the other way around ---
    //
    // public void TransitionTo(TodoState target, DateTimeOffset? scheduledFor, IClock clock)
    // {
    //     // A3 legal transitions: Todo<->Scheduled, Todo->Done, Scheduled->Done.
    //     // Done is terminal — no transition leaves it.
    //     // Scheduled requires scheduledFor; unscheduling (Scheduled->Todo) clears it.
    //     // On any illegal transition: throw new InvalidTodoTransitionException(State, target);
    //     throw new NotImplementedException("Implement via TDD in Slice 2 — see design.md's state diagram.");
    // }

    public void UpdateDetails(string title, string locationAddress, double? lat, double? lng, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        Title = title;
        LocationAddress = locationAddress;
        LocationLatitude = lat;
        LocationLongitude = lng;
        UpdatedAt = clock.UtcNow;
    }
}
