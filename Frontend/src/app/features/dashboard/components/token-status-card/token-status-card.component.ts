import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../../core/services/auth.service';
import { UserProfile } from '../../../../core/models/auth.models';

@Component({
  selector: 'app-token-status-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './token-status-card.component.html',
  styleUrl: './token-status-card.component.css'
})
export class TokenStatusCardComponent {
  private readonly authService = inject(AuthService);

  readonly isTesting = signal(false);
  readonly testResult = signal<string | null>(null);

  testProtectedEndpoint(): void {
    this.isTesting.set(true);
    this.testResult.set(null);

    this.authService.getProfile().subscribe({
      next: (profile: UserProfile) => {
        this.isTesting.set(false);
        this.testResult.set(`200 OK: Validated claims for "${profile.email}" (ID: ${profile.id})`);
      },
      error: (err) => {
        this.isTesting.set(false);
        this.testResult.set(`Error: ${err.status} ${err.statusText}`);
      }
    });
  }
}
