import React, { useState } from 'react'
import { useMutation } from 'react-query'
import { Form, Button, Card, Alert } from 'react-bootstrap'
import { CustomersApi, Configuration, CreateCustomerRequest } from '../api/generated'

const apiConfig = new Configuration({
  basePath: ''
})
const customersApi = new CustomersApi(apiConfig)

interface CreateCustomerProps {
  onSuccess?: () => void
}

function CreateCustomer({ onSuccess }: CreateCustomerProps) {
  const [formData, setFormData] = useState<CreateCustomerRequest>({
    name: '',
    email: '',
    countryCode: 'US'
  })

  const [showSuccess, setShowSuccess] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const createCustomerMutation = useMutation(async (data: CreateCustomerRequest) => {
    return await customersApi.apiCustomersPost({ createCustomerRequest: data })
  })

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setErrorMessage(null)

    if (!formData.name?.trim() || !formData.email?.trim() || !formData.countryCode) {
      setErrorMessage('Please fill in all required fields')
      return
    }

    // Basic email validation
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    if (!emailRegex.test(formData.email)) {
      setErrorMessage('Please enter a valid email address')
      return
    }

    createCustomerMutation.mutate(formData, {
      onSuccess: () => {
        setShowSuccess(true)
        setFormData({
          name: '',
          email: '',
          countryCode: 'US'
        })
        setTimeout(() => {
          setShowSuccess(false)
          onSuccess?.()
        }, 2000)
      },
      onError: (error: any) => {
        const message = error?.message || 'Failed to create customer'
        setErrorMessage(message)
      }
    })
  }

  return (
    <div className="container-lg">
      <div className="row mb-4">
        <div className="col">
          <h1 className="mb-2">Create Customer</h1>
          <p className="text-muted">Add a new customer to the system</p>
        </div>
      </div>

      <div className="row">
        <div className="col-lg-6">
          {showSuccess && (
            <Alert variant="success" dismissible onClose={() => setShowSuccess(false)}>
              ✓ Customer created successfully!
            </Alert>
          )}

          {errorMessage && (
            <Alert variant="danger" dismissible onClose={() => setErrorMessage(null)}>
              {errorMessage}
            </Alert>
          )}

          <Card>
            <Card.Body>
              <Form onSubmit={handleSubmit}>
                <Form.Group className="mb-3">
                  <Form.Label>Name *</Form.Label>
                  <Form.Control
                    type="text"
                    placeholder="Enter customer name"
                    value={formData.name || ''}
                    onChange={(e) =>
                      setFormData({ ...formData, name: e.target.value })
                    }
                    required
                  />
                  <Form.Text className="text-muted">
                    Full name or business name
                  </Form.Text>
                </Form.Group>

                <Form.Group className="mb-3">
                  <Form.Label>Email *</Form.Label>
                  <Form.Control
                    type="email"
                    placeholder="Enter email address"
                    value={formData.email || ''}
                    onChange={(e) =>
                      setFormData({ ...formData, email: e.target.value })
                    }
                    required
                  />
                  <Form.Text className="text-muted">
                    Used for searching and identifying customers
                  </Form.Text>
                </Form.Group>

                <Form.Group className="mb-4">
                  <Form.Label>Country Code *</Form.Label>
                  <Form.Control
                    as="select"
                    value={formData.countryCode || 'US'}
                    onChange={(e) =>
                      setFormData({ ...formData, countryCode: e.target.value })
                    }
                  >
                    <option value="US">United States (US)</option>
                    <option value="CA">Canada (CA)</option>
                    <option value="GB">United Kingdom (GB)</option>
                    <option value="AU">Australia (AU)</option>
                    <option value="DE">Germany (DE)</option>
                    <option value="FR">France (FR)</option>
                    <option value="JP">Japan (JP)</option>
                    <option value="CN">China (CN)</option>
                    <option value="IN">India (IN)</option>
                  </Form.Control>
                </Form.Group>

                <Button
                  variant="primary"
                  type="submit"
                  disabled={createCustomerMutation.isLoading}
                  className="w-100"
                >
                  {createCustomerMutation.isLoading ? 'Creating...' : 'Create Customer'}
                </Button>
              </Form>
            </Card.Body>
          </Card>

          <Card className="mt-4 bg-light">
            <Card.Body>
              <p className="text-muted small mb-0">
                <strong>Next step:</strong> After creating a customer, go to "Create Order" to create an order for them using their email to search.
              </p>
            </Card.Body>
          </Card>
        </div>
      </div>
    </div>
  )
}

export default CreateCustomer
