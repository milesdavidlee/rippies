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
  highlighted?: boolean;
  style?: StyleProp<ViewStyle>;
};

const managerAvailable =
  Platform.OS === 'ios' &&
  UIManager.getViewManagerConfig('RippiesGlassView') != null;
const IOSGlassView = managerAvailable
  ? requireNativeComponent<Props>('RippiesGlassView')
  : null;

export function NativeGlassView({
  children,
  highlighted = false,
  style,
  ...props
}: Props) {
  if (IOSGlassView) {
    return (
      <IOSGlassView highlighted={highlighted} style={style} {...props}>
        {children}
      </IOSGlassView>
    );
  }

  return (
    <View
      style={[
        styles.fallback,
        highlighted && styles.fallbackHighlighted,
        style,
      ]}
      {...props}>
      {children}
    </View>
  );
}

const styles = {
  fallback: {
    backgroundColor: 'rgba(242, 246, 255, 0.20)',
    borderColor: 'rgba(255, 255, 255, 0.24)',
    borderWidth: 1,
  } satisfies ViewStyle,
  fallbackHighlighted: {
    backgroundColor: 'rgba(255, 255, 255, 0.42)',
    borderColor: 'rgba(255, 255, 255, 0.52)',
  } satisfies ViewStyle,
};
