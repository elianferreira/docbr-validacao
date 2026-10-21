# AGENTS.md — docbr-validacao

Validação estrutural de documentos brasileiros: biblioteca .NET, API minimal, OCR de CNH
e um app Expo universal. Segue o **padrão Univali** (`C:\Projetos\univali-stack`) — leia
`BACKEND_STACK.md`, `FRONTEND_STACK.md` e `DEPLOY_STACK.md` de lá antes de decidir
arquitetura aqui. Este arquivo só registra o que **não** se deduz lendo o código.

## Comandos

```powershell
# backend
dotnet test                                  # 104 testes (solução inteira)
dotnet test Api.Host.Tests --filter asvs     # só as regras fixas de segurança do §03
dotnet run --project Api.Host --no-launch-profile --urls http://localhost:5199

# frontend (tudo em Bun)
cd frontend
bun install
bun run gen:api        # regenera o cliente a partir do /openapi/v1.json (API tem de estar no ar)
bun run web            # dev server em :8081, com proxy de /api para EXPO_API_TARGET
bun run lint && bun run typecheck && bun run test && bun run build:web

# medição do OCR (API tem de estar no ar)
.\tools\medir-ocr.ps1
```

**Definition of Done:** backend `dotnet test` (build com `TreatWarningsAsErrors`, 0 warnings)
+ `docker build`; frontend `lint` + `typecheck` + `test` + `expo export -p web`, com tema
claro e escuro conferidos.

## Estrutura

Layout de repo só-backend do `BACKEND_STACK.md` §05: projetos direto na raiz.

```
Api.Host/              host: Program.cs, Common/, Configuration/, Modules/<Modulo>/{Api,Application}
Api.Host.Tests/        xUnit + WebApplicationFactory (integração + ASVS)
DocBr.Validacao/       o DOMÍNIO: 9 validadores, POCO puro, sem ASP.NET
DocBr.Ocr/             extrator por tipo de documento (Tesseract + PDFtoImage + SkiaSharp)
DocBr.Validacao.Tests/ os 67 testes de vetor da biblioteca
DocBr.Cli/             CLI multi-tipo
frontend/              app Expo universal (Expo Router §09)
k8s/                   manifests desenv/producao
tools/                 gerar-vetores.py, medir-ocr.ps1
```

`DocBr.Validacao` e `DocBr.Ocr` **são** a camada de domínio dos módulos — por isso os
módulos do `Api.Host` têm só `Api/` e `Application/`, sem `Domain/` nem `Infrastructure/`:
não há persistência, o domínio é cálculo puro e já mora nessas bibliotecas.

## Divergências do padrão (todas deliberadas)

| Regra do padrão | Aqui | Por quê |
| --- | --- | --- |
| `RequireAuthorization()` em tudo, GAM Bearer (§03) | **Tudo anônimo**, com rate limit por IP em cada grupo | O serviço não tem banco, sessão nem dado guardado: recebe um número, calcula e responde. GAM exigiria config válida no `ValidateOnStart` para nada. A contrapartida do §03 — anônimo **sempre** com `RequireRateLimiting` — é cumprida sem exceção. |
| `/health` checa o banco (`CanConnect`) | Checa o **motor de OCR** | Não há Oracle. A dependência externa real é o modelo de idioma do Tesseract. |
| `AuthProvider` + `Portao` no layout raiz (§09.3/09.4) | Não existem | Decorre da linha acima: sem sessão não há o que guardar. O resto da §09 (rota fina, tela em `src/telas/`, `_layout` único com `<Slot/>`, filtro na URL com Zod, `Link` sem `asChild`) está inteiro. |
| Filtro de tela sempre na URL (§09.6) | `TelaArquivo.web` guarda o arquivo em `useState` | O conteúdo é um arquivo enviado pelo usuário — dado pessoal, que não cabe num link compartilhável. As outras duas telas seguem a regra. |

## Gotchas medidos

1. **`useMutation: true` no Orval quebra os GET em silêncio.** Com ele, o Orval v8 gera
   `useListarDocumentos` como *mutation*: o hook nunca dispara sozinho, a tela abre vazia,
   zero XHR no network e **nada no console**. Está fixado em `false` no `orval.config.ts`.
2. **O contrato multipart só aparece no OpenAPI com `IFormFile`.** Lendo o `HttpRequest`
   cru, o `/openapi/v1.json` não descreve o corpo e o Orval gera uma chamada sem `FormData`.
3. **`PdfSuportado` é um `[SupportedOSPlatformGuard]`, não um `if` qualquer.** O analisador
   do CA1416 não reconhece uma propriedade comum como guarda; sem os atributos o build
   falha (`TreatWarningsAsErrors`). PDFium não cobre toda plataforma.
4. **SkiaSharp gerenciado e os dois `NativeAssets` (Win32 e Linux) têm de casar** na versão
   (4.150.1). Divergindo, o *type initializer* estoura em runtime com as DLLs nativas presentes.
5. **`tesseract` e `leptonica` são dependências nativas do container.** O pacote NuGet
   `Tesseract` só traz o binário de Windows; no OracleLinux 10 elas vêm do EPEL (ver `Dockerfile`).
6. **Vetores de teste vêm da implementação de referência, nunca escritos à mão.** Conferência
   nos dois sentidos, 0 divergências em 2.400 documentos. Os gists de CNH em C# que aparecem
   na busca erram o desconto do segundo dígito.
7. **Validade não é confiança, no caminho de OCR.** O módulo 11 aceita ~1 em 11 sequências por
   acaso; a janela deslizante já produziu um número válido inexistente no documento. Só vira
   resposta quem é válido **e** tem evidência de posição.
8. **Rótulo curto de campo precisa de fronteira de palavra.** `"RG"` com `Contains` casa
   dentro de `"ORG. EMISSOR"`, impresso na própria CNH, e aí *todo* número da página vira
   "rotulado" — perdendo justamente a evidência que separa leitura de chute. O
   `PerfilDeLeitura` monta um regex com fronteira; há teste cobrindo esse caso.
9. **O tipo do OCR vem da rota, nunca é adivinhado.** CPF e CNH têm os mesmos 11 dígitos e
   uma CNH traz os dois impressos: sem o tipo pedido não há como escolher entre eles.
10. **`Ocr__PastaDeDiagnostico` grava a imagem do documento em disco.** É dado pessoal: fica
   vazio em produção, liga-se só para investigar leitura ruim.

## Divergências deliberadas com a implementação de referência

- O **título de eleitor** aqui recusa código de UF fora de 01–28; a referência não checa esse campo.
- O **RENAVAM** não recusa dígitos repetidos, porque a referência não faz essa checagem e
  divergir criaria falso negativo.
- **`rg-sp` é o único `Experimental`**: RG não tem padrão nacional, não há referência
  independente e vale só para São Paulo. Não estender para outras UFs sem fonte.

## O que falta

- **Inscrição Estadual** (27 algoritmos, um por UF) — não implementada.
- **OCR lê CNH, CPF e RG-SP.** Cada tipo novo é um `PerfilDeLeitura` (tamanho, rótulos do
  campo, validador) — a certidão de 32 dígitos ainda não tem perfil.
- **Nunca testado com documento real** (é PII). A medição usa amostra sintética degradada;
  sombra atravessando o documento é a degradação que o Tesseract não vence.
- **Autenticidade real** (Senatran, CRC Nacional, Receita) exige convênio.
- **Alvo nativo do app**: falta `eas.json`, `assets/icon.png` e o job de `jest-expo`.
