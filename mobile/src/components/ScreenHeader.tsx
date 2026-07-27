import React from 'react';
import {StyleSheet, Text, View} from 'react-native';

import {tokens} from '../design/tokens';

type Props = {
  eyebrow: string;
  title: string;
  detail?: string;
  trailing?: React.ReactNode;
};

export function ScreenHeader({eyebrow, title, detail, trailing}: Props) {
  return (
    <View style={styles.row}>
      <View style={styles.copy}>
        <Text style={styles.eyebrow}>{eyebrow}</Text>
        <Text style={styles.title}>{title}</Text>
        {detail ? <Text style={styles.detail}>{detail}</Text> : null}
      </View>
      {trailing}
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    alignItems: 'flex-start',
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
  copy: {
    flex: 1,
    paddingRight: 14,
  },
  eyebrow: {
    color: tokens.color.cyan,
    fontSize: 11,
    fontWeight: '900',
    letterSpacing: 1.8,
  },
  title: {
    color: tokens.color.text,
    fontSize: 32,
    fontWeight: '900',
    letterSpacing: -1,
    marginTop: 8,
  },
  detail: {
    color: tokens.color.textMuted,
    fontSize: 14,
    lineHeight: 21,
    marginTop: 7,
  },
});
