import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HostProfile } from './host-profile';

describe('HostProfile', () => {
  let component: HostProfile;
  let fixture: ComponentFixture<HostProfile>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostProfile],
    }).compileComponents();

    fixture = TestBed.createComponent(HostProfile);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
