import { useState } from 'react'
import { useQuery } from 'react-query'
import { Spinner, Badge, Button } from 'react-bootstrap'
import { OrdersApi, Configuration, OrderResponse } from '../api/generated'
import OrderDetail from './OrderDetail'

const apiConfig = new Configuration({
  basePath: ''
})
const ordersApi = new OrdersApi(apiConfig)

function OrdersList() {
  const [selectedOrder, setSelectedOrder] = useState<OrderResponse | null>(null)
  const [showDetail, setShowDetail] = useState(false)

  const { data: orders, isLoading, error, refetch } = useQuery('orders', async () => {
    try {
      return await ordersApi.apiOrdersGet()
    } catch (err) {
      console.error('Error fetching orders:', err)
      return []
    }
  })

  const handleViewOrder = (order: OrderResponse) => {
    setSelectedOrder(order)
    setShowDetail(true)
  }

  const handleCloseDetail = () => {
    setShowDetail(false)
    setSelectedOrder(null)
    refetch()
  }

  const getStatusBadge = (status?: string | null) => {
    const statusClass = `badge-${(status || '').toLowerCase()}`
    return <Badge className={statusClass}>{status || 'Unknown'}</Badge>
  }

  if (isLoading) {
    return (
      <div className="container-lg">
        <div className="text-center py-5">
          <Spinner animation="border" className="mb-3" />
          <p className="text-muted">Loading orders...</p>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="container-lg">
        <div className="alert alert-danger">
          Error loading orders. Make sure the API is running at http://localhost:5063
        </div>
      </div>
    )
  }

  return (
    <div className="container-lg">
      <div className="row mb-4">
        <div className="col">
          <h1 className="mb-2">Orders</h1>
          <p className="text-muted">Manage your orders and track their status</p>
        </div>
      </div>

      {orders && orders.length === 0 ? (
        <div className="row">
          <div className="col">
            <div className="card">
              <div className="card-body text-center py-5">
                <p className="text-muted">No orders yet. Create your first order!</p>
              </div>
            </div>
          </div>
        </div>
      ) : (
        <div className="row">
          <div className="col">
            <div className="card">
              <div className="table-responsive">
                <table className="table table-hover mb-0">
                  <thead>
                    <tr>
                      <th>Order ID</th>
                      <th>Customer</th>
                      <th>Status</th>
                      <th>Amount</th>
                      <th>Created</th>
                      <th>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {orders?.map((order) => (
                      <tr key={order.id}>
                        <td className="font-monospace small">{(order.id || '').substring(0, 8)}...</td>
                        <td>{order.customerName || 'Unknown'}</td>
                        <td>{getStatusBadge(order.status)}</td>
                        <td>
                          {order.totalAmount} {order.currencyCode}
                        </td>
                        <td>{order.createdAt ? new Date(order.createdAt).toLocaleDateString() : '-'}</td>
                        <td>
                          <Button
                            variant="sm"
                            onClick={() => handleViewOrder(order)}
                            className="btn-sm"
                          >
                            View
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>
      )}

      {selectedOrder && (
        <OrderDetail order={selectedOrder} show={showDetail} onClose={handleCloseDetail} />
      )}
    </div>
  )
}

export default OrdersList
