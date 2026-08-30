export function utcCalendarDate(instant: Date): string {
  return instant.toISOString().slice(0, 10);
}

export function addCalendarDays(isoDate: string, days: number): string {
  const [year, month, day] = isoDate.split('-').map((part) => Number(part));
  if (!year || !month || !day) {
    throw new Error('next_review_at must be a YYYY-MM-DD calendar date.');
  }

  const next = new Date(Date.UTC(year, month - 1, day + days));
  return utcCalendarDate(next);
}
