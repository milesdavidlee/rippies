import React, {useCallback, useEffect, useState} from 'react';
import {StatusBar, StyleSheet, View} from 'react-native';
import {SafeAreaView} from 'react-native-safe-area-context';

import {TabBar, type AppTab} from '../components/TabBar';
import {fakeInventory, type InventoryPack} from '../data/fakeInventory';
import {tokens} from '../design/tokens';
import {RevealExperience} from '../reveal/RevealExperience';
import type {CardPayload} from '../reveal/contracts';
import {
  loadOpenedPackIds,
  markPackOpened,
  resetFakeCollection,
} from '../reveal/fakeRevealStore';
import {CollectionScreen} from '../screens/CollectionScreen';
import {DiscoverScreen} from '../screens/DiscoverScreen';
import {ProfileScreen} from '../screens/ProfileScreen';

export function AppShell() {
  const [activeTab, setActiveTab] = useState<AppTab>('discover');
  const [collectionView, setCollectionView] = useState<'packs' | 'cards'>(
    'packs',
  );
  const [selectedPack, setSelectedPack] = useState<InventoryPack | null>(null);
  const [inspectionCardId, setInspectionCardId] = useState<string | null>(null);
  const [openedPackIds, setOpenedPackIds] = useState<string[]>([]);

  useEffect(() => {
    loadOpenedPackIds().then(setOpenedPackIds);
  }, []);

  const completeReveal = useCallback(async (pack: InventoryPack) => {
    const next = await markPackOpened(pack.inventoryId);
    setOpenedPackIds(next);
    setSelectedPack(null);
    setInspectionCardId(null);
    setCollectionView('cards');
    setActiveTab('collection');
  }, []);

  const openPack = useCallback((pack: InventoryPack) => {
    setInspectionCardId(null);
    setSelectedPack(pack);
  }, []);

  const inspectCard = useCallback((pack: InventoryPack, card: CardPayload) => {
    setInspectionCardId(card.id);
    setSelectedPack(pack);
  }, []);

  const dismissReveal = useCallback(() => {
    setSelectedPack(null);
    setInspectionCardId(null);
  }, []);

  const reset = useCallback(async () => {
    await resetFakeCollection();
    setOpenedPackIds([]);
    setCollectionView('packs');
    setActiveTab('collection');
  }, []);

  return (
    <View style={styles.root}>
      <StatusBar barStyle="light-content" backgroundColor={tokens.color.canvas} />
      <SafeAreaView edges={['top']} style={styles.safeArea}>
        <View style={styles.content}>
          {activeTab === 'discover' ? (
            <DiscoverScreen
              featuredPack={fakeInventory[2]}
              onOpen={openPack}
              onViewCollection={() => setActiveTab('collection')}
            />
          ) : null}
          {activeTab === 'collection' ? (
            <CollectionScreen
              activeView={collectionView}
              onInspectCard={inspectCard}
              onOpen={openPack}
              onViewChange={setCollectionView}
              openedPackIds={openedPackIds}
              packs={fakeInventory}
            />
          ) : null}
          {activeTab === 'profile' ? (
            <ProfileScreen
              onReset={reset}
              openedCount={openedPackIds.length}
            />
          ) : null}
        </View>
        <TabBar activeTab={activeTab} onChange={setActiveTab} />
      </SafeAreaView>

      <RevealExperience
        inspectionCardId={inspectionCardId}
        onCancel={dismissReveal}
        onComplete={completeReveal}
        pack={selectedPack}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  root: {
    backgroundColor: tokens.color.canvas,
    flex: 1,
  },
  safeArea: {
    flex: 1,
  },
  content: {
    flex: 1,
  },
});
