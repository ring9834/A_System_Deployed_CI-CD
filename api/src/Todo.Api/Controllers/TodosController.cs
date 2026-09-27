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
}
