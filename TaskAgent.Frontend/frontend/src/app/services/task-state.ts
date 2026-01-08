import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, timer } from 'rxjs';
import { switchMap, tap, catchError, shareReplay } from 'rxjs/operators';
import { Task } from '../models/task.model';
import { TaskApiService } from './task-api';

@Injectable({
  providedIn: 'root'
})
export class TaskStateService {
  // BehaviorSubject holds the current state and emits to all subscribers
  private tasksSubject = new BehaviorSubject<Task[]>([]);
  private loadingSubject = new BehaviorSubject<boolean>(true);
  private lastFetchTime = 0;
  private readonly CACHE_DURATION = 5000; // 5 seconds minimum between fetches

  // Public observables that components subscribe to
  public tasks$: Observable<Task[]> = this.tasksSubject.asObservable();
  public loading$: Observable<boolean> = this.loadingSubject.asObservable();

  constructor(private taskApi: TaskApiService) {
    // Start periodic refresh (60 seconds)
    this.startPeriodicRefresh();
  }

  /**
   * Fetch tasks from backend
   * Uses caching to prevent duplicate requests within 5 seconds
   */
  public refreshTasks(force: boolean = false): void {
    const now = Date.now();

    // Debounce: Skip if fetched recently (unless forced)
    if (!force && (now - this.lastFetchTime) < this.CACHE_DURATION) {
      console.log('⏭️ Skipping fetch (cached)');
      return;
    }

    console.log('🔄 Fetching tasks from backend...');
    this.loadingSubject.next(true);
    this.lastFetchTime = now;

    this.taskApi.getTasks().subscribe({
      next: (tasks) => {
        console.log('✅ Tasks loaded:', tasks.length);
        this.tasksSubject.next(tasks);
        this.loadingSubject.next(false);
      },
      error: (error) => {
        console.error('❌ Error loading tasks:', error);
        this.loadingSubject.next(false);
        // Keep existing tasks on error (don't clear the list)
      }
    });
  }

  /**
   * Add a newly created task to the local state immediately
   * Then trigger a background refresh to sync with backend
   */
  public addTask(task: Task): void {
    const currentTasks = this.tasksSubject.value;
    this.tasksSubject.next([task, ...currentTasks]); // Prepend new task

    // Background refresh after 2 seconds to get agent updates
    setTimeout(() => this.refreshTasks(true), 2000);
  }

  /**
   * Periodic background refresh every 60 seconds
   */
  private startPeriodicRefresh(): void {
    timer(0, 60000) // Initial fetch + every 60 seconds
      .pipe(
        tap(() => console.log('⏰ Periodic refresh triggered')),
        switchMap(() => this.taskApi.getTasks()),
        catchError((error) => {
          console.error('❌ Periodic refresh failed:', error);
          return []; // Return empty array on error
        })
      )
      .subscribe((tasks) => {
        if (tasks.length > 0 || this.tasksSubject.value.length === 0) {
          this.tasksSubject.next(tasks);
          this.loadingSubject.next(false);
        }
      });
  }

  /**
   * Filter tasks by status (client-side filtering)
   */
  public getFilteredTasks(status?: string): Observable<Task[]> {
    return this.tasks$.pipe(
      switchMap(tasks => {
        if (!status) {
          return [tasks];
        }
        return [tasks.filter(t => t.status === status)];
      })
    );
  }

  /**
   * Get current snapshot of tasks (synchronous)
   */
  public getCurrentTasks(): Task[] {
    return this.tasksSubject.value;
  }
}
