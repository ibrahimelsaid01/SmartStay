import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Meesages } from './meesages';

describe('Meesages', () => {
  let component: Meesages;
  let fixture: ComponentFixture<Meesages>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Meesages],
    }).compileComponents();

    fixture = TestBed.createComponent(Meesages);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
