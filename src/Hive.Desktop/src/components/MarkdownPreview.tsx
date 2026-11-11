import type { ReactNode } from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'

interface MarkdownPreviewProps {
  content: string
  /** Called with the zero-based index of a task list checkbox the reader clicked. */
  onToggleTask?: (index: number) => void
  className?: string
}

/**
 * Renders note content as markdown. Styling is written out element by element because
 * the project does not use the Tailwind typography plugin.
 */
export default function MarkdownPreview({ content, onToggleTask, className = '' }: MarkdownPreviewProps) {
  // react-markdown renders nodes in document order, so a counter incremented as each
  // checkbox is rendered gives every task list item its position in the source.
  const counter = { value: 0 }

  return (
    <div className={`text-slate-800 leading-relaxed ${className}`}>
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        components={{
          h1: ({ children }) => (
            <h1 className="text-2xl font-bold text-slate-900 mt-6 mb-3 first:mt-0">{children}</h1>
          ),
          h2: ({ children }) => (
            <h2 className="text-xl font-semibold text-slate-900 mt-5 mb-2 first:mt-0">{children}</h2>
          ),
          h3: ({ children }) => (
            <h3 className="text-lg font-semibold text-slate-900 mt-4 mb-2 first:mt-0">{children}</h3>
          ),
          h4: ({ children }) => (
            <h4 className="text-base font-semibold text-slate-900 mt-4 mb-2 first:mt-0">{children}</h4>
          ),
          p: ({ children }) => <p className="my-2">{children}</p>,
          a: ({ children, href }) => (
            <a
              href={href}
              target="_blank"
              rel="noreferrer"
              className="text-amber-600 underline underline-offset-2 hover:text-amber-700"
            >
              {children}
            </a>
          ),
          ul: ({ children }) => <ul className="my-2 ml-5 list-disc space-y-1 marker:text-slate-400">{children}</ul>,
          ol: ({ children }) => <ol className="my-2 ml-5 list-decimal space-y-1 marker:text-slate-400">{children}</ol>,
          li: ({ children, className: liClass }) => (
            <li className={liClass?.includes('task-list-item') ? 'list-none -ml-5 flex items-start gap-2' : ''}>
              {children as ReactNode}
            </li>
          ),
          input: ({ checked, type }) => {
            if (type !== 'checkbox') return null
            const index = counter.value++
            return (
              <input
                type="checkbox"
                checked={!!checked}
                onChange={() => onToggleTask?.(index)}
                disabled={!onToggleTask}
                className="mt-1 h-4 w-4 rounded border-slate-300 text-amber-500 focus:ring-amber-500"
              />
            )
          },
          blockquote: ({ children }) => (
            <blockquote className="my-3 border-l-4 border-amber-400 pl-4 italic text-slate-600">
              {children}
            </blockquote>
          ),
          code: ({ className: codeClass, children }) => {
            const isBlock = (codeClass || '').startsWith('language-')
            if (isBlock) {
              return (
                <code className="block text-sm font-mono text-slate-800">{children}</code>
              )
            }
            return (
              <code className="rounded bg-slate-100 px-1.5 py-0.5 text-sm font-mono text-pink-600">
                {children}
              </code>
            )
          },
          pre: ({ children }) => (
            <pre className="my-3 overflow-x-auto rounded-lg bg-slate-100 border border-slate-200 p-3">
              {children}
            </pre>
          ),
          hr: () => <hr className="my-5 border-slate-200" />,
          table: ({ children }) => (
            <div className="my-3 overflow-x-auto">
              <table className="min-w-full border border-slate-200 text-sm">{children}</table>
            </div>
          ),
          th: ({ children }) => (
            <th className="border border-slate-200 bg-slate-50 px-3 py-1.5 text-left font-semibold">
              {children}
            </th>
          ),
          td: ({ children }) => (
            <td className="border border-slate-200 px-3 py-1.5">{children}</td>
          ),
          strong: ({ children }) => <strong className="font-semibold text-slate-900">{children}</strong>
        }}
      >
        {content}
      </ReactMarkdown>
    </div>
  )
}
