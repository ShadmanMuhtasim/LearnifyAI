import Navbar from './Navbar';

interface LayoutProps {
  children: React.ReactNode;
}

export default function Layout({ children }: LayoutProps) {
  return (
    <div style={{ minHeight: '100vh', background: '#f5f7fa' }}>
      <Navbar />
      <main style={{ padding: '2rem 1rem' }}>
        {children}
      </main>
    </div>
  );
}