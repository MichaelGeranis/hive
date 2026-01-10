import { useEffect, useState, useCallback } from 'react'
import { Plus, FileText, Check, X, ChevronDown, ChevronUp, Edit, Trash2, CheckCircle, XCircle, ClipboardList, Star, HelpCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { checklistTemplatesApi, checklistInstancesApi } from '../services/api'
import { ChecklistType, ChecklistItemType, ChecklistItemStatus, ChecklistInstanceStatus } from '../types'
import type {
  ChecklistTemplate,
  ChecklistTemplateWithItems,
  ChecklistInstance,
  ChecklistInstanceWithItems,
  CreateChecklistTemplateDto,
  CreateChecklistTemplateItemDto,
  CreateInterviewInstanceDto,
  CreateOnboardingInstanceDto
} from '../types'
import { useEscapeKey } from '../hooks/useEscapeKey'

const typeLabels: Record<ChecklistType, string> = {
  [ChecklistType.Interview]: 'Interview',
  [ChecklistType.Onboarding]: 'Onboarding'
}

const itemTypeLabels: Record<ChecklistItemType, string> = {
  [ChecklistItemType.Question]: 'Question',
  [ChecklistItemType.Topic]: 'Topic',
  [ChecklistItemType.Task]: 'Task',
  [ChecklistItemType.Document]: 'Document',
  [ChecklistItemType.Training]: 'Training'
}

const itemTypeColors: Record<ChecklistItemType, string> = {
  [ChecklistItemType.Question]: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
  [ChecklistItemType.Topic]: 'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400',
  [ChecklistItemType.Task]: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400',
  [ChecklistItemType.Document]: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
  [ChecklistItemType.Training]: 'bg-indigo-100 text-indigo-700 dark:bg-indigo-900/30 dark:text-indigo-400'
}

const instanceStatusColors: Record<ChecklistInstanceStatus, string> = {
  [ChecklistInstanceStatus.NotStarted]: 'bg-slate-100 text-slate-700 dark:bg-slate-700 dark:text-slate-300',
  [ChecklistInstanceStatus.InProgress]: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
  [ChecklistInstanceStatus.Completed]: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400',
  [ChecklistInstanceStatus.Cancelled]: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400'
}

type Tab = 'templates' | 'instances'

export default function Checklists() {
  const [activeTab, setActiveTab] = useState<Tab>('templates')
  const [loading, setLoading] = useState(true)

  // Templates state
  const [templates, setTemplates] = useState<ChecklistTemplate[]>([])
  const [expandedTemplates, setExpandedTemplates] = useState<Set<string>>(new Set())
  const [templateDetails, setTemplateDetails] = useState<Record<string, ChecklistTemplateWithItems>>({})
  const [showTemplateForm, setShowTemplateForm] = useState(false)
  const [editingTemplateId, setEditingTemplateId] = useState<string | null>(null)
  const [templateFormData, setTemplateFormData] = useState<CreateChecklistTemplateDto>({
    name: '',
    description: '',
    type: ChecklistType.Interview
  })

  // Template items state
  const [showItemForm, setShowItemForm] = useState(false)
  const [currentTemplateId, setCurrentTemplateId] = useState<string | null>(null)
  const [itemFormData, setItemFormData] = useState<CreateChecklistTemplateItemDto>({
    content: '',
    itemType: ChecklistItemType.Question,
    isRequired: true,
    helpText: '',
    estimatedMinutes: undefined
  })

  // Instances state
  const [instances, setInstances] = useState<ChecklistInstance[]>([])
  const [expandedInstances, setExpandedInstances] = useState<Set<string>>(new Set())
  const [instanceDetails, setInstanceDetails] = useState<Record<string, ChecklistInstanceWithItems>>({})
  const [showInstanceForm, setShowInstanceForm] = useState(false)
  const [instanceType, setInstanceType] = useState<ChecklistType>(ChecklistType.Interview)
  const [interviewFormData, setInterviewFormData] = useState<CreateInterviewInstanceDto>({
    templateId: '',
    title: '',
    candidateName: '',
    position: '',
    interviewDate: new Date().toISOString().split('T')[0]
  })
  const [onboardingFormData, setOnboardingFormData] = useState<CreateOnboardingInstanceDto>({
    templateId: '',
    title: '',
    newHireName: '',
    startDate: new Date().toISOString().split('T')[0],
    targetCompletionDate: undefined
  })

  // Filters
  const [typeFilter, setTypeFilter] = useState<ChecklistType | 'all'>('all')
  const [statusFilter, setStatusFilter] = useState<ChecklistInstanceStatus | 'all'>('all')

  const closeTemplateModal = useCallback(() => {
    setShowTemplateForm(false)
    setEditingTemplateId(null)
    setTemplateFormData({ name: '', description: '', type: ChecklistType.Interview })
  }, [])

  const closeItemModal = useCallback(() => {
    setShowItemForm(false)
    setCurrentTemplateId(null)
    setItemFormData({ content: '', itemType: ChecklistItemType.Question, isRequired: true, helpText: '', estimatedMinutes: undefined })
  }, [])

  const closeInstanceModal = useCallback(() => {
    setShowInstanceForm(false)
    setInterviewFormData({ templateId: '', title: '', candidateName: '', position: '', interviewDate: new Date().toISOString().split('T')[0] })
    setOnboardingFormData({ templateId: '', title: '', newHireName: '', startDate: new Date().toISOString().split('T')[0], targetCompletionDate: undefined })
  }, [])

  useEscapeKey(closeTemplateModal, showTemplateForm)
  useEscapeKey(closeItemModal, showItemForm)
  useEscapeKey(closeInstanceModal, showInstanceForm)

  useEffect(() => {
    loadData()
  }, [])

  const loadData = async () => {
    try {
      setLoading(true)
      const [templatesData, instancesData] = await Promise.all([
        checklistTemplatesApi.getAll(),
        checklistInstancesApi.getAll()
      ])
      setTemplates(templatesData)
      setInstances(instancesData)
    } catch (err) {
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  const loadTemplates = async () => {
    try {
      const data = await checklistTemplatesApi.getAll()
      setTemplates(data)
    } catch (err) {
      console.error(err)
    }
  }

  const loadInstances = async () => {
    try {
      const data = await checklistInstancesApi.getAll()
      setInstances(data)
    } catch (err) {
      console.error(err)
    }
  }

  const loadTemplateDetails = async (templateId: string) => {
    try {
      const data = await checklistTemplatesApi.getWithItems(templateId)
      setTemplateDetails(prev => ({ ...prev, [templateId]: data }))
    } catch (err) {
      console.error(err)
    }
  }

  const loadInstanceDetails = async (instanceId: string) => {
    try {
      const data = await checklistInstancesApi.getWithItems(instanceId)
      setInstanceDetails(prev => ({ ...prev, [instanceId]: data }))
    } catch (err) {
      console.error(err)
    }
  }

  const toggleTemplateExpand = (templateId: string) => {
    setExpandedTemplates(prev => {
      const newSet = new Set(prev)
      if (newSet.has(templateId)) {
        newSet.delete(templateId)
      } else {
        newSet.add(templateId)
        if (!templateDetails[templateId]) {
          loadTemplateDetails(templateId)
        }
      }
      return newSet
    })
  }

  const toggleInstanceExpand = (instanceId: string) => {
    setExpandedInstances(prev => {
      const newSet = new Set(prev)
      if (newSet.has(instanceId)) {
        newSet.delete(instanceId)
      } else {
        newSet.add(instanceId)
        if (!instanceDetails[instanceId]) {
          loadInstanceDetails(instanceId)
        }
      }
      return newSet
    })
  }

  // Template CRUD
  const handleCreateTemplate = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      await checklistTemplatesApi.create(templateFormData)
      closeTemplateModal()
      loadTemplates()
    } catch (err) {
      console.error(err)
    }
  }

  const handleUpdateTemplate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!editingTemplateId) return
    try {
      await checklistTemplatesApi.update(editingTemplateId, {
        name: templateFormData.name,
        description: templateFormData.description
      })
      closeTemplateModal()
      loadTemplates()
    } catch (err) {
      console.error(err)
    }
  }

  const handleDeleteTemplate = async (id: string) => {
    if (!confirm('Are you sure you want to delete this template?')) return
    try {
      await checklistTemplatesApi.delete(id)
      loadTemplates()
    } catch (err) {
      console.error(err)
    }
  }

  const handleToggleTemplateActive = async (template: ChecklistTemplate) => {
    try {
      if (template.isActive) {
        await checklistTemplatesApi.deactivate(template.id)
      } else {
        await checklistTemplatesApi.activate(template.id)
      }
      loadTemplates()
    } catch (err) {
      console.error(err)
    }
  }

  // Template Item CRUD
  const handleAddItem = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!currentTemplateId) return
    try {
      await checklistTemplatesApi.addItem(currentTemplateId, itemFormData)
      closeItemModal()
      loadTemplateDetails(currentTemplateId)
      loadTemplates()
    } catch (err) {
      console.error(err)
    }
  }

  const handleDeleteItem = async (itemId: string, templateId: string) => {
    try {
      await checklistTemplatesApi.deleteItem(itemId)
      loadTemplateDetails(templateId)
      loadTemplates()
    } catch (err) {
      console.error(err)
    }
  }

  // Instance CRUD
  const handleCreateInstance = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      if (instanceType === ChecklistType.Interview) {
        await checklistInstancesApi.createInterview(interviewFormData)
      } else {
        await checklistInstancesApi.createOnboarding(onboardingFormData)
      }
      closeInstanceModal()
      loadInstances()
    } catch (err) {
      console.error(err)
    }
  }

  const handleCompleteInstance = async (id: string) => {
    try {
      await checklistInstancesApi.complete(id)
      loadInstances()
      if (instanceDetails[id]) {
        loadInstanceDetails(id)
      }
    } catch (err) {
      console.error(err)
    }
  }

  const handleCancelInstance = async (id: string) => {
    if (!confirm('Are you sure you want to cancel this checklist?')) return
    try {
      await checklistInstancesApi.cancel(id)
      loadInstances()
      if (instanceDetails[id]) {
        loadInstanceDetails(id)
      }
    } catch (err) {
      console.error(err)
    }
  }

  const handleDeleteInstance = async (id: string) => {
    if (!confirm('Are you sure you want to delete this checklist?')) return
    try {
      await checklistInstancesApi.delete(id)
      loadInstances()
    } catch (err) {
      console.error(err)
    }
  }

  // Instance Item Actions
  const handleCompleteItem = async (itemId: string, instanceId: string, notes?: string, score?: number) => {
    try {
      await checklistInstancesApi.completeItem(itemId, { notes, score })
      loadInstanceDetails(instanceId)
      loadInstances()
    } catch (err) {
      console.error(err)
    }
  }

  const handleSkipItem = async (itemId: string, instanceId: string) => {
    try {
      await checklistInstancesApi.skipItem(itemId)
      loadInstanceDetails(instanceId)
      loadInstances()
    } catch (err) {
      console.error(err)
    }
  }

  // Filtered data
  const filteredTemplates = templates.filter(t =>
    typeFilter === 'all' || t.type === typeFilter
  )

  const filteredInstances = instances.filter(i => {
    if (typeFilter !== 'all' && i.type !== typeFilter) return false
    if (statusFilter !== 'all' && i.status !== statusFilter) return false
    return true
  })

  const activeTemplates = templates.filter(t => t.isActive)

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-amber-500"></div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-white">Checklists</h1>
          <p className="text-slate-600 dark:text-slate-400">Interview and onboarding checklists</p>
        </div>
        <button
          onClick={() => {
            if (activeTab === 'templates') {
              setShowTemplateForm(true)
            } else {
              setShowInstanceForm(true)
            }
          }}
          className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
        >
          <Plus className="w-5 h-5" />
          {activeTab === 'templates' ? 'New Template' : 'New Checklist'}
        </button>
      </div>

      {/* Tabs */}
      <div className="flex gap-2 border-b border-slate-200 dark:border-slate-700">
        <button
          onClick={() => setActiveTab('templates')}
          className={`px-4 py-2 font-medium transition-colors border-b-2 -mb-px ${
            activeTab === 'templates'
              ? 'text-amber-500 border-amber-500'
              : 'text-slate-600 dark:text-slate-400 border-transparent hover:text-slate-900 dark:hover:text-white'
          }`}
        >
          <div className="flex items-center gap-2">
            <FileText className="w-4 h-4" />
            Templates ({templates.length})
          </div>
        </button>
        <button
          onClick={() => setActiveTab('instances')}
          className={`px-4 py-2 font-medium transition-colors border-b-2 -mb-px ${
            activeTab === 'instances'
              ? 'text-amber-500 border-amber-500'
              : 'text-slate-600 dark:text-slate-400 border-transparent hover:text-slate-900 dark:hover:text-white'
          }`}
        >
          <div className="flex items-center gap-2">
            <ClipboardList className="w-4 h-4" />
            Active Checklists ({instances.filter(i => i.status !== ChecklistInstanceStatus.Completed && i.status !== ChecklistInstanceStatus.Cancelled).length})
          </div>
        </button>
      </div>

      {/* Filters */}
      <div className="flex gap-2 flex-wrap">
        <select
          value={typeFilter}
          onChange={(e) => setTypeFilter(e.target.value === 'all' ? 'all' : Number(e.target.value) as ChecklistType)}
          className="px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white"
        >
          <option value="all">All Types</option>
          <option value={ChecklistType.Interview}>Interview</option>
          <option value={ChecklistType.Onboarding}>Onboarding</option>
        </select>

        {activeTab === 'instances' && (
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value === 'all' ? 'all' : Number(e.target.value) as ChecklistInstanceStatus)}
            className="px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white"
          >
            <option value="all">All Statuses</option>
            <option value={ChecklistInstanceStatus.NotStarted}>Not Started</option>
            <option value={ChecklistInstanceStatus.InProgress}>In Progress</option>
            <option value={ChecklistInstanceStatus.Completed}>Completed</option>
            <option value={ChecklistInstanceStatus.Cancelled}>Cancelled</option>
          </select>
        )}
      </div>

      {/* Templates Tab */}
      {activeTab === 'templates' && (
        <div className="space-y-4">
          {filteredTemplates.length === 0 ? (
            <Card>
              <CardContent className="py-12 text-center text-slate-500 dark:text-slate-400">
                No templates found. Create your first template to get started.
              </CardContent>
            </Card>
          ) : (
            filteredTemplates.map(template => (
              <Card key={template.id}>
                <div className="p-4">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <button
                        onClick={() => toggleTemplateExpand(template.id)}
                        className="p-1 hover:bg-slate-100 dark:hover:bg-slate-700 rounded"
                      >
                        {expandedTemplates.has(template.id) ? (
                          <ChevronUp className="w-5 h-5 text-slate-500" />
                        ) : (
                          <ChevronDown className="w-5 h-5 text-slate-500" />
                        )}
                      </button>
                      <div>
                        <div className="flex items-center gap-2">
                          <h3 className="font-semibold text-slate-900 dark:text-white">{template.name}</h3>
                          <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${
                            template.type === ChecklistType.Interview
                              ? 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400'
                              : 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
                          }`}>
                            {typeLabels[template.type]}
                          </span>
                          {!template.isActive && (
                            <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-slate-100 text-slate-500 dark:bg-slate-700 dark:text-slate-400">
                              Inactive
                            </span>
                          )}
                        </div>
                        <p className="text-sm text-slate-600 dark:text-slate-400">{template.description}</p>
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <span className="text-sm text-slate-500">{template.itemCount} items</span>
                      <button
                        onClick={() => handleToggleTemplateActive(template)}
                        className={`px-3 py-1 rounded text-sm font-medium ${
                          template.isActive
                            ? 'bg-slate-100 text-slate-700 hover:bg-slate-200 dark:bg-slate-700 dark:text-slate-300'
                            : 'bg-green-100 text-green-700 hover:bg-green-200 dark:bg-green-900/30 dark:text-green-400'
                        }`}
                      >
                        {template.isActive ? 'Deactivate' : 'Activate'}
                      </button>
                      <button
                        onClick={() => {
                          setEditingTemplateId(template.id)
                          setTemplateFormData({
                            name: template.name,
                            description: template.description,
                            type: template.type
                          })
                          setShowTemplateForm(true)
                        }}
                        className="p-2 text-slate-500 hover:text-slate-700 dark:hover:text-slate-300"
                      >
                        <Edit className="w-4 h-4" />
                      </button>
                      <button
                        onClick={() => handleDeleteTemplate(template.id)}
                        className="p-2 text-red-500 hover:text-red-700"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>
                  </div>

                  {/* Expanded Template Items */}
                  {expandedTemplates.has(template.id) && (
                    <div className="mt-4 pt-4 border-t border-slate-200 dark:border-slate-700">
                      <div className="flex items-center justify-between mb-3">
                        <h4 className="text-sm font-medium text-slate-700 dark:text-slate-300">Checklist Items</h4>
                        <button
                          onClick={() => {
                            setCurrentTemplateId(template.id)
                            setShowItemForm(true)
                          }}
                          className="flex items-center gap-1 px-2 py-1 text-sm bg-amber-500 text-white rounded hover:bg-amber-600"
                        >
                          <Plus className="w-4 h-4" />
                          Add Item
                        </button>
                      </div>

                      {templateDetails[template.id]?.items.length === 0 ? (
                        <p className="text-sm text-slate-500 dark:text-slate-400 italic">No items yet. Add your first item.</p>
                      ) : (
                        <div className="space-y-2">
                          {templateDetails[template.id]?.items.map((item, index) => (
                            <div key={item.id} className="flex items-center gap-3 p-2 bg-slate-50 dark:bg-slate-800 rounded-lg">
                              <span className="text-sm text-slate-400 w-6">{index + 1}.</span>
                              <span className={`px-2 py-0.5 rounded text-xs font-medium ${itemTypeColors[item.itemType]}`}>
                                {itemTypeLabels[item.itemType]}
                              </span>
                              <span className="flex-1 text-sm text-slate-900 dark:text-white">{item.content}</span>
                              {item.isRequired && (
                                <span className="text-xs text-red-500">Required</span>
                              )}
                              {item.helpText && (
                                <span title={item.helpText}><HelpCircle className="w-4 h-4 text-slate-400" /></span>
                              )}
                              <button
                                onClick={() => handleDeleteItem(item.id, template.id)}
                                className="p-1 text-red-500 hover:text-red-700"
                              >
                                <X className="w-4 h-4" />
                              </button>
                            </div>
                          ))}
                        </div>
                      )}
                    </div>
                  )}
                </div>
              </Card>
            ))
          )}
        </div>
      )}

      {/* Instances Tab */}
      {activeTab === 'instances' && (
        <div className="space-y-4">
          {filteredInstances.length === 0 ? (
            <Card>
              <CardContent className="py-12 text-center text-slate-500 dark:text-slate-400">
                No checklists found. Create a new checklist from a template to get started.
              </CardContent>
            </Card>
          ) : (
            filteredInstances.map(instance => (
              <Card key={instance.id}>
                <div className="p-4">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <button
                        onClick={() => toggleInstanceExpand(instance.id)}
                        className="p-1 hover:bg-slate-100 dark:hover:bg-slate-700 rounded"
                      >
                        {expandedInstances.has(instance.id) ? (
                          <ChevronUp className="w-5 h-5 text-slate-500" />
                        ) : (
                          <ChevronDown className="w-5 h-5 text-slate-500" />
                        )}
                      </button>
                      <div>
                        <div className="flex items-center gap-2">
                          <h3 className="font-semibold text-slate-900 dark:text-white">{instance.title}</h3>
                          <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${
                            instance.type === ChecklistType.Interview
                              ? 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400'
                              : 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
                          }`}>
                            {typeLabels[instance.type]}
                          </span>
                          <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${instanceStatusColors[instance.status]}`}>
                            {instance.statusName}
                          </span>
                        </div>
                        <p className="text-sm text-slate-600 dark:text-slate-400">
                          {instance.type === ChecklistType.Interview
                            ? `Candidate: ${instance.candidateName} - ${instance.position}`
                            : `New Hire: ${instance.newHireName}`
                          }
                        </p>
                      </div>
                    </div>
                    <div className="flex items-center gap-4">
                      {/* Progress Bar */}
                      <div className="flex items-center gap-2">
                        <div className="w-24 h-2 bg-slate-200 dark:bg-slate-700 rounded-full overflow-hidden">
                          <div
                            className="h-full bg-amber-500 transition-all"
                            style={{ width: `${instance.progressPercent}%` }}
                          />
                        </div>
                        <span className="text-sm text-slate-500">{instance.progressPercent}%</span>
                      </div>

                      {instance.status !== ChecklistInstanceStatus.Completed && instance.status !== ChecklistInstanceStatus.Cancelled && (
                        <>
                          <button
                            onClick={() => handleCompleteInstance(instance.id)}
                            className="p-2 text-green-500 hover:text-green-700"
                            title="Mark as Completed"
                          >
                            <CheckCircle className="w-5 h-5" />
                          </button>
                          <button
                            onClick={() => handleCancelInstance(instance.id)}
                            className="p-2 text-red-500 hover:text-red-700"
                            title="Cancel"
                          >
                            <XCircle className="w-5 h-5" />
                          </button>
                        </>
                      )}
                      <button
                        onClick={() => handleDeleteInstance(instance.id)}
                        className="p-2 text-slate-500 hover:text-red-500"
                        title="Delete"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>
                  </div>

                  {/* Expanded Instance Items */}
                  {expandedInstances.has(instance.id) && instanceDetails[instance.id] && (
                    <div className="mt-4 pt-4 border-t border-slate-200 dark:border-slate-700">
                      <h4 className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-3">
                        Checklist Items ({instanceDetails[instance.id].completedItems}/{instanceDetails[instance.id].totalItems})
                      </h4>

                      <div className="space-y-2">
                        {instanceDetails[instance.id].items.map((item, index) => (
                          <div key={item.id} className="flex items-center gap-3 p-3 bg-slate-50 dark:bg-slate-800 rounded-lg">
                            <span className="text-sm text-slate-400 w-6">{index + 1}.</span>

                            {/* Item Status Toggle */}
                            {item.status === ChecklistItemStatus.Completed ? (
                              <Check className="w-5 h-5 text-green-500" />
                            ) : item.status === ChecklistItemStatus.Skipped ? (
                              <X className="w-5 h-5 text-amber-500" />
                            ) : (
                              <div className="flex gap-1">
                                <button
                                  onClick={() => handleCompleteItem(item.id, instance.id)}
                                  className="p-1 text-slate-400 hover:text-green-500"
                                  title="Complete"
                                >
                                  <Check className="w-4 h-4" />
                                </button>
                                {!item.isRequired && (
                                  <button
                                    onClick={() => handleSkipItem(item.id, instance.id)}
                                    className="p-1 text-slate-400 hover:text-amber-500"
                                    title="Skip"
                                  >
                                    <X className="w-4 h-4" />
                                  </button>
                                )}
                              </div>
                            )}

                            <span className={`px-2 py-0.5 rounded text-xs font-medium ${itemTypeColors[item.itemType]}`}>
                              {itemTypeLabels[item.itemType]}
                            </span>

                            <span className={`flex-1 text-sm ${
                              item.status === ChecklistItemStatus.Completed || item.status === ChecklistItemStatus.Skipped
                                ? 'text-slate-500 line-through'
                                : 'text-slate-900 dark:text-white'
                            }`}>
                              {item.content}
                            </span>

                            {item.isRequired && item.status === ChecklistItemStatus.Pending && (
                              <span className="text-xs text-red-500">Required</span>
                            )}

                            {/* Score for completed items */}
                            {item.score && (
                              <div className="flex items-center gap-1">
                                <Star className="w-4 h-4 text-amber-500" />
                                <span className="text-sm text-slate-600">{item.score}/5</span>
                              </div>
                            )}
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              </Card>
            ))
          )}
        </div>
      )}

      {/* Template Form Modal */}
      {showTemplateForm && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <Card className="w-full max-w-lg mx-4">
            <CardHeader title={editingTemplateId ? 'Edit Template' : 'Create Template'} />
            <CardContent>
              <form onSubmit={editingTemplateId ? handleUpdateTemplate : handleCreateTemplate} className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Name</label>
                  <input
                    type="text"
                    value={templateFormData.name}
                    onChange={(e) => setTemplateFormData(prev => ({ ...prev, name: e.target.value }))}
                    required
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Description</label>
                  <textarea
                    value={templateFormData.description}
                    onChange={(e) => setTemplateFormData(prev => ({ ...prev, description: e.target.value }))}
                    rows={3}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white"
                  />
                </div>

                {!editingTemplateId && (
                  <div>
                    <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Type</label>
                    <select
                      value={templateFormData.type}
                      onChange={(e) => setTemplateFormData(prev => ({ ...prev, type: Number(e.target.value) as ChecklistType }))}
                      className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white"
                    >
                      <option value={ChecklistType.Interview}>Interview</option>
                      <option value={ChecklistType.Onboarding}>Onboarding</option>
                    </select>
                  </div>
                )}

                <div className="flex justify-end gap-3 pt-4">
                  <button
                    type="button"
                    onClick={closeTemplateModal}
                    className="px-4 py-2 text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700 rounded-lg"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    className="px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600"
                  >
                    {editingTemplateId ? 'Update' : 'Create'}
                  </button>
                </div>
              </form>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Item Form Modal */}
      {showItemForm && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <Card className="w-full max-w-lg mx-4">
            <CardHeader title="Add Checklist Item" />
            <CardContent>
              <form onSubmit={handleAddItem} className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Content</label>
                  <textarea
                    value={itemFormData.content}
                    onChange={(e) => setItemFormData(prev => ({ ...prev, content: e.target.value }))}
                    required
                    rows={3}
                    placeholder="Enter the question, topic, or task..."
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Item Type</label>
                  <select
                    value={itemFormData.itemType}
                    onChange={(e) => setItemFormData(prev => ({ ...prev, itemType: Number(e.target.value) as ChecklistItemType }))}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white"
                  >
                    <option value={ChecklistItemType.Question}>Question</option>
                    <option value={ChecklistItemType.Topic}>Topic</option>
                    <option value={ChecklistItemType.Task}>Task</option>
                    <option value={ChecklistItemType.Document}>Document</option>
                    <option value={ChecklistItemType.Training}>Training</option>
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Help Text (optional)</label>
                  <input
                    type="text"
                    value={itemFormData.helpText || ''}
                    onChange={(e) => setItemFormData(prev => ({ ...prev, helpText: e.target.value }))}
                    placeholder="Additional guidance for this item..."
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white"
                  />
                </div>

                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="isRequired"
                    checked={itemFormData.isRequired}
                    onChange={(e) => setItemFormData(prev => ({ ...prev, isRequired: e.target.checked }))}
                    className="rounded border-slate-300 dark:border-slate-600"
                  />
                  <label htmlFor="isRequired" className="text-sm text-slate-700 dark:text-slate-300">
                    Required item
                  </label>
                </div>

                <div className="flex justify-end gap-3 pt-4">
                  <button
                    type="button"
                    onClick={closeItemModal}
                    className="px-4 py-2 text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700 rounded-lg"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    className="px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600"
                  >
                    Add Item
                  </button>
                </div>
              </form>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Instance Form Modal */}
      {showInstanceForm && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <Card className="w-full max-w-lg mx-4">
            <CardHeader title="Create New Checklist" />
            <CardContent>
              <form onSubmit={handleCreateInstance} className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Checklist Type</label>
                  <select
                    value={instanceType}
                    onChange={(e) => setInstanceType(Number(e.target.value) as ChecklistType)}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white"
                  >
                    <option value={ChecklistType.Interview}>Interview</option>
                    <option value={ChecklistType.Onboarding}>Onboarding</option>
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Template</label>
                  <select
                    value={instanceType === ChecklistType.Interview ? interviewFormData.templateId : onboardingFormData.templateId}
                    onChange={(e) => {
                      if (instanceType === ChecklistType.Interview) {
                        setInterviewFormData(prev => ({ ...prev, templateId: e.target.value }))
                      } else {
                        setOnboardingFormData(prev => ({ ...prev, templateId: e.target.value }))
                      }
                    }}
                    required
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white"
                  >
                    <option value="">Select a template...</option>
                    {activeTemplates.filter(t => t.type === instanceType).map(t => (
                      <option key={t.id} value={t.id}>{t.name}</option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Title</label>
                  <input
                    type="text"
                    value={instanceType === ChecklistType.Interview ? interviewFormData.title : onboardingFormData.title}
                    onChange={(e) => {
                      if (instanceType === ChecklistType.Interview) {
                        setInterviewFormData(prev => ({ ...prev, title: e.target.value }))
                      } else {
                        setOnboardingFormData(prev => ({ ...prev, title: e.target.value }))
                      }
                    }}
                    required
                    placeholder={instanceType === ChecklistType.Interview ? "e.g., Technical Interview - John Doe" : "e.g., Onboarding - Jane Smith"}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white"
                  />
                </div>

                {instanceType === ChecklistType.Interview ? (
                  <>
                    <div>
                      <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Candidate Name</label>
                      <input
                        type="text"
                        value={interviewFormData.candidateName}
                        onChange={(e) => setInterviewFormData(prev => ({ ...prev, candidateName: e.target.value }))}
                        required
                        className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Position</label>
                      <input
                        type="text"
                        value={interviewFormData.position}
                        onChange={(e) => setInterviewFormData(prev => ({ ...prev, position: e.target.value }))}
                        required
                        className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Interview Date</label>
                      <input
                        type="date"
                        value={interviewFormData.interviewDate}
                        onChange={(e) => setInterviewFormData(prev => ({ ...prev, interviewDate: e.target.value }))}
                        required
                        className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white"
                      />
                    </div>
                  </>
                ) : (
                  <>
                    <div>
                      <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">New Hire Name</label>
                      <input
                        type="text"
                        value={onboardingFormData.newHireName}
                        onChange={(e) => setOnboardingFormData(prev => ({ ...prev, newHireName: e.target.value }))}
                        required
                        className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Start Date</label>
                      <input
                        type="date"
                        value={onboardingFormData.startDate}
                        onChange={(e) => setOnboardingFormData(prev => ({ ...prev, startDate: e.target.value }))}
                        required
                        className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Target Completion Date (optional)</label>
                      <input
                        type="date"
                        value={onboardingFormData.targetCompletionDate || ''}
                        onChange={(e) => setOnboardingFormData(prev => ({ ...prev, targetCompletionDate: e.target.value || undefined }))}
                        className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white"
                      />
                    </div>
                  </>
                )}

                <div className="flex justify-end gap-3 pt-4">
                  <button
                    type="button"
                    onClick={closeInstanceModal}
                    className="px-4 py-2 text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700 rounded-lg"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    className="px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600"
                  >
                    Create Checklist
                  </button>
                </div>
              </form>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  )
}
