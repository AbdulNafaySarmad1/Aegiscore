import React from 'react';
import { ScrollView, Text, TouchableOpacity, View } from 'react-native';
import { MobileScreen } from './screenData';
import { styles } from '../ui/styles';

export function makeScreen(screen: MobileScreen) {
  return function Screen() {
    return (
      <ScrollView contentContainerStyle={styles.page}>
        <Text accessibilityRole="header" style={styles.title}>{screen.title}</Text>
        <Text style={styles.summary}>{screen.summary}</Text>
        <View style={styles.stats}>
          {screen.stats.map((stat) => (
            <View key={stat.label} style={[styles.stat, stat.tone === 'danger' && styles.statDanger]}>
              <Text style={styles.statLabel}>{stat.label}</Text>
              <Text style={styles.statValue}>{stat.value}</Text>
            </View>
          ))}
        </View>
        <View style={styles.actions}>
          {screen.actions.map((action) => (
            <TouchableOpacity key={action} accessibilityRole="button" style={styles.button}>
              <Text style={styles.buttonText}>{action}</Text>
            </TouchableOpacity>
          ))}
        </View>
      </ScrollView>
    );
  };
}
