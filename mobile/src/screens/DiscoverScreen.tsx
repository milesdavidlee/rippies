import React from 'react';
import {
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  View,
  useWindowDimensions,
} from 'react-native';

import {PackArtwork} from '../components/PackArtwork';
import {ScreenHeader} from '../components/ScreenHeader';
import type {InventoryPack} from '../data/fakeInventory';
import {tokens} from '../design/tokens';

type Props = {
  featuredPack: InventoryPack;
  onOpen: (pack: InventoryPack) => void;
  onViewCollection: () => void;
};

export function DiscoverScreen({
  featuredPack,
  onOpen,
  onViewCollection,
}: Props) {
  const {width} = useWindowDimensions();
  const packWidth = Math.min(220, width * 0.5);

  return (
    <ScrollView
      contentContainerStyle={styles.content}
      showsVerticalScrollIndicator={false}>
      <ScreenHeader
        eyebrow="TODAY'S DROP"
        title="Find your next pull."
        detail="Digital-first collectible cards with a reveal worth remembering."
        trailing={
          <View style={styles.avatar}>
            <Text style={styles.avatarText}>MD</Text>
          </View>
        }
      />

      <View style={styles.hero}>
        <View style={[styles.glow, {backgroundColor: featuredPack.theme.accent}]} />
        <PackArtwork pack={featuredPack} width={packWidth} />
        <View style={styles.heroCopy}>
          <Text style={styles.kicker}>FEATURED PACK</Text>
          <Text style={styles.heroTitle}>{featuredPack.name} Edition</Text>
          <Text style={styles.heroDetail}>
            One sealed pack from the original Rippies run. All five cards are
            assigned before the reveal begins.
          </Text>
          <Pressable
            accessibilityRole="button"
            onPress={() => onOpen(featuredPack)}
            style={({pressed}) => [
              styles.primaryButton,
              {backgroundColor: featuredPack.theme.accent},
              pressed && styles.pressed,
            ]}>
            <Text style={styles.primaryButtonText}>Open owned pack</Text>
          </Pressable>
        </View>
      </View>

      <View style={styles.sectionRow}>
        <View>
          <Text style={styles.sectionLabel}>YOUR VAULT</Text>
          <Text style={styles.sectionTitle}>6 packs waiting</Text>
        </View>
        <Pressable onPress={onViewCollection} style={styles.textButton}>
          <Text style={styles.textButtonLabel}>View all →</Text>
        </Pressable>
      </View>

      <View style={styles.infoCard}>
        <Text style={styles.infoIcon}>◎</Text>
        <View style={styles.infoCopy}>
          <Text style={styles.infoTitle}>Reveal receipts are permanent</Text>
          <Text style={styles.infoBody}>
            Close the app at any point and the same five assigned cards will be
            waiting when you return.
          </Text>
        </View>
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  content: {
    paddingBottom: 126,
    paddingHorizontal: 20,
    paddingTop: 18,
  },
  avatar: {
    alignItems: 'center',
    backgroundColor: tokens.color.surfaceRaised,
    borderColor: tokens.color.line,
    borderRadius: 18,
    borderWidth: 1,
    height: 42,
    justifyContent: 'center',
    width: 42,
  },
  avatarText: {
    color: tokens.color.text,
    fontSize: 12,
    fontWeight: '800',
  },
  hero: {
    alignItems: 'center',
    backgroundColor: tokens.color.surface,
    borderColor: tokens.color.line,
    borderRadius: tokens.radius.lg,
    borderWidth: 1,
    marginTop: 28,
    overflow: 'hidden',
    paddingHorizontal: 18,
    paddingTop: 28,
  },
  glow: {
    borderRadius: 999,
    height: 270,
    opacity: 0.12,
    position: 'absolute',
    top: 10,
    width: 270,
  },
  heroCopy: {
    alignItems: 'center',
    paddingBottom: 22,
    paddingTop: 24,
  },
  kicker: {
    color: tokens.color.textMuted,
    fontSize: 10,
    fontWeight: '900',
    letterSpacing: 1.6,
  },
  heroTitle: {
    color: tokens.color.text,
    fontSize: 25,
    fontWeight: '900',
    marginTop: 7,
  },
  heroDetail: {
    color: tokens.color.textMuted,
    fontSize: 14,
    lineHeight: 20,
    marginTop: 8,
    maxWidth: 310,
    textAlign: 'center',
  },
  primaryButton: {
    borderRadius: tokens.radius.pill,
    marginTop: 20,
    paddingHorizontal: 24,
    paddingVertical: 14,
  },
  primaryButtonText: {
    color: '#061016',
    fontSize: 14,
    fontWeight: '900',
  },
  pressed: {
    opacity: 0.72,
    transform: [{scale: 0.98}],
  },
  sectionRow: {
    alignItems: 'flex-end',
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginTop: 30,
  },
  sectionLabel: {
    color: tokens.color.cyan,
    fontSize: 10,
    fontWeight: '900',
    letterSpacing: 1.5,
  },
  sectionTitle: {
    color: tokens.color.text,
    fontSize: 20,
    fontWeight: '800',
    marginTop: 6,
  },
  textButton: {
    borderColor: tokens.color.line,
    borderRadius: tokens.radius.pill,
    borderWidth: 1,
    paddingHorizontal: 13,
    paddingVertical: 8,
  },
  textButtonLabel: {
    color: tokens.color.textMuted,
    fontSize: 13,
    fontWeight: '700',
  },
  infoCard: {
    backgroundColor: tokens.color.surface,
    borderColor: tokens.color.line,
    borderRadius: tokens.radius.md,
    borderWidth: 1,
    flexDirection: 'row',
    gap: 14,
    marginTop: 16,
    padding: 18,
  },
  infoIcon: {
    color: tokens.color.success,
    fontSize: 24,
  },
  infoCopy: {
    flex: 1,
  },
  infoTitle: {
    color: tokens.color.text,
    fontSize: 14,
    fontWeight: '800',
  },
  infoBody: {
    color: tokens.color.textMuted,
    fontSize: 12,
    lineHeight: 18,
    marginTop: 5,
  },
});
