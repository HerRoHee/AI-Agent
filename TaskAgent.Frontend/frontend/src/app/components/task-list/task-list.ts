import { Component, OnInit } from '@angular/core';
import { TaskApiService } from '../../services/task-api';
import { Task } from '../../models/task.model';
import { interval } from 'rxjs';
import { startWith, switchMap } from 'rxjs/operators';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';

@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './task-list.html',
  styleUrls: ['./task-list.css']
})
export class TaskListComponent implements OnInit {
  tasks: Task[] = [];
  filteredStatus: string = '';
  isLoading = true;

  statuses = ['', 'Pending', 'Active', 'Snoozed', 'Escalated', 'Completed'];

  constructor(private taskApi: TaskApiService) {}

  ngOnInit(): void {
    // Auto-refresh every 5 seconds to see agent actions
    interval(5000)
      .pipe(
        startWith(0),
        switchMap(() => this.taskApi.getTasks(this.filteredStatus || undefined))
      )
      .subscribe({
        next: (tasks) => {
          this.tasks = tasks;
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Error loading tasks:', error);
          this.isLoading = false;
        }
      });
  }

  onStatusFilterChange(): void {
    this.isLoading = true;
    this.taskApi.getTasks(this.filteredStatus || undefined).subscribe({
      next: (tasks) => {
        this.tasks = tasks;
        this.isLoading = false;
      }
    });
  }

  getStatusClass(status: string): string {
    return `status-${status.toLowerCase()}`;
  }

  getPriorityClass(priority: string): string {
    return `priority-${priority.toLowerCase()}`;
  }
}
