import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReminderPriority } from '../../../../core/models/reminder.models';

@Component({
  selector: 'app-priority-badge',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './priority-badge.component.html',
  styleUrl: './priority-badge.component.css'
})
export class PriorityBadgeComponent {
  @Input() priority: ReminderPriority = ReminderPriority.Medium;

  get PriorityEnum(): typeof ReminderPriority {
    return ReminderPriority;
  }
}
