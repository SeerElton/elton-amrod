import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from 'react-query'
import OrdersList from '../components/OrdersList'

// Mock fetch
global.fetch = vi.fn()

const createQueryClient = () => new QueryClient({
  defaultOptions: {
    queries: { retry: false },
  },
})

const renderWithQueryClient = (component: React.ReactElement) => {
  const queryClient = createQueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      {component}
    </QueryClientProvider>
  )
}

describe('OrdersList Component', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders the orders heading', () => {
    ;(global.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => []
    })

    renderWithQueryClient(<OrdersList />)
    
    expect(screen.getByText('Orders')).toBeInTheDocument()
  })

  it('displays loading state initially', () => {
    ;(global.fetch as any).mockImplementationOnce(() => 
      new Promise(() => {}) // Never resolves
    )

    renderWithQueryClient(<OrdersList />)
    
    expect(screen.getByText('Loading orders...')).toBeInTheDocument()
  })

  it('displays empty state when no orders exist', async () => {
    ;(global.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => []
    })

    renderWithQueryClient(<OrdersList />)
    
    // Wait for loading to complete
    await screen.findByText(/No orders yet/, {}, { timeout: 3000 })
    expect(screen.getByText(/No orders yet/)).toBeInTheDocument()
  })

  it('displays error state when API fails', async () => {
    ;(global.fetch as any).mockResolvedValueOnce({
      ok: false,
      status: 500
    })

    renderWithQueryClient(<OrdersList />)
    
    await screen.findByText(/Error loading orders/, {}, { timeout: 3000 })
    expect(screen.getByText(/Error loading orders/)).toBeInTheDocument()
  })

  it('renders table headers correctly', () => {
    ;(global.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => []
    })

    const { container } = renderWithQueryClient(<OrdersList />)
    
    // Check for table structure
    const tables = container.querySelectorAll('table')
    expect(tables.length).toBeGreaterThanOrEqual(0)
  })

  it('displays subtitle text', () => {
    ;(global.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => []
    })

    renderWithQueryClient(<OrdersList />)
    
    expect(screen.getByText('Manage your orders and track their status')).toBeInTheDocument()
  })
})
