import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from 'react-query'
import App from '../App'

// Mock fetch
global.fetch = vi.fn()

const createQueryClient = () => new QueryClient({
  defaultOptions: {
    queries: { retry: false },
  },
})

describe('App Component', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    ;(global.fetch as any).mockResolvedValue({
      ok: true,
      json: async () => []
    })
  })

  it('renders the app with navigation and orders view by default', () => {
    render(<App />)
    
    expect(screen.getByText('AMROD')).toBeInTheDocument()
    expect(screen.getByText('Orders')).toBeInTheDocument()
  })

  it('switches to create order view when button is clicked', async () => {
    const user = userEvent.setup()
    render(<App />)
    
    const createButtons = screen.getAllByRole('button', { name: /Create Order/i })
    await user.click(createButtons[1]) // Click the nav button (second one)
    
    await screen.findByText(/Add a new order to the system/)
    expect(screen.getByText(/Add a new order to the system/)).toBeInTheDocument()
  })

  it('switches back to orders view', async () => {
    const user = userEvent.setup()
    render(<App />)
    
    // First go to create order
    const createButtons = screen.getAllByRole('button', { name: /Create Order/i })
    await user.click(createButtons[1])
    
    await screen.findByText(/Add a new order to the system/)
    
    // Then click Orders
    const ordersButtons = screen.getAllByRole('button', { name: /Orders/i })
    await user.click(ordersButtons[0]) // Click the first Orders button (nav)
    
    // Should show orders heading
    await screen.findByText(/Manage your orders and track their status/)
  })

  it('renders with QueryClientProvider', () => {
    render(<App />)
    
    // If QueryClientProvider was missing, react-query hooks would error
    expect(screen.getByText('AMROD')).toBeInTheDocument()
  })

  it('navigation is properly wired', () => {
    render(<App />)
    
    const navigation = screen.getByRole('navigation')
    expect(navigation).toBeInTheDocument()
  })
})
