import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi, beforeEach } from "vitest";
import { AuthGate } from "./AuthGate";

describe("AuthGate", () => {
  beforeEach(() => { vi.stubGlobal("fetch", vi.fn()); });

  it("shows children after a successful login", async () => {
    (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({ ok: true });
    render(<AuthGate><p>Protected content</p></AuthGate>);
    const user = userEvent.setup();

    await user.type(screen.getByLabelText("Username"), "alice");
    await user.type(screen.getByLabelText("Password"), "hunter22222");
    await user.click(screen.getByRole("button", { name: /^log in$/i }));

    expect(await screen.findByText("Protected content")).toBeInTheDocument();
  });

  it("shows an error and does not reveal children on failed login", async () => {
    (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: false, status: 401, json: async () => ({ detail: "Invalid username or password." }),
    });
    render(<AuthGate><p>Protected content</p></AuthGate>);
    const user = userEvent.setup();

    await user.type(screen.getByLabelText("Username"), "alice");
    await user.type(screen.getByLabelText("Password"), "wrong");
    await user.click(screen.getByRole("button", { name: /^log in$/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Invalid username or password.");
    expect(screen.queryByText("Protected content")).not.toBeInTheDocument();
  });

  it("switches to register mode and calls /auth/register", async () => {
    (fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({ ok: true });
    render(<AuthGate><p>Protected content</p></AuthGate>);
    const user = userEvent.setup();

    await user.click(screen.getByRole("button", { name: /need an account/i }));
    await user.type(screen.getByLabelText("Username"), "newuser");
    await user.type(screen.getByLabelText("Password"), "hunter22222");
    await user.click(screen.getByRole("button", { name: /^register$/i }));

    expect(fetch).toHaveBeenCalledWith("/api/auth/register", expect.objectContaining({ method: "POST" }));
    expect(await screen.findByText("Protected content")).toBeInTheDocument();
  });
});
