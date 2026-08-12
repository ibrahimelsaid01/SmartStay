import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StayCard } from './stay-card';

describe('StayCard', () => {
  let component: StayCard;
  let fixture: ComponentFixture<StayCard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StayCard],
    }).compileComponents();

    fixture = TestBed.createComponent(StayCard);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
