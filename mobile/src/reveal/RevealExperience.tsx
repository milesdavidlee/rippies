import React, {useEffect, useMemo, useRef, useState} from 'react';
import {
  Animated,
  Easing,
  Modal,
  PanResponder,
  Pressable,
  StyleSheet,
  Text,
  View,
  useWindowDimensions,
} from 'react-native';
import {SafeAreaView} from 'react-native-safe-area-context';

import {PackArtwork} from '../components/PackArtwork';
import type {InventoryPack} from '../data/fakeInventory';
import {tokens} from '../design/tokens';
import {UnityRevealBridge} from '../bridge/UnityRevealBridge';
import type {RevealExperienceId, RevealReceipt} from './contracts';
import {
  prepareFakeReveal,
  updatePresentationState,
} from './fakeRevealStore';

type Stage = 'preparing' | 'ready' | 'revealing' | 'complete' | 'error';

type Props = {
  pack: InventoryPack | null;
  inspectionCardId?: string | null;
  revealExperience: RevealExperienceId;
  onCancel: () => void;
  onComplete: (pack: InventoryPack) => void;
};

export function RevealExperience({
  pack,
  inspectionCardId,
  revealExperience,
  onCancel,
  onComplete,
}: Props) {
  const {height, width} = useWindowDimensions();
  const [stage, setStage] = useState<Stage>('preparing');
  const [receipt, setReceipt] = useState<RevealReceipt | null>(null);
  const [nativeMode, setNativeMode] = useState(false);
  const entry = useRef(new Animated.Value(0)).current;
  const tearProgress = useRef(new Animated.Value(0)).current;
  const revealProgress = useRef(new Animated.Value(0)).current;
  const gestureCue = useRef(new Animated.Value(0)).current;
  const receiptRef = useRef<RevealReceipt | null>(null);
  const usesVerticalGesture = revealExperience === 'silver_packet';

  useEffect(() => {
    receiptRef.current = receipt;
  }, [receipt]);

  useEffect(() => {
    if (!pack) {
      return;
    }

    let active = true;
    let eventSubscription: {remove(): void} | undefined;
    const nativeAvailable = UnityRevealBridge.isAvailable();
    setNativeMode(nativeAvailable);
    setStage('preparing');
    tearProgress.setValue(0);
    revealProgress.setValue(0);

    Animated.timing(entry, {
      duration: tokens.motion.hero,
      easing: Easing.out(Easing.cubic),
      toValue: 1,
      useNativeDriver: true,
    }).start();

    const prepare = async () => {
      try {
        const nextReceipt = await prepareFakeReveal(pack);
        if (!active) {
          return;
        }
        setReceipt(nextReceipt);

        if (nativeAvailable) {
          eventSubscription = UnityRevealBridge.addEventListener(async event => {
            if (!active) {
              return;
            }
            const currentReceipt = receiptRef.current ?? nextReceipt;
            if (event.eventName === 'sceneReady') {
              if (inspectionCardId) {
                setStage('revealing');
                try {
                  await UnityRevealBridge.skipReveal();
                } catch {
                  if (active) {
                    setStage('error');
                  }
                }
              } else {
                setStage('ready');
              }
            } else if (event.eventName === 'tearStarted') {
              const updated = await updatePresentationState(
                currentReceipt,
                'started',
              );
              setReceipt(updated);
            } else if (event.eventName === 'cardVisible') {
              const updated = await updatePresentationState(
                currentReceipt,
                'cardVisible',
              );
              setReceipt(updated);
            } else if (event.eventName === 'revealComplete') {
              const updated = await updatePresentationState(
                currentReceipt,
                'complete',
              );
              setReceipt(updated);
              setStage('complete');
            } else if (event.eventName === 'collectionRequested') {
              if (inspectionCardId) {
                onCancel();
              } else {
                onComplete(pack);
              }
            }
          });
          await UnityRevealBridge.prepareReveal(
            {
              ...nextReceipt.payload,
              revealExperienceId: revealExperience,
              ...(inspectionCardId
                ? {
                  inspectionCardId,
                  presentationMode: 'inspection',
                  }
                : {}),
            },
          );
        } else {
          setTimeout(() => {
            if (active) {
              if (inspectionCardId) {
                revealProgress.setValue(1);
              }
              setStage(
                inspectionCardId ||
                  nextReceipt.presentationState === 'complete'
                  ? 'complete'
                  : 'ready',
              );
            }
          }, 720);
        }
      } catch {
        if (active) {
          setStage('error');
        }
      }
    };

    prepare();
    return () => {
      active = false;
      eventSubscription?.remove();
      if (nativeAvailable) {
        UnityRevealBridge.disposeReveal().catch(() => {
          // Unity may already be unloaded during app teardown.
        });
      }
    };
  }, [
    entry,
    inspectionCardId,
    onCancel,
    onComplete,
    pack,
    revealExperience,
    revealProgress,
    tearProgress,
  ]);

  useEffect(() => {
    if (stage !== 'ready') {
      gestureCue.stopAnimation();
      gestureCue.setValue(0);
      return;
    }

    const loop = Animated.loop(
      Animated.timing(gestureCue, {
        duration: 2200,
        easing: Easing.inOut(Easing.cubic),
        toValue: 1,
        useNativeDriver: true,
      }),
    );
    loop.start();
    return () => loop.stop();
  }, [gestureCue, stage]);

  const commitFakeReveal = async () => {
    if (!receipt || !pack || stage !== 'ready') {
      return;
    }

    setStage('revealing');
    const started = await updatePresentationState(receipt, 'started');
    setReceipt(started);
    Animated.parallel([
      Animated.timing(tearProgress, {
        duration: 180,
        toValue: 1,
        useNativeDriver: false,
      }),
      Animated.timing(revealProgress, {
        delay: 140,
        duration: tokens.motion.reveal,
        easing: Easing.out(Easing.cubic),
        toValue: 1,
        useNativeDriver: true,
      }),
    ]).start(async () => {
      const completed = await updatePresentationState(started, 'complete');
      setReceipt(completed);
      setStage('complete');
    });
  };

  const panResponder = useMemo(
    () =>
      PanResponder.create({
        onMoveShouldSetPanResponder: (_, gesture) => {
          const primary = usesVerticalGesture ? gesture.dy : gesture.dx;
          const secondary = usesVerticalGesture ? gesture.dx : gesture.dy;
          return (
            stage === 'ready' &&
            primary > 4 &&
            Math.abs(primary) > Math.abs(secondary)
          );
        },
        onPanResponderMove: (_, gesture) => {
          const distance = usesVerticalGesture ? gesture.dy : gesture.dx;
          const requiredDistance = usesVerticalGesture ? 260 : 240;
          const progress = Math.max(
            0,
            Math.min(1, distance / requiredDistance),
          );
          tearProgress.setValue(progress);
        },
        onPanResponderRelease: (_, gesture) => {
          const distance = usesVerticalGesture ? gesture.dy : gesture.dx;
          const requiredDistance = usesVerticalGesture ? 260 : 240;
          if (distance / requiredDistance >= 0.76) {
            commitFakeReveal();
          } else {
            Animated.spring(tearProgress, {
              friction: 8,
              tension: 90,
              toValue: 0,
              useNativeDriver: false,
            }).start();
          }
        },
      }),
    // commitFakeReveal intentionally reads the current receipt and stage.
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [stage, receipt, usesVerticalGesture],
  );

  if (!pack) {
    return null;
  }

  const inspectionCard = inspectionCardId
    ? pack.reveal.cards.find(card => card.id === inspectionCardId)
    : undefined;
  const displayedCard = inspectionCard ?? pack.reveal.card;
  const inspectionMode = Boolean(inspectionCard);
  const heroPackWidth = Math.min(tokens.pack.heroWidth, width * 0.57);
  const packTranslateY = revealProgress.interpolate({
    inputRange: [0, 1],
    outputRange: [0, height * 0.52],
  });
  const packOpacity = revealProgress.interpolate({
    inputRange: [0, 0.62, 1],
    outputRange: [1, 1, 0],
  });
  const cardTranslateY = revealProgress.interpolate({
    inputRange: [0, 1],
    outputRange: [160, 0],
  });
  const cardScale = revealProgress.interpolate({
    inputRange: [0, 1],
    outputRange: [0.72, 1],
  });
  const ready = stage === 'ready';
  const complete = stage === 'complete';
  const revealedCardCount = pack.reveal.cards?.length ?? 1;

  return (
    <Modal
      animationType="none"
      onRequestClose={onCancel}
      presentationStyle="fullScreen"
      visible={Boolean(pack)}>
      <SafeAreaView style={styles.safeArea}>
        <Animated.View
          style={[
            styles.scene,
            {
              opacity: entry,
              transform: [
                {
                  scale: entry.interpolate({
                    inputRange: [0, 1],
                    outputRange: [1.04, 1],
                  }),
                },
              ],
            },
          ]}>
          <View
            style={[
              styles.ambientGlow,
              {backgroundColor: pack.theme.accent},
            ]}
          />
          <View style={styles.topBar}>
            <Pressable
              accessibilityLabel="Close reveal"
              accessibilityRole="button"
              disabled={stage === 'revealing'}
              onPress={onCancel}
              style={styles.iconButton}>
              <Text style={styles.iconButtonText}>×</Text>
            </Pressable>
          </View>

          {complete ? (
            <View style={styles.completionHeader}>
              <Text style={styles.completionTitle}>
                {inspectionMode
                  ? displayedCard.name
                  : `Added ${revealedCardCount} cards to your collection`}
              </Text>
              <Text style={styles.completionDetail}>
                {inspectionMode
                  ? 'Drag horizontally to inspect every angle.'
                  : 'Tap a card to inspect it. Tap again to return.'}
              </Text>
            </View>
          ) : null}

          <View style={styles.stage}>
            {ready ? (
              <View pointerEvents="none" style={styles.gestureCueFrame}>
                <Animated.View
                  style={[
                    styles.gestureCue,
                    usesVerticalGesture
                      ? styles.gestureCueHorizontal
                      : styles.gestureCueVertical,
                    {
                      backgroundColor: pack.theme.accent,
                      opacity: gestureCue.interpolate({
                        inputRange: [0, 0.15, 0.85, 1],
                        outputRange: [0, 0.12, 0.12, 0],
                      }),
                      transform: [
                        usesVerticalGesture
                          ? {
                              translateY: gestureCue.interpolate({
                                inputRange: [0, 1],
                                outputRange: [-210, 210],
                              }),
                            }
                          : {
                              translateX: gestureCue.interpolate({
                                inputRange: [0, 1],
                                outputRange: [-170, 170],
                              }),
                            },
                      ],
                    },
                  ]}
                />
              </View>
            ) : null}
            <Animated.View
              style={[
                styles.packWrap,
                {
                  opacity: packOpacity,
                  transform: [
                    {
                      translateY: entry.interpolate({
                        inputRange: [0, 1],
                        outputRange: [80, 0],
                      }),
                    },
                    {translateY: packTranslateY},
                    {
                      scale: entry.interpolate({
                        inputRange: [0, 1],
                        outputRange: [0.72, 1],
                      }),
                    },
                  ],
                },
              ]}>
              <PackArtwork pack={pack} width={heroPackWidth} />
              <Animated.View
                style={[
                  styles.tearLine,
                  {
                    backgroundColor: pack.theme.accent,
                    width: tearProgress.interpolate({
                      inputRange: [0, 1],
                      outputRange: ['0%', '100%'],
                    }),
                  },
                ]}
              />
            </Animated.View>

            <Animated.View
              pointerEvents={complete ? 'auto' : 'none'}
              style={[
                styles.cardWrap,
                {
                  opacity: revealProgress,
                  transform: [
                    {translateY: cardTranslateY},
                    {scale: cardScale},
                  ],
                },
              ]}>
              <View
                style={[
                  styles.card,
                  {
                    borderColor: pack.theme.accent,
                    shadowColor: pack.theme.accent,
                  },
                ]}>
                <View
                  style={[
                    styles.cardArt,
                    {backgroundColor: pack.theme.accentSoft},
                  ]}>
                  <View
                    style={[
                      styles.cardOrb,
                      {borderColor: pack.theme.accent},
                    ]}
                  />
                  <Text style={[styles.cardGlyph, {color: pack.theme.accent}]}>
                    {pack.theme.symbol}
                  </Text>
                </View>
                <Text style={styles.cardGrade}>{displayedCard.grade}</Text>
                <Text style={styles.cardName}>{displayedCard.name}</Text>
                <Text
                  style={[styles.cardRarity, {color: pack.theme.accent}]}>
                  {displayedCard.rarityTier.toUpperCase()} ·{' '}
                  {displayedCard.archetype.toUpperCase()}
                </Text>
                <View style={styles.statsRow}>
                  {[
                    ['ATK', displayedCard.attack],
                    ['DEF', displayedCard.defense],
                    ['SPD', displayedCard.speed],
                    ['LCK', displayedCard.luck],
                  ].map(([label, value]) => (
                    <View key={label} style={styles.cardStat}>
                      <Text style={styles.cardStatValue}>{value}</Text>
                      <Text style={styles.cardStatLabel}>{label}</Text>
                    </View>
                  ))}
                </View>
              </View>
            </Animated.View>
          </View>

          <View style={styles.bottom}>
            {stage === 'preparing' ? (
              <>
                <Text style={styles.promptTitle}>
                  {inspectionMode ? 'Loading 3D inspection' : 'Securing your reveal'}
                </Text>
                <Text style={styles.promptDetail}>
                  {inspectionMode
                    ? 'Restoring the original card asset…'
                    : 'Restoring immutable receipt…'}
                </Text>
              </>
            ) : null}
            {ready ? (
              <Pressable
                accessibilityHint={
                  usesVerticalGesture
                    ? 'Swipe down from the top seam or double tap to reveal'
                    : 'Swipe right for the full gesture or double tap to reveal'
                }
                accessibilityLabel="Rip pack"
                accessibilityRole="button"
                onPress={
                  nativeMode
                    ? () => {
                        UnityRevealBridge.beginReveal().catch(() => {
                          // The visual swipe remains available if the bridge unloads.
                        });
                      }
                    : commitFakeReveal
                }
                {...(nativeMode ? {} : panResponder.panHandlers)}>
                <Text style={styles.promptTitle}>
                  Swipe to rip
                </Text>
                <Text style={styles.promptDetail}>
                  {usesVerticalGesture
                    ? 'Start at the top seam and pull down.'
                    : 'Drag across the top seam from left to right.'}
                </Text>
              </Pressable>
            ) : null}
            {stage === 'revealing' ? (
              <>
                <Text style={styles.promptTitle}>
                  {inspectionMode
                    ? 'Bringing your card forward'
                    : 'Your cards are emerging'}
                </Text>
                <Text style={styles.promptDetail}>
                  {inspectionMode
                    ? displayedCard.name
                    : 'Reveal committed securely.'}
                </Text>
              </>
            ) : null}
            {complete ? (
              <Pressable
                accessibilityRole="button"
                onPress={() =>
                  inspectionMode ? onCancel() : onComplete(pack)
                }
                style={({pressed}) => [
                  styles.doneButton,
                  {backgroundColor: pack.theme.accent},
                  pressed && styles.pressed,
                ]}>
                {inspectionMode ? (
                  <Text style={styles.doneButtonText}>Back to collection</Text>
                ) : (
                  <Text style={styles.doneButtonText}>View collection</Text>
                )}
              </Pressable>
            ) : null}
            {stage === 'error' ? (
              <>
                <Text style={styles.promptTitle}>Reveal paused safely</Text>
                <Text style={styles.promptDetail}>
                  Close and retry. Your five assigned cards will not change.
                </Text>
              </>
            ) : null}
          </View>
        </Animated.View>
      </SafeAreaView>
    </Modal>
  );
}

