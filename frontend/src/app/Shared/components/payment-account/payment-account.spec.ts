import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PaymentAccount } from './payment-account';

describe('PaymentAccount', () => {
  let component: PaymentAccount;
  let fixture: ComponentFixture<PaymentAccount>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PaymentAccount],
    }).compileComponents();

    fixture = TestBed.createComponent(PaymentAccount);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
