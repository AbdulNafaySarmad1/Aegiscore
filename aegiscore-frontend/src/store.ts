import { configureStore, createSlice, PayloadAction } from "@reduxjs/toolkit";

type Role = "student" | "faculty";
type UiState = { role: Role; page: string; dark: boolean; unread: number };

const initialState: UiState = { role: "student", page: "dashboard", dark: false, unread: 3 };

const ui = createSlice({
  name: "ui",
  initialState,
  reducers: {
    setRole: (s, a: PayloadAction<Role>) => { s.role = a.payload; s.page = "dashboard"; },
    setPage: (s, a: PayloadAction<string>) => { s.page = a.payload; },
    toggleDark: s => { s.dark = !s.dark; },
    receiveNotification: s => { s.unread += 1; }
  }
});

export const { setRole, setPage, toggleDark, receiveNotification } = ui.actions;
export const store = configureStore({ reducer: { ui: ui.reducer } });
export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
