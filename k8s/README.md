# Manifests k8s — docbr-validacao

Namespaces `desenv` e `producao` (compartilhados). **Nunca criar namespace dedicado:** o
service account do CI so tem RBAC nesses dois e qualquer outro volta `Forbidden`.

| Ambiente | Deployment | Imagem |
| --- | --- | --- |
| desenv | `docbrd-backend` | `skdocker.univali.br/sistemas/docbrd-backend` |
| producao | `docbr-backend` | `skdocker.univali.br/sistemas/docbr-backend` |

As probes batem em `/`, nunca em `/health`. O `/health` checa o motor de OCR; usar ele como
liveness derruba o pod quando a dependencia oscila, em vez de so reportar.

Este servico **nao tem Secret**: nao acessa Oracle nem GAM, e nao guarda nada. Toda a
configuracao esta em `env` no proprio deployment.

Bootstrap (o CI faz sozinho na primeira vez):

```bash
kubectl apply -f k8s/desenv/
kubectl apply -f k8s/producao/
```
