import { TestBed } from '@angular/core/testing';

import { HostDashboardservice } from './HostDashboardservice';

describe('HostDashboardservice', () => {
  let service: HostDashboardservice;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(HostDashboardservice);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
