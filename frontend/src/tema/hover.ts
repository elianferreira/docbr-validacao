/**
 * `:hover` na web. RN não tem pseudo-classe, o `Pressable` do react-native-web só
 * expõe `hovered` em callback de estilo (obrigaria trocar todo estilo estático por
 * função) e o `Link` do expo-router é **texto**, não Pressable.
 *
 * Saída: o `dataSet` do react-native-web sai como atributo `data-hover` no DOM, e a
 * regra de `:hover` mora no CSS global (`documentoWeb.web.ts`). No nativo o `dataSet`
 * é ignorado — lá não existe cursor.
 *
 * **Espalhe, não passe como atributo** (`{...hover.menu}`): `dataSet` não existe nos
 * tipos do RN nem do expo-router, e o spread é o que o TS aceita.
 */
type PropsDeHover = { dataSet: { hover: string } }

const de = (nome: string): PropsDeHover => ({ dataSet: { hover: nome } })

export const hover = {
  /** Item do menu (fundo escuro): véu claro. */
  menu: de('menu'),
  /** Linha clicável de tabela/lista. */
  linha: de('linha'),
}
