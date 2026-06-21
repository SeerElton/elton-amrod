import { useState } from 'react'
import { QueryClient, QueryClientProvider } from 'react-query'
import Navigation from './components/Navigation'
import OrdersList from './components/OrdersList'
import CreateOrder from './components/CreateOrder'
import CreateCustomer from './components/CreateCustomer'
import './App.css'

const queryClient = new QueryClient()

function App() {
  const [activeView, setActiveView] = useState<'list' | 'create' | 'create-customer'>('list')

  const handleOrderCreated = () => {
    setActiveView('list')
  }

  const handleCustomerCreated = () => {
    setActiveView('create')
  }

  return (
    <QueryClientProvider client={queryClient}>
      <Navigation onViewChange={setActiveView} />
      <main className="main-container">
        {activeView === 'list' && <OrdersList />}
        {activeView === 'create' && <CreateOrder onSuccess={handleOrderCreated} />}
        {activeView === 'create-customer' && <CreateCustomer onSuccess={handleCustomerCreated} />}
      </main>
    </QueryClientProvider>
  )
}

export default App
