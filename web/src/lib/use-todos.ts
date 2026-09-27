import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createTodo, fetchTodos, type CreateTodoRequest } from "./todos-api";

export function useTodos(page = 1, pageSize = 20) {
  return useQuery({
    queryKey: ["todos", page, pageSize],
    queryFn: () => fetchTodos(page, pageSize),
  });
}

export function useCreateTodo() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateTodoRequest) => createTodo(request),
    // ADR-0023: create is NOT idempotent, so this mutation does not auto-retry
    // (TanStack Query's default — confirmed correct, not overridden).
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["todos"] });
    },
  });
}
