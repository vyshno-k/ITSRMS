export interface SlaConfiguration {
  id: number;
  priorityId: number;
  priorityName: string;
  responseTargetMinutes: number;
  resolutionTargetMinutes: number;
  resolutionTargetBusinessDays: number;
}

export interface UpdateSlaConfiguration {
  priorityId: number;
  responseTargetMinutes: number;
  resolutionTargetMinutes: number;
  resolutionTargetBusinessDays: number;
}

export interface SlaTicket {
  id: number;
  ticketNumber: string;
  priorityId: number;
  priorityName: string;
  status: string;
  createdAt: string;
  responseDueAt?: string;
  resolutionDueAt?: string;
  responseSlaStatus: string;
  resolutionSlaStatus: string;
  isBreached: boolean;
  isPaused: boolean;
  engineerId?: number | null;
  engineerName?: string | null;
  subject: string;
}
