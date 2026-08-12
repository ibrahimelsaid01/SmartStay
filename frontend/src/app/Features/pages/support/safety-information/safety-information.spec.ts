import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SafetyInformationComponent } from './safety-information';

describe('SafetyInformationComponent', () => {
  let component: SafetyInformationComponent;
  let fixture: ComponentFixture<SafetyInformationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SafetyInformationComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(SafetyInformationComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});