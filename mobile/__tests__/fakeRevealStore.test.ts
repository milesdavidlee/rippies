import AsyncStorage from '@react-native-async-storage/async-storage';

import {fakeInventory} from '../src/data/fakeInventory';
import {
  loadOpenedPackIds,
  markPackOpened,
  prepareFakeReveal,
  resetFakeCollection,
  updatePresentationState,
} from '../src/reveal/fakeRevealStore';

beforeEach(async () => {
  await AsyncStorage.clear();
});

test('restores the same immutable reveal receipt', async () => {
  const pack = fakeInventory[2];
  const prepared = await prepareFakeReveal(pack);
  const completed = await updatePresentationState(prepared, 'complete');
  const restored = await prepareFakeReveal(pack);

  expect(restored).toEqual(completed);
  expect(restored.payload.card.id).toBe('card_prism_042');
});

test('tracks opened packs idempotently and resets the demo', async () => {
  await markPackOpened('pack_001');
  await markPackOpened('pack_001');
  await markPackOpened('pack_002');

  expect(await loadOpenedPackIds()).toEqual(['pack_001', 'pack_002']);

  await resetFakeCollection();
  expect(await loadOpenedPackIds()).toEqual([]);
});
