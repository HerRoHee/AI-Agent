import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TaskApiService } from '../../services/task-api';
import { TaskStateService } from '../../services/task-state';
import { CreateTaskRequest } from '../../models/task.model';

@Component({
  selector: 'app-task-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './task-form.html',
  styleUrls: ['./task-form.css']
})
export class TaskFormComponent {
  task: CreateTaskRequest = {
    title: '',
    description: null,
    priority: 'Medium',
    dueDate: null
  };

  priorities = ['Low', 'Medium', 'High', 'Critical'];
  isSubmitting = false;
  successMessage = '';
  errorMessage = '';

  constructor(
    private taskApi: TaskApiService,
    private taskState: TaskStateService // ← Inject shared state
  ) {}

  onSubmit(): void {
    if (this.isSubmitting) return;

    this.isSubmitting = true;
    this.successMessage = '';
    this.errorMessage = '';

    this.taskApi.createTask(this.task).subscribe({
      next: (createdTask) => {
        console.log('✅ Task created:', createdTask);

        // 1. Show success message
        this.successMessage = `✅ Task "${createdTask.title}" created! Agent will process it.`;

        // 2. Add to shared state immediately (optimistic update)
        this.taskState.addTask(createdTask);

        // 3. Reset form
        this.resetForm();
        this.isSubmitting = false;

        // Auto-clear success message after 5 seconds
        setTimeout(() => {
          this.successMessage = '';
        }, 5000);
      },
      error: (error) => {
        console.error('❌ Error creating task:', error);
        this.errorMessage = `❌ Error: ${error.error?.error || error.message}`;
        this.isSubmitting = false;
      }
    });
  }

  resetForm(): void {
    this.task = {
      title: '',
      description: null,
      priority: 'Medium',
      dueDate: null
    };
  }
}
