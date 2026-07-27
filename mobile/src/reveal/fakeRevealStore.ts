import AsyncStorage from '@react-native-async-storage/async-storage';

import type {InventoryPack} from '../data/fakeInventory';
import type {RevealReceipt} from './contracts';

const receiptKey = (revealId: string) => `rippies:receipt:${revealId}`;
const openedPacksKey = 'rippies:opened-pack-ids';

export async function prepareFakeReveal(
  pack: InventoryPack,
): Promise<RevealReceipt> {
  const key = receiptKey(pack.reveal.revealId);
  const stored = await AsyncStorage.getItem(key);

  if (stored) {
    return JSON.parse(stored) as RevealReceipt;
  }

  const receipt: RevealReceipt = {
    payload: pack.reveal,
    preparedAt: new Date().toISOString(),
    presentationState: 'prepared',
  };
  await AsyncStorage.setItem(key, JSON.stringify(receipt));
  return receipt;
}

export async function updatePresentationState(
  receipt: RevealReceipt,
  presentationState: RevealReceipt['presentationState'],
): Promise<RevealReceipt> {
  const next = {...receipt, presentationState};
  await AsyncStorage.setItem(
    receiptKey(receipt.payload.revealId),
    JSON.stringify(next),
  );
  return next;
}

export async function loadOpenedPackIds(): Promise<string[]> {
  const stored = await AsyncStorage.getItem(openedPacksKey);
  return stored ? (JSON.parse(stored) as string[]) : [];
}

export async function markPackOpened(inventoryId: string): Promise<string[]> {
  const ids = await loadOpenedPackIds();
  const next = ids.includes(inventoryId) ? ids : [...ids, inventoryId];
  await AsyncStorage.setItem(openedPacksKey, JSON.stringify(next));
  return next;
}

export async function resetFakeCollection(): Promise<void> {
  const keys = await AsyncStorage.getAllKeys();
  const rippiesKeys = keys.filter(key => key.startsWith('rippies:'));
  await Promise.all(rippiesKeys.map(key => AsyncStorage.removeItem(key)));
}
