import { TestBed } from '@angular/core/testing';

import { TaskState } from './task-state';

describe('TaskState', () => {
  let service: TaskState;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(TaskState);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
