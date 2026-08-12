import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Propertydetails } from './propertydetails';

describe('Propertydetails', () => {
  let component: Propertydetails;
  let fixture: ComponentFixture<Propertydetails>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Propertydetails],
    }).compileComponents();

    fixture = TestBed.createComponent(Propertydetails);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
