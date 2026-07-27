import type {RevealPayload} from '../reveal/contracts';

export type PackTheme = {
  accent: string;
  accentSoft: string;
  symbol: string;
};

export type InventoryPack = {
  inventoryId: string;
  packTypeId: string;
  name: string;
  series: string;
  edition: string;
  ownedAt: string;
  theme: PackTheme;
  reveal: RevealPayload;
};

const makeReveal = (
  inventoryId: string,
  packTypeId: string,
  card: RevealPayload['card'],
): RevealPayload => ({
  orderId: `ord_fake_${inventoryId}`,
  revealId: `rev_fake_${inventoryId}`,
  packTypeId,
  assetVersion: 'ios-fake-pass-1',
  card,
  receiptSignature: `fake-signed-receipt-${inventoryId}`,
});

export const fakeInventory: InventoryPack[] = [
  {
    inventoryId: 'pack_001',
    packTypeId: 'rippies_chrome',
    name: 'Chrome',
    series: 'First Edition',
    edition: '01 / 250',
    ownedAt: '2026-07-27T14:00:00.000Z',
    theme: {accent: '#68E4FF', accentSoft: '#173A48', symbol: 'C'},
    reveal: makeReveal('pack_001', 'rippies_chrome', {
      id: 'card_chrome_001',
      name: 'Chrome Warden',
      grade: 'PROTOTYPE 112',
      rarityTier: 'rare',
      archetype: 'Sentinel',
      accentHex: '#68E4FF',
      flavorText: 'Polished by pressure. Unmoved by noise.',
      attack: 67,
      defense: 92,
      speed: 60,
      luck: 73,
      frontImageUrl: '',
      backImageUrl: '',
    }),
  },
  {
    inventoryId: 'pack_002',
    packTypeId: 'rippies_solar',
    name: 'Solar',
    series: 'First Edition',
    edition: '18 / 250',
    ownedAt: '2026-07-26T19:20:00.000Z',
    theme: {accent: '#FFB84D', accentSoft: '#4A2D12', symbol: 'S'},
    reveal: makeReveal('pack_002', 'rippies_solar', {
      id: 'card_solar_018',
      name: 'Solar Drifter',
      grade: 'PROTOTYPE 089',
      rarityTier: 'ultra',
      archetype: 'Vanguard',
      accentHex: '#FFB84D',
      flavorText: 'Every horizon is an invitation.',
      attack: 88,
      defense: 61,
      speed: 94,
      luck: 78,
      frontImageUrl: '',
      backImageUrl: '',
    }),
  },
  {
    inventoryId: 'pack_003',
    packTypeId: 'rippies_prism',
    name: 'Prism',
    series: 'First Edition',
    edition: '42 / 250',
    ownedAt: '2026-07-25T17:10:00.000Z',
    theme: {accent: '#B96CFF', accentSoft: '#32194D', symbol: 'P'},
    reveal: makeReveal('pack_003', 'rippies_prism', {
      id: 'card_prism_042',
      name: 'Prism Titan',
      grade: 'PROTOTYPE 112',
      rarityTier: 'grail',
      archetype: 'Wildcard',
      accentHex: '#B96CFF',
      flavorText: 'Nothing stays sealed forever.',
      attack: 91,
      defense: 88,
      speed: 76,
      luck: 97,
      frontImageUrl: '',
      backImageUrl: '',
    }),
  },
  {
    inventoryId: 'pack_004',
    packTypeId: 'rippies_ember',
    name: 'Ember',
    series: 'First Edition',
    edition: '77 / 250',
    ownedAt: '2026-07-24T13:45:00.000Z',
    theme: {accent: '#FF6767', accentSoft: '#48191C', symbol: 'E'},
    reveal: makeReveal('pack_004', 'rippies_ember', {
      id: 'card_ember_077',
      name: 'Ember Fox',
      grade: 'PROTOTYPE 077',
      rarityTier: 'rare',
      archetype: 'Runner',
      accentHex: '#FF6767',
      flavorText: 'A spark only needs one opening.',
      attack: 82,
      defense: 53,
      speed: 96,
      luck: 81,
      frontImageUrl: '',
      backImageUrl: '',
    }),
  },
  {
    inventoryId: 'pack_005',
    packTypeId: 'rippies_tide',
    name: 'Tide',
    series: 'First Edition',
    edition: '103 / 250',
    ownedAt: '2026-07-23T11:15:00.000Z',
    theme: {accent: '#4F8CFF', accentSoft: '#142A52', symbol: 'T'},
    reveal: makeReveal('pack_005', 'rippies_tide', {
      id: 'card_tide_103',
      name: 'Tide Oracle',
      grade: 'PROTOTYPE 103',
      rarityTier: 'rare',
      archetype: 'Oracle',
      accentHex: '#4F8CFF',
      flavorText: 'The current remembers every turn.',
      attack: 58,
      defense: 79,
      speed: 68,
      luck: 93,
      frontImageUrl: '',
      backImageUrl: '',
    }),
  },
  {
    inventoryId: 'pack_006',
    packTypeId: 'rippies_verdant',
    name: 'Verdant',
    series: 'First Edition',
    edition: '144 / 250',
    ownedAt: '2026-07-22T09:05:00.000Z',
    theme: {accent: '#67E59A', accentSoft: '#153C2A', symbol: 'V'},
    reveal: makeReveal('pack_006', 'rippies_verdant', {
      id: 'card_verdant_144',
      name: 'Verdant Giant',
      grade: 'PROTOTYPE 144',
      rarityTier: 'ultra',
      archetype: 'Guardian',
      accentHex: '#67E59A',
      flavorText: 'Roots below. Reach beyond.',
      attack: 73,
      defense: 96,
      speed: 44,
      luck: 72,
      frontImageUrl: '',
      backImageUrl: '',
    }),
  },
];
