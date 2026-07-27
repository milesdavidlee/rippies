import React from 'react';
import {
  Pressable,
  ScrollView,
  StatusBar,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import {
  SafeAreaProvider,
  SafeAreaView,
} from 'react-native-safe-area-context';

const PACKS = [
  {id: 'rippies_chrome', name: 'Chrome', accent: '#68E4FF'},
  {id: 'rippies_solar', name: 'Solar', accent: '#FFB84D'},
  {id: 'rippies_prism', name: 'Prism', accent: '#B96CFF'},
  {id: 'rippies_ember', name: 'Ember', accent: '#FF6767'},
];

function App(): React.JSX.Element {
  return (
    <SafeAreaProvider>
      <SafeAreaView style={styles.safeArea}>
        <StatusBar
          barStyle="light-content"
          backgroundColor={styles.safeArea.backgroundColor}
        />
        <ScrollView contentContainerStyle={styles.content}>
          <View style={styles.header}>
            <Text style={styles.eyebrow}>RIPPIES COLLECTION</Text>
            <Text style={styles.title}>Your unopened packs</Text>
            <Text style={styles.subtitle}>
              Choose a pack to prepare its immutable reveal receipt.
            </Text>
          </View>

          <View style={styles.grid}>
            {PACKS.map(pack => (
              <Pressable
                accessibilityRole="button"
                accessibilityLabel={`Open ${pack.name} pack`}
                key={pack.id}
                style={({pressed}) => [
                  styles.pack,
                  {borderColor: pack.accent},
                  pressed && styles.packPressed,
                ]}>
                <View style={[styles.packArt, {backgroundColor: pack.accent}]} />
                <Text style={styles.packName}>{pack.name}</Text>
                <Text style={styles.packStatus}>UNOPENED</Text>
              </Pressable>
            ))}
          </View>

          <View style={styles.handoff}>
            <Text style={styles.handoffTitle}>Unity reveal host</Text>
            <Text style={styles.handoffBody}>
              The native PackRevealView will mount here after the selected
              reveal payload is restored and Unity emits sceneReady.
            </Text>
          </View>
        </ScrollView>
      </SafeAreaView>
    </SafeAreaProvider>
  );
}

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: '#090A10',
  },
  content: {
    paddingHorizontal: 20,
    paddingBottom: 36,
  },
  header: {
    paddingBottom: 24,
    paddingTop: 28,
  },
  eyebrow: {
    color: '#7DE9FF',
    fontSize: 12,
    fontWeight: '800',
    letterSpacing: 1.8,
  },
  title: {
    color: '#FFFFFF',
    fontSize: 32,
    fontWeight: '800',
    marginTop: 10,
  },
  subtitle: {
    color: '#A8ACBB',
    fontSize: 16,
    lineHeight: 23,
    marginTop: 8,
  },
  grid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 12,
  },
  pack: {
    backgroundColor: '#151722',
    borderRadius: 18,
    borderWidth: 1,
    minHeight: 210,
    padding: 14,
    width: '48%',
  },
  packPressed: {
    opacity: 0.72,
    transform: [{scale: 0.98}],
  },
  packArt: {
    borderRadius: 12,
    flex: 1,
    opacity: 0.82,
  },
  packName: {
    color: '#FFFFFF',
    fontSize: 18,
    fontWeight: '800',
    marginTop: 14,
  },
  packStatus: {
    color: '#84899A',
    fontSize: 10,
    fontWeight: '700',
    letterSpacing: 1.4,
    marginTop: 4,
  },
  handoff: {
    backgroundColor: '#11131B',
    borderColor: '#292D3C',
    borderRadius: 16,
    borderWidth: 1,
    marginTop: 24,
    padding: 18,
  },
  handoffTitle: {
    color: '#FFFFFF',
    fontSize: 16,
    fontWeight: '700',
  },
  handoffBody: {
    color: '#A8ACBB',
    fontSize: 14,
    lineHeight: 21,
    marginTop: 6,
  },
});

export default App;
