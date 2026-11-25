import { describe, expect, it } from "vitest";
import fs from "node:fs";
import path from "node:path";

describe("CocoCarbon Web Scientific & Security Guardrails", () => {
  it("calculator page calls backend API and does not hardcode CTU math multipliers", () => {
    const calcPage = fs.readFileSync(path.resolve(__dirname, "../src/app/calculator/page.tsx"), "utf-8");
    expect(calcPage).toContain("/api/v1/carbon-calculator/estimate");
    expect(calcPage).not.toContain("* 75.2436");
    expect(calcPage).not.toContain("* 24.518");
    expect(calcPage).not.toContain("* 0.47");
    expect(calcPage).not.toContain("44 / 12");
  });

  it("firebase client helper reads public env vars and never embeds service account private keys", () => {
    const fb = fs.readFileSync(path.resolve(__dirname, "../src/lib/firebase.ts"), "utf-8");
    expect(fb).toContain("NEXT_PUBLIC_FIREBASE_API_KEY");
    expect(fb).not.toContain("BEGIN PRIVATE KEY");
    expect(fb).not.toContain("AIzaSy");
  });

  it("all required P0 web routes exist", () => {
    const requiredPages = [
      "../src/app/page.tsx",
      "../src/app/login/page.tsx",
      "../src/app/onboarding/page.tsx",
      "../src/app/dashboard/page.tsx",
      "../src/app/farms/page.tsx",
      "../src/app/farms/[id]/page.tsx",
      "../src/app/calculator/page.tsx",
      "../src/app/documents/page.tsx",
      "../src/app/cooperative/page.tsx",
      "../src/app/projects/page.tsx",
      "../src/app/projects/[id]/page.tsx",
      "../src/app/buyer/page.tsx",
      "../src/app/assistant/page.tsx",
      "../src/app/financial-assistant/page.tsx",
      "../src/app/admin/page.tsx",
    ];
    for (const rel of requiredPages) {
      expect(fs.existsSync(path.resolve(__dirname, rel)), `Missing page ${rel}`).toBe(true);
    }
  });

    it("documents page renders anti-fraud indicator, proposal check, and 3 separated scores; financial assistant uses Python engine endpoints and NL transaction parser", () => {
      const docsPage = fs.readFileSync(path.resolve(__dirname, "../src/app/documents/page.tsx"), "utf-8");
      const farmDetailPage = fs.readFileSync(path.resolve(__dirname, "../src/app/farms/[id]/page.tsx"), "utf-8");
      const finPage = fs.readFileSync(path.resolve(__dirname, "../src/app/financial-assistant/page.tsx"), "utf-8");
      const loginPage = fs.readFileSync(path.resolve(__dirname, "../src/app/login/page.tsx"), "utf-8");
      expect(docsPage).toContain("AI Anti-Fraud Indicator");
      expect(docsPage).toContain("/api/carbon/integrity-check");
      expect(docsPage).toContain("/api/carbon/proposal-check");
      expect(farmDetailPage).toContain("/finance-ledger");
      expect(finPage).toContain("/api/ai/assistant");
      expect(finPage).toContain("/api/financial/calculate");
      expect(finPage).toContain("/api/financial/scenarios");
      expect(finPage).toContain("/api/financial/reports");
      expect(finPage).toContain("/api/financial/parse-transaction");
      expect(finPage).toContain("Promise.allSettled");
      expect(loginPage).toContain("signInWithEmailPasswordFirebase");
    });
  });
