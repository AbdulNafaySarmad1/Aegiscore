import React from 'react';
import { Text, TouchableOpacity, View } from 'react-native';
import { useDispatch, useSelector } from 'react-redux';
import { setRole } from '../state/sessionSlice';
import { RootState } from '../state/store';
import { styles } from './styles';

export function RoleSwitcher() {
  const dispatch = useDispatch();
  const role = useSelector((state: RootState) => state.session.role);

  return (
    <View style={styles.roleBar}>
      {(['student', 'faculty'] as const).map((nextRole) => (
        <TouchableOpacity
          accessibilityRole="button"
          key={nextRole}
          onPress={() => dispatch(setRole(nextRole))}
          style={[styles.roleButton, role === nextRole && styles.roleButtonActive]}
        >
          <Text style={[styles.roleButtonText, role === nextRole && styles.roleButtonTextActive]}>
            {nextRole === 'student' ? 'Student' : 'Faculty'}
          </Text>
        </TouchableOpacity>
      ))}
    </View>
  );
}
