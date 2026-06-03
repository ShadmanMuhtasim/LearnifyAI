import apiClient from './api';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  confirmPassword?: string;
  role?: string;
}

export interface AuthUser {
  id: string;
  name: string;
  email: string;
  role: string;
}

export interface AuthResponse {
  user: AuthUser;
  token: string;
  refreshToken: string;
  expiresAt?: string;
}

export interface TokenRefreshRequest {
  refreshToken: string;
  userId: string;
}

export interface TokenRefreshResponse {
  accessToken: string;
  refreshToken: string;
  user: AuthUser;
}

class AuthService {
  /**
   * Login user and store JWT tokens in localStorage
   */
  async login(data: LoginRequest): Promise<AuthResponse> {
    const response = await apiClient.post<any>('/api/auth/login', data);
    const apiResponse = response.data;
    // Backend returns: { success: true, data: { token, refreshToken, userId, fullName, email, role, ... }, message }
    const dataObj = apiResponse.data || apiResponse;
    const user: AuthUser = {
      id: dataObj.userId || dataObj.id || '',
      name: dataObj.fullName || dataObj.name || '',
      email: dataObj.email || '',
      role: dataObj.role || '',
    };
    const token = dataObj.token || dataObj.accessToken || '';
    const refreshToken = dataObj.refreshToken || '';
    this.storeTokens(token, refreshToken, user);
    return { user, token, refreshToken };
  }

  /**
    * Register new user and store JWT tokens
    */
  async register(data: RegisterRequest): Promise<AuthResponse> {
    const payload = {
      fullName: data.name,
      email: data.email,
      password: data.password,
      confirmPassword: data.confirmPassword || data.password,
      role: data.role || 'Student',
    };
    const response = await apiClient.post<any>('/api/auth/register', payload);
    const apiResponse = response.data;
    const dataObj = apiResponse.data || apiResponse;
    const user: AuthUser = {
      id: dataObj.userId || dataObj.id || '',
      name: dataObj.fullName || dataObj.name || '',
      email: dataObj.email || '',
      role: dataObj.role || '',
    };
    const token = dataObj.token || dataObj.accessToken || '';
    const refreshToken = dataObj.refreshToken || '';
    this.storeTokens(token, refreshToken, user);
    return { user, token, refreshToken };
  }

  /**
   * Refresh expired access token using refresh token
   */
  async refreshTokens(data: TokenRefreshRequest): Promise<TokenRefreshResponse> {
    const response = await apiClient.post<any>('/api/auth/refresh', data);
    const dataObj = response.data.data || response.data;
    const token = dataObj.token || dataObj.accessToken || '';
    const refreshToken = dataObj.refreshToken || '';
    const user: AuthUser = {
      id: dataObj.userId || dataObj.id || data.userId,
      name: dataObj.fullName || dataObj.name || this.getCurrentUser()?.name || '',
      email: dataObj.email || this.getCurrentUser()?.email || '',
      role: dataObj.role || this.getCurrentUser()?.role || '',
    };
    this.storeTokens(token, refreshToken, user);
    return { accessToken: token, refreshToken, user };
  }

  /**
   * Logout and clear stored tokens
   */
  logout(): void {
    this.clearTokens();
  }

  async deleteAccount(): Promise<void> {
    await apiClient.delete('/api/users/me');
    this.clearTokens();
  }

  /**
   * Get current access token from localStorage
   */
  getAccessToken(): string | null {
    return localStorage.getItem('accessToken');
  }

  /**
   * Get current refresh token from localStorage
   */
  getRefreshToken(): string | null {
    return localStorage.getItem('refreshToken');
  }

  /**
   * Check if user is authenticated (has valid access token)
   */
  isAuthenticated(): boolean {
    return !!this.getAccessToken();
  }

  /**
   * Get current user from localStorage
   */
  getCurrentUser(): AuthUser | null {
    const userStr = localStorage.getItem('user');
    return userStr ? JSON.parse(userStr) : null;
  }

  private storeTokens(accessToken: string, refreshToken: string, user: AuthUser): void {
    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('refreshToken', refreshToken);
    localStorage.setItem('user', JSON.stringify(user));
  }

  private clearTokens(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
  }
}

export const authService = new AuthService();
export default authService;
