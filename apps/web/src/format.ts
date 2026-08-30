export function formatPercent(value: number): string {
  return `${new Intl.NumberFormat("es-PA", { maximumFractionDigits: 1 }).format(value)}%`;
}

export function formatDuration(totalSeconds: number | null): string {
  if (totalSeconds === null) return "—";
  const safeSeconds = Math.max(0, Math.round(totalSeconds));
  const minutes = Math.floor(safeSeconds / 60);
  const seconds = safeSeconds % 60;
  return `${minutes}:${seconds.toString().padStart(2, "0")}`;
}

export function formatDate(value: string | null): string {
  if (!value) return "Fecha no disponible";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "Fecha no disponible";
  }

  return new Intl.DateTimeFormat("es-PA", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}

export function winRate(wins: number, games: number): string {
  return games > 0 ? formatPercent((wins * 100) / games) : "—";
}

export function queueLabel(queueId: number | null): string {
  if (queueId === null) return "Cola no disponible";
  const knownQueues: Record<number, string> = {
    400: "Normal reclutamiento",
    420: "Clasificatoria Solo/Dúo",
    430: "Normal a ciegas",
    440: "Clasificatoria Flexible",
    450: "ARAM",
    490: "Partida rápida",
  };
  return knownQueues[queueId] ?? `Cola ${queueId}`;
}
