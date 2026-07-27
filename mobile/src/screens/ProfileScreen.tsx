import React from 'react';
import {Pressable, ScrollView, StyleSheet, Text, View} from 'react-native';

import {ScreenHeader} from '../components/ScreenHeader';
import {tokens} from '../design/tokens';

type Props = {
  openedCount: number;
  onReset: () => void;
};

export function ProfileScreen({openedCount, onReset}: Props) {
  return (
    <ScrollView contentContainerStyle={styles.content}>
      <ScreenHeader
        eyebrow="COLLECTOR 001"
        title="Miles"
        detail="First Edition member · Joined July 2026"
      />

      <View style={styles.stats}>
        <View style={styles.stat}>
          <Text style={styles.statValue}>6</Text>
          <Text style={styles.statLabel}>OWNED</Text>
        </View>
        <View style={styles.divider} />
        <View style={styles.stat}>
          <Text style={styles.statValue}>{openedCount}</Text>
          <Text style={styles.statLabel}>OPENED</Text>
        </View>
        <View style={styles.divider} />
        <View style={styles.stat}>
          <Text style={styles.statValue}>250</Text>
          <Text style={styles.statLabel}>FOUNDERS</Text>
        </View>
      </View>

      <Text style={styles.sectionLabel}>DEMO CONTROLS</Text>
      <View style={styles.settingsCard}>
        <View style={styles.setting}>
          <View>
            <Text style={styles.settingTitle}>Reveal recovery</Text>
            <Text style={styles.settingDetail}>
              Fake receipts persist across app launches.
            </Text>
          </View>
          <Text style={styles.enabled}>ON</Text>
        </View>
        <View style={styles.hairline} />
        <Pressable
          accessibilityRole="button"
          onPress={onReset}
          style={({pressed}) => [styles.reset, pressed && styles.pressed]}>
          <Text style={styles.resetLabel}>Reset fake collection</Text>
          <Text style={styles.resetArrow}>↻</Text>
        </Pressable>
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
  stats: {
    backgroundColor: tokens.color.surface,
    borderColor: tokens.color.line,
    borderRadius: tokens.radius.md,
    borderWidth: 1,
    flexDirection: 'row',
    marginTop: 28,
    paddingVertical: 20,
  },
  stat: {
    alignItems: 'center',
    flex: 1,
  },
  statValue: {
    color: tokens.color.text,
    fontSize: 23,
    fontWeight: '900',
  },
  statLabel: {
    color: tokens.color.textMuted,
    fontSize: 9,
    fontWeight: '800',
    letterSpacing: 1.2,
    marginTop: 5,
  },
  divider: {
    backgroundColor: tokens.color.line,
    width: 1,
  },
  sectionLabel: {
    color: tokens.color.cyan,
    fontSize: 10,
    fontWeight: '900',
    letterSpacing: 1.5,
    marginBottom: 10,
    marginTop: 30,
  },
  settingsCard: {
    backgroundColor: tokens.color.surface,
    borderColor: tokens.color.line,
    borderRadius: tokens.radius.md,
    borderWidth: 1,
    paddingHorizontal: 17,
  },
  setting: {
    alignItems: 'center',
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingVertical: 17,
  },
  settingTitle: {
    color: tokens.color.text,
    fontSize: 14,
    fontWeight: '800',
  },
  settingDetail: {
    color: tokens.color.textMuted,
    fontSize: 11,
    marginTop: 4,
  },
  enabled: {
    color: tokens.color.success,
    fontSize: 10,
    fontWeight: '900',
    letterSpacing: 1,
  },
  hairline: {
    backgroundColor: tokens.color.line,
    height: 1,
  },
  reset: {
    alignItems: 'center',
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingVertical: 17,
  },
  resetLabel: {
    color: tokens.color.warning,
    fontSize: 14,
    fontWeight: '700',
  },
  resetArrow: {
    color: tokens.color.warning,
    fontSize: 18,
  },
  pressed: {
    opacity: 0.55,
  },
});
