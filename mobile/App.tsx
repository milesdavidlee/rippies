import React from 'react';
import {SafeAreaProvider} from 'react-native-safe-area-context';

import {AppShell} from './src/navigation/AppShell';

function App(): React.JSX.Element {
  return (
    <SafeAreaProvider>
      <AppShell />
    </SafeAreaProvider>
  );
}

export default App;
