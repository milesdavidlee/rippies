import React, {useEffect, useRef, useState} from 'react';
import {Animated, Pressable, StyleSheet, Text, View} from 'react-native';

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
  const [barWidth, setBarWidth] = useState(0);
  const activeIndex = tabs.findIndex(tab => tab.id === activeTab);
  const selectionPosition = useRef(new Animated.Value(activeIndex)).current;
  const tabWidth = Math.max(0, (barWidth - 16) / tabs.length);

  useEffect(() => {
    Animated.spring(selectionPosition, {
      damping: 20,
      mass: 0.72,
      stiffness: 190,
      toValue: activeIndex,
      useNativeDriver: true,
    }).start();
  }, [activeIndex, selectionPosition]);

  return (
    <View
      onLayout={event => setBarWidth(event.nativeEvent.layout.width)}
      style={styles.container}>
      <NativeGlassView pointerEvents="none" style={styles.glass} />
      {tabWidth > 0 ? (
        <Animated.View
          pointerEvents="none"
          style={[
            styles.selection,
            {
              transform: [{translateX: Animated.multiply(selectionPosition, tabWidth)}],
              width: tabWidth,
            },
          ]}>
          <NativeGlassView
            highlighted
            pointerEvents="none"
            style={styles.selectionGlass}
          />
        </Animated.View>
      ) : null}
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
    backgroundColor: 'rgba(242, 246, 255, 0.16)',
    borderRadius: tokens.radius.pill,
    bottom: 0,
    left: 0,
    position: 'absolute',
    right: 0,
    top: 0,
  },
  selection: {
    bottom: 7,
    left: 8,
    position: 'absolute',
    top: 7,
  },
  selectionGlass: {
    backgroundColor: 'rgba(248, 250, 255, 0.78)',
    borderRadius: tokens.radius.pill,
    bottom: 0,
    left: 0,
    position: 'absolute',
    right: 0,
    top: 0,
  },
  tab: {
    alignItems: 'center',
    borderRadius: tokens.radius.pill,
    flex: 1,
    gap: 2,
    paddingVertical: 6,
    zIndex: 1,
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
    color: '#10131A',
    textShadowColor: 'rgba(255, 255, 255, 0.55)',
    textShadowOffset: {width: 0, height: 0},
    textShadowRadius: 10,
  },
});
