const BRL = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });
const DATE = new Intl.DateTimeFormat("pt-BR");

/** Formata valor em reais (pt-BR). */
export function formatBRL(value: number): string {
  return BRL.format(value);
}

/** Formata data no padrão pt-BR (dd/mm/aaaa). */
export function formatDate(value: Date | string | number): string {
  return DATE.format(new Date(value));
}
