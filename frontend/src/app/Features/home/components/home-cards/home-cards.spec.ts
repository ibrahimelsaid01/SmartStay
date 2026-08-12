import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HomeCards } from './home-cards';

describe('HomeCards', () => {
  let component: HomeCards;
  let fixture: ComponentFixture<HomeCards>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HomeCards],
    }).compileComponents();

    fixture = TestBed.createComponent(HomeCards);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
