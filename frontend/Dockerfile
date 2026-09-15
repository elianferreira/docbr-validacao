# Padrão Univali: OracleLinux 10 nos dois estágios.
#
# Build **só com Bun** — não instala Node. Metro/Expo CLI rodam sob Bun (medido: é
# assim que o `radweb` builda o que está em produção). Runtime é nginx servindo o
# estático do `expo export`; não há servidor de aplicação.
#
# O bundle servido aqui é o MESMO código que roda em Android/iOS: o Metro passa o app
# por react-native-web e cospe HTML+JS+CSS.

# ---------- build ----------
FROM oraclelinux:10 AS build
WORKDIR /app

RUN dnf install -y --setopt=install_weak_deps=False \
        tar gzip unzip curl ca-certificates \
    && dnf clean all
ENV BUN_INSTALL=/usr/local
RUN curl -fsSL https://bun.sh/install | bash

# Cache de dependências.
COPY package.json bun.lock* bunfig.toml ./
RUN bun install --frozen-lockfile

# `app.json` e `babel.config.js` fazem parte da entrada do bundler: o primeiro é onde
# o Expo lê nome, `web.output` e os plugins; o segundo é o preset aplicado a cada módulo.
COPY app.json babel.config.js metro.config.js tsconfig.json ./
COPY app ./app
COPY src ./src
# `public/` vai verbatim para a raiz do export (favicon institucional).
COPY public ./public

# Sem .env de propósito (o .dockerignore o exclui): EXPO_PUBLIC_* é inlinada em build
# time, e vazia é o que a imagem quer — no web o app chama /api relativo e o nginx
# deste container proxia. URL absoluta aqui furaria o proxy e cairia no CORS.
RUN bun run build:web

# ---------- runtime ----------
FROM oraclelinux:10 AS runtime
ENV TZ=America/Sao_Paulo LANG=C.UTF-8

RUN dnf install -y --setopt=install_weak_deps=False nginx \
    && dnf clean all \
    && rm -f /etc/nginx/conf.d/*.conf

COPY nginx.conf /etc/nginx/nginx.conf
COPY --from=build /app/dist /usr/share/nginx/html

# Não-root com UID NUMÉRICO: com nome, o kubelet não verifica o UID e recusa o pod
# com `runAsNonRoot: true`. O master do nginx precisa escrever pid/cache/log.
RUN chown -R 10001:0 /var/lib/nginx /var/log/nginx /usr/share/nginx/html \
    && chmod -R g+w /var/lib/nginx /var/log/nginx
USER 10001

# :8080 porque porta < 1024 exige root. O Service mapeia 80 -> 8080.
EXPOSE 8080
CMD ["nginx", "-g", "daemon off;"]
