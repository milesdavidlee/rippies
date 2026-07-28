export type CardPayload = {
  id: string;
  name: string;
  grade: string;
  rarityTier: string;
  archetype: string;
  accentHex: string;
  flavorText: string;
  attack: number;
  defense: number;
  speed: number;
  luck: number;
  frontImageUrl: string;
  backImageUrl: string;
};

export type RevealExperienceId = 'silver_packet' | 'animated_loot_pack';

export type RevealPayload = {
  orderId: string;
  revealId: string;
  packTypeId: string;
  assetVersion: string;
  presentationMode?: 'reveal' | 'inspection';
  inspectionCardId?: string;
  revealExperienceId?: RevealExperienceId;
  cards: CardPayload[];
  /** Primary hit retained for backward-compatible receipt recovery. */
  card: CardPayload;
  receiptSignature: string;
};

export type RevealReceipt = {
  payload: RevealPayload;
  preparedAt: string;
  presentationState: 'prepared' | 'started' | 'cardVisible' | 'complete';
};

export type UnityRevealEventName =
  | 'sceneReady'
  | 'tearStarted'
  | 'cardVisible'
  | 'revealComplete'
  | 'collectionRequested';

export type UnityRevealEvent = {
  eventName: UnityRevealEventName;
  value: string;
};
