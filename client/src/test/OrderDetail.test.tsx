import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import OrderDetail from '../components/OrderDetail'
import { OrderResponse } from '../types'

global.fetch = vi.fn()

describe('OrderDetail Component', () => {
  const mockOrder: OrderResponse = {
    id: 'test-order-123',
    customerId: 'customer-456',
    status: 'Pending',
    currencyCode: 'USD',
    totalAmount: 150.00,
    createdAt: new Date('2026-01-15T10:30:00Z'),
    lineItems: [
      {
        id: 'item-1',
        productSku: 'WIDGET-001',
        quantity: 2,
        unitPrice: 75.00
      }
    ]
  }

  const mockOnClose = vi.fn()

  it('renders order details modal', () => {
    render(
      <OrderDetail 
        order={mockOrder} 
        show={true} 
        onClose={mockOnClose}
      />
    )
    
    expect(screen.getByText('Order Details')).toBeInTheDocument()
  })

  it('displays order information', () => {
    render(
      <OrderDetail 
        order={mockOrder} 
        show={true} 
        onClose={mockOnClose}
      />
    )
    
    expect(screen.getByText('Order ID')).toBeInTheDocument()
    expect(screen.getByText('Customer ID')).toBeInTheDocument()
    expect(screen.getByText('Status')).toBeInTheDocument()
    expect(screen.getByText('Amount')).toBeInTheDocument()
  })

  it('displays status badge with correct class', () => {
    const { container } = render(
      <OrderDetail 
        order={mockOrder} 
        show={true} 
        onClose={mockOnClose}
      />
    )
    
    const badge = container.querySelector('.badge-pending')
    expect(badge).toBeInTheDocument()
  })

  it('displays line items in table', () => {
    const { container } = render(
      <OrderDetail 
        order={mockOrder} 
        show={true} 
        onClose={mockOnClose}
      />
    )
    
    expect(screen.getByText('WIDGET-001')).toBeInTheDocument()
  })

  it('shows valid transitions for Pending status', () => {
    render(
      <OrderDetail 
        order={mockOrder} 
        show={true} 
        onClose={mockOnClose}
      />
    )
    
    expect(screen.getByText(/Valid transitions/)).toBeInTheDocument()
  })

  it('shows read-only state for Fulfilled orders', () => {
    const fulfilledOrder: OrderResponse = {
      ...mockOrder,
      status: 'Fulfilled'
    }

    render(
      <OrderDetail 
        order={fulfilledOrder} 
        show={true} 
        onClose={mockOnClose}
      />
    )
    
    const alert = screen.getByText(/final state/)
    expect(alert).toBeInTheDocument()
  })

  it('closes modal when close button is clicked', async () => {
    const user = userEvent.setup()
    render(
      <OrderDetail 
        order={mockOrder} 
        show={true} 
        onClose={mockOnClose}
      />
    )
    
    const closeButton = screen.getByRole('button', { name: /Close/i })
    await user.click(closeButton)
    
    expect(mockOnClose).toHaveBeenCalled()
  })

  it('displays formatted date', () => {
    render(
      <OrderDetail 
        order={mockOrder} 
        show={true} 
        onClose={mockOnClose}
      />
    )
    
    const dateText = screen.getByText(/Jan 15, 2026/)
    expect(dateText).toBeInTheDocument()
  })

  it('displays currency with total amount', () => {
    render(
      <OrderDetail 
        order={mockOrder} 
        show={true} 
        onClose={mockOnClose}
      />
    )
    
    expect(screen.getByText(/150 USD/)).toBeInTheDocument()
  })
})
