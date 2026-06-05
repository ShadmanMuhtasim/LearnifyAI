import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import Navbar from './Navbar';
import { useAuthStore } from '../../store/authStore';

vi.mock('../AI/AiProviderBadge', () => ({
  default: () => <span>Gemini</span>,
}));

describe('Navbar', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({
      user: { id: 'user-1', name: 'Test User', email: 'test@example.com', role: 'Student' },
      isAuthenticated: true,
      isLoading: false,
      error: null,
    });
  });

  it('enables analytics and achievements while keeping future items disabled', () => {
    render(
      <MemoryRouter initialEntries={['/analytics']}>
        <Navbar />
      </MemoryRouter>
    );

    expect(screen.getByRole('link', { name: /analytics/i })).toHaveAttribute('href', '/analytics');
    expect(screen.getByRole('link', { name: /achievements/i })).toHaveAttribute('href', '/achievements');
    expect(screen.getByText('AI Tutor').closest('.sidebar-link')).toHaveAttribute('aria-disabled', 'true');
    expect(screen.getByText('Study Planner').closest('.sidebar-link')).toHaveAttribute('aria-disabled', 'true');
  });
});
