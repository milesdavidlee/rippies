import React from 'react';
import {
  Platform,
  requireNativeComponent,
  StyleProp,
  UIManager,
  View,
  ViewProps,
  ViewStyle,
} from 'react-native';

type Props = ViewProps & {
  style?: StyleProp<ViewStyle>;
};

const managerAvailable =
  Platform.OS === 'ios' &&
  UIManager.getViewManagerConfig('RippiesGlassView') != null;
const IOSGlassView = managerAvailable
  ? requireNativeComponent<Props>('RippiesGlassView')
  : null;

export function NativeGlassView({children, style, ...props}: Props) {
  if (IOSGlassView) {
    return (
      <IOSGlassView style={style} {...props}>
        {children}
      </IOSGlassView>
    );
  }

  return (
    <View style={[styles.fallback, style]} {...props}>
      {children}
    </View>
  );
}

const styles = {
  fallback: {
    backgroundColor: 'rgba(22, 26, 38, 0.88)',
    borderColor: 'rgba(255, 255, 255, 0.12)',
    borderWidth: 1,
  } satisfies ViewStyle,
};
