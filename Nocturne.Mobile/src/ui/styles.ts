import { StyleSheet } from 'react-native';

export const styles = StyleSheet.create({
  page: {
    gap: 18,
    padding: 20,
    paddingBottom: 48,
    backgroundColor: '#f7f8fb'
  },
  title: {
    color: '#14213d',
    fontSize: 26,
    fontWeight: '800'
  },
  summary: {
    color: '#3f4b5f',
    fontSize: 16,
    lineHeight: 23
  },
  stats: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 10
  },
  stat: {
    minWidth: 120,
    borderRadius: 8,
    backgroundColor: '#ffffff',
    borderColor: '#d8dee9',
    borderWidth: 1,
    padding: 14
  },
  statDanger: {
    borderColor: '#d1495b',
    backgroundColor: '#fff5f6'
  },
  statLabel: {
    color: '#65758b',
    fontSize: 12,
    textTransform: 'uppercase'
  },
  statValue: {
    color: '#14213d',
    fontSize: 20,
    fontWeight: '700',
    marginTop: 4
  },
  actions: {
    gap: 10
  },
  button: {
    minHeight: 48,
    justifyContent: 'center',
    borderRadius: 8,
    backgroundColor: '#0f766e',
    paddingHorizontal: 16
  },
  buttonText: {
    color: '#ffffff',
    fontSize: 16,
    fontWeight: '700',
    textAlign: 'center'
  },
  roleBar: {
    flexDirection: 'row',
    gap: 8,
    paddingHorizontal: 12,
    paddingTop: 48,
    paddingBottom: 8,
    backgroundColor: '#ffffff'
  },
  roleButton: {
    flex: 1,
    borderRadius: 8,
    borderColor: '#cbd5e1',
    borderWidth: 1,
    paddingVertical: 10
  },
  roleButtonActive: {
    backgroundColor: '#14213d',
    borderColor: '#14213d'
  },
  roleButtonText: {
    color: '#14213d',
    fontWeight: '700',
    textAlign: 'center'
  },
  roleButtonTextActive: {
    color: '#ffffff'
  }
});
