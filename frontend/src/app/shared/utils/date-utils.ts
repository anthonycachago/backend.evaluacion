/** Inicio del día local en formato ISO (UTC), para filtros "desde". */
export function startOfDayIso(date: Date): string {
  const start = new Date(date);
  start.setHours(0, 0, 0, 0);
  return start.toISOString();
}

/** Fin del día local en formato ISO (UTC), para filtros "hasta" inclusivos. */
export function endOfDayIso(date: Date): string {
  const end = new Date(date);
  end.setHours(23, 59, 59, 999);
  return end.toISOString();
}

export function isSameDay(a: Date, b: Date): boolean {
  return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
}

/**
 * Fecha a enviar para una transacción elegida en un datepicker (que no tiene hora):
 * si es hoy se usa la hora actual; si es otro día, el mediodía local para evitar saltos por zona horaria.
 */
export function transactionDateIso(selected: Date): string {
  const now = new Date();
  if (isSameDay(selected, now)) return now.toISOString();

  const noon = new Date(selected);
  noon.setHours(12, 0, 0, 0);
  return noon.toISOString();
}
