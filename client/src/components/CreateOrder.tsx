import React, { useState } from 'react'
import { useMutation, useQuery } from 'react-query'
import { Form, Button, Card, Alert, Spinner, ListGroup } from 'react-bootstrap'
import { OrdersApi, CustomersApi, Configuration, CreateOrderRequest, OrderLineItemRequest, CustomerResponse } from '../api/generated'

const apiConfig = new Configuration({
  basePath: ''
})
const ordersApi = new OrdersApi(apiConfig)
const customersApi = new CustomersApi(apiConfig)

interface CreateOrderProps {
  onSuccess?: () => void
}

function CreateOrder({ onSuccess }: CreateOrderProps) {
  const [customerSearchQuery, setCustomerSearchQuery] = useState('')
  const [selectedCustomer, setSelectedCustomer] = useState<CustomerResponse | null>(null)
  const [showCustomerDropdown, setShowCustomerDropdown] = useState(false)

  const { data: searchResults } = useQuery(
    ['customers', customerSearchQuery],
    async () => {
      if (!customerSearchQuery.trim()) return []
      try {
        return await customersApi.apiCustomersSearchGet({ query: customerSearchQuery })
      } catch (err) {
        console.error('Error searching customers:', err)
        return []
      }
    },
    { enabled: customerSearchQuery.length > 0 }
  )

  const [formData, setFormData] = useState<CreateOrderRequest>({
    customerId: '',
    currencyCode: 'USD',
    totalAmount: 0,
    lineItems: []
  })

  const [lineItem, setLineItem] = useState<OrderLineItemRequest>({
    productSku: '',
    quantity: 1,
    unitPrice: 0
  })

  const [showSuccess, setShowSuccess] = useState(false)

  const createOrderMutation = useMutation(async (data: CreateOrderRequest) => {
    return await ordersApi.apiOrdersPost({ createOrderRequest: data })
  })

  const handleSelectCustomer = (customer: CustomerResponse) => {
    setSelectedCustomer(customer)
    setFormData({ ...formData, customerId: customer.id || '' })
    setCustomerSearchQuery('')
    setShowCustomerDropdown(false)
  }

  const handleAddLineItem = () => {
    if (!lineItem.productSku || !lineItem.quantity || !lineItem.unitPrice) {
      alert('Please fill in all line item fields')
      return
    }

    const newLineItems = [...(formData.lineItems || []), lineItem]
    const newTotal = newLineItems.reduce((sum, item) => sum + (item.quantity || 0) * (item.unitPrice || 0), 0)

    setFormData({
      ...formData,
      lineItems: newLineItems,
      totalAmount: newTotal
    })

    setLineItem({
      productSku: '',
      quantity: 1,
      unitPrice: 0
    })
  }

  const handleRemoveLineItem = (index: number) => {
    const newLineItems = formData.lineItems?.filter((_, i) => i !== index) || []
    const newTotal = newLineItems.reduce((sum, item) => sum + (item.quantity || 0) * (item.unitPrice || 0), 0)

    setFormData({
      ...formData,
      lineItems: newLineItems,
      totalAmount: newTotal
    })
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!selectedCustomer || !formData.currencyCode || !formData.totalAmount) {
      alert('Please select a customer, currency, and add line items')
      return
    }

    createOrderMutation.mutate(formData, {
      onSuccess: () => {
        setShowSuccess(true)
        setSelectedCustomer(null)
        setFormData({
          customerId: '',
          currencyCode: 'USD',
          totalAmount: 0,
          lineItems: []
        })
        setTimeout(() => {
          setShowSuccess(false)
          onSuccess?.()
        }, 2000)
      }
    })
  }

  return (
    <div className="container-lg">
      <div className="row mb-4">
        <div className="col">
          <h1 className="mb-2">Create Order</h1>
          <p className="text-muted">Add a new order to the system</p>
        </div>
      </div>

      <div className="row">
        <div className="col-lg-8">
          {showSuccess && (
            <Alert variant="success" dismissible onClose={() => setShowSuccess(false)}>
              ✓ Order created successfully!
            </Alert>
          )}

          <Card>
            <Card.Body>
              <Form onSubmit={handleSubmit}>
                <Form.Group className="mb-3">
                  <Form.Label>Customer *</Form.Label>
                  {selectedCustomer ? (
                    <div className="d-flex align-items-center gap-2">
                      <div className="flex-grow-1">
                        <Form.Control disabled value={`${selectedCustomer.name} (${selectedCustomer.email})`} />
                      </div>
                      <Button variant="outline-secondary" size="sm" onClick={() => setSelectedCustomer(null)}>
                        Change
                      </Button>
                    </div>
                  ) : (
                    <div className="position-relative">
                      <Form.Control
                        type="text"
                        placeholder="Search by email or name..."
                        value={customerSearchQuery}
                        onChange={(e) => {
                          setCustomerSearchQuery(e.target.value)
                          setShowCustomerDropdown(true)
                        }}
                        onFocus={() => setShowCustomerDropdown(true)}
                      />
                      {showCustomerDropdown && customerSearchQuery && searchResults && searchResults.length > 0 && (
                        <ListGroup className="position-absolute w-100 mt-1" style={{ zIndex: 1000 }}>
                          {searchResults.map((customer) => (
                            <ListGroup.Item
                              key={customer.id}
                              action
                              onClick={() => handleSelectCustomer(customer)}
                            >
                              <div className="d-flex justify-content-between">
                                <strong>{customer.name}</strong>
                                <span className="text-muted">{customer.email}</span>
                              </div>
                            </ListGroup.Item>
                          ))}
                        </ListGroup>
                      )}
                      {showCustomerDropdown && customerSearchQuery && (!searchResults || searchResults.length === 0) && (
                        <ListGroup className="position-absolute w-100 mt-1" style={{ zIndex: 1000 }}>
                          <ListGroup.Item disabled>No customers found</ListGroup.Item>
                        </ListGroup>
                      )}
                    </div>
                  )}
                </Form.Group>

                <Form.Group className="mb-3">
                  <Form.Label>Currency Code *</Form.Label>
                  <Form.Control
                    as="select"
                    value={formData.currencyCode}
                    onChange={(e) =>
                      setFormData({ ...formData, currencyCode: e.target.value })
                    }
                  >
                    <option>USD</option>
                    <option>EUR</option>
                    <option>GBP</option>
                    <option>JPY</option>
                    <option>AUD</option>
                  </Form.Control>
                </Form.Group>

                <div className="card card-light mb-4 p-3">
                  <h5 className="mb-3">Line Items</h5>

                  {formData.lineItems && formData.lineItems.length > 0 && (
                    <div className="mb-3">
                      <table className="table table-sm">
                        <thead>
                          <tr>
                            <th>Product SKU</th>
                            <th>Quantity</th>
                            <th>Unit Price</th>
                            <th>Subtotal</th>
                            <th></th>
                          </tr>
                        </thead>
                        <tbody>
                          {formData.lineItems.map((item, idx) => (
                            <tr key={idx}>
                              <td>{item.productSku}</td>
                              <td>{item.quantity || 1}</td>
                              <td>${(item.unitPrice || 0).toFixed(2)}</td>
                              <td>${((item.quantity || 1) * (item.unitPrice || 0)).toFixed(2)}</td>
                              <td>
                                <Button
                                  variant="sm"
                                  size="sm"
                                  onClick={() => handleRemoveLineItem(idx)}
                                  className="btn-outline-danger"
                                >
                                  ✕
                                </Button>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}

                  <div className="row g-2 mb-3">
                    <div className="col-md-5">
                      <Form.Label className="d-block small mb-1 text-muted">Product SKU</Form.Label>
                      <Form.Control
                        placeholder="e.g., WIDGET-001"
                        value={lineItem.productSku}
                        onChange={(e) =>
                          setLineItem({ ...lineItem, productSku: e.target.value })
                        }
                      />
                    </div>
                    <div className="col-md-3">
                      <Form.Label className="d-block small mb-1 text-muted">Quantity</Form.Label>
                      <Form.Control
                        type="number"
                        placeholder="Qty"
                        min="1"
                        value={lineItem.quantity}
                        onChange={(e) =>
                          setLineItem({
                            ...lineItem,
                            quantity: parseInt(e.target.value) || 1
                          })
                        }
                      />
                    </div>
                    <div className="col-md-3">
                      <Form.Label className="d-block small mb-1 text-muted">Unit Price</Form.Label>
                      <Form.Control
                        type="number"
                        placeholder="Price"
                        step="0.01"
                        min="0"
                        value={lineItem.unitPrice}
                        onChange={(e) =>
                          setLineItem({
                            ...lineItem,
                            unitPrice: parseFloat(e.target.value) || 0
                          })
                        }
                      />
                    </div>
                    <div className="col-md-1">
                      <Form.Label className="d-block small mb-1 text-muted">&nbsp;</Form.Label>
                      <Button
                        variant="secondary"
                        size="sm"
                        onClick={handleAddLineItem}
                        className="w-100"
                      >
                        Add
                      </Button>
                    </div>
                  </div>
                </div>

                <Form.Group className="mb-4">
                  <Form.Label>Total Amount</Form.Label>
                  <Form.Control
                    type="number"
                    value={formData.totalAmount}
                    onChange={(e) =>
                      setFormData({
                        ...formData,
                        totalAmount: parseFloat(e.target.value) || 0
                      })
                    }
                    step="0.01"
                    min="0"
                    disabled
                    className="bg-light"
                  />
                  <small className="text-muted">Auto-calculated from line items</small>
                </Form.Group>

                <div className="d-flex gap-2">
                  <Button
                    variant="primary"
                    type="submit"
                    disabled={createOrderMutation.isLoading}
                  >
                    {createOrderMutation.isLoading ? (
                      <>
                        <Spinner animation="border" size="sm" className="me-2" />
                        Creating...
                      </>
                    ) : (
                      'Create Order'
                    )}
                  </Button>
                </div>

                {createOrderMutation.error ? (
                  <Alert variant="danger" className="mt-3">
                    {createOrderMutation.error instanceof Error
                      ? createOrderMutation.error.message
                      : String(createOrderMutation.error) || 'An error occurred'}
                  </Alert>
                ) : null}
              </Form>
            </Card.Body>
          </Card>
        </div>
      </div>
    </div>
  )
}

export default CreateOrder
