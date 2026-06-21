import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from 'react-query'
import CreateOrder from '../components/CreateOrder'

// Mock the generated API
vi.mock('../api/generated', () => ({
  OrdersApi: vi.fn(),
  CustomersApi: vi.fn(),
  Configuration: vi.fn(),
}))

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

describe('CreateOrder Component', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders the create order form', () => {
    renderWithQueryClient(<CreateOrder />)
    
    expect(screen.getByText('Create Order')).toBeInTheDocument()
    expect(screen.getByText('Add a new order to the system')).toBeInTheDocument()
  })

  it('renders customer input field', () => {
    renderWithQueryClient(<CreateOrder />)
    
    const customerInput = screen.getByPlaceholderText(/Search by email or name/i)
    expect(customerInput).toBeInTheDocument()
  })

  it('renders currency dropdown', () => {
    renderWithQueryClient(<CreateOrder />)
    
    const currencySelects = screen.getAllByDisplayValue('USD')
    expect(currencySelects.length).toBeGreaterThan(0)
  })

  it('renders product SKU label', () => {
    renderWithQueryClient(<CreateOrder />)
    
    expect(screen.getByText('Product SKU')).toBeInTheDocument()
  })

  it('renders quantity label', () => {
    renderWithQueryClient(<CreateOrder />)
    
    expect(screen.getByText('Quantity')).toBeInTheDocument()
  })

  it('renders unit price label', () => {
    renderWithQueryClient(<CreateOrder />)
    
    expect(screen.getByText('Unit Price')).toBeInTheDocument()
  })

  it('renders total amount field', () => {
    renderWithQueryClient(<CreateOrder />)
    
    const totalAmountLabels = screen.getAllByText('Total Amount')
    expect(totalAmountLabels.length).toBeGreaterThan(0)
  })

  it('renders submit button', () => {
    renderWithQueryClient(<CreateOrder onSuccess={() => {}} />)
    
    const submitButtons = screen.getAllByRole('button', { name: /submit/i })
    expect(submitButtons.length).toBeGreaterThan(0)
  })
})
