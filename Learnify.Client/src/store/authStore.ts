import { create } from 'zustand';
import { authService } from '../services/authService';
import type { AuthResponse } from '../services/authService';

interface AuthUser {
  id: string;
  name: string;
  email: string;
  role: string;
}

interface AuthState {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;

  // Actions
  login: (email: string, password: string) => Promise<void>;
  register: (name: string, email: string, password: string) => Promise<void>;
  logout: () => void;
  loadUser: () => void;
  clearError: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isAuthenticated: false,
  isLoading: false,
  error: null,

  login: async (email: string, password: string) => {
    set({ isLoading: true, error: null });
    try {
      const response: AuthResponse = await authService.login({ email, password });
      set({
        user: response.user,
        isAuthenticated: true,
        isLoading: false,
        error: null,
      });
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Login failed';
      set({ isLoading: false, error: message });
      throw error;
    }
  },

  register: async (name: string, email: string, password: string) => {
    set({ isLoading: true, error: null });
    try {
      const response: AuthResponse = await authService.register({ name, email, password });
      set({
        user: response.user,
        isAuthenticated: true,
        isLoading: false,
        error: null,
      });
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Registration failed';
      set({ isLoading: false, error: message });
      throw error;
    }
  },

  logout: () => {
    authService.logout();
    set({
      user: null,
      isAuthenticated: false,
      error: null,
    });
  },

  loadUser: () => {
    const user = authService.getCurrentUser();
    const isAuth = authService.isAuthenticated();
    set({ user, isAuthenticated: isAuth });
  },

  clearError: () => set({ error: null }),
}));