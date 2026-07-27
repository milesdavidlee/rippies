import React from 'react';
import {
  StyleProp,
  StyleSheet,
  Text,
  View,
  ViewStyle,
} from 'react-native';

import type {InventoryPack} from '../data/fakeInventory';
import {tokens} from '../design/tokens';

type Props = {
  pack: InventoryPack;
  width: number;
  style?: StyleProp<ViewStyle>;
  opened?: boolean;
};

export function PackArtwork({pack, width, style, opened = false}: Props) {
  const height = width / tokens.pack.aspectRatio;

  return (
    <View
      style={[
        styles.shadow,
        {height, width, shadowColor: pack.theme.accent},
        style,
      ]}>
      <View
        style={[
          styles.pack,
          {backgroundColor: pack.theme.accentSoft, borderColor: pack.theme.accent},
          opened && styles.opened,
        ]}>
        <View style={[styles.foilBand, {backgroundColor: pack.theme.accent}]} />
        <View style={styles.crimpTop}>
          {Array.from({length: 8}).map((_, index) => (
            <View key={index} style={styles.crimpLine} />
          ))}
        </View>

        <View style={[styles.orbit, {borderColor: pack.theme.accent}]} />
        <View
          style={[styles.orbitSmall, {borderColor: pack.theme.accent}]}
        />
        <Text style={[styles.symbol, {color: pack.theme.accent}]}>
          {pack.theme.symbol}
        </Text>

        <View style={styles.copy}>
          <Text style={styles.brand}>RIPPIES</Text>
          <Text style={[styles.name, {color: pack.theme.accent}]}>
            {pack.name.toUpperCase()}
          </Text>
          <Text style={styles.series}>{pack.series.toUpperCase()}</Text>
        </View>

        <View style={styles.crimpBottom}>
          {Array.from({length: 8}).map((_, index) => (
            <View key={index} style={styles.crimpLine} />
          ))}
        </View>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  shadow: {
    elevation: 12,
    shadowOffset: {width: 0, height: 12},
    shadowOpacity: 0.34,
    shadowRadius: 18,
  },
  pack: {
    borderRadius: 18,
    borderWidth: 1,
    flex: 1,
    overflow: 'hidden',
  },
  opened: {
    opacity: 0.32,
  },
  foilBand: {
    height: '100%',
    left: '47%',
    opacity: 0.08,
    position: 'absolute',
    transform: [{skewX: '-17deg'}],
    width: '28%',
  },
  crimpTop: {
    flexDirection: 'row',
    height: 12,
    justifyContent: 'space-around',
    left: 4,
    opacity: 0.38,
    position: 'absolute',
    right: 4,
    top: 5,
  },
  crimpBottom: {
    bottom: 5,
    flexDirection: 'row',
    height: 12,
    justifyContent: 'space-around',
    left: 4,
    opacity: 0.38,
    position: 'absolute',
    right: 4,
  },
  crimpLine: {
    backgroundColor: '#FFFFFF',
    height: 12,
    transform: [{rotate: '22deg'}],
    width: 1,
  },
  orbit: {
    borderRadius: 999,
    borderWidth: 1,
    height: '62%',
    left: '-24%',
    opacity: 0.24,
    position: 'absolute',
    top: '12%',
    transform: [{rotate: '-24deg'}],
    width: '148%',
  },
  orbitSmall: {
    borderRadius: 999,
    borderWidth: 1,
    height: '38%',
    left: '12%',
    opacity: 0.3,
    position: 'absolute',
    top: '21%',
    transform: [{rotate: '28deg'}],
    width: '76%',
  },
  symbol: {
    fontSize: 48,
    fontWeight: '200',
    left: 0,
    opacity: 0.55,
    position: 'absolute',
    right: 0,
    textAlign: 'center',
    top: '26%',
  },
  copy: {
    bottom: 24,
    left: 12,
    position: 'absolute',
    right: 12,
  },
  brand: {
    color: '#FFFFFF',
    fontSize: 10,
    fontWeight: '900',
    letterSpacing: 2.6,
  },
  name: {
    fontSize: 19,
    fontWeight: '900',
    letterSpacing: 0.8,
    marginTop: 3,
  },
  series: {
    color: '#B6BDCC',
    fontSize: 7,
    fontWeight: '700',
    letterSpacing: 1.2,
    marginTop: 4,
  },
});
