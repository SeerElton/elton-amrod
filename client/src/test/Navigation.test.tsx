import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import Navigation from '../components/Navigation'

describe('Navigation Component', () => {
  it('renders the brand name', () => {
    const mockOnViewChange = vi.fn()
    render(<Navigation onViewChange={mockOnViewChange} />)
    
    expect(screen.getByText('AMROD')).toBeInTheDocument()
  })

  it('renders navigation buttons', () => {
    const mockOnViewChange = vi.fn()
    render(<Navigation onViewChange={mockOnViewChange} />)
    
    expect(screen.getByText('Orders')).toBeInTheDocument()
    expect(screen.getByText('Create Order')).toBeInTheDocument()
  })

  it('calls onViewChange with "list" when Orders button is clicked', async () => {
    const user = userEvent.setup()
    const mockOnViewChange = vi.fn()
    render(<Navigation onViewChange={mockOnViewChange} />)
    
    const ordersButton = screen.getByRole('button', { name: /Orders/i })
    await user.click(ordersButton)
    
    expect(mockOnViewChange).toHaveBeenCalledWith('list')
  })

  it('calls onViewChange with "create" when Create Order button is clicked', async () => {
    const user = userEvent.setup()
    const mockOnViewChange = vi.fn()
    render(<Navigation onViewChange={mockOnViewChange} />)
    
    const createButton = screen.getAllByRole('button', { name: /Create Order/i })[1]
    await user.click(createButton)
    
    expect(mockOnViewChange).toHaveBeenCalledWith('create')
  })

  it('has proper styling classes', () => {
    const mockOnViewChange = vi.fn()
    const { container } = render(<Navigation onViewChange={mockOnViewChange} />)
    
    const navbar = container.querySelector('nav')
    expect(navbar).toHaveClass('navbar')
    expect(navbar).toHaveClass('navbar-expand-lg')
  })
})
