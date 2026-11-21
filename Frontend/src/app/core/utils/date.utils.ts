/**
 * Utility functions for robust date parsing and timezone handling.
 */

/**
 * Normalizes an API date string so that if it represents UTC but lacks a trailing 'Z'
 * (e.g., from databases returning "2026-11-21T10:50:00"), 'Z' is appended so
 * JavaScript and Angular format it in the user's local timezone instead of treating it as local time.
 */
export function ensureUtcIsoString(dateInput: string | Date | null | undefined): string {
  if (!dateInput) return '';
  if (dateInput instanceof Date) return dateInput.toISOString();

  const s = String(dateInput).trim();
  if (s.includes('T') && !s.endsWith('Z') && !/[+-]\d{2}:\d{2}$/.test(s)) {
    return s + 'Z';
  }
  return s;
}

/**
 * Parses a date value safely into a JavaScript Date object, ensuring UTC interpretation.
 */
export function parseUtcDate(dateInput: string | Date | null | undefined): Date | null {
  const iso = ensureUtcIsoString(dateInput);
  if (!iso) return null;
  const d = new Date(iso);
  return isNaN(d.getTime()) ? null : d;
}
