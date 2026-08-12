import {
  ComponentFixture,
  TestBed,
} from '@angular/core/testing';

import { LocationMap } from './location-map';

describe('LocationMap', () => {
  let component: LocationMap;
  let fixture: ComponentFixture<LocationMap>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LocationMap],
    }).compileComponents();

    fixture = TestBed.createComponent(LocationMap);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should build OpenStreetMap links for valid coordinates', () => {
    component.latitude = 30.0444;
    component.longitude = 31.2357;

    expect(component.hasValidCoordinates).toBe(true);
    expect(component.mapUrl).toBeTruthy();
    expect(component.openMapUrl).toContain(
      'mlat=30.044400',
    );
    expect(component.openMapUrl).toContain(
      'mlon=31.235700',
    );
  });

  it('should show the empty state for invalid coordinates', () => {
    component.latitude = 91;
    component.longitude = 31.2357;

    expect(component.hasValidCoordinates).toBe(false);
    expect(component.mapUrl).toBeNull();
    expect(component.openMapUrl).toBe('');
  });
});