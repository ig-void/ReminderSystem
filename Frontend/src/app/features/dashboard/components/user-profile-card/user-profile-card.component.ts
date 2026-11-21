import { Component, Input } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { UserProfile } from '../../../../core/models/auth.models';

@Component({
  selector: 'app-user-profile-card',
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: './user-profile-card.component.html',
  styleUrl: './user-profile-card.component.css'
})
export class UserProfileCardComponent {
  @Input() user: UserProfile | null = null;
}
