

interface NavigationProps {
  onViewChange: (view: 'list' | 'create' | 'create-customer') => void
}

function Navigation({ onViewChange }: NavigationProps) {
  return (
    <nav className="navbar navbar-expand-lg sticky-top">
      <div className="container-fluid">
        <a className="navbar-brand" href="#">
          AMROD
        </a>
        <button
          className="navbar-toggler"
          type="button"
          data-bs-toggle="collapse"
          data-bs-target="#navbarNav"
          aria-controls="navbarNav"
          aria-expanded="false"
          aria-label="Toggle navigation"
        >
          <span className="navbar-toggler-icon"></span>
        </button>
        <div className="collapse navbar-collapse" id="navbarNav">
          <ul className="navbar-nav ms-auto">
            <li className="nav-item">
              <button 
                className="nav-link btn btn-link" 
                onClick={() => onViewChange('list')}
              >
                Orders
              </button>
            </li>
            <li className="nav-item">
              <button 
                className="nav-link btn btn-link" 
                onClick={() => onViewChange('create-customer')}
              >
                Create Customer
              </button>
            </li>
            <li className="nav-item">
              <button 
                className="nav-link btn btn-link" 
                onClick={() => onViewChange('create')}
              >
                Create Order
              </button>
            </li>
          </ul>
        </div>
      </div>
    </nav>
  )
}

export default Navigation
