/* global jest */

jest.mock('@react-native-async-storage/async-storage', () => ({
  __esModule: true,
  default: (() => {
    const mockStorage = new Map();
    return {
      clear: jest.fn(async () => {
        mockStorage.clear();
      }),
      getAllKeys: jest.fn(async () => Array.from(mockStorage.keys())),
      getItem: jest.fn(async key => mockStorage.get(key) ?? null),
      removeItem: jest.fn(async key => {
        mockStorage.delete(key);
      }),
      setItem: jest.fn(async (key, value) => {
        mockStorage.set(key, value);
      }),
    };
  })(),
}));
