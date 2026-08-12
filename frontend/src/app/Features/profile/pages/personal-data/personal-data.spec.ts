import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PersonalData } from './personal-data';

describe('PersonalData', () => {
  let component: PersonalData;
  let fixture: ComponentFixture<PersonalData>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PersonalData],
    }).compileComponents();

    fixture = TestBed.createComponent(PersonalData);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
