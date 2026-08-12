import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PropertyGallery } from './property-gallery';

describe('PropertyGallery', () => {
  let component: PropertyGallery;
  let fixture: ComponentFixture<PropertyGallery>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PropertyGallery],
    }).compileComponents();

    fixture = TestBed.createComponent(PropertyGallery);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
