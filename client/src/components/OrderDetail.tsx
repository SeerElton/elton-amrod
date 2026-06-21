import { useState } from 'react'
import { useMutation } from 'react-query'
import { Modal, Button, Badge, Spinner, Alert } from 'react-bootstrap'
import { OrdersApi, Configuration, OrderResponse, UpdateOrderStatusRequest } from '../api/generated'

const apiConfig = new Configuration({
  basePath: ''
})
const ordersApi = new OrdersApi(apiConfig)

const VALID_TRANSITIONS: Record<string, string[]> = {
  Pending: ['Paid', 'Cancelled'],
  Paid: ['Fulfilled', 'Cancelled'],
  Fulfilled: [],
  Cancelled: []
}

interface OrderDetailProps {
  order: OrderResponse
  show: boolean
  onClose: () => void
}

function OrderDetail({ order, show, onClose }: OrderDetailProps) {
  const [newStatus, setNewStatus] = useState<string>(order.status || '')

  const updateStatusMutation = useMutation(async (status: string) => {
    const request: UpdateOrderStatusRequest = { status }
    return await ordersApi.apiOrdersIdStatusPut({ id: order.id || '', updateOrderStatusRequest: request })
  })

  const handleStatusUpdate = async () => {
    if (newStatus === order.status) {
      alert('Please select a different status')
      return
    }

    updateStatusMutation.mutate(newStatus, {
      onSuccess: () => {
        setTimeout(onClose, 1000)
      }
    })
  }

  const validTransitions = VALID_TRANSITIONS[order.status || ''] || []
  const canTransition = validTransitions.length > 0

  const getStatusBadge = (status?: string | null) => {
    const statusClass = `badge-${(status || '').toLowerCase()}`
    return <Badge className={statusClass}>{status || 'Unknown'}</Badge>
  }

  return (
    <Modal show={show} onHide={onClose} size="lg" centered>
      <Modal.Header closeButton>
        <Modal.Title>Order Details</Modal.Title>
      </Modal.Header>

      <Modal.Body>
        {updateStatusMutation.error ? (
          <Alert variant="danger">
            {updateStatusMutation.error instanceof Error
              ? updateStatusMutation.error.message
              : String(updateStatusMutation.error) || 'An error occurred'}
          </Alert>
        ) : null}

        {updateStatusMutation.isSuccess && (
          <Alert variant="success">✓ Order status updated successfully!</Alert>
        )}

        <div className="row mb-4">
          <div className="col-md-6">
            <p className="text-muted small">Order ID</p>
            <p className="font-monospace">{order.id || '-'}</p>
          </div>
          <div className="col-md-6">
            <p className="text-muted small">Customer ID</p>
            <p className="font-monospace">{order.customerId || '-'}</p>
          </div>
        </div>

        <div className="row mb-4">
          <div className="col-md-6">
            <p className="text-muted small">Status</p>
            <p>{getStatusBadge(order.status)}</p>
          </div>
          <div className="col-md-6">
            <p className="text-muted small">Amount</p>
            <p className="h5">
              {order.totalAmount} {order.currencyCode}
            </p>
          </div>
        </div>

        <div className="row mb-4">
          <div className="col">
            <p className="text-muted small">Created</p>
            <p>{order.createdAt ? new Date(order.createdAt).toLocaleString() : '-'}</p>
          </div>
        </div>

        {order.lineItems && order.lineItems.length > 0 && (
          <div className="mb-4">
            <p className="text-muted small mb-2">Line Items</p>
            <div className="table-responsive">
              <table className="table table-sm table-borderless">
                <thead>
                  <tr>
                    <th>Product SKU</th>
                    <th>Quantity</th>
                    <th>Unit Price</th>
                    <th>Subtotal</th>
                  </tr>
                </thead>
                <tbody>
                  {order.lineItems.map((item) => (
                    <tr key={item.id}>
                      <td>{item.productSku}</td>
                      <td>{item.quantity}</td>
                      <td>${(item.unitPrice || 0).toFixed(2)}</td>
                      <td>${(((item.quantity || 0) * (item.unitPrice || 0))).toFixed(2)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {canTransition && (
          <div className="mb-4 p-3 bg-light rounded">
            <label className="form-label text-muted small">Update Status</label>
            <div className="d-flex gap-2 align-items-center">
              <select
                className="form-select form-select-sm"
                value={newStatus || ''}
                onChange={(e) => setNewStatus(e.target.value)}
              >
                <option value={order.status || ''}>{order.status || 'Select status'}</option>
                {validTransitions.map((status) => (
                  <option key={status} value={status}>
                    {status}
                  </option>
                ))}
              </select>
              <Button
                variant="primary"
                size="sm"
                onClick={handleStatusUpdate}
                disabled={updateStatusMutation.isLoading || newStatus === order.status}
              >
                {updateStatusMutation.isLoading ? (
                  <Spinner animation="border" size="sm" />
                ) : (
                  'Update'
                )}
              </Button>
            </div>
            <small className="text-muted d-block mt-2">
              Valid transitions from {order.status}: {validTransitions.join(', ')}
            </small>
          </div>
        )}

        {!canTransition && (
          <Alert variant="info">
            This order is in a final state ({order.status}) and cannot be updated.
          </Alert>
        )}
      </Modal.Body>

      <Modal.Footer>
        <Button variant="secondary" onClick={onClose}>
          Close
        </Button>
      </Modal.Footer>
    </Modal>
  )
}

export default OrderDetail
