import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import Navbar from './Navbar';

vi.mock('../AI/AiProviderBadge', () => ({
  default: () => <span>Gemini</span>,
}));

vi.mock('../../store/authStore', () => ({
  useAuthStore: () => ({
    user: { name: 'Shadman', role: 'Student' },
    logout: vi.fn(),
  }),
}));

describe('Navbar', () => {
  it('enables the Study Planner navigation link', () => {
    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <Navbar />
      </MemoryRouter>
    );

    expect(screen.getByRole('link', { name: /Study Planner/i })).toHaveAttribute('href', '/study-planner');
    expect(screen.queryByRole('link', { name: /AI Tutor/i })).not.toBeInTheDocument();
  });
});
