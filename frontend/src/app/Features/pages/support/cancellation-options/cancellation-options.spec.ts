import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CancellationOptions } from './cancellation-options';

describe('CancellationOptions', () => {
  let component: CancellationOptions;
  let fixture: ComponentFixture<CancellationOptions>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CancellationOptions],
    }).compileComponents();

    fixture = TestBed.createComponent(CancellationOptions);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
