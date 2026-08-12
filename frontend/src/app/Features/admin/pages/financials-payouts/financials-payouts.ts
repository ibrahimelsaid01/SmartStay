import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  OnInit,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { finalize } from 'rxjs';
import {
  AdminFinancialCurrencySummary,
  AdminFinancialTransaction,
  AdminFinancialTransactionsQuery,
  AdminFinancialTransactionsResponse,
  AdminFinancialTransactionType,
  AdminFinancialsService,
  AdminFinancialsSummaryResponse,
} from '../../services/admin-financials';

@Component({
  selector: 'app-financials-payouts',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
  ],
  templateUrl: './financials-payouts.html',
  styleUrl: './financials-payouts.css',
})
export class FinancialsPayouts implements OnInit {
  currencies: AdminFinancialCurrencySummary[] = [];
  transactions: AdminFinancialTransaction[] = [];

  summaryGeneratedAt: string | null = null;

  summaryLoading = false;
  transactionsLoading = false;

  summaryErrorMessage = '';
  transactionsErrorMessage = '';
  filterErrorMessage = '';

  search = '';
  type: AdminFinancialTransactionType = 'all';
  currency = '';
  status = '';
  fromDate = '';
  toDate = '';

  page = 1;
  pageSize = 10;
  totalPages = 1;
  totalCount = 0;

  private transactionRequestId = 0;

  readonly typeOptions: Array<{
    value: AdminFinancialTransactionType;
    label: string;
  }> = [
    {
      value: 'all',
      label: 'Payments and refunds',
    },
    {
      value: 'payment',
      label: 'Payments only',
    },
    {
      value: 'refund',
      label: 'Refunds only',
    },
  ];

  private readonly commonStatusOptions = [
    {
      value: '',
      label: 'All statuses',
    },
    {
      value: 'Pending',
      label: 'Pending',
    },
    {
      value: 'Succeeded',
      label: 'Succeeded',
    },
    {
      value: 'Failed',
      label: 'Failed',
    },
    {
      value: 'Cancelled',
      label: 'Cancelled',
    },
  ];

  private readonly paymentOnlyStatusOptions = [
    {
      value: 'PartiallyRefunded',
      label: 'Partially refunded',
    },
    {
      value: 'Refunded',
      label: 'Refunded',
    },
  ];

  private readonly refundOnlyStatusOptions = [
    {
      value: 'RequiresAction',
      label: 'Requires action',
    },
  ];

  constructor(
    private readonly adminFinancialsService:
      AdminFinancialsService,
    private readonly changeDetectorRef:
      ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadFinancials();
  }

  get isAnyLoading(): boolean {
    return (
      this.summaryLoading ||
      this.transactionsLoading
    );
  }

  get availableStatusOptions(): Array<{
    value: string;
    label: string;
  }> {
    if (this.type === 'payment') {
      return [
        ...this.commonStatusOptions,
        ...this.paymentOnlyStatusOptions,
      ];
    }

    if (this.type === 'refund') {
      return [
        ...this.commonStatusOptions,
        ...this.refundOnlyStatusOptions,
      ];
    }

    return this.commonStatusOptions;
  }

  loadFinancials(): void {
    this.loadSummary();
    this.loadTransactions(this.page);
  }

  loadSummary(): void {
    if (this.summaryLoading) {
      return;
    }

    this.summaryLoading = true;
    this.summaryErrorMessage = '';

    this.changeDetectorRef.detectChanges();

    this.adminFinancialsService
      .getSummary()
      .pipe(
        finalize(() => {
          this.summaryLoading = false;
          this.changeDetectorRef.detectChanges();
        }),
      )
      .subscribe({
        next: (
          response:
            AdminFinancialsSummaryResponse,
        ) => {
          this.summaryGeneratedAt =
            response.generatedAt;

          this.currencies =
            response.currencies ?? [];
        },

        error: (error: unknown) => {
          this.currencies = [];
          this.summaryGeneratedAt = null;

          this.summaryErrorMessage =
            this.extractErrorMessage(error) ||
            'Failed to load the financial summary.';
        },
      });
  }

  loadTransactions(
    page = this.page,
  ): void {
    if (this.transactionsLoading) {
      return;
    }

    if (!this.validateDateRange()) {
      this.changeDetectorRef.detectChanges();
      return;
    }

    const requestId =
      ++this.transactionRequestId;

    this.page = page;
    this.transactionsLoading = true;
    this.transactionsErrorMessage = '';
    this.filterErrorMessage = '';

    this.changeDetectorRef.detectChanges();

    const query:
      AdminFinancialTransactionsQuery = {
        search: this.search,
        type: this.type,
        currency: this.currency,
        status: this.status,

        fromDate: this.fromDate
          ? this.toLocalBoundaryIso(
              this.fromDate,
              false,
            )
          : undefined,

        toDate: this.toDate
          ? this.toLocalBoundaryIso(
              this.toDate,
              true,
            )
          : undefined,

        page: this.page,
        pageSize: this.pageSize,
      };

    this.adminFinancialsService
      .getTransactions(query)
      .pipe(
        finalize(() => {
          if (
            requestId ===
            this.transactionRequestId
          ) {
            this.transactionsLoading = false;
            this.changeDetectorRef.detectChanges();
          }
        }),
      )
      .subscribe({
        next: (
          response:
            AdminFinancialTransactionsResponse,
        ) => {
          if (
            requestId !==
            this.transactionRequestId
          ) {
            return;
          }

          this.transactions =
            response.items ?? [];

          this.totalCount =
            response.totalCount ?? 0;

          this.totalPages = Math.max(
            1,
            response.totalPages ?? 1,
          );

          this.page =
            response.page ||
            this.page;
        },

        error: (error: unknown) => {
          if (
            requestId !==
            this.transactionRequestId
          ) {
            return;
          }

          this.transactions = [];
          this.totalCount = 0;
          this.totalPages = 1;

          this.transactionsErrorMessage =
            this.extractErrorMessage(error) ||
            'Failed to load financial transactions.';
        },
      });
  }

