import axios from 'axios';
import type { AxiosInstance, AxiosResponse, InternalAxiosRequestConfig } from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5073';

const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

const refreshClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

interface RetryRequestConfig extends InternalAxiosRequestConfig {
  _retry?: boolean;
}

function clearAuthAndRedirect() {
  localStorage.removeItem('accessToken');
  localStorage.removeItem('refreshToken');
  localStorage.removeItem('user');

  if (window.location.pathname !== '/login') {
    window.location.href = '/login';
  }
}

// Request interceptor: Attach JWT token to every request
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = localStorage.getItem('accessToken');
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor: Handle 401 errors globally
apiClient.interceptors.response.use(
  (response: AxiosResponse) => response,
  async (error) => {
    const originalRequest = error.config as RetryRequestConfig | undefined;

    if (
      error.response?.status === 401 &&
      originalRequest &&
      !originalRequest._retry &&
      !originalRequest.url?.includes('/api/auth/refresh')
    ) {
      originalRequest._retry = true;

      const refreshToken = localStorage.getItem('refreshToken');
      const userJson = localStorage.getItem('user');
      const user = userJson ? JSON.parse(userJson) as { id?: string } : null;

      if (!refreshToken || !user?.id) {
        clearAuthAndRedirect();
        return Promise.reject(error);
      }

      try {
        const response = await refreshClient.post('/api/auth/refresh', {
          refreshToken,
          userId: user.id,
        });

        const dataObj = response.data.data || response.data;
        const token = dataObj.token || dataObj.accessToken || '';
        const nextRefreshToken = dataObj.refreshToken || refreshToken;
        const nextUser = {
          id: dataObj.userId || dataObj.id || user.id,
          name: dataObj.fullName || dataObj.name || '',
          email: dataObj.email || '',
          role: dataObj.role || '',
        };

        localStorage.setItem('accessToken', token);
        localStorage.setItem('refreshToken', nextRefreshToken);
        localStorage.setItem('user', JSON.stringify(nextUser));

        originalRequest.headers.Authorization = `Bearer ${token}`;
        return apiClient(originalRequest);
      } catch (refreshError) {
        clearAuthAndRedirect();
        return Promise.reject(refreshError);
      }
    }

    if (error.response?.status === 401) {
      clearAuthAndRedirect();
    }

    return Promise.reject(error);
  }
);

export default apiClient;
