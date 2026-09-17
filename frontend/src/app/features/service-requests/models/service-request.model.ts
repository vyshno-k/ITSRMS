export type TicketStatus = 'New' | 'Assigned' | 'In Progress' | 'Resolved' | 'Closed' | 'Reopened';

export interface Employee {
  id: number;
  fullName: string;
  email: string;
  isActive?: boolean;
}

export interface ResolutionView {
  id: number;
  serviceRequestId: number;
  investigationNotes: string;
  resolutionNotes: string;
  resolvedBy?: number | null;
  resolvedAt: string;
  resolutionDueAt?: string | null;
  slaResult: string;
}

export interface ServiceRequest {
  id: number;
  ticketNumber: string;
  employeeId: number;
  categoryId: number;
  categoryName?: string;
  serviceTypeId: number;
  serviceTypeName?: string;
  priorityId: number;
  priorityName?: string;
  subject: string;
  description: string;
  status: TicketStatus;
  createdAt: string;
  updatedAt?: string | null;
  responseDueAt?: string | null;
  resolutionDueAt?: string | null;
  resolvedAt?: string | null;
  closedAt?: string | null;
  engineerId?: number | null;
  engineerName?: string | null;
  effectiveResponseDueAt?: string | null;
  effectiveResolutionDueAt?: string | null;
  responseSlaStatus?: string;
  resolutionSlaStatus?: string;
  isSlaBreached?: boolean;
  isSlaPaused?: boolean;
  latestResolution?: ResolutionView | null;
}

export interface CreateServiceRequest {
  employeeId: number;
  categoryId: number;
  serviceTypeId: number;
  priorityId: number;
  subject: string;
  description: string;
}

export interface UpdateServiceRequest {
  categoryId: number;
  serviceTypeId: number;
  priorityId: number;
  subject: string;
  description: string;
}

export interface AssignTicket {
  engineerId: number;
  performedBy: number;
}

export interface UpdateStatusRequest {
  status: string;
  performedBy?: number;
  engineerId?: number;
}

export interface ResolveTicket {
  investigationNotes: string;
  resolutionNotes: string;
  resolvedBy: number;
  engineerId: number;
}

export interface ReopenTicket {
  reopenedBy?: number;
  reason?: string;
}

export interface AddComment {
  employeeId?: number | null;
  commentText: string;
}

export interface TicketHistoryEntry {
  id: number;
  serviceRequestId: number;
  action: string;
  oldValue?: string | null;
  newValue?: string | null;
  performedBy?: number | null;
  createdAt: string;
}

export interface TicketComment {
  id: number;
  serviceRequestId: number;
  employeeId?: number | null;
  commentText: string;
  createdAt: string;
}

export interface SupportEngineer {
  id: number;
  fullName: string;
  email: string;
  isActive: boolean;
}

export interface ServiceRequestFilter {
  search?: string;
  status?: string;
  priorityId?: number;
  categoryId?: number;
  employeeId?: number;
}
