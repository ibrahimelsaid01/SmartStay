import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';

export interface PaymentMethod {
  id: number;
  brand: string;
  last4: string;
  expiry: string;
  isDefault?: boolean;
}

export interface AddPaymentMethodPayload {
  cardholderName: string;
  cardNumber: string;
  expirationDate: string;
  cvv: string;
  streetAddress: string;
  city: string;
  stateProvince: string;
  zipCode: string;
  country: string;
  saveForFuture: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class PaymentService {
  private mockPaymentMethods: PaymentMethod[] = [];
  private nextId = 1;

  getPaymentMethods(): Observable<PaymentMethod[]> {
    return of([...this.mockPaymentMethods]);
  }

  addPaymentMethod(
    payload: AddPaymentMethodPayload,
  ): Observable<PaymentMethod> {
    const last4 = payload.cardNumber
      .replace(/\s/g, '')
      .slice(-4);

    const brand = this.detectBrand(
      payload.cardNumber,
    );

    const newCard: PaymentMethod = {
      id: this.nextId++,
      brand,
      last4,
      expiry: payload.expirationDate,
      isDefault:
        this.mockPaymentMethods.length === 0,
    };

    this.mockPaymentMethods.push(newCard);

    return of({ ...newCard });
  }

  deletePaymentMethod(
    id: number,
  ): Observable<{ message: string }> {
    this.mockPaymentMethods =
      this.mockPaymentMethods.filter(
        (card) => card.id !== id,
      );

    return of({
      message:
        'Payment method removed locally. Saved-card backend API is not implemented yet.',
    });
  }

  setDefaultPaymentMethod(
    id: number,
  ): Observable<{ message: string }> {
    this.mockPaymentMethods =
      this.mockPaymentMethods.map(
        (card) => ({
          ...card,
          isDefault: card.id === id,
        }),
      );

    return of({
      message:
        'Default payment method updated locally.',
    });
  }

  private detectBrand(
    cardNumber: string,
  ): string {
    const number =
      cardNumber.replace(/\s/g, '');

    if (number.startsWith('4')) {
      return 'Visa';
    }

    if (
      number.startsWith('5') ||
      number.startsWith('2')
    ) {
      return 'MasterCard';
    }

    if (number.startsWith('3')) {
      return 'Amex';
    }

    return 'Card';
  }
}