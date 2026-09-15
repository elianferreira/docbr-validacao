/**
 * `useWindowDimensions` do react-native-web lê `document.documentElement.clientWidth`,
 * que no jsdom é **0** — sem isto, **todo** teste roda como mobile e o ramo desktop
 * nunca é exercitado (medido: menu retrátil passava verde sem nunca ter renderizado).
 */
export function comLargura(px: number): void {
  Object.defineProperty(document.documentElement, 'clientWidth', {
    configurable: true,
    value: px,
  })
  Object.defineProperty(document.documentElement, 'clientHeight', {
    configurable: true,
    value: 900,
  })
  window.dispatchEvent(new Event('resize'))
}

export const LARGURA_DESKTOP = 1280
export const LARGURA_MOBILE = 390
