// Metro roda sob Bun (medido: a imagem de build deste template instala só Bun).
//
// Duas coisas moram aqui, e as duas custaram medição em app real:
//
// 1. **Proxy de `/api` no dev server web.** O CORS do serviço .NET libera portas de
//    dev conhecidas, e o Expo web sobe em 8081; com o proxy, o bundle chama caminho
//    relativo (mesma origem) e CORS nem entra na conta — igual ao que o nginx da
//    imagem faz em produção. No nativo não há CORS: lá vale `EXPO_PUBLIC_API_URL`.
// 2. **`watchFolders`** (comentado): só é necessário quando o app consumir
//    `@univali/*` como **source do workspace**. Consumindo do Package Registry,
//    nada a fazer. Ver `FRONTEND_STACK.md` §06 e §08 antes de ligar isso.
const { getDefaultConfig } = require("expo/metro-config");

const config = getDefaultConfig(__dirname);

const ALVO_API = process.env.EXPO_API_TARGET || "http://localhost:5000";

config.server = {
  ...config.server,
  enhanceMiddleware: (middleware) => {
    const http = require("node:http");
    const { URL } = require("node:url");

    return (req, res, next) => {
      if (!req.url?.startsWith("/api")) {
        return middleware(req, res, next);
      }

      const alvo = new URL(req.url, ALVO_API);
      const requisicao = http.request(
        {
          hostname: alvo.hostname,
          port: alvo.port,
          path: alvo.pathname + alvo.search,
          method: req.method,
          headers: { ...req.headers, host: alvo.host },
        },
        (respostaApi) => {
          res.writeHead(respostaApi.statusCode || 502, respostaApi.headers);
          respostaApi.pipe(res);
        },
      );

      requisicao.on("error", (erro) => {
        res.writeHead(502, { "content-type": "application/json" });
        res.end(JSON.stringify({ message: `Proxy não alcançou ${ALVO_API}: ${erro.message}` }));
      });

      req.pipe(requisicao);
      return undefined;
    };
  },
};

module.exports = config;
