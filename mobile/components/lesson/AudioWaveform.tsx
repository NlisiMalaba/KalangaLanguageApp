import { StyleSheet, View } from 'react-native';

import { Colors } from '@/constants/theme';
import { useColorScheme } from '@/hooks/use-color-scheme';

const BAR_COUNT = 5;
const BAR_HEIGHTS = [10, 18, 14, 22, 12];

export function AudioWaveform({ playing }: { playing: boolean }) {
  const scheme = useColorScheme() ?? 'light';
  const color = playing ? Colors[scheme].tint : Colors[scheme].icon;

  return (
    <View
      style={styles.row}
      accessibilityRole="image"
      accessibilityLabel={playing ? 'Audio is playing' : 'Audio is stopped'}>
      {BAR_HEIGHTS.slice(0, BAR_COUNT).map((height, index) => (
        <View
          key={index}
          style={[
            styles.bar,
            {
              height: playing ? height : Math.max(6, height * 0.45),
              backgroundColor: color,
            },
          ]}
        />
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    alignItems: 'flex-end',
    gap: 4,
    height: 24,
  },
  bar: {
    width: 4,
    borderRadius: 2,
  },
});
