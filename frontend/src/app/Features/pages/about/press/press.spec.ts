import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Press } from './press';

describe('Press', () => {
  let component: Press;
  let fixture: ComponentFixture<Press>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Press],
    }).compileComponents();

    fixture = TestBed.createComponent(Press);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
