export interface Task {
  id: string;
  title: string;
  description: string | null;
  status: 'Pending' | 'Active' | 'Snoozed' | 'Escalated' | 'Completed' | 'Rejected';
  priority: 'Low' | 'Medium' | 'High' | 'Critical';
  createdAt: string;
  updatedAt: string;
  dueDate: string | null;
  completedAt: string | null;
  snoozedUntil: string | null;
  escalationCount: number;
}

export interface CreateTaskRequest {
  title: string;
  description: string | null;
  priority: string;
  dueDate: string | null;
}

export interface SystemStats {
  totalTasks: number;
  pendingTasks: number;
  activeTasks: number;
  snoozedTasks: number;
  escalatedTasks: number;
  completedTasks: number;
  rejectedTasks: number;
  overdueTasks: number;
}