const styles = StyleSheet.create({
  safeArea: {
    backgroundColor: tokens.color.canvas,
    flex: 1,
  },
  scene: {
    flex: 1,
    overflow: 'hidden',
  },
  ambientGlow: {
    borderRadius: 999,
    height: 440,
    left: '50%',
    marginLeft: -220,
    opacity: 0.12,
    position: 'absolute',
    top: 85,
    width: 440,
  },
  topBar: {
    alignItems: 'center',
    flexDirection: 'row',
    justifyContent: 'flex-start',
    paddingHorizontal: 18,
    paddingTop: 8,
  },
  iconButton: {
    alignItems: 'center',
    backgroundColor: tokens.color.surface,
    borderColor: tokens.color.line,
    borderRadius: tokens.radius.pill,
    borderWidth: 1,
    height: 42,
    justifyContent: 'center',
    width: 42,
  },
  iconButtonText: {
    color: tokens.color.text,
    fontSize: 22,
    fontWeight: '500',
  },
  stage: {
    alignItems: 'center',
    flex: 1,
    justifyContent: 'center',
  },
  completionHeader: {
    alignItems: 'center',
    left: 28,
    position: 'absolute',
    right: 28,
    top: 84,
    zIndex: 2,
  },
  completionTitle: {
    color: tokens.color.text,
    fontSize: 24,
    fontWeight: '900',
    textAlign: 'center',
  },
  completionDetail: {
    color: tokens.color.textMuted,
    fontSize: 12,
    marginTop: 7,
    textAlign: 'center',
  },
  packWrap: {
    position: 'absolute',
  },
  tearLine: {
    borderRadius: 2,
    height: 3,
    left: 0,
    position: 'absolute',
    top: 27,
  },
  cardWrap: {
    position: 'absolute',
  },
  card: {
    backgroundColor: '#ECEEF3',
    borderRadius: 20,
    borderWidth: 2,
    padding: 12,
    shadowOffset: {width: 0, height: 0},
    shadowOpacity: 0.55,
    shadowRadius: 26,
    width: 260,
  },
  cardArt: {
    alignItems: 'center',
    borderRadius: 13,
    height: 250,
    justifyContent: 'center',
    overflow: 'hidden',
  },
  cardOrb: {
    borderRadius: 999,
    borderWidth: 1,
    height: 210,
    opacity: 0.45,
    position: 'absolute',
    transform: [{rotate: '-24deg'}],
    width: 150,
  },
  cardGlyph: {
    fontSize: 96,
    fontWeight: '200',
    opacity: 0.72,
  },
  cardGrade: {
    color: '#686D79',
    fontSize: 9,
    fontWeight: '900',
    letterSpacing: 1.2,
    marginTop: 13,
  },
  cardName: {
    color: '#10131A',
    fontSize: 22,
    fontWeight: '900',
    marginTop: 4,
  },
  cardRarity: {
    fontSize: 9,
    fontWeight: '900',
    letterSpacing: 0.8,
    marginTop: 4,
  },
  statsRow: {
    borderTopColor: '#D0D3DA',
    borderTopWidth: 1,
    flexDirection: 'row',
    marginTop: 12,
    paddingTop: 10,
  },
  cardStat: {
    alignItems: 'center',
    flex: 1,
  },
  cardStatValue: {
    color: '#10131A',
    fontSize: 14,
    fontWeight: '900',
  },
  cardStatLabel: {
    color: '#747A86',
    fontSize: 7,
    fontWeight: '900',
    letterSpacing: 0.7,
    marginTop: 2,
  },
  bottom: {
    minHeight: 176,
    paddingBottom: 20,
    paddingHorizontal: 24,
  },
  promptTitle: {
    color: tokens.color.text,
    fontSize: 22,
    fontWeight: '900',
    textAlign: 'center',
  },
  promptDetail: {
    color: tokens.color.textMuted,
    fontSize: 13,
    marginTop: 6,
    textAlign: 'center',
  },
  gestureCueFrame: {
    bottom: 28,
    left: 20,
    overflow: 'hidden',
    position: 'absolute',
    right: 20,
    top: 28,
  },
  gestureCue: {
    borderRadius: 999,
    position: 'absolute',
    shadowColor: '#FFFFFF',
    shadowOffset: {width: 0, height: 0},
    shadowOpacity: 0.24,
    shadowRadius: 24,
  },
  gestureCueHorizontal: {
    height: 22,
    left: 0,
    right: 0,
    top: '50%',
  },
  gestureCueVertical: {
    bottom: 0,
    left: '50%',
    top: 0,
    width: 22,
  },
  doneButton: {
    alignSelf: 'center',
    borderRadius: tokens.radius.pill,
    marginTop: 18,
    paddingHorizontal: 30,
    paddingVertical: 14,
  },
  doneButtonText: {
    color: '#071016',
    fontSize: 14,
    fontWeight: '900',
  },
  pressed: {
    opacity: 0.72,
    transform: [{scale: 0.98}],
  },
});
