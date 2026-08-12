import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CityCard } from './city-card';

describe('CityCard', () => {
  let component: CityCard;
  let fixture: ComponentFixture<CityCard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CityCard],
    }).compileComponents();

    fixture = TestBed.createComponent(CityCard);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
