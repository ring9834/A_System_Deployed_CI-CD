"use client";

import { useEffect, useState } from "react";

type HealthResponse = {
  status: string;
};

type HealthState =
  | { kind: "loading" }
  | { kind: "error"; message: string }
  | { kind: "ok"; data: HealthResponse };

export default function Home() {
  const [health, setHealth] = useState<HealthState>({ kind: "loading" });

  useEffect(() => {
    let cancelled = false;

    async function checkHealth() {
      try {
        const res = await fetch("/api/health");
        if (!res.ok) {
          throw new Error(`API responded with ${res.status}`);
        }
        const data = (await res.json()) as HealthResponse;
        if (!cancelled) setHealth({ kind: "ok", data });
      } catch (err) {
        if (!cancelled) {
          setHealth({
            kind: "error",
            message: err instanceof Error ? err.message : "Unknown error",
          });
        }
      }
    }

    void checkHealth();
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <div className="flex flex-col flex-1 items-center justify-center bg-zinc-50 font-sans dark:bg-black">
      <main className="flex w-full max-w-md flex-col items-center gap-4 py-32 px-8 text-center">
        <h1 className="text-2xl font-semibold text-black dark:text-zinc-50">
          Todo Planner
        </h1>

        {health.kind === "loading" && (
          <p className="text-zinc-600 dark:text-zinc-400">Checking API…</p>
        )}

        {health.kind === "ok" && (
          <p className="rounded bg-green-100 px-3 py-1.5 text-green-800 dark:bg-green-950 dark:text-green-300">
            API status: {health.data.status}
          </p>
        )}

        {health.kind === "error" && (
          <p className="rounded bg-red-100 px-3 py-1.5 text-red-800 dark:bg-red-950 dark:text-red-300">
            Could not reach the API: {health.message}
          </p>
        )}
      </main>
    </div>
  );
}
