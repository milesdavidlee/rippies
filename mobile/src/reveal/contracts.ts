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

export type RevealPayload = {
  orderId: string;
  revealId: string;
  packTypeId: string;
  assetVersion: string;
  card: CardPayload;
  receiptSignature: string;
};

export type UnityRevealEventName =
  | 'sceneReady'
  | 'tearStarted'
  | 'cardVisible'
  | 'revealComplete';

export type UnityRevealEvent = {
  eventName: UnityRevealEventName;
  value: string;
};
