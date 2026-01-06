import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TaskFormComponent } from './components/task-form/task-form';
import { TaskListComponent } from './components/task-list/task-list';
import { SystemStatsComponent } from './components/system-stats/system-stats';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    TaskFormComponent,
    TaskListComponent,
    SystemStatsComponent
  ],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class AppComponent {
  title = 'Task Agent Dashboard';
}
