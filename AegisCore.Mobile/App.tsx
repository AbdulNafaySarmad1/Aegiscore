import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { Provider, useSelector } from 'react-redux';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { Text } from 'react-native';
import { RoleSwitcher } from './src/ui/RoleSwitcher';
import { makeScreen } from './src/screens/Screen';
import { facultyScreens, studentScreens } from './src/screens/screenData';
import { RootState, store } from './src/state/store';

const Tabs = createBottomTabNavigator();
const queryClient = new QueryClient();

function TabsByRole() {
  const role = useSelector((state: RootState) => state.session.role);
  const screens = role === 'faculty' ? facultyScreens : studentScreens;

  return (
    <>
      <RoleSwitcher />
      <Tabs.Navigator screenOptions={{ headerShown: false, tabBarLabelPosition: 'below-icon' }}>
        {screens.map((screen) => (
          <Tabs.Screen
            key={screen.key}
            name={screen.title}
            component={makeScreen(screen)}
            options={{ tabBarIcon: ({ color }) => <Text style={{ color }}>{screen.icon}</Text> }}
          />
        ))}
      </Tabs.Navigator>
    </>
  );
}

export default function App() {
  return (
    <Provider store={store}>
      <QueryClientProvider client={queryClient}>
        <SafeAreaProvider>
          <NavigationContainer>
            <TabsByRole />
          </NavigationContainer>
        </SafeAreaProvider>
      </QueryClientProvider>
    </Provider>
  );
}
