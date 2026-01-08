import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TaskStateService } from '../../services/task-state';
import { Task } from '../../models/task.model';
import { Observable } from 'rxjs';
import { TaskApiService } from '../../services/task-api';

@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './task-list.html',
  styleUrls: ['./task-list.css']
})
export class TaskListComponent implements OnInit {
  tasks$: Observable<Task[]>; // ← Observable from shared state
  loading$: Observable<boolean>; // ← Loading state from service
  filteredStatus: string = '';

  statuses = ['', 'Pending', 'Active', 'Snoozed', 'Escalated', 'Completed'];

  constructor(private taskState: TaskStateService, private taskApi: TaskApiService) {
    // Subscribe to shared state observables
    this.tasks$ = taskState.tasks$;
    this.loading$ = taskState.loading$;
  }

  ngOnInit(): void {
    console.log('📋 TaskListComponent initialized');
    // Initial fetch is handled by TaskStateService constructor
  }

  onStatusFilterChange(): void {
    console.log('🔍 Filter changed to:', this.filteredStatus);
    // Update the observable with filtered tasks
    if (this.filteredStatus) {
      this.tasks$ = this.taskState.getFilteredTasks(this.filteredStatus);
    } else {
      this.tasks$ = this.taskState.tasks$;
    }
  }

  onManualRefresh(): void {
    console.log('🔄 Manual refresh triggered');
    this.taskState.refreshTasks(true); // Force refresh
  }

  getStatusClass(status: string): string {
    return `status-${status.toLowerCase()}`;
  }

  getPriorityClass(priority: string): string {
    return `priority-${priority.toLowerCase()}`;
  }

  onCompleteTask(taskId: string): void {
    if (!confirm('Mark this task as complete?')) return;

    this.taskApi.completeTask(taskId).subscribe({
      next: () => {
        console.log('✅ Task completed');
        this.taskState.refreshTasks(true); // Force refresh
      },
      error: (error) => {
        console.error('❌ Error completing task:', error);
        alert(`Error: ${error.error?.error || error.message}`);
      }
    });
  }

  onSnoozeTask(taskId: string): void {
    const hours = prompt('Snooze for how many hours? (default: 4)', '4');
    if (hours === null) return; // User cancelled

    const snoozeDuration = hours ? parseFloat(hours) : undefined;

    this.taskApi.snoozeTask(taskId, snoozeDuration).subscribe({
      next: () => {
        console.log('⏰ Task snoozed');
        this.taskState.refreshTasks(true); // Force refresh
      },
      error: (error) => {
        console.error('❌ Error snoozing task:', error);
        alert(`Error: ${error.error?.error || error.message}`);
      }
    });
  }

  onRejectTask(taskId: string): void {
    if (!confirm('Reject this task? This action cannot be undone.')) return;

    this.taskApi.rejectTask(taskId).subscribe({
      next: () => {
        console.log('🚫 Task rejected');
        this.taskState.refreshTasks(true); // Force refresh
      },
      error: (error) => {
        console.error('❌ Error rejecting task:', error);
        alert(`Error: ${error.error?.error || error.message}`);
      }
    });
  }
}
