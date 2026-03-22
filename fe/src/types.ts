export interface AuthResponse {
  token: string
  userId: string
  tenantId: string
  tenantSubdomain: string
  email: string
  name: string
  role: string
}

export interface LeadDto {
  id: string
  name: string
  company: string | null
  email: string | null
  phone: string | null
  location: string | null
  city: string | null
  state: string | null
  requirement: string | null
  source: string
  status: string
  assignedToUserId: string | null
  createdAt: string
  updatedAt: string | null
}

export interface FireOpsDashboardDto {
  totalLeads: number
  activeAmcContracts: number
  openServiceRequests: number
  pendingQuotations: number
  amcRevenueActive: number
  upcomingAmcVisitsNext30Days: number
}

export interface CustomerDto {
  id: string
  name: string
  contactPerson: string | null
  phone: string | null
  email: string | null
  address: string | null
  createdAt: string
}

export interface SiteDto {
  id: string
  customerId: string
  name: string
  address: string | null
  city: string | null
  state: string | null
  siteType: string
  complianceStatus: string | null
}

export interface ProductDto {
  id: string
  name: string
  category: string
  price: number
  description: string | null
  isActive: boolean
}

export interface QuotationDto {
  id: string
  customerId: string
  customerName: string
  siteId: string | null
  siteName: string | null
  totalAmount: number
  status: string
  createdAt: string
  items: { id: string; productId: string; productName: string; quantity: number; unitPrice: number; lineTotal: number }[]
}

export interface InstallationDto {
  id: string
  customerId: string
  customerName: string
  siteId: string
  siteName: string
  technicianUserId: string | null
  technicianName: string | null
  scheduledDate: string | null
  completedDate: string | null
  status: string
  checklistNotes: string | null
  photoUrls: string | null
}

export interface AmcContractDto {
  id: string
  customerId: string
  customerName: string
  siteId: string
  siteName: string
  startDate: string
  endDate: string
  visitFrequencyPerYear: number
  status: string
  contractValue: number | null
}

export interface AmcVisitDto {
  id: string
  amcContractId: string
  scheduledDate: string
  completedDate: string | null
  technicianUserId: string | null
  technicianName: string | null
  status: string
}

export interface ServiceRequestDto {
  id: string
  customerId: string
  customerName: string
  siteId: string | null
  siteName: string | null
  description: string
  status: string
  priority: string | null
  assignedToUserId: string | null
  assignedToName: string | null
  createdAt: string
}

export interface OpsTaskDto {
  id: string
  title: string
  assignedToUserId: string
  assignedToName: string | null
  dueDate: string
  status: string
  taskType: string
  serviceRequestId: string | null
  amcVisitId: string | null
  installationJobId: string | null
}
