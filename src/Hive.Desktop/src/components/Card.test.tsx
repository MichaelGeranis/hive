import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '../test/test-utils'
import { Card, CardHeader, CardContent, StatCard } from './Card'

describe('Card Components', () => {
  describe('Card', () => {
    it('should render children', () => {
      render(<Card>Card content</Card>)

      expect(screen.getByText('Card content')).toBeInTheDocument()
    })

    it('should apply default styles', () => {
      render(<Card><span data-testid="card-content">Content</span></Card>)

      const card = screen.getByTestId('card-content').closest('div')
      expect(card).toHaveClass('bg-white')
      expect(card).toHaveClass('rounded-xl')
      expect(card).toHaveClass('shadow-sm')
    })

    it('should apply custom className', () => {
      render(<Card className="custom-class"><span data-testid="card-content">Content</span></Card>)

      const card = screen.getByTestId('card-content').closest('div')
      expect(card).toHaveClass('custom-class')
    })
  })

  describe('CardHeader', () => {
    it('should render title', () => {
      render(<CardHeader title="Test Title" />)

      expect(screen.getByText('Test Title')).toBeInTheDocument()
    })

    it('should render subtitle when provided', () => {
      render(<CardHeader title="Title" subtitle="Subtitle text" />)

      expect(screen.getByText('Subtitle text')).toBeInTheDocument()
    })

    it('should not render subtitle when not provided', () => {
      render(<CardHeader title="Title" />)

      expect(screen.queryByText('Subtitle')).not.toBeInTheDocument()
    })

    it('should render action when provided', () => {
      render(
        <CardHeader
          title="Title"
          action={<button>Action Button</button>}
        />
      )

      expect(screen.getByText('Action Button')).toBeInTheDocument()
    })

    it('should apply correct title styles', () => {
      render(<CardHeader title="Test Title" />)

      const title = screen.getByText('Test Title')
      expect(title).toHaveClass('text-lg')
      expect(title).toHaveClass('font-semibold')
    })
  })

  describe('CardContent', () => {
    it('should render children', () => {
      render(<CardContent>Content here</CardContent>)

      expect(screen.getByText('Content here')).toBeInTheDocument()
    })

    it('should apply default padding', () => {
      render(<CardContent><span data-testid="card-inner">Content</span></CardContent>)

      const content = screen.getByTestId('card-inner').closest('div')
      expect(content).toHaveClass('px-6')
      expect(content).toHaveClass('py-4')
    })

    it('should apply custom className', () => {
      render(<CardContent className="extra-class"><span data-testid="card-inner">Content</span></CardContent>)

      const content = screen.getByTestId('card-inner').closest('div')
      expect(content).toHaveClass('extra-class')
    })
  })

  describe('StatCard', () => {
    it('should render title and value', () => {
      render(<StatCard title="Total Users" value={150} />)

      expect(screen.getByText('Total Users')).toBeInTheDocument()
      expect(screen.getByText('150')).toBeInTheDocument()
    })

    it('should render string value', () => {
      render(<StatCard title="Status" value="Active" />)

      expect(screen.getByText('Active')).toBeInTheDocument()
    })

    it('should render subtitle when provided', () => {
      render(
        <StatCard
          title="Revenue"
          value="$1,234"
          subtitle="This month"
        />
      )

      expect(screen.getByText('This month')).toBeInTheDocument()
    })

    it('should render positive trend', () => {
      render(
        <StatCard
          title="Growth"
          value={100}
          trend={{ value: 15, isPositive: true }}
        />
      )

      expect(screen.getByText('+15%')).toBeInTheDocument()
    })

    it('should render negative trend', () => {
      render(
        <StatCard
          title="Decline"
          value={50}
          trend={{ value: -10, isPositive: false }}
        />
      )

      expect(screen.getByText('-10%')).toBeInTheDocument()
    })

    it('should render icon when provided', () => {
      render(
        <StatCard
          title="With Icon"
          value={42}
          icon={<span data-testid="test-icon">Icon</span>}
        />
      )

      expect(screen.getByTestId('test-icon')).toBeInTheDocument()
    })

    it('should be clickable when onClick is provided', () => {
      const handleClick = vi.fn()

      render(
        <StatCard
          title="Clickable"
          value={10}
          onClick={handleClick}
        />
      )

      // Find and click the card
      const card = screen.getByText('Clickable').closest('div[class*="cursor-pointer"]') as HTMLElement
      expect(card).toBeInTheDocument()

      card.click()
      expect(handleClick).toHaveBeenCalledTimes(1)
    })

    it('should not have cursor-pointer class when not clickable', () => {
      render(<StatCard title="Not Clickable" value={10} />)

      const wrapper = screen.getByText('Not Clickable').closest('div')
      // Find the outermost div
      const outerDiv = wrapper?.parentElement?.parentElement?.parentElement

      expect(outerDiv?.className).not.toContain('cursor-pointer')
    })

    it('should apply correct color style for blue', () => {
      render(
        <StatCard
          title="Blue Card"
          value={10}
          icon={<span>Icon</span>}
          color="blue"
        />
      )

      const iconContainer = screen.getByText('Icon').parentElement
      expect(iconContainer).toHaveClass('bg-blue-50')
    })

    it('should apply correct color style for red', () => {
      render(
        <StatCard
          title="Red Card"
          value={10}
          icon={<span>Icon</span>}
          color="red"
        />
      )

      const iconContainer = screen.getByText('Icon').parentElement
      expect(iconContainer).toHaveClass('bg-red-50')
    })

    it('should apply correct color style for green', () => {
      render(
        <StatCard
          title="Green Card"
          value={10}
          icon={<span>Icon</span>}
          color="green"
        />
      )

      const iconContainer = screen.getByText('Icon').parentElement
      expect(iconContainer).toHaveClass('bg-green-50')
    })

    it('should default to blue color', () => {
      render(
        <StatCard
          title="Default Color"
          value={10}
          icon={<span>Icon</span>}
        />
      )

      const iconContainer = screen.getByText('Icon').parentElement
      expect(iconContainer).toHaveClass('bg-blue-50')
    })
  })
})
