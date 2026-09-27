"use client";

import { useTodos } from "../lib/use-todos";

const stateStyles: Record<string, { border: string; dot: string }> = {
  todo: { border: "border-l-moss-600", dot: "bg-moss-600" },
  scheduled: { border: "border-l-gold-600", dot: "bg-gold-600" },
  done: { border: "border-l-sage-600", dot: "bg-sage-600" },
};

export function TodoList() {
  const { data, isLoading, isError, error } = useTodos();

  if (isLoading) {
    return <p className="text-ink/60">Pulling up today's jobs…</p>;
  }

  if (isError) {
    return (
      <p role="alert" className="rounded-md bg-danger/10 px-4 py-3 text-danger">
        Couldn't reach the job list. Check your connection and try again.
        <span className="block text-sm text-danger/70">{error.message}</span>
      </p>
    );
  }

  if (!data) {
    return null; // narrows `data` for TS below — isLoading/isError above don't, since
                 // destructuring breaks the discriminated-union link TanStack Query relies on
  }

  if (data.items.length === 0) {
    return (
      <p className="rounded-md border border-dashed border-ink/20 px-4 py-6 text-center text-ink/60">
        No jobs on the list yet. Add the first one above.
      </p>
    );
  }

  return (
    <ul className="space-y-3">
      {data.items.map((todo) => {
        const style = stateStyles[todo.state] ?? stateStyles.todo;
        return (
          <li
            key={todo.id}
            className={`rounded-md border border-ink/10 border-l-4 ${style.border} bg-surface p-4 shadow-sm`}
          >
            <div className="flex items-start justify-between gap-3">
              <h3 className="font-heading font-medium text-ink">{todo.title}</h3>
              <span className="flex items-center gap-1.5 text-xs text-ink/60">
                <span className={`h-2 w-2 rounded-full ${style.dot}`} />
                {todo.state}
              </span>
            </div>
            <p className="mt-1 text-sm text-ink/60">{todo.location.address}</p>
          </li>
        );
      })}
    </ul>
  );
}
