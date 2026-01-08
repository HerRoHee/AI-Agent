import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TaskApiService } from '../../services/task-api';
import { SystemStats } from '../../models/task.model';
import { Observable, timer } from 'rxjs';
import { switchMap, shareReplay } from 'rxjs/operators';

@Component({
  selector: 'app-system-stats',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './system-stats.html',
  styleUrls: ['./system-stats.css']
})
export class SystemStatsComponent implements OnInit {
  stats$!: Observable<SystemStats>;

  constructor(private taskApi: TaskApiService) {}

  ngOnInit(): void {
    // Fetch stats every 60 seconds
    this.stats$ = timer(0, 60000).pipe(
      switchMap(() => this.taskApi.getStats()),
      shareReplay(1) // Cache latest value
    );
  }
}
