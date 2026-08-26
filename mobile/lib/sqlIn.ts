export function sqlInPlaceholders(count: number): string {
  return Array.from({ length: count }, () => '?').join(', ');
}
