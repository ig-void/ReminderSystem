export enum ReminderPriority {
  Low = 1,
  Medium = 2,
  High = 3,
  Urgent = 4
}

export interface Reminder {
  id: number;
  title: string;
  description?: string;
  dueDate: string;
  priority: ReminderPriority;
  priorityName: string;
  isCompleted: boolean;
  isPublic: boolean;
  remindBeforeMinutes: number;
  isEmailSent: boolean;
  emailSentAt?: string;
  webhookUrl?: string;
  createdAt: string;
  updatedAt?: string;
  userId: string;
  userEmail?: string;
  userFullName?: string;
  isOwner: boolean;
}

export interface CreateReminderRequest {
  title: string;
  description?: string;
  dueDate: string;
  priority: ReminderPriority;
  isPublic: boolean;
  remindBeforeMinutes: number;
  webhookUrl?: string;
}

export interface UpdateReminderRequest {
  title: string;
  description?: string;
  dueDate: string;
  priority: ReminderPriority;
  isCompleted: boolean;
  isPublic: boolean;
  remindBeforeMinutes: number;
  webhookUrl?: string;
}

export interface ReminderFilter {
  search?: string;
  isCompleted?: boolean;
  priority?: ReminderPriority;
  sortBy?: string;
  isDescending?: boolean;
}
