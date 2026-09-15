# k8s — docbr-validacao

Manifests versionados, padrão Univali. Tudo nos namespaces compartilhados
`producao` / `desenv` (NUNCA criar namespace dedicado).

| Ambiente | Recursos | Host |
| --- | --- | --- |
| `desenv/` | `docbr-validacaod` (Deployment/Service/Ingress) | `docbr-validacaod.univali.br` |
| `producao/` | `docbr-validacao` (Deployment/Service/Ingress) | `docbr-validacao.univali.br` |

Pré-requisitos no cluster (já existem para outros apps):

- ImagePullSecret `skdocker-regcred` em `producao` e `desenv`.
- TLS `wildcard-univali-tls` em `producao` e `desenv`.

O CI aplica estes manifests automaticamente na 1ª subida (self-bootstrap) e
depois só faz `set image` + `rollout status`. Aplicar à mão:

```bash
kubectl apply -f k8s/desenv/
kubectl apply -f k8s/producao/
```

> Convenção real de nome: apps Univali usam `skapp<NN>` / `skapp<NN>d`. Ajustar
> os nomes/host se este app receber um número skapp.
