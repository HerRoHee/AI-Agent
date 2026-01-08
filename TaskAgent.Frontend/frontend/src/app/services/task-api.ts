import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Task, CreateTaskRequest, SystemStats } from '../models/task.model';

@Injectable({
  providedIn: 'root'
})
export class TaskApiService {
  private apiUrl = 'http://localhost:5000/api';

  constructor(private http: HttpClient) { }

  getTasks(status?: string): Observable<Task[]> {
    const url = status
      ? `${this.apiUrl}/tasks?status=${status}`
      : `${this.apiUrl}/tasks`;
    return this.http.get<Task[]>(url);
  }

  getTask(id: string): Observable<Task> {
    return this.http.get<Task>(`${this.apiUrl}/tasks/${id}`);
  }

  createTask(request: CreateTaskRequest): Observable<Task> {
    return this.http.post<Task>(`${this.apiUrl}/tasks`, request);
  }

  getStats(): Observable<SystemStats> {
    return this.http.get<SystemStats>(`${this.apiUrl}/tasks/stats`);
  }

  /**
   * User action: Complete a task
   */
  completeTask(id: string): Observable<{message: string}> {
    return this.http.post<{message: string}>(
      `${this.apiUrl}/tasks/${id}/complete`,
      {}
    );
  }

  /**
   * User action: Snooze a task
   */
  snoozeTask(id: string, hours?: number): Observable<{message: string}> {
    const body = hours ? { snoozeDurationHours: hours } : {};
    return this.http.post<{message: string}>(
      `${this.apiUrl}/tasks/${id}/snooze`,
      body
    );
  }

  /**
   * User action: Reject a task
   */
  rejectTask(id: string): Observable<{message: string}> {
    return this.http.post<{message: string}>(
      `${this.apiUrl}/tasks/${id}/reject`,
      {}
    );
  }
}
