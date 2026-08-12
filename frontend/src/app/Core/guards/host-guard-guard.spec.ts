import { TestBed } from '@angular/core/testing';
import { CanActivateFn } from '@angular/router';

import { hostGuard } from './host-guard-guard';

describe('hostGuardGuard', () => {
  const executeGuard: CanActivateFn = (...guardParameters) =>
    TestBed.runInInjectionContext(() => hostGuard(...guardParameters));

  beforeEach(() => {
    TestBed.configureTestingModule({});
  });

  it('should be created', () => {
    expect(executeGuard).toBeTruthy();
  });
});
