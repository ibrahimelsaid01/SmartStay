import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { PaymentService, PaymentMethod } from '../../services/payment';

@Component({
  selector: 'app-payment-account',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './payment-account.html',
  styleUrl: './payment-account.css',
})
export class PaymentAccount implements OnInit {

  paymentMethods: PaymentMethod[] = [];
  isLoading = true;
  showModal = false;
  isSaving = false;

  paymentForm: FormGroup;

  constructor(
    private paymentService: PaymentService,
    private fb: FormBuilder
  ) {
    this.paymentForm = this.fb.group({
      cardholderName: ['', [Validators.required]],
      cardNumber: ['', [Validators.required, Validators.minLength(19)]],
      expirationDate: ['', [Validators.required, Validators.pattern(/^\d{2}\/\d{2}$/)]],
      cvv: ['', [Validators.required, Validators.minLength(3)]],
      streetAddress: ['', [Validators.required]],
      city: ['', [Validators.required]],
      stateProvince: ['', [Validators.required]],
      zipCode: ['', [Validators.required]],
      country: ['', [Validators.required]],
      saveForFuture: [false],
    });
  }

  ngOnInit(): void {
    this.loadPaymentMethods();
  }

  loadPaymentMethods(): void {
    this.paymentService.getPaymentMethods().subscribe({
      next: (data) => {
        this.paymentMethods = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading payment methods', err);
        this.isLoading = false;
      }
    });
  }

  openModal(): void {
    this.paymentForm.reset({ saveForFuture: false });
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
  }

  onCardNumberInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    let value = input.value.replace(/\D/g, '').slice(0, 16);
    value = value.replace(/(.{4})/g, '$1 ').trim();
    input.value = value;
    this.paymentForm.get('cardNumber')?.setValue(value, { emitEvent: false });
  }

  onExpiryInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    let value = input.value.replace(/\D/g, '').slice(0, 4);
    if (value.length >= 3) {
      value = value.slice(0, 2) + '/' + value.slice(2);
    }
    input.value = value;
    this.paymentForm.get('expirationDate')?.setValue(value, { emitEvent: false });
  }

  onCvvInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    input.value = input.value.replace(/\D/g, '').slice(0, 4);
    this.paymentForm.get('cvv')?.setValue(input.value, { emitEvent: false });
  }

  saveCard(): void {
    if (this.paymentForm.invalid) {
      this.paymentForm.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.paymentService.addPaymentMethod(this.paymentForm.value).subscribe({
      next: (newCard) => {
        this.paymentMethods.push(newCard);
        this.isSaving = false;
        this.closeModal();
      },
      error: (err) => {
        console.error('Error adding payment method', err);
        this.isSaving = false;
      }
    });
  }

  deleteCard(id: number): void {
    this.paymentService.deletePaymentMethod(id).subscribe({
      next: () => {
        this.paymentMethods = this.paymentMethods.filter(c => c.id !== id);
      },
      error: (err) => console.error('Error deleting payment method', err)
    });
  }

  setDefault(id: number): void {
    this.paymentService.setDefaultPaymentMethod(id).subscribe({
      next: () => {
        this.paymentMethods = this.paymentMethods.map(card => ({
          ...card,
          isDefault: card.id === id,
        }));
      },
      error: (err) => console.error('Error setting default', err)
    });
  }

  isFieldInvalid(field: string): boolean {
    const control = this.paymentForm.get(field);
    return !!(control && control.invalid && control.touched);
  }
}