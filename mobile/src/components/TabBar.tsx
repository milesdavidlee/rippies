import React from 'react';
import {Pressable, StyleSheet, Text, View} from 'react-native';

import {tokens} from '../design/tokens';
import {NativeGlassView} from './NativeGlassView';

export type AppTab = 'discover' | 'collection' | 'profile';

const tabs: {id: AppTab; icon: string; label: string}[] = [
  {id: 'discover', icon: '✦', label: 'Discover'},
  {id: 'collection', icon: '▦', label: 'Collection'},
  {id: 'profile', icon: '●', label: 'Profile'},
];

type Props = {
  activeTab: AppTab;
  onChange: (tab: AppTab) => void;
};

export function TabBar({activeTab, onChange}: Props) {
  return (
    <View style={styles.container}>
      <NativeGlassView pointerEvents="none" style={styles.glass} />
      {tabs.map(tab => {
        const active = activeTab === tab.id;
        return (
          <Pressable
            accessibilityRole="tab"
            accessibilityState={{selected: active}}
            key={tab.id}
            onPress={() => onChange(tab.id)}
            style={styles.tab}>
            <Text style={[styles.icon, active && styles.active]}>{tab.icon}</Text>
            <Text style={[styles.label, active && styles.active]}>
              {tab.label}
            </Text>
          </Pressable>
        );
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    backgroundColor: 'transparent',
    borderRadius: tokens.radius.pill,
    bottom: 10,
    flexDirection: 'row',
    left: 14,
    overflow: 'hidden',
    paddingHorizontal: 8,
    paddingVertical: 7,
    position: 'absolute',
    right: 14,
    shadowColor: '#000000',
    shadowOffset: {width: 0, height: 12},
    shadowOpacity: 0.28,
    shadowRadius: 24,
  },
  glass: {
    bottom: 0,
    left: 0,
    position: 'absolute',
    right: 0,
    top: 0,
    borderRadius: tokens.radius.pill,
  },
  tab: {
    alignItems: 'center',
    borderRadius: tokens.radius.pill,
    flex: 1,
    gap: 2,
    paddingVertical: 6,
  },
  icon: {
    color: '#646B7B',
    fontSize: 18,
  },
  label: {
    color: '#737A8B',
    fontSize: 10,
    fontWeight: '700',
  },
  active: {
    color: tokens.color.cyan,
    textShadowColor: 'rgba(112, 230, 255, 0.38)',
    textShadowOffset: {width: 0, height: 0},
    textShadowRadius: 10,
  },
});
