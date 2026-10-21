# DocBr — validação de documentos brasileiros

Valida a **estrutura** de nove documentos brasileiros, lê o número de CNH, CPF e RG a partir de
foto ou PDF, e expõe tudo como API, CLI e app web.

> Validação estrutural não prova que o documento existe, que é autêntico, nem que está
> regular no órgão emissor. Para isso é preciso convênio com Senatran, CRC Nacional ou
> Receita Federal.

## Documentos cobertos

`cpf` · `cnpj` (numérico e alfanumérico) · `cnh` · `certidao` (nascimento, casamento e
óbito — 32 dígitos do Provimento 63 do CNJ) · `titulo-eleitor` · `pis` · `cns` ·
`renavam` · `rg-sp` (**experimental**, só São Paulo).

## Como rodar

```powershell
# API em http://localhost:5199 (contrato OpenAPI em /openapi/v1.json, só em Development)
dotnet run --project Api.Host --no-launch-profile --urls http://localhost:5199

# app web em http://localhost:8081
cd frontend; bun install; bun run web

# CLI
dotnet run --project DocBr.Cli -- --tipos
dotnet run --project DocBr.Cli -- cpf --gerar 5
dotnet run --project DocBr.Cli -- --identificar 35440631331

# testes
dotnet test                                   # 104, backend inteiro
dotnet test Api.Host.Tests --filter asvs      # regras fixas de segurança
cd frontend; bun run test
```

## API

| Rota | O que faz |
| --- | --- |
| `GET /api/documentos` | Catálogo dos tipos suportados, em `PagedResult<T>`. |
| `POST /api/documentos/{slug}/numero` | Valida um número contra um tipo. |
| `GET /api/documentos/{slug}/exemplo` | Gera um número válido do tipo (massa de teste). |
| `POST /api/documentos/identificar` | Lista os tipos que aceitam o número informado. |
| `POST /api/documentos/{slug}/arquivo` | Lê o número de uma imagem ou PDF (multipart). Só `cnh`, `cpf` e `rg-sp`. |
| `GET /` | Raiz anônima — é onde as probes do k8s batem. |
| `GET /health` | Estado do motor de OCR. |

Todas as rotas são anônimas e **todas** têm rate limit por IP. As que carregam número de
documento respondem com `Cache-Control: no-store`.

## Padrão

Segue o padrão de engenharia da Univali (`C:\Projetos\univali-stack`): .NET 10 minimal
API modular no backend, Expo universal com Expo Router no frontend, Docker sobre
OracleLinux 10 e k8s nos namespaces `desenv`/`producao`.

Decisões, divergências deliberadas e armadilhas já pagas estão no [`AGENTS.md`](./AGENTS.md).
