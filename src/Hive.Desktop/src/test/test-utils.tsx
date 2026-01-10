import { ReactElement, ReactNode } from 'react'
import { render, RenderOptions } from '@testing-library/react'
import { MemoryRouter, MemoryRouterProps } from 'react-router-dom'
import { ThemeProvider } from '../contexts/ThemeContext'

interface WrapperProps {
  children: ReactNode
}

interface CustomRenderOptions extends Omit<RenderOptions, 'wrapper'> {
  routerProps?: MemoryRouterProps
}

function AllTheProviders({ children }: WrapperProps) {
  return (
    <ThemeProvider>
      <MemoryRouter>
        {children}
      </MemoryRouter>
    </ThemeProvider>
  )
}

function createWrapper(routerProps?: MemoryRouterProps) {
  return function Wrapper({ children }: WrapperProps) {
    return (
      <ThemeProvider>
        <MemoryRouter {...routerProps}>
          {children}
        </MemoryRouter>
      </ThemeProvider>
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
export * from '@testing-library/react'
export { default as userEvent } from '@testing-library/user-event'

// Override render method
export { customRender as render }
