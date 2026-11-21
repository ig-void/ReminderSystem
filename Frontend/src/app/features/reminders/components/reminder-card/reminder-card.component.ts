import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { Reminder } from '../../../../core/models/reminder.models';
import { PriorityBadgeComponent } from '../../../../shared/components/ui/priority-badge/priority-badge.component';

@Component({
  selector: 'app-reminder-card',
  standalone: true,
  imports: [CommonModule, DatePipe, PriorityBadgeComponent],
  templateUrl: './reminder-card.component.html',
  styleUrl: './reminder-card.component.css'
})
export class ReminderCardComponent {
  @Input() reminder!: Reminder;
  @Input() isOthersView = false;

  @Output() toggleComplete = new EventEmitter<number>();
  @Output() edit = new EventEmitter<Reminder>();
  @Output() delete = new EventEmitter<Reminder>();

  isOverdue(): boolean {
    if (this.reminder.isCompleted) return false;
    return new Date(this.reminder.dueDate) < new Date();
  }

  onToggle(): void {
    if (this.reminder.isOwner) {
      this.toggleComplete.emit(this.reminder.id);
    }
  }

  onEdit(): void {
    this.edit.emit(this.reminder);
  }

  onDelete(): void {
    this.delete.emit(this.reminder);
  }
}
