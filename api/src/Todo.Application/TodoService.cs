using Microsoft.EntityFrameworkCore;
using Todo.Application.Todos.Dtos;
using Todo.Domain;
using Todo.Domain.Entities;
using Todo.Infrastructure;

namespace Todo.Application.Todos;

// Concrete class, injected directly — no ITodoService interface with a single
// implementation (that would be indirection with no consumer that needs it, same
// principle as ADR-0016's "no repository layer").
public class TodoService
{
    private readonly TodoDbContext _db;
    private readonly IClock _clock;

    public TodoService(TodoDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<TodoDto> CreateAsync(Guid ownerId, CreateTodoRequest request, CancellationToken ct)
    {
        var todo = TodoItem.Create(
            ownerId, request.Title, request.Location.Address,
            request.Location.Latitude, request.Location.Longitude, _clock);

        _db.Todos.Add(todo);
        await _db.SaveChangesAsync(ct);
        return ToDto(todo);
    }

    public async Task<TodoPageDto> ListAsync(
        Guid ownerId, TodoState? state, int page, int pageSize, CancellationToken ct)
    {
        // Q1: every query filtered by the authenticated caller's OwnerId — never from
        // the request body. Q3: filtered by state, paginated.
        var query = _db.Todos.Where(t => t.OwnerId == ownerId);
        if (state is not null)
            query = query.Where(t => t.State == state);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new TodoPageDto(items.Select(ToDto).ToList(), page, pageSize, totalCount);
    }

    private static TodoDto ToDto(TodoItem t) => new(
        t.Id, t.Title, t.State.ToString(),
        new LocationDto(t.LocationAddress, t.LocationLatitude, t.LocationLongitude),
        t.ScheduledFor, t.CreatedAt, t.UpdatedAt,
        ETag: "" /* filled by the controller from the xmin shadow property — Slice 3 */);
}
