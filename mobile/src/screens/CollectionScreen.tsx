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
  packs: InventoryPack[];
  openedPackIds: string[];
  activeView: 'packs' | 'cards';
  onOpen: (pack: InventoryPack) => void;
  onViewChange: (view: 'packs' | 'cards') => void;
};

export function CollectionScreen({
  packs,
  openedPackIds,
  activeView,
  onOpen,
  onViewChange,
}: Props) {
  const {width} = useWindowDimensions();
  const gap = 14;
  const tileWidth = (width - 40 - gap) / 2;
  const artworkWidth = tileWidth - 24;
  const openedPacks = packs.filter(pack =>
    openedPackIds.includes(pack.inventoryId),
  );
  const openedCards = openedPacks.flatMap(pack =>
    (pack.reveal.cards?.length ? pack.reveal.cards : [pack.reveal.card]).map(
      card => ({card, pack}),
    ),
  );

  return (
    <ScrollView
      contentContainerStyle={styles.content}
      showsVerticalScrollIndicator={false}>
      <ScreenHeader
        eyebrow="THE VAULT"
        title="Your collection"
        detail="Select any unopened pack. Its five assigned cards are restored on every retry."
      />

      <View style={styles.filterRow}>
        <Pressable
          accessibilityRole="tab"
          accessibilityState={{selected: activeView === 'packs'}}
          onPress={() => onViewChange('packs')}
          style={activeView === 'packs' ? styles.activeFilter : styles.filter}>
          <Text
            style={
              activeView === 'packs'
                ? styles.activeFilterText
                : styles.filterText
            }>
            Unopened
          </Text>
          <View style={styles.count}>
            <Text style={styles.countText}>
              {packs.length - openedPackIds.length}
            </Text>
          </View>
        </Pressable>
        <Pressable
          accessibilityRole="tab"
          accessibilityState={{selected: activeView === 'cards'}}
          onPress={() => onViewChange('cards')}
          style={activeView === 'cards' ? styles.activeFilter : styles.filter}>
          <Text
            style={
              activeView === 'cards'
                ? styles.activeFilterText
                : styles.filterText
            }>
            Cards
          </Text>
          <View style={styles.count}>
            <Text style={styles.countText}>{openedCards.length}</Text>
          </View>
        </Pressable>
      </View>

      {activeView === 'packs' ? (
        <View style={[styles.grid, {gap}]}>
          {packs
            .filter(pack => !openedPackIds.includes(pack.inventoryId))
            .map(pack => (
              <Pressable
                accessibilityLabel={`${pack.name} pack, unopened`}
                accessibilityRole="button"
                key={pack.inventoryId}
                onPress={() => onOpen(pack)}
                style={({pressed}) => [
                  styles.tile,
                  {width: tileWidth},
                  pressed && styles.pressed,
                ]}>
                <PackArtwork pack={pack} width={artworkWidth} />
                <View style={styles.tileCopy}>
                  <View>
                    <Text style={styles.packName}>{pack.name}</Text>
                    <Text style={styles.edition}>{pack.edition}</Text>
                  </View>
                  <View
                    style={[
                      styles.statusDot,
                      {backgroundColor: pack.theme.accent},
                    ]}
                  />
                </View>
              </Pressable>
            ))}
        </View>
      ) : openedCards.length ? (
        <View style={[styles.grid, {gap}]}>
          {openedCards.map(({card, pack}) => (
            <View
              accessibilityLabel={`${card.name}, ${card.rarityTier}`}
              key={card.id}
              style={[styles.cardTile, {width: tileWidth}]}>
              <View
                style={[
                  styles.cardArt,
                  {
                    backgroundColor: pack.theme.accentSoft,
                    borderColor: pack.theme.accent,
                  },
                ]}>
                <View
                  style={[styles.cardOrbit, {borderColor: pack.theme.accent}]}
                />
                <Text style={[styles.cardGlyph, {color: pack.theme.accent}]}>
                  {pack.theme.symbol}
                </Text>
              </View>
              <Text style={styles.cardName}>{card.name}</Text>
              <Text style={[styles.rarity, {color: pack.theme.accent}]}>
                {card.rarityTier.toUpperCase()} ·{' '}
                {card.archetype.toUpperCase()}
              </Text>
              <Text style={styles.edition}>{card.grade}</Text>
            </View>
          ))}
        </View>
      ) : (
        <View style={styles.empty}>
          <Text style={styles.emptyGlyph}>◇</Text>
          <Text style={styles.emptyTitle}>Your first card is still sealed</Text>
          <Text style={styles.emptyDetail}>
            Open a pack and it will appear here with the same permanent receipt.
          </Text>
        </View>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  content: {
    paddingBottom: 126,
    paddingHorizontal: 20,
    paddingTop: 18,
  },
  filterRow: {
    flexDirection: 'row',
    gap: 8,
    marginTop: 24,
  },
  activeFilter: {
    alignItems: 'center',
    backgroundColor: tokens.color.text,
    borderRadius: tokens.radius.pill,
    flexDirection: 'row',
    gap: 8,
    paddingHorizontal: 14,
    paddingVertical: 9,
  },
  activeFilterText: {
    color: tokens.color.canvas,
    fontSize: 12,
    fontWeight: '800',
  },
  count: {
    alignItems: 'center',
    backgroundColor: '#DCE1EA',
    borderRadius: tokens.radius.pill,
    height: 18,
    justifyContent: 'center',
    minWidth: 18,
  },
  countText: {
    color: tokens.color.canvas,
    fontSize: 10,
    fontWeight: '900',
  },
  filter: {
    alignItems: 'center',
    borderColor: tokens.color.line,
    borderRadius: tokens.radius.pill,
    borderWidth: 1,
    flexDirection: 'row',
    gap: 8,
    paddingHorizontal: 14,
    paddingVertical: 9,
  },
  filterText: {
    color: tokens.color.textMuted,
    fontSize: 12,
    fontWeight: '700',
  },
  grid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    marginTop: 20,
  },
  tile: {
    backgroundColor: tokens.color.surface,
    borderColor: tokens.color.line,
    borderRadius: tokens.radius.md,
    borderWidth: 1,
    padding: 12,
  },
  pressed: {
    opacity: 0.7,
    transform: [{scale: 0.98}],
  },
  tileCopy: {
    alignItems: 'center',
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginTop: 13,
  },
  packName: {
    color: tokens.color.text,
    fontSize: 15,
    fontWeight: '800',
  },
  edition: {
    color: tokens.color.textMuted,
    fontSize: 9,
    fontWeight: '700',
    letterSpacing: 0.8,
    marginTop: 3,
  },
  statusDot: {
    borderRadius: 999,
    height: 7,
    width: 7,
  },
  cardTile: {
    backgroundColor: '#ECEEF3',
    borderRadius: tokens.radius.md,
    padding: 9,
  },
  cardArt: {
    alignItems: 'center',
    aspectRatio: 0.82,
    borderRadius: 12,
    borderWidth: 1,
    justifyContent: 'center',
    overflow: 'hidden',
  },
  cardOrbit: {
    borderRadius: 999,
    borderWidth: 1,
    height: '80%',
    opacity: 0.45,
    position: 'absolute',
    transform: [{rotate: '-24deg'}],
    width: '58%',
  },
  cardGlyph: {
    fontSize: 58,
    fontWeight: '200',
    opacity: 0.72,
  },
  cardName: {
    color: '#10131A',
    fontSize: 14,
    fontWeight: '900',
    marginTop: 10,
  },
  rarity: {
    fontSize: 7,
    fontWeight: '900',
    letterSpacing: 0.65,
    marginTop: 3,
  },
  empty: {
    alignItems: 'center',
    backgroundColor: tokens.color.surface,
    borderColor: tokens.color.line,
    borderRadius: tokens.radius.lg,
    borderWidth: 1,
    marginTop: 20,
    paddingHorizontal: 28,
    paddingVertical: 44,
  },
  emptyGlyph: {
    color: tokens.color.cyan,
    fontSize: 38,
  },
  emptyTitle: {
    color: tokens.color.text,
    fontSize: 17,
    fontWeight: '800',
    marginTop: 14,
    textAlign: 'center',
  },
  emptyDetail: {
    color: tokens.color.textMuted,
    fontSize: 12,
    lineHeight: 18,
    marginTop: 7,
    textAlign: 'center',
  },
});
