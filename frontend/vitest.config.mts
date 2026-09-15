import { fileURLToPath, URL } from "node:url";
import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";

/**
 * Teste de componente em Vitest + jsdom — roda em **Bun**, e é a mesma perna que o
 * browser executa. `jest-expo` (3 presets) só entra quando o app declarar alvo
 * nativo; ver o `AGENTS.md` deste app.
 *
 * As quatro peças abaixo são o que faz React Native rodar aqui, e são as mesmas que
 * o Metro usa com `platform=web`. Nenhuma é opcional:
 *
 * 1. `react-native` → `react-native-web`, com âncora (`^react-native$`) para não
 *    pegar `react-native-svg`/`react-native-safe-area-context`, que têm build web.
 * 2. Extensões `.web.*` na FRENTE: com o par `Campo.tsx`/`Campo.web.tsx`, sem isso o
 *    teste carrega a versão nativa.
 * 3. Dublê do `expo-router` (`src/test/expoRouterFake.tsx`): o pacote real traz
 *    sintaxe Flow do RN e não passa pelo transform (`Unexpected token 'typeof'`) —
 *    e nenhum teste de unidade quer o roteador de verdade.
 * 4. `react-native-svg` apontado para a entrada ESM: pelo especificador nu o Node
 *    externaliza e cai no build nativo, que estoura no parse.
 */
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: [
      { find: /^react-native$/, replacement: "react-native-web" },
      {
        find: /^expo-router$/,
        replacement: fileURLToPath(new URL("./src/test/expoRouterFake.tsx", import.meta.url)),
      },
      { find: /^react-native-svg$/, replacement: "react-native-svg/lib/module/index.js" },
      // O Vitest não lê `paths` do tsconfig: o alias de `@/` mora aqui também.
      { find: "@", replacement: fileURLToPath(new URL("./src", import.meta.url)) },
    ],
    extensions: [".web.tsx", ".web.ts", ".web.jsx", ".web.js", ".tsx", ".ts", ".jsx", ".js", ".json"],
  },
  server: { deps: { inline: ["react-native-svg"] } },
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: ["./src/test/setup.ts"],
    include: ["src/**/*.test.ts", "src/**/*.test.tsx"],
    // 5s estoura na suíte inteira (contenção de CPU) com os mesmos casos passando
    // sozinhos — medido no gestao-espaco-fisico.
    testTimeout: 20_000,
  },
});
