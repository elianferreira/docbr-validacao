import js from '@eslint/js'
import globals from 'globals'
import tseslint from 'typescript-eslint'
import reactHooks from 'eslint-plugin-react-hooks'

/**
 * Config local, flat. Migra para `@univali/config/eslint-expo` quando a Fase 2 do
 * `PLANO_EXPO.md` publicar o preset Expo — até lá, o app não depende de pacote que
 * ainda fixa `globals.browser` e `lib: ["DOM"]`.
 *
 * `eslint-plugin-react-hooks` fica em **v5** de propósito: na v7 o idioma padrão do
 * RN `useRef(new Animated.Value(x)).current` passa a violar `react-hooks/refs`
 * (medido: 32 erros em 5 componentes animados). Ao subir, trocar por
 * `useState(() => new Animated.Value(x))`.
 */
export default tseslint.config(
  { ignores: ['dist/**', '.expo/**', 'src/**/__generated__/**', 'node_modules/**'] },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  {
    files: ['**/*.{ts,tsx}'],
    languageOptions: {
      globals: { ...globals.browser, ...globals.node },
    },
    plugins: { 'react-hooks': reactHooks },
    rules: {
      ...reactHooks.configs.recommended.rules,
      '@typescript-eslint/no-unused-vars': ['error', { argsIgnorePattern: '^_' }],
    },
  },
  {
    files: ['*.config.{js,mjs,mts}', 'metro.config.js', 'babel.config.js'],
    languageOptions: { globals: { ...globals.node } },
    rules: { '@typescript-eslint/no-require-imports': 'off' },
  },
)
