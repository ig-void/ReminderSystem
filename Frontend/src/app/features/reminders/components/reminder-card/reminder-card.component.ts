import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { Reminder } from '../../../../core/models/reminder.models';
import { PriorityBadgeComponent } from '../../../../shared/components/ui/priority-badge/priority-badge.component';
import { ensureUtcIsoString, parseUtcDate } from '../../../../core/utils/date.utils';

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

  readonly ensureUtc = ensureUtcIsoString;

  isOverdue(): boolean {
    if (this.reminder.isCompleted) return false;
    const due = parseUtcDate(this.reminder.dueDate);
    return due ? due < new Date() : false;
  }

  getLeadTimeLabel(minutes?: number): string {
    if (minutes === undefined || minutes === null) return '15m before';
    if (minutes === 0) return 'At event time';
    if (minutes < 60) return `${minutes}m before`;
    if (minutes % 60 === 0) {
      const hours = minutes / 60;
      if (hours < 24) return `${hours}h before`;
      const days = hours / 24;
      return `${days}d before`;
    }
    const h = Math.floor(minutes / 60);
    const m = minutes % 60;
    return `${h}h ${m}m before`;
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