  onTypeChanged(): void {
    const isSelectedStatusAvailable =
      this.availableStatusOptions.some(
        (option) =>
          option.value === this.status,
      );

    if (!isSelectedStatusAvailable) {
      this.status = '';
    }
  }

  applyFilters(): void {
    if (this.transactionsLoading) {
      return;
    }

    this.loadTransactions(1);
  }

  resetFilters(): void {
    if (this.transactionsLoading) {
      return;
    }

    this.search = '';
    this.type = 'all';
    this.currency = '';
    this.status = '';
    this.fromDate = '';
    this.toDate = '';
    this.filterErrorMessage = '';

    this.loadTransactions(1);
  }

  refreshAll(): void {
    if (this.isAnyLoading) {
      return;
    }

    this.loadFinancials();
  }

  goToPreviousPage(): void {
    if (
      this.page <= 1 ||
      this.transactionsLoading
    ) {
      return;
    }

    this.loadTransactions(
      this.page - 1,
    );
  }

  goToNextPage(): void {
    if (
      this.page >= this.totalPages ||
      this.transactionsLoading
    ) {
      return;
    }

    this.loadTransactions(
      this.page + 1,
    );
  }

  trackCurrency(
    _index: number,
    item: AdminFinancialCurrencySummary,
  ): string {
    return item.currency;
  }

  trackTransaction(
    _index: number,
    item: AdminFinancialTransaction,
  ): string {
    return item.transactionId;
  }

  getStatusClass(
    status: string,
  ): string {
    const normalizedStatus =
      status?.toLowerCase() ?? '';

    if (
      normalizedStatus.includes('success')
    ) {
      return 'status-success';
    }

    if (
      normalizedStatus.includes('pending') ||
      normalizedStatus.includes(
        'requiresaction',
      )
    ) {
      return 'status-pending';
    }

    if (
      normalizedStatus.includes('failed')
    ) {
      return 'status-failed';
    }

    if (
      normalizedStatus.includes('refund')
    ) {
      return 'status-refund';
    }

    if (
      normalizedStatus.includes('cancel')
    ) {
      return 'status-cancelled';
    }

    return 'status-default';
  }

  getDirectionClass(
    direction: string,
  ): string {
    const normalizedDirection =
      direction?.toLowerCase() ?? '';

    if (
      normalizedDirection === 'incoming'
    ) {
      return 'direction-in';
    }

    if (
      normalizedDirection === 'outgoing'
    ) {
      return 'direction-out';
    }

    return 'direction-default';
  }

  formatMoney(
    amount: number,
    currency: string,
  ): string {
    return `${(amount ?? 0).toLocaleString(
      undefined,
      {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      },
    )} ${currency || 'EGP'}`;
  }

  private validateDateRange(): boolean {
    this.filterErrorMessage = '';

    if (
      this.fromDate &&
      this.toDate &&
      this.fromDate > this.toDate
    ) {
      this.filterErrorMessage =
        'The From date must be earlier than or equal to the To date.';

      return false;
    }

    return true;
  }

  private toLocalBoundaryIso(
    dateValue: string,
    endOfDay: boolean,
  ): string {
    const [
      year,
      month,
      day,
    ] = dateValue
      .split('-')
      .map(Number);

    const date = new Date(
      year,
      month - 1,
      day,
      endOfDay ? 23 : 0,
      endOfDay ? 59 : 0,
      endOfDay ? 59 : 0,
      endOfDay ? 999 : 0,
    );

    return date.toISOString();
  }

  private extractErrorMessage(
    error: unknown,
  ): string {
    const typedError = error as {
      error?: unknown;
      message?: string;
      status?: number;
    };

    const parsedError =
      this.parseErrorBody(
        typedError.error,
      );

    if (parsedError) {
      const firstValidationError =
        parsedError.errors
          ? Object.values(
              parsedError.errors,
            )[0]?.[0]
          : undefined;

      return (
        parsedError.detail ||
        parsedError.message ||
        firstValidationError ||
        parsedError.title ||
        typedError.message ||
        ''
      );
    }

    if (
      typeof typedError.error === 'string' &&
      typedError.error.trim()
    ) {
      return typedError.error.trim();
    }

    if (typedError.status === 0) {
      return 'Cannot reach the server. Check your connection and try again.';
    }

    return typedError.message || '';
  }

  private parseErrorBody(
    errorBody: unknown,
  ):
    | {
        detail?: string;
        message?: string;
        title?: string;
        errors?: Record<
          string,
          string[]
        >;
      }
    | null {
    if (
      errorBody &&
      typeof errorBody === 'object'
    ) {
      return errorBody as {
        detail?: string;
        message?: string;
        title?: string;
        errors?: Record<
          string,
          string[]
        >;
      };
    }

    if (
      typeof errorBody !== 'string' ||
      !errorBody.trim()
    ) {
      return null;
    }

    try {
      return JSON.parse(errorBody) as {
        detail?: string;
        message?: string;
        title?: string;
        errors?: Record<
          string,
          string[]
        >;
      };
    } catch {
      return null;
    }
  }
}