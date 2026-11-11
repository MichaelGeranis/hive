import { ReactElement, ReactNode } from 'react'
import { render, RenderOptions } from '@testing-library/react'
import { MemoryRouter, MemoryRouterProps } from 'react-router-dom'
import { ConfirmProvider } from '../contexts/ConfirmContext'

interface WrapperProps {
  children: ReactNode
}

interface CustomRenderOptions extends Omit<RenderOptions, 'wrapper'> {
  routerProps?: MemoryRouterProps
}

// eslint-disable-next-line react-refresh/only-export-components -- test helper, not a hot-reloaded module
function AllTheProviders({ children }: WrapperProps) {
  return (
    <MemoryRouter>
      <ConfirmProvider>{children}</ConfirmProvider>
    </MemoryRouter>
  )
}

function createWrapper(routerProps?: MemoryRouterProps) {
  return function Wrapper({ children }: WrapperProps) {
    return (
      <MemoryRouter {...routerProps}>
        <ConfirmProvider>{children}</ConfirmProvider>
      </MemoryRouter>
    )
  }
}

function customRender(
  ui: ReactElement,
  options?: CustomRenderOptions
) {
  const { routerProps, ...renderOptions } = options || {}
  return render(ui, {
    wrapper: routerProps ? createWrapper(routerProps) : AllTheProviders,
    ...renderOptions,
  })
}

// Re-export everything from testing-library
// eslint-disable-next-line react-refresh/only-export-components -- test helper, not a hot-reloaded module
export * from '@testing-library/react'
export { default as userEvent } from '@testing-library/user-event'

// Override render method
export { customRender as render }
