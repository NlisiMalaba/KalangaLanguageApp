const { getDefaultConfig } = require('expo/metro-config');

/** @type {import('expo/metro-config').MetroConfig} */
const config = getDefaultConfig(__dirname);

// Colocated Jest files under app/ are picked up by Expo Router's require.context.
// They must not enter the native bundle (@testing-library/react-native imports Node "console").
const testModules = /\.(test|spec)\.[cm]?[jt]sx?$/;
const existing = config.resolver.blockList;
config.resolver.blockList = existing ? [existing, testModules].flat() : testModules;

module.exports = config;
