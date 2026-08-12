import { TestBed } from '@angular/core/testing';

import { Propertydetailservice } from './propertydetailservice';

describe('Propertydetailservice', () => {
  let service: Propertydetailservice;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Propertydetailservice);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
