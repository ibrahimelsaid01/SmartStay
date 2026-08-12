import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MyReviews } from './my-reviews';

describe('MyReviews', () => {
  let component: MyReviews;
  let fixture: ComponentFixture<MyReviews>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MyReviews],
    }).compileComponents();

    fixture = TestBed.createComponent(MyReviews);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
