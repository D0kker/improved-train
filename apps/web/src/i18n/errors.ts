import { ApiError } from "../api";

export type ApiErrorMessageKey =
  | "badRequest"
  | "playerNotFound"
  | "matchNotFound"
  | "notFound"
  | "rateLimited"
  | "riotUnavailable"
  | "timeout"
  | "unexpected";

export function apiErrorMessageKey(
  error: unknown,
  context: "player" | "match" | "general" = "general",
): ApiErrorMessageKey | null {
  if (!(error instanceof ApiError)) return null;
  switch (error.status) {
    case 400:
      return "badRequest";
    case 404:
      return context === "player"
        ? "playerNotFound"
        : context === "match"
          ? "matchNotFound"
          : "notFound";
    case 429:
      return "rateLimited";
    case 502:
    case 503:
      return "riotUnavailable";
    case 504:
      return "timeout";
    default:
      return "unexpected";
  }
}
