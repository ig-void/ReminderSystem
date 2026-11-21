import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';
import { ReminderService } from '../../core/services/reminder.service';
import { Reminder, ReminderFilter, ReminderPriority } from '../../core/models/reminder.models';
import { NavbarComponent } from '../../shared/components/layout/navbar/navbar.component';
import { AlertComponent } from '../../shared/components/ui/alert/alert.component';
import { ReminderCardComponent } from '../reminders/components/reminder-card/reminder-card.component';
import { ReminderFormModalComponent } from '../reminders/components/reminder-form-modal/reminder-form-modal.component';
import { DeleteConfirmModalComponent } from '../reminders/components/delete-confirm-modal/delete-confirm-modal.component';
import { ReminderFilterBarComponent } from '../reminders/components/reminder-filter-bar/reminder-filter-bar.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    NavbarComponent,
    AlertComponent,
    ReminderCardComponent,
    ReminderFormModalComponent,
    DeleteConfirmModalComponent,
    ReminderFilterBarComponent
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly reminderService = inject(ReminderService);

  readonly user = this.authService.currentUser;

  // View state: 'my' or 'others'
  readonly activeTab = signal<'my' | 'others'>('my');

  // Reminders data
  readonly myReminders = signal<Reminder[]>([]);
  readonly othersReminders = signal<Reminder[]>([]);
  readonly isLoading = signal(false);
  readonly statusMessage = signal<{ type: 'success' | 'error'; text: string } | null>(null);

  // Filter state
  private filterState: ReminderFilter = {
    search: '',
    isCompleted: undefined,
    priority: undefined,
    sortBy: 'duedate',
    isDescending: false
  };

  // Modal states
  readonly isFormModalOpen = signal(false);
  readonly reminderToEdit = signal<Reminder | null>(null);
  readonly isSavingReminder = signal(false);

  readonly isDeleteModalOpen = signal(false);
  readonly reminderToDelete = signal<Reminder | null>(null);
  readonly isDeletingReminder = signal(false);

  // Statistics getters
  get totalMyReminders(): number {
    return this.myReminders().length;
  }

  get pendingMyReminders(): number {
    return this.myReminders().filter((r) => !r.isCompleted).length;
  }

  get completedMyReminders(): number {
    return this.myReminders().filter((r) => r.isCompleted).length;
  }

  get publicMyReminders(): number {
    return this.myReminders().filter((r) => r.isPublic).length;
  }

  get displayedReminders(): Reminder[] {
    return this.activeTab() === 'my' ? this.myReminders() : this.othersReminders();
  }

  ngOnInit(): void {
    if (this.authService.isLoggedIn()) {
      this.authService.getProfile().subscribe({
        error: (err) => console.error('Failed to load profile', err)
      });
      this.loadReminders();
    }
  }

  setTab(tab: 'my' | 'others'): void {
    this.activeTab.set(tab);
    this.statusMessage.set(null);
    this.loadReminders();
  }

  loadReminders(): void {
    this.isLoading.set(true);

    if (this.activeTab() === 'my') {
      this.reminderService.getMyReminders(this.filterState).subscribe({
        next: (data) => {
          this.myReminders.set(data);
          this.isLoading.set(false);
        },
        error: (err) => {
          this.isLoading.set(false);
          this.showStatus('error', err.error?.message || 'Failed to load reminders.');
        }
      });
    } else {
      this.reminderService.getOthersReminders(this.filterState).subscribe({
        next: (data) => {
          this.othersReminders.set(data);
          this.isLoading.set(false);
        },
        error: (err) => {
          this.isLoading.set(false);
          this.showStatus('error', err.error?.message || "Failed to load other users' reminders.");
        }
      });
    }
  }

  // Filter actions
  onSearchChange(term: string): void {
    this.filterState.search = term;
    this.loadReminders();
  }

  onStatusFilterChange(status: 'all' | 'pending' | 'completed'): void {
    this.filterState.isCompleted = status === 'all' ? undefined : status === 'completed';
    this.loadReminders();
  }

  onPriorityFilterChange(priority: ReminderPriority | undefined): void {
    this.filterState.priority = priority;
    this.loadReminders();
  }

  onSortChange(sortBy: string): void {
    this.filterState.sortBy = sortBy;
    this.loadReminders();
  }

  // Modal actions
  openCreateModal(): void {
    this.reminderToEdit.set(null);
    this.isFormModalOpen.set(true);
  }

  openEditModal(reminder: Reminder): void {
    this.reminderToEdit.set(reminder);
    this.isFormModalOpen.set(true);
  }

  closeFormModal(): void {
    this.isFormModalOpen.set(false);
    this.reminderToEdit.set(null);
  }

  onSaveReminder(payload: { isEdit: boolean; id?: number; data: any }): void {
    this.isSavingReminder.set(true);

    if (payload.isEdit && payload.id) {
      this.reminderService.updateReminder(payload.id, payload.data).subscribe({
        next: () => {
          this.isSavingReminder.set(false);
          this.closeFormModal();
          this.showStatus('success', 'Reminder updated successfully.');
          this.loadReminders();
        },
        error: (err) => {
          this.isSavingReminder.set(false);
          this.showStatus('error', err.error?.message || 'Failed to update reminder.');
        }
      });
    } else {
      this.reminderService.createReminder(payload.data).subscribe({
        next: () => {
          this.isSavingReminder.set(false);
          this.closeFormModal();
          this.showStatus('success', 'Reminder created successfully.');
          this.loadReminders();
        },
        error: (err) => {
          this.isSavingReminder.set(false);
          this.showStatus('error', err.error?.message || 'Failed to create reminder.');
        }
      });
    }
  }

  openDeleteModal(reminder: Reminder): void {
    this.reminderToDelete.set(reminder);
    this.isDeleteModalOpen.set(true);
  }

  closeDeleteModal(): void {
    this.isDeleteModalOpen.set(false);
    this.reminderToDelete.set(null);
  }

  onConfirmDelete(): void {
    const reminder = this.reminderToDelete();
    if (!reminder) return;

    this.isDeletingReminder.set(true);
    this.reminderService.deleteReminder(reminder.id).subscribe({
      next: () => {
        this.isDeletingReminder.set(false);
        this.closeDeleteModal();
        this.showStatus('success', 'Reminder deleted successfully.');
        this.loadReminders();
      },
      error: (err) => {
        this.isDeletingReminder.set(false);
        this.showStatus('error', err.error?.message || 'Failed to delete reminder.');
      }
    });
  }

  onToggleComplete(id: number): void {
    this.reminderService.toggleComplete(id).subscribe({
      next: (updated) => {
        this.myReminders.update((list) =>
          list.map((item) => (item.id === id ? updated : item))
        );
      },
      error: (err) => {
        this.showStatus('error', err.error?.message || 'Failed to toggle status.');
      }
    });
  }

  onLogout(): void {
    this.authService.logout();
  }

  private showStatus(type: 'success' | 'error', text: string): void {
    this.statusMessage.set({ type, text });
    setTimeout(() => {
      this.statusMessage.set(null);
    }, 4000);
  }
}
