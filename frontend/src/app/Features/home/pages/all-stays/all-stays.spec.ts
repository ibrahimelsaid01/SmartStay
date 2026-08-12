import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AllStays } from './all-stays';

describe('AllStays', () => {
  let component: AllStays;
  let fixture: ComponentFixture<AllStays>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AllStays],
    }).compileComponents();

    fixture = TestBed.createComponent(AllStays);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
