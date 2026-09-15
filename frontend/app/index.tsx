import { Redirect } from 'expo-router'

// A raiz nao e tela: redireciona para a primeira tela real (§09.7).
export default function Index() {
  return <Redirect href="/validar" />
}
