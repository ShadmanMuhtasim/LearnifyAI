import { create } from 'zustand';
import axios from 'axios';
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
  deleteAccount: () => Promise<void>;
  logout: () => void;
  loadUser: () => void;
  clearError: () => void;
}

function getAuthErrorMessage(error: unknown, fallback: string): string {
  if (axios.isAxiosError(error) && error.response?.data) {
    const data = error.response.data;
    if (Array.isArray(data.errors) && data.errors.length > 0) {
      return data.errors[0];
    }
    if (data.message) {
      return data.message;
    }
  }

  return error instanceof Error ? error.message : fallback;
}

function getPersistedAuthState() {
  const user = authService.getCurrentUser();
  const isAuthenticated = authService.isAuthenticated() && !!user;

  return {
    user: isAuthenticated ? user : null,
    isAuthenticated,
  };
}

const persistedAuthState = getPersistedAuthState();

export const useAuthStore = create<AuthState>((set) => ({
  user: persistedAuthState.user,
  isAuthenticated: persistedAuthState.isAuthenticated,
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
      set({ isLoading: false, error: getAuthErrorMessage(error, 'Login failed') });
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
      set({ isLoading: false, error: getAuthErrorMessage(error, 'Registration failed') });
      throw error;
    }
  },

  deleteAccount: async () => {
    set({ isLoading: true, error: null });
    try {
      await authService.deleteAccount();
      set({
        user: null,
        isAuthenticated: false,
        isLoading: false,
        error: null,
      });
    } catch (error: unknown) {
      set({ isLoading: false, error: getAuthErrorMessage(error, 'Account deletion failed') });
      throw error;
    }
  },

  logout: () => {
    authService.logout();
    set({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      error: null,
    });
  },

  loadUser: () => {
    const user = authService.getCurrentUser();
    const isAuth = authService.isAuthenticated() && !!user;
    set({ user: isAuth ? user : null, isAuthenticated: isAuth, isLoading: false });
  },

  clearError: () => set({ error: null }),
}));
