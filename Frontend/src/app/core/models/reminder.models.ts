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
}

export interface UpdateReminderRequest {
  title: string;
  description?: string;
  dueDate: string;
  priority: ReminderPriority;
  isCompleted: boolean;
  isPublic: boolean;
}

export interface ReminderFilter {
  search?: string;
  isCompleted?: boolean;
  priority?: ReminderPriority;
  sortBy?: string;
  isDescending?: boolean;
}
