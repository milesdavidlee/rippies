import {NativeEventEmitter, NativeModules} from 'react-native';

import type {RevealPayload, UnityRevealEvent} from '../reveal/contracts';

type UnityRevealNativeModule = {
  isAvailable?: boolean;
  prepareReveal(payloadJson: string): Promise<void>;
  beginReveal(): Promise<void>;
  skipReveal(): Promise<void>;
  pauseReveal(paused: boolean): Promise<void>;
  setMuted(muted: boolean): Promise<void>;
  disposeReveal(): Promise<void>;
};

const nativeModule = NativeModules.RippiesUnityReveal as
  | UnityRevealNativeModule
  | undefined;

function requireNativeModule(): UnityRevealNativeModule {
  if (!nativeModule) {
    throw new Error(
      'RippiesUnityReveal is unavailable. Add the native Unity host module before presenting a reveal.',
    );
  }

  return nativeModule;
}

export const UnityRevealBridge = {
  isAvailable() {
    return nativeModule?.isAvailable === true;
  },
  prepareReveal(payload: RevealPayload) {
    return requireNativeModule().prepareReveal(JSON.stringify(payload));
  },
  beginReveal() {
    return requireNativeModule().beginReveal();
  },
  skipReveal() {
    return requireNativeModule().skipReveal();
  },
  pauseReveal(paused: boolean) {
    return requireNativeModule().pauseReveal(paused);
  },
  setMuted(muted: boolean) {
    return requireNativeModule().setMuted(muted);
  },
  disposeReveal() {
    return requireNativeModule().disposeReveal();
  },
  addEventListener(listener: (event: UnityRevealEvent) => void) {
    const module = requireNativeModule();
    return new NativeEventEmitter(module as never).addListener(
      'RippiesUnityRevealEvent',
      listener,
    );
  },
};
