import { describe, it, expect } from "vitest";
import { formatBRL, formatDate } from "./format";

describe("format pt-BR", () => {
  it("formata reais", () => {
    expect(formatBRL(1234.5)).toContain("1.234,50");
  });

  it("formata data", () => {
    expect(formatDate("2026-06-30T12:00:00Z")).toMatch(/\d{2}\/\d{2}\/\d{4}/);
  });
});
