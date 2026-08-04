import { createSlice, PayloadAction } from '@reduxjs/toolkit';

export type UserRole = 'student' | 'faculty';

type SessionState = {
  role: UserRole;
  offline: boolean;
};

const initialState: SessionState = {
  role: 'student',
  offline: false
};

const sessionSlice = createSlice({
  name: 'session',
  initialState,
  reducers: {
    setRole(state, action: PayloadAction<UserRole>) {
      state.role = action.payload;
    },
    setOffline(state, action: PayloadAction<boolean>) {
      state.offline = action.payload;
    }
  }
});

export const { setRole, setOffline } = sessionSlice.actions;
export default sessionSlice.reducer;
