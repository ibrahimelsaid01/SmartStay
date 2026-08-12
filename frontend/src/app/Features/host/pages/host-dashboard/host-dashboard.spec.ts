import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HostDashboard } from './host-dashboard';

describe('HostDashboard', () => {
  let component: HostDashboard;
  let fixture: ComponentFixture<HostDashboard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostDashboard],
    }).compileComponents();

    fixture = TestBed.createComponent(HostDashboard);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
