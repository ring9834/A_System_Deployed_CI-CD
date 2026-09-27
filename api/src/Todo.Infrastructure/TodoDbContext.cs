using Microsoft.EntityFrameworkCore;
using Todo.Domain.Entities;

namespace Todo.Infrastructure;

// Injected directly into Application services — no repository/port wrapper (ADR-0016):
// DbContext already IS a Unit-of-Work/Repository; wrapping it again adds indirection
// without adding testability, since Domain (the thing worth unit-testing) has zero
// EF Core dependency already.
public class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options) { }

    public DbSet<TodoItem> Todos => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoItem>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Title).HasMaxLength(200).IsRequired();
            entity.Property(t => t.LocationAddress).HasMaxLength(500).IsRequired();
            entity.Property(t => t.State).HasConversion<string>().HasMaxLength(20);

            // ADR-0004: PostgreSQL's xmin system column as the optimistic-concurrency
            // token (Q6) — NOT "RowVersion", which is SQL Server-specific.
            entity.Property<uint>("xmin").IsRowVersion();

            // design.md: the one index this app actually needs — every list query
            // filters by owner and (optionally) state together (Q1 + Q3).
            entity.HasIndex(t => new { t.OwnerId, t.State });
        });
    }
}
