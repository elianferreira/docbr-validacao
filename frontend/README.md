# docbr-validacao

App **Expo universal** (web + Android + iOS do mesmo código) no padrão Univali.
Toolchain **Bun**. A stack é fechada e mora em `univali/univali-stack`
(`FRONTEND_STACK.md`).

## Rodar

```bash
bun install
cp .env.example .env        # nativo: EXPO_PUBLIC_API_URL; web usa /api relativo
bun run web                 # Expo web (http://localhost:8081)
```

Nativo: `bun run android` / `bun run ios` (precisa de device/emulador e prebuild).

> **Bun roda tudo** — install, audit, `expo start`, `expo export`, Vitest e
> Playwright. Node só é necessário se o app adotar `jest-expo` (alvo nativo).
> O `bunfig.toml` fixa `linker = "hoisted"`: o Metro resolve pelo resolver do node e
> quebra no layout isolado.

## Scripts

| Script | O quê |
| --- | --- |
| `bun run web` / `start` | Expo dev server (web / seletor de plataforma). |
| `bun run build:web` | `expo export --platform web` → `dist/` (o que vai para o k8s). |
| `bun run serve:web` | Serve o `dist` em :4173 com fallback SPA (`--single`). |
| `bun run lint` / `typecheck` | ESLint flat / `tsc --noEmit`. |
| `bun run test` | Vitest + jsdom + react-native-web. |
| `bun run test:e2e` | Playwright contra o `dist` exportado. |
| `bun run gen:api` | Orval: OpenAPI do .NET → hooks Query em `src/api/__generated__`. |
| `bun run audit` | `bun audit --audit-level=high` (mesmo gate do CI). |

## Estrutura

```
app/                    SÓ rotas — arquivo é a URL
  _layout.tsx           único layout: providers + Portao + casca
  index.tsx             Redirect para a primeira tela real
  inicio.tsx            rota fina: aponta a tela
src/
  telas/                as telas de verdade
  rotas/                schemas Zod da query + useFiltrosDaUrl
  componentes/          Portao (guarda) + layout/ (casca)
  tema/                 tokens claro/escuro + TemaContext + <head> do web
  auth/                 sessão, token por plataforma (SecureStore / sessionStorage)
  api/                  Axios + queryClient + mutator + __generated__ (Orval)
  test/                 dublê do expo-router, setup, helper de largura de janela
e2e/                    Playwright contra o export
```

## O que já vem pronto

- **Expo Router** no desenho da §09 do `FRONTEND_STACK.md`: rota fina, tela em
  `src/telas/`, guarda de sessão única, filtro de tela **na URL** com Zod.
- **Tema claro e escuro** (obrigatório no padrão): segue o SO
  (`userInterfaceStyle: "automatic"`) e aceita troca manual, persistida por plataforma.
- **Sessão**: login no GAM pela tela do app, token em `expo-secure-store` (nativo) e
  `sessionStorage` (web), 401 encerrando a sessão.
- **TanStack Query** + Axios + Zod + Orval ligados.
- **Deploy**: `Dockerfile` OL10 **só com Bun** + nginx não-root em `:8080` com CSP,
  fallback SPA e cache imutável de `/_expo/` e `/assets/`; `k8s/` + `.gitlab-ci.yml`
  (desenv no push, produção na tag).

## Antes do primeiro deploy

1. Trocar `docbr-validacao` pelo número **`skapp<NN>`** quando atribuído (imagens, k8s,
   hosts do ingress).
2. Apontar o `orval.config.ts` para o OpenAPI do serviço .NET e rodar `bun run gen:api`.
3. Ajustar a permission do GAM em `src/auth/authContext.ts` (`PERMISSAO_APP`).
4. Alvo nativo: adicionar `assets/icon.png` + `eas.json` e o job de `jest-expo`
   (3 presets, em imagem `node:`) antes de prometer binário.
