import { createContext, useContext, useMemo, useState } from 'react';
import { api } from '../lib/api';
import type { SessionUser } from '../types/api';

const storageKey = 'paw-order-session';

interface AuthContextValue {
  user: SessionUser | null;
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function getStoredUser(): SessionUser | null {
  const raw = window.localStorage.getItem(storageKey);
  return raw ? (JSON.parse(raw) as SessionUser) : null;
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<SessionUser | null>(() => getStoredUser());

  const value = useMemo<AuthContextValue>(() => ({
    user,
    login: async (username, password) => {
      const response = await api.post<{ user: SessionUser }>('/auth/login', { username, password });
      setUser(response.user);
      window.localStorage.setItem(storageKey, JSON.stringify(response.user));
    },
    logout: () => {
      setUser(null);
      window.localStorage.removeItem(storageKey);
    },
  }), [user]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }

  return context;
}
