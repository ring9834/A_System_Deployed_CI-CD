"use client";

import { useState } from "react";
import { login, register } from "../lib/todos-api";

export function AuthGate({ children }: { children: React.ReactNode }) {
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [mode, setMode] = useState<"login" | "register">("login");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Known simplification: this state is client-side only, so a page refresh loses it
  // even though the session cookie itself persists. A `/auth/me` check on mount would
  // close this gap; not built now, since it's a UX polish item, not a security one — the
  // server still fully enforces auth regardless of what this component believes.
  if (isLoggedIn) return <>{children}</>;

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      if (mode === "register") await register(username, password);
      else await login(username, password);
      setIsLoggedIn(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Something went wrong.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="mx-auto flex min-h-screen max-w-sm flex-col justify-center px-4">
      <form onSubmit={handleSubmit} className="rounded-lg border border-moss-900/10 bg-surface p-6 shadow-sm">
        <h1 className="font-heading text-xl font-semibold text-ink">
          {mode === "login" ? "Log in" : "Create an account"}
        </h1>

        <div className="mt-4 space-y-3">
          <div>
            <label htmlFor="username" className="block text-sm font-medium text-ink/80">Username</label>
            <input
              id="username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              required
              className="mt-1 block w-full rounded-md border border-ink/15 px-3 py-2"
            />
          </div>
          <div>
            <label htmlFor="password" className="block text-sm font-medium text-ink/80">Password</label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              className="mt-1 block w-full rounded-md border border-ink/15 px-3 py-2"
            />
          </div>

          <button
            type="submit"
            disabled={isSubmitting}
            className="h-11 w-full rounded-md bg-moss-600 font-medium text-white hover:bg-moss-700 disabled:opacity-60"
          >
            {mode === "login" ? "Log in" : "Register"}
          </button>

          {error && <p role="alert" className="text-sm text-danger">{error}</p>}

          <button
            type="button"
            onClick={() => { setMode(mode === "login" ? "register" : "login"); setError(null); }}
            className="text-sm text-ink/60 underline"
          >
            {mode === "login" ? "Need an account? Register" : "Already have an account? Log in"}
          </button>
        </div>
      </form>
    </main>
  );
}
