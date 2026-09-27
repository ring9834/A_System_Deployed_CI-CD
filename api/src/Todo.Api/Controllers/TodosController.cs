using Microsoft.AspNetCore.Mvc;
using Todo.Application.Todos;
using Todo.Application.Todos.Dtos;
using Todo.Domain.Entities;

namespace Todo.Api.Controllers;

[ApiController]
[Route("todos")] // ingress strips /api before forwarding — see ADR-0002's rewrite note
public class TodosController : ControllerBase
{
    private readonly TodoService _todos;

    // TODO(Slice 4): replace with the real authenticated user's OwnerId from the JWT
    // cookie (ADR-0007). Hardcoded here deliberately — Slices 1-3 build against a single
    // seeded user so their tests stay simple; Slice 4 retrofits real ownership on top of
    // already-working, already-tested behaviour.
    private static readonly Guid DevOwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public TodosController(TodoService todos) => _todos = todos;

    [HttpPost]
    public async Task<ActionResult<TodoDto>> Create(
        [FromBody] CreateTodoRequest request, CancellationToken ct)
    {
        var dto = await _todos.CreateAsync(DevOwnerId, request, ct);
        return CreatedAtAction(nameof(Create), new { id = dto.Id }, dto);
    }

    [HttpGet]
    public async Task<ActionResult<TodoPageDto>> List(
        [FromQuery] TodoState? state,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            return ValidationProblem("page must be >=1 and pageSize must be 1-100.");

        var result = await _todos.ListAsync(DevOwnerId, state, page, pageSize, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/transition")]
    public async Task<ActionResult<TodoDto>> Transition(
        Guid id, [FromBody] TransitionTodoRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<TodoState>(request.TargetState, ignoreCase: true, out var target))
            return ValidationProblem($"Unknown target state '{request.TargetState}'.");

        var dto = await _todos.TransitionAsync(DevOwnerId, id, target, request.ScheduledFor, ct);
        return Ok(dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TodoDto>> Update(
        Guid id, [FromBody] UpdateTodoRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ifMatch))
            return ValidationProblem("If-Match header is required.");

        var dto = await _todos.UpdateAsync(DevOwnerId, id, request, ifMatch, ct);
        return Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _todos.DeleteAsync(DevOwnerId, id, ct);
        return NoContent();
    }
}
