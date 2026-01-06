import { Component, OnInit } from '@angular/core';
import { TaskApiService } from '../../services/task-api';
import { SystemStats } from '../../models/task.model';
import { interval } from 'rxjs';
import { startWith, switchMap } from 'rxjs/operators';
import {CommonModule} from '@angular/common';

@Component({
  selector: 'app-system-stats',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './system-stats.html',
  styleUrls: ['./system-stats.css']
})
export class SystemStatsComponent implements OnInit {
  stats: SystemStats | null = null;

  constructor(private taskApi: TaskApiService) {}

  ngOnInit(): void {
    // Auto-refresh every 3 seconds
    interval(3000)
      .pipe(
        startWith(0),
        switchMap(() => this.taskApi.getStats())
      )
      .subscribe({
        next: (stats) => {
          this.stats = stats;
        },
        error: (error) => {
          console.error('Error loading stats:', error);
        }
      });
  }
}
