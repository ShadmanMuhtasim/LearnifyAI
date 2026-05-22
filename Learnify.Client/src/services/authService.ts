import apiClient from './api';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  user: {
    id: string;
    name: string;
    email: string;
    role: string;
  };
}

export interface TokenRefreshRequest {
  refreshToken: string;
}

export interface TokenRefreshResponse {
  accessToken: string;
  refreshToken: string;
}

class AuthService {
  /**
   * Login user and store JWT tokens in localStorage
   */
  async login(data: LoginRequest): Promise<AuthResponse> {
    const response = await apiClient.post<AuthResponse>('/auth/login', data);
    this.storeTokens(response.data.accessToken, response.data.refreshToken);
    return response.data;
  }

  /**
   * Register new user and store JWT tokens
   */
  async register(data: RegisterRequest): Promise<AuthResponse> {
    const response = await apiClient.post<AuthResponse>('/auth/register', data);
    this.storeTokens(response.data.accessToken, response.data.refreshToken);
    return response.data;
  }

  /**
   * Refresh expired access token using refresh token
   */
  async refreshTokens(data: TokenRefreshRequest): Promise<TokenRefreshResponse> {
    const response = await apiClient.post<TokenRefreshResponse>('/auth/refresh-token', data);
    this.storeTokens(response.data.accessToken, response.data.refreshToken);
    return response.data;
  }

  /**
   * Logout and clear stored tokens
   */
  logout(): void {
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
  getCurrentUser(): { id: string; name: string; email: string; role: string } | null {
    const userStr = localStorage.getItem('user');
    return userStr ? JSON.parse(userStr) : null;
  }

  private storeTokens(accessToken: string, refreshToken: string): void {
    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('refreshToken', refreshToken);
  }

  private clearTokens(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
  }
}

export const authService = new AuthService();
export default authService;