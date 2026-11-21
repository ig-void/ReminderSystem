import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReminderPriority } from '../../../../core/models/reminder.models';

@Component({
  selector: 'app-reminder-filter-bar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reminder-filter-bar.component.html',
  styleUrl: './reminder-filter-bar.component.css'
})
export class ReminderFilterBarComponent {
  @Input() isOthersView = false;
  @Input() totalCount = 0;

  @Output() searchChange = new EventEmitter<string>();
  @Output() statusFilterChange = new EventEmitter<'all' | 'pending' | 'completed'>();
  @Output() priorityFilterChange = new EventEmitter<ReminderPriority | undefined>();
  @Output() sortChange = new EventEmitter<string>();
  @Output() addReminderClick = new EventEmitter<void>();

  searchTerm = '';
  selectedStatus: 'all' | 'pending' | 'completed' = 'all';
  selectedPriority: string = '';
  selectedSort: string = 'duedate';

  onSearchChange(): void {
    this.searchChange.emit(this.searchTerm.trim());
  }

  onStatusChange(status: 'all' | 'pending' | 'completed'): void {
    this.selectedStatus = status;
    this.statusFilterChange.emit(status);
  }

  onPriorityChange(): void {
    const priority = this.selectedPriority ? (Number(this.selectedPriority) as ReminderPriority) : undefined;
    this.priorityFilterChange.emit(priority);
  }

  onSortChange(): void {
    this.sortChange.emit(this.selectedSort);
  }

  onAddClick(): void {
    this.addReminderClick.emit();
  }
}
