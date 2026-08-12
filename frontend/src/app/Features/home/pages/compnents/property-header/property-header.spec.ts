import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PropertyHeader } from './property-header';

describe('PropertyHeader', () => {
  let component: PropertyHeader;
  let fixture: ComponentFixture<PropertyHeader>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PropertyHeader],
    }).compileComponents();

    fixture = TestBed.createComponent(PropertyHeader);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
