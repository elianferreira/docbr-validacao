# AGENTS.md — docbr-validacao (frontend Expo)

App Expo universal no padrão Univali. **A stack é fechada e mora em outro repo**
(`univali/univali-stack`) — este arquivo não repete regra de stack, só aponta e
guarda o não-óbvio DESTE app.

## Fonte de verdade (ler antes de decidir)

- `FRONTEND_STACK.md` (univali-stack) — Expo universal (React 19 + React Native /
  react-native-web, Metro, Bun), Expo Router, TanStack, Zod, Orval. **§09 é o padrão
  de roteamento e vale como escrito.** Divergência só com justificativa registrada aqui.
- `DEPLOY_STACK.md` — Docker (OracleLinux 10), nginx não-root + CSP, k8s, CI GitLab.
- `PLANO_EXPO.md` — decisões fechadas (Bun como runtime, TS prioritário, tema escuro,
  favicon institucional) e o que ainda está aberto.
- `FRONTEND_STACK.md` §08 — se este app for exibir tela que mora em **outro repo**:
  default é pacote `@univali/*`; alias do Metro para a fonte do irmão é **exceção**,
  com contrato fechado (env de override, preflight, `blockList`, versões alinhadas).
  Nunca clone só-fonte nem cópia da tela para cá — o que for usado se registra aqui.

## Comandos

```bash
bun install
cp .env.example .env
bun run web                                                   # dev web
bun run lint && bun run typecheck && bun run test             # Definition of Done
bun run build:web && bun run test:e2e                         # export + Playwright
```

**Bun roda tudo**, inclusive Metro (`expo start`, `expo export`) — medido: a imagem de
build deste app instala só Bun. Node só entra se o app adotar `jest-expo`.

## Regras deste app que não se negociam

- **Rota é arquivo fino.** `app/<rota>.tsx` só faz `return <TelaX />`; a tela mora em
  `src/telas/`. Tela escrita dentro de `app/` mata o reuso e o teste sem roteador.
- **Um `_layout.tsx` só**, na raiz, com a ordem dos providers como está. `<Slot />`,
  não `<Stack />`, enquanto for painel administrativo.
- **Guarda de sessão é o `Portao`**, um só, no topo. No Expo Router as rotas existem
  sempre (arquivo em disco) — não há `beforeLoad`. Permissão por tela filtra menu e é
  checada dentro da tela.
- **Filtro de tela vive na URL** (`useFiltrosDaUrl` + schema Zod em `src/rotas/`),
  nunca em `useState`: a tela tem de ser compartilhável por link. `z.coerce` nos
  números (o `useLocalSearchParams` entrega string), `.catch()` no lugar de default de
  rota, `undefined` para remover param.
- **`Link` do expo-router SEM `asChild`** — com ele a âncora do react-native-web fica
  com a navegação nativa do browser e o clique recarrega o documento (medido).
- **Estilo é `StyleSheet` + tokens** (`src/tema/tokens.ts`), com cor vinda de
  `useTema()`. Sem Tailwind/`className`, sem NativeWind. **Tema claro e escuro são
  obrigatórios** — toda tela nova tem de ficar legível nos dois.
- **Módulo com variante de plataforma se importa SEM extensão** (`./tokenStorage`,
  não `./tokenStorage.ts`): com extensão explícita o Metro resolve o literal e o web
  carrega a versão nativa, sem erro nenhum.
- **Token: `expo-secure-store` no nativo, `sessionStorage` no web.** Nunca
  `AsyncStorage` para credencial.
- **`EXPO_PUBLIC_*` é inlinada no bundle** (texto claro no APK). Nunca segredo ali.
- **`src/api/__generated__` é gerado** por `bun run gen:api` — não editar à mão.

## Teste

- Componente e lógica: **Vitest + jsdom + react-native-web** (roda em Bun). O
  `expo-router` real não passa no transform → dublê em `src/test/expoRouterFake.tsx`,
  aliasado no `vitest.config.mts`.
- `useWindowDimensions` mede **0** no jsdom: use `comLargura()` (`src/test/janela.ts`)
  ou todo teste roda como mobile e o ramo desktop nunca é exercitado.
- **`jest-expo` (3 presets) só quando este app declarar alvo nativo.** Aí o job roda em
  imagem `node:` — na `oven/bun` o `node` é symlink do bun e a suíte fecha verde sem
  executar teste.
- E2E ataca o **`expo export` servido** (`serve dist --single`), não o dev server.

## Deploy / infra

- **k8s só em `producao`/`desenv`** (namespaces compartilhados). NUNCA ns dedicado.
- **Probe em `/`** (estático, anônimo). Nunca em `/health` de backend.
- **nginx não-root, UID numérico 10001, porta 8080**; o Service mapeia 80 → 8080.
- **Naming de prod**: trocar `docbr-validacao` por `skapp<NN>` / `skapp<NN>d` quando
  atribuído.
- **`/api`**: no web o app chama caminho relativo; quem proxia é o Metro (dev) ou o
  Ingress/nginx (produção). Não embutir URL de backend na imagem.
- **Sem co-autoria nos commits** (regra global Univali). Conventional Commits.

## Pendências herdadas do padrão (não são bug deste app)

- `@univali/ui` v1.0 em RN não existe (Fase 3): a casca daqui é mínima e será
  substituída pelo pacote. Não reimplementar sidebar animada/skeleton por conta.
- Preset `@univali/config/eslint-expo` não existe (Fase 2): `eslint.config.js` e
  `prettier.config.js` são locais até lá.
- `<head>` do web: título e CSS global em `src/tema/documentoWeb.web.ts`. Se precisar
  de fonte custom, ver a divergência `+html` × `documentoWeb` aberta no `PLANO_EXPO.md`.
