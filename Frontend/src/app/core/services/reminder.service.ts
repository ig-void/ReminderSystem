import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CreateReminderRequest,
  Reminder,
  ReminderFilter,
  UpdateReminderRequest
} from '../models/reminder.models';

@Injectable({
  providedIn: 'root'
})
export class ReminderService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5062/api/reminders';

  getMyReminders(filter?: ReminderFilter): Observable<Reminder[]> {
    let params = new HttpParams();

    if (filter) {
      if (filter.search) {
        params = params.set('search', filter.search);
      }
      if (filter.isCompleted !== undefined && filter.isCompleted !== null) {
        params = params.set('isCompleted', filter.isCompleted.toString());
      }
      if (filter.priority) {
        params = params.set('priority', filter.priority.toString());
      }
      if (filter.sortBy) {
        params = params.set('sortBy', filter.sortBy);
      }
      if (filter.isDescending !== undefined) {
        params = params.set('isDescending', filter.isDescending.toString());
      }
    }

    return this.http.get<Reminder[]>(this.baseUrl, { params });
  }

  getOthersReminders(filter?: ReminderFilter): Observable<Reminder[]> {
    let params = new HttpParams();

    if (filter) {
      if (filter.search) {
        params = params.set('search', filter.search);
      }
      if (filter.isCompleted !== undefined && filter.isCompleted !== null) {
        params = params.set('isCompleted', filter.isCompleted.toString());
      }
      if (filter.priority) {
        params = params.set('priority', filter.priority.toString());
      }
    }

    return this.http.get<Reminder[]>(`${this.baseUrl}/others`, { params });
  }

  getReminderById(id: number): Observable<Reminder> {
    return this.http.get<Reminder>(`${this.baseUrl}/${id}`);
  }

  createReminder(data: CreateReminderRequest): Observable<Reminder> {
    return this.http.post<Reminder>(this.baseUrl, data);
  }

  updateReminder(id: number, data: UpdateReminderRequest): Observable<Reminder> {
    return this.http.put<Reminder>(`${this.baseUrl}/${id}`, data);
  }

  deleteReminder(id: number): Observable<{ message: string; id: number }> {
    return this.http.delete<{ message: string; id: number }>(`${this.baseUrl}/${id}`);
  }

  toggleComplete(id: number): Observable<Reminder> {
    return this.http.patch<Reminder>(`${this.baseUrl}/${id}/toggle-complete`, {});
  }
}
