import { Link, usePathname } from 'expo-router'
import { Pressable, ScrollView, StyleSheet, Text, View, useWindowDimensions } from 'react-native'
import { hover } from '@/tema/hover'
import { useTema } from '@/tema/TemaContext'
import { espacos, raios } from '@/tema/tokens'

export interface ItemNav {
  para: string
  rotulo: string
}

interface Props {
  nomeApp: string
  itensNav: ItemNav[]
  titulo: string
  subtitulo?: string
  /** Nome exibido no header. Omitido em app sem sessao. */
  nomeUsuario?: string
  /** Acao de sair. Omitida em app sem sessao — e o caso deste servico, que e publico. */
  onSair?: () => void
  children: React.ReactNode
}

/** Breakpoint único do app. Nunca constante de largura espalhada por tela. */
const LARGURA_DESKTOP = 768

/**
 * Casca mínima: menu + header + área de conteúdo. Deliberadamente simples — quando
 * o `@univali/ui` v1.0 em RN existir (Fase 3 do `PLANO_EXPO.md`), sidebar animada,
 * skeleton e painel de detalhe vêm dele; aqui não se reimplementa o pacote.
 */
export function AppLayout({
  nomeApp,
  itensNav,
  titulo,
  subtitulo,
  nomeUsuario,
  onSair,
  children,
}: Props) {
  const { cores, tema, alternar } = useTema()
  const { width } = useWindowDimensions()
  const desktop = width >= LARGURA_DESKTOP
  const caminho = usePathname()

  return (
    <View style={[estilos.raiz, { backgroundColor: cores.pagina }, desktop && estilos.raizDesktop]}>
      <View
        style={[
          estilos.menu,
          { backgroundColor: cores.marca },
          desktop ? estilos.menuDesktop : estilos.menuMobile,
        ]}
      >
        <Text style={[estilos.nomeApp, { color: cores.marcaTexto }]}>{nomeApp}</Text>
        <View style={desktop ? estilos.navDesktop : estilos.navMobile}>
          {itensNav.map((item) => {
            const ativo = caminho === item.para || caminho.startsWith(`${item.para}/`)
            return (
              // Sem `asChild`: com ele o `<a href>` do react-native-web fica com a
              // navegação NATIVA do browser — o clique recarrega o documento (medido).
              <Link
                key={item.para}
                href={item.para}
                {...(ativo ? {} : hover.menu)}
                style={StyleSheet.flatten([
                  estilos.item,
                  ativo && { backgroundColor: cores.superficie },
                ])}
              >
                <Text
                  style={[estilos.itemTexto, { color: ativo ? cores.marca : cores.marcaTexto }]}
                >
                  {item.rotulo}
                </Text>
              </Link>
            )
          })}
        </View>
      </View>

      <View style={estilos.coluna}>
        <View style={[estilos.header, { backgroundColor: cores.superficie, borderColor: cores.borda }]}>
          <View style={estilos.headerTextos}>
            <Text style={[estilos.titulo, { color: cores.texto }]}>{titulo}</Text>
            {subtitulo ? (
              <Text style={[estilos.subtitulo, { color: cores.textoFraco }]}>{subtitulo}</Text>
            ) : null}
          </View>
          <View style={estilos.headerAcoes}>
            <Pressable
              onPress={alternar}
              accessibilityRole="button"
              accessibilityLabel={tema === 'claro' ? 'Ativar tema escuro' : 'Ativar tema claro'}
              style={[estilos.acao, { borderColor: cores.borda }]}
            >
              <Text style={{ color: cores.texto }}>{tema === 'claro' ? 'Escuro' : 'Claro'}</Text>
            </Pressable>
            {nomeUsuario ? (
              <Text style={[estilos.usuario, { color: cores.textoFraco }]}>{nomeUsuario}</Text>
            ) : null}
            {onSair ? (
              <Pressable
                onPress={onSair}
                accessibilityRole="button"
                style={[estilos.acao, { borderColor: cores.borda }]}
              >
                <Text style={{ color: cores.texto }}>Sair</Text>
              </Pressable>
            ) : null}
          </View>
        </View>

        <ScrollView style={estilos.conteudo} contentContainerStyle={estilos.conteudoInterno}>
          {children}
        </ScrollView>
      </View>
    </View>
  )
}

const estilos = StyleSheet.create({
  raiz: { flex: 1 },
  raizDesktop: { flexDirection: 'row' },
  menu: { padding: espacos.md, gap: espacos.md },
  menuDesktop: { width: 240 },
  menuMobile: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  nomeApp: { fontSize: 16, fontWeight: '700' },
  navDesktop: { gap: espacos.xs },
  navMobile: { flexDirection: 'row', gap: espacos.xs },
  item: { borderRadius: raios.md, paddingHorizontal: espacos.md, paddingVertical: espacos.sm },
  itemTexto: { fontSize: 14, fontWeight: '600' },
  coluna: { flex: 1 },
  header: {
    borderBottomWidth: StyleSheet.hairlineWidth,
    padding: espacos.md,
    gap: espacos.sm,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    flexWrap: 'wrap',
  },
  headerTextos: { gap: 2, flexShrink: 1 },
  headerAcoes: { flexDirection: 'row', alignItems: 'center', gap: espacos.sm },
  titulo: { fontSize: 18, fontWeight: '700' },
  subtitulo: { fontSize: 13 },
  usuario: { fontSize: 13 },
  acao: {
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: raios.md,
    paddingHorizontal: espacos.sm,
    paddingVertical: espacos.xs,
  },
  conteudo: { flex: 1 },
  conteudoInterno: { padding: espacos.md, gap: espacos.md },
})
