import { Component } from '@angular/core';
import { TaskApiService } from '../../services/task-api';
import { CreateTaskRequest } from '../../models/task.model';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';

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

  constructor(private taskApi: TaskApiService) {}

  onSubmit(): void {
    this.isSubmitting = true;
    this.successMessage = '';
    this.errorMessage = '';

    this.taskApi.createTask(this.task).subscribe({
      next: (createdTask) => {
        this.successMessage = `Task "${createdTask.title}" created successfully! Agent will process it.`;
        this.resetForm();
        this.isSubmitting = false;
      },
      error: (error) => {
        this.errorMessage = `Error: ${error.error?.error || error.message}`;
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
