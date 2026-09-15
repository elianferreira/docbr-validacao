// Preset único do Expo: é ele que o Metro aplica em cada módulo (JSX, TS,
// react-native-web no target web) e que ensina o `expo-router` a resolver `app/`.
module.exports = function (api) {
  api.cache(true);
  return { presets: ["babel-preset-expo"] };
};
