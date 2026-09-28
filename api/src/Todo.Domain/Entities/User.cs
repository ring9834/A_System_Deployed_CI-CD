namespace Todo.Domain.Entities;

// Minimal, self-issued account (ADR-0007) — not a real identity system. No email
// verification, no password reset, no rate limiting (flagged as a deferred gap in
// design.md's STRIDE table). Good enough to prove real owner isolation (Q1), not more.
public class User
{
    public Guid Id { get; private set; }
    public string Username { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;

    private User() { }

    public static User Create(string username, string passwordHash) => new()
    {
        Id = Guid.NewGuid(),
        Username = username,
        PasswordHash = passwordHash,
    };
}
