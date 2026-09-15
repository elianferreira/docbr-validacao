/**
 * `<head>` da build web. Com `web.output: "single"` o Expo serve o próprio
 * `index.html` e, medido no dev server, **ignora** `app/+html.tsx`, `expo.web.name` e
 * `expo.web.favicon` — a única chave que entra no export é `expo.web.lang`.
 * O favicon vem de `public/favicon.ico`, que o Expo serve verbatim.
 *
 * > Divergência ainda aberta no `PLANO_EXPO.md`: o `radweb` carrega a fonte pelo
 * > `app/+html.jsx` e afirma que funciona no export. Antes de trocar de mecanismo,
 * > conferir no `dist` exportado. Enquanto isso, título e CSS global ficam aqui.
 */
if (typeof document !== 'undefined') {
  document.title = 'docbr-validacao'

  // `:hover` não existe em RN: o react-native-web só expõe `hovered` em callback de
  // estilo, e o `Link` do expo-router é texto, não Pressable. A saída é `dataSet`
  // (vira `data-*` no DOM) + uma regra CSS de verdade, injetada uma vez.
  const ID = 'univali-css-global'
  if (!document.getElementById(ID)) {
    const estilo = document.createElement('style')
    estilo.id = ID
    estilo.textContent = `
      [data-hover]:hover { transition: filter 150ms ease-out; }
      [data-hover='menu']:hover { filter: brightness(1.12); }
      [data-hover='linha']:hover { filter: brightness(0.98); }
    `
    document.head.appendChild(estilo)
  }
}
