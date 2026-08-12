import { CommonModule } from "@angular/common";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import {
  ChangeDetectorRef,
  Component,
  DestroyRef,
  OnInit,
  inject,
} from "@angular/core";
import { FormsModule } from "@angular/forms";
import { Router } from "@angular/router";
import {
  Observable,
  catchError,
  finalize,
  forkJoin,
  map,
  of,
  switchMap,
} from "rxjs";
import { Chatbot, ChatMessage } from "../../services/chatbot";
import {
  Feedback,
  FeedbackModel,
  ReportModel,
  SupportTicketAttachmentType,
  SupportTicketCategory,
  SupportTicketListItemResponse,
  SupportTicketResponse,
  SupportTicketStatus,
  SupportTicketUrgency,
} from "../../services/feedback";
import {
  GuestBookingListItem,
  GuestBookingsService,
} from "../../services/guest-bookings";
import { UserProfileService } from "../../services/user-profile-service";

interface ToastState {
  show: boolean;
  type: "success" | "danger";
  message: string;
}

interface SupportOption {
  id: string;
  title: string;
  subtitle: string;
  icon: string;
  actionType: "report" | "help" | "feedback" | "chat" | "tickets";
}

interface AttachmentTypeOption {
  value: SupportTicketAttachmentType;
  label: string;
  description: string;
}

interface EvidenceUploadAttempt {
  file: File;
  succeeded: boolean;
}

interface EvidenceUploadResult {
  ticket: SupportTicketResponse;
  succeededCount: number;
  failedCount: number;
  failedFiles: File[];
}

@Component({
  selector: "app-support",
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: "./support.html",
  styleUrl: "./support.css",
})
export class Support implements OnInit {
  userEmail = "";
  fullName = "";

  isSubmitting = false;
  reportFormError = "";

  isReportModalOpen = false;
  reportSubject = "";
  reportDescription = "";
  reportCategory: SupportTicketCategory = "TechnicalIssue";
  reportUrgency: SupportTicketUrgency = "High";
  reportBookingId = "";
  reportPropertyId = "";
  reportAttachmentType: SupportTicketAttachmentType = "IssueEvidence";
  selectedReportFiles: File[] = [];

  isFeedbackModalOpen = false;
  feedbackComments = "";
  selectedRating = 0;

  readonly ratingOptions: number[] = [1, 2, 3, 4, 5];

  tickets: SupportTicketListItemResponse[] = [];
  selectedTicket: SupportTicketResponse | null = null;
  selectedTicketIdLoading: string | null = null;

  isTicketsLoading = false;
  isTicketDetailsLoading = false;
  isMessageSubmitting = false;
  isAttachmentUploading = false;

  ticketSearch = "";
  ticketStatusFilter: SupportTicketStatus | "All" = "All";

  ticketsPage = 1;
  ticketsPageSize = 10;
  ticketsTotalPages = 1;
  ticketsTotalCount = 0;

  newTicketMessage = "";

  selectedTicketFiles: File[] = [];

  selectedAttachmentType: SupportTicketAttachmentType =
    "IssueEvidence";

  userBookings: GuestBookingListItem[] = [];
  isBookingsLoading = false;
  bookingLoadError = "";

  isChatOpen = false;
  newMessage = "";
  isBotTyping = false;
  chatMessages: ChatMessage[] = [];

  readonly maxAttachmentSizeInBytes =
    5 * 1024 * 1024;

  readonly maximumFilesPerBatch = 5;

  readonly suggestedQuestions: string[] = [
    "I want to make a new booking",
    "Can I change my booking dates?",
    "How do I track a refund?",
    "I want to cancel my booking",
  ];

  readonly categories: SupportTicketCategory[] = [
    "General",
    "PaymentIssue",
    "BookingIssue",
    "PropertyIssue",
    "HostIssue",
    "AccountIssue",
    "RefundIssue",
    "TechnicalIssue",
    "Other",
  ];

  readonly urgencies: SupportTicketUrgency[] = [
    "Low",
    "Medium",
    "High",
    "Critical",
  ];

  readonly ticketStatuses: Array<
    SupportTicketStatus | "All"
  > = [
    "All",
    "Open",
    "InProgress",
    "Resolved",
    "Closed",
  ];

  readonly attachmentTypeOptions:
    AttachmentTypeOption[] = [
      {
        value: "PropertyPhoto",
        label: "Property photo",
        description:
          "A photo showing the property or listing mismatch.",
      },
      {
        value: "SelfieAtProperty",
        label: "Selfie at property",
        description:
          "A selfie that helps verify your presence at the property.",
      },
      {
        value: "IssueEvidence",
        label: "Issue evidence",
        description:
          "A screenshot or photo showing the reported issue.",
      },
      {
        value: "PaymentEvidence",
        label: "Payment evidence",
        description:
          "A receipt or screenshot related to a payment or refund.",
      },
      {
        value: "Other",
        label: "Other image evidence",
        description:
          "Another relevant image that supports the report.",
      },
    ];

  readonly supportOptions: SupportOption[] = [
    {
      id: "report",
      title: "Report an issue",
      subtitle:
        "Create a support ticket with verified image evidence.",
      icon: "bi-exclamation-circle",
      actionType: "report",
    },
    {
      id: "tickets",
      title: "My support requests",
      subtitle:
        "Track replies, decisions, and uploaded evidence.",
      icon: "bi-ticket-detailed",
      actionType: "tickets",
    },
    {
      id: "help",
      title: "Visit the help center",
      subtitle:
        "Read common booking and refund answers.",
      icon: "bi-question-circle",
      actionType: "help",
    },
    {
      id: "feedback",
      title: "Share your feedback",
      subtitle:
        "Tell us what should be improved.",
      icon: "bi-pencil",
      actionType: "feedback",
    },
  ];

  toast: ToastState = {
    show: false,
    type: "success",
    message: "",
  };

  private readonly destroyRef =
    inject(DestroyRef);

  private autoDerivedReportPropertyId = "";

  constructor(
    public readonly router: Router,
    private readonly userProfileService:
      UserProfileService,
    private readonly feedBack: Feedback,
    private readonly guestBookingsService:
      GuestBookingsService,
    private readonly chatBot: Chatbot,
    private readonly cdr:
      ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.userProfileService.currentUser$
      .pipe(
        takeUntilDestroyed(
          this.destroyRef,
        ),
      )
      .subscribe({
        next: (profile) => {
          this.userEmail =
            profile?.email ?? "";

          this.fullName = [
            profile?.firstName,
            profile?.lastName,
          ]
            .filter(Boolean)
            .join(" ");

          this.cdr.detectChanges();
        },

        error: () => {
          this.userEmail = "";
          this.fullName = "";

          this.cdr.detectChanges();
        },
      });

    this.initWelcomeMessage();
    this.loadTickets();
    this.loadUserBookings();
  }

  get selectedReportBooking():
    GuestBookingListItem | null {
    const normalizedBookingId =
      this.reportBookingId
        .trim()
        .toLowerCase();

    if (!normalizedBookingId) {
      return null;
    }

    return (
      this.userBookings.find(
        (booking) =>
          booking.bookingId
            .trim()
            .toLowerCase() ===
          normalizedBookingId,
      ) ?? null
    );
  }

  get reportCategoryRequiresBooking():
    boolean {
    return [
      "PaymentIssue",
      "BookingIssue",
      "RefundIssue",
    ].includes(
      this.reportCategory,
    );
  }

  get isSelectedTicketReadOnly():
    boolean {
    return (
      this.selectedTicket?.status ===
        "Resolved" ||
      this.selectedTicket?.status ===
        "Closed"
    );
  }

  get selectedReportAttachmentDescription():
    string {
    return (
      this.attachmentTypeOptions.find(
        (option) =>
          option.value ===
          this.reportAttachmentType,
      )?.description ?? ""
    );
  }

  get selectedTicketAttachmentDescription():
    string {
    return (
      this.attachmentTypeOptions.find(
        (option) =>
          option.value ===
          this.selectedAttachmentType,
      )?.description ?? ""
    );
  }

  showToast(
    type: "success" | "danger",
    message: string,
  ): void {
    this.toast = {
      show: true,
      type,
      message,
    };

    this.cdr.detectChanges();

    setTimeout(() => {
      this.toast.show = false;
      this.cdr.detectChanges();
    }, 4500);
  }

  handleOptionClick(
    option: SupportOption,
  ): void {
    switch (option.actionType) {
      case "chat":
        this.isChatOpen = true;
        return;

      case "report":
        this.toggleReportModal(true);
        return;

      case "feedback":
        this.toggleFeedbackModal(true);
        return;

      case "tickets":
        this.loadTickets();
        this.scrollToTicketsSection();
        return;

      case "help":
      default:
        void this.router.navigate([
          "/help-center",
        ]);
    }
  }

  toggleReportModal(
    isOpen: boolean,
    forceClose = false,
  ): void {
    if (
      !isOpen &&
      this.isSubmitting &&
      !forceClose
    ) {
      return;
    }

    this.isReportModalOpen = isOpen;
    this.reportFormError = "";

    if (isOpen) {
      this.reportSubject = "";
      this.reportDescription = "";
      this.reportCategory =
        "TechnicalIssue";

      this.reportUrgency = "High";
      this.reportBookingId = "";
      this.reportPropertyId = "";

      this.autoDerivedReportPropertyId =
        "";

      this.reportAttachmentType =
        "IssueEvidence";

      this.selectedReportFiles = [];

      if (
        !this.userBookings.length &&
        !this.isBookingsLoading
      ) {
        this.loadUserBookings();
      }
    }

    this.cdr.detectChanges();
  }

  onReportCategoryChanged(): void {
    if (
      this.reportCategory ===
        "PaymentIssue" ||
      this.reportCategory ===
        "RefundIssue"
    ) {
      this.reportAttachmentType =
        "PaymentEvidence";
    } else if (
      this.reportCategory ===
        "PropertyIssue" ||
      this.reportCategory ===
        "HostIssue"
    ) {
      this.reportAttachmentType =
        "PropertyPhoto";
    } else {
      this.reportAttachmentType =
        "IssueEvidence";
    }

    this.reportFormError = "";

    this.cdr.detectChanges();
  }

  onReportBookingIdChanged(): void {
    const booking =
      this.selectedReportBooking;

    if (booking) {
      this.reportPropertyId =
        booking.property.id;

      this.autoDerivedReportPropertyId =
        booking.property.id;
    } else if (
      this.autoDerivedReportPropertyId &&
      this.reportPropertyId ===
        this.autoDerivedReportPropertyId
    ) {
      this.reportPropertyId = "";

      this.autoDerivedReportPropertyId =
        "";
    }

    this.reportFormError = "";

    this.cdr.detectChanges();
  }

  onReportFilesSelected(
    event: Event,
  ): void {
    const input =
      event.target as HTMLInputElement;

    const files = Array.from(
      input.files ?? [],
    );

    this.selectedReportFiles =
      this.filterValidFiles(files);

    input.value = "";

    this.cdr.detectChanges();
  }

  removeReportFile(
    index: number,
  ): void {
    if (this.isSubmitting) {
      return;
    }

    this.selectedReportFiles =
      this.selectedReportFiles.filter(
        (_file, fileIndex) =>
          fileIndex !== index,
      );

    this.cdr.detectChanges();
  }

  submitReport(): void {
    if (
      this.isSubmitting ||
      !this.validateReportForm()
    ) {
      this.cdr.detectChanges();
      return;
    }

    this.isSubmitting = true;
    this.reportFormError = "";

    this.cdr.detectChanges();

    const reportData: ReportModel = {
      subject:
        this.reportSubject.trim(),

      description:
        this.reportDescription.trim(),

      userEmail:
        this.userEmail,

      category:
        this.reportCategory,

      urgency:
        this.reportUrgency,

      bookingId:
        this.reportBookingId.trim() ||
        null,

      propertyId:
        this.reportPropertyId.trim() ||
        null,
    };

    this.feedBack
      .sendReport(reportData)
      .pipe(
        switchMap((ticket) =>
          this.uploadEvidenceBatch(
            ticket,
            this.selectedReportFiles,
            this.reportAttachmentType,
          ),
        ),

        finalize(() => {
          this.isSubmitting = false;
          this.cdr.detectChanges();
        }),

        takeUntilDestroyed(
          this.destroyRef,
        ),
      )
      .subscribe({
        next: (result) => {
          this.toggleReportModal(
            false,
            true,
          );

          this.selectedTicket =
            result.ticket;

          this.selectedReportFiles = [];
          this.ticketsPage = 1;

          this.loadTickets(false);

          if (
            result.failedCount > 0
          ) {
            this.selectedTicketFiles =
              result.failedFiles;

            this.selectedAttachmentType =
              this.reportAttachmentType;

            this.showToast(
              "danger",
              `The report was created. ${result.succeededCount} evidence image(s) uploaded and ${result.failedCount} failed. The failed files are ready to retry below.`,
            );

            setTimeout(() => {
              document
                .querySelector(
                  ".ticket-details-panel",
                )
                ?.scrollIntoView({
                  behavior: "smooth",
                  block: "start",
                });
            }, 50);

            return;
          }

          this.showToast(
            "success",
            result.succeededCount > 0
              ? "Your report and evidence images were sent successfully."
              : "Your report was sent successfully.",
          );
        },

        error: (error: unknown) => {
          this.reportFormError =
            this.extractErrorMessage(
              error,
              "Failed to create your report. Please try again.",
            );

          this.cdr.detectChanges();
        },
      });
  }

  toggleFeedbackModal(
    isOpen: boolean,
    forceClose = false,
  ): void {
    if (
      !isOpen &&
      this.isSubmitting &&
      !forceClose
    ) {
      return;
    }

    this.isFeedbackModalOpen =
      isOpen;

    if (isOpen) {
      this.selectedRating = 0;
      this.feedbackComments = "";
    }

    this.cdr.detectChanges();
  }

  selectRating(
    rating: number,
  ): void {
    if (this.isSubmitting) {
      return;
    }

    this.selectedRating = rating;
  }

  submitFeedback(): void {
    if (
      this.selectedRating <= 0 ||
      this.isSubmitting
    ) {
      return;
    }

    if (
      this.feedbackComments.length >
      3500
    ) {
      this.showToast(
        "danger",
        "Feedback comments cannot exceed 3500 characters.",
      );

      return;
    }

    this.isSubmitting = true;

    this.cdr.detectChanges();

    const feedbackData:
      FeedbackModel = {
        userName:
          this.fullName,

        email:
          this.userEmail,

        rating:
          this.selectedRating,

        comments:
          this.feedbackComments.trim(),
      };

    this.feedBack
      .sendFeedback(feedbackData)
      .pipe(
        finalize(() => {
          this.isSubmitting = false;
          this.cdr.detectChanges();
        }),

        takeUntilDestroyed(
          this.destroyRef,
        ),
      )
      .subscribe({
        next: () => {
          this.toggleFeedbackModal(
            false,
            true,
          );

          this.loadTickets();

          this.showToast(
            "success",
            "Thank you. Your feedback was sent to SmartStay support.",
          );
        },

        error: (error: unknown) => {
          this.showToast(
            "danger",
            this.extractErrorMessage(
              error,
              "Failed to send feedback. Please try again.",
            ),
          );
        },
      });
  }

  loadTickets(
    resetPage = true,
  ): void {
    if (this.isTicketsLoading) {
      return;
    }

    if (resetPage) {
      this.ticketsPage = 1;
    }

    this.isTicketsLoading = true;

    this.cdr.detectChanges();

    this.feedBack
      .getMyTickets({
        search:
          this.ticketSearch,

        status:
          this.ticketStatusFilter ===
          "All"
            ? null
            : this.ticketStatusFilter,

        page:
          this.ticketsPage,

        pageSize:
          this.ticketsPageSize,
      })
      .pipe(
        finalize(() => {
          this.isTicketsLoading =
            false;

          this.cdr.detectChanges();
        }),

        takeUntilDestroyed(
          this.destroyRef,
        ),
      )
      .subscribe({
        next: (response) => {
          this.tickets =
            response.items ?? [];

          this.ticketsPage =
            response.page ||
            this.ticketsPage;

          this.ticketsTotalPages =
            Math.max(
              1,
              response.totalPages ?? 1,
            );

          this.ticketsTotalCount =
            response.totalCount ?? 0;
        },

        error: (error: unknown) => {
          this.tickets = [];

          this.ticketsTotalPages = 1;
          this.ticketsTotalCount = 0;

          this.showToast(
            "danger",
            this.extractErrorMessage(
              error,
              "Failed to load your support requests.",
            ),
          );
        },
      });
  }

  openTicket(
    ticketId: string,
  ): void {
    if (
      !ticketId ||
      this.isTicketDetailsLoading
    ) {
      return;
    }

    this.isTicketDetailsLoading =
      true;

    this.selectedTicketIdLoading =
      ticketId;

    this.newTicketMessage = "";
    this.selectedTicketFiles = [];

    this.cdr.detectChanges();

    this.feedBack
      .getTicketById(ticketId)
      .pipe(
        finalize(() => {
          this.isTicketDetailsLoading =
            false;

          this.selectedTicketIdLoading =
            null;

          this.cdr.detectChanges();
        }),

        takeUntilDestroyed(
          this.destroyRef,
        ),
      )
      .subscribe({
        next: (ticket) => {
          this.selectedTicket =
            ticket;

          this.selectedAttachmentType =
            this.getDefaultAttachmentTypeForTicket(
              ticket,
            );
        },

        error: (error: unknown) => {
          this.showToast(
            "danger",
            this.extractErrorMessage(
              error,
              "Failed to load ticket details.",
            ),
          );
        },
      });
  }

  closeTicketDetails(): void {
    if (
      this.isTicketDetailsLoading ||
      this.isMessageSubmitting ||
      this.isAttachmentUploading
    ) {
      return;
    }

    this.selectedTicket = null;
    this.newTicketMessage = "";
    this.selectedTicketFiles = [];

    this.cdr.detectChanges();
  }

  sendTicketMessage(): void {
    const message =
      this.newTicketMessage.trim();

    if (
      !this.selectedTicket ||
      this.isSelectedTicketReadOnly ||
      !message ||
      this.isMessageSubmitting
    ) {
      return;
    }

    if (message.length > 4000) {
      this.showToast(
        "danger",
        "The follow-up message cannot exceed 4000 characters.",
      );

      return;
    }

    this.isMessageSubmitting =
      true;

    this.cdr.detectChanges();

    this.feedBack
      .addMessage(
        this.selectedTicket.ticketId,
        message,
      )
      .pipe(
        finalize(() => {
          this.isMessageSubmitting =
            false;

          this.cdr.detectChanges();
        }),

        takeUntilDestroyed(
          this.destroyRef,
        ),
      )
      .subscribe({
        next: (ticket) => {
          this.selectedTicket =
            ticket;

          this.newTicketMessage = "";

          this.loadTickets(false);
        },

        error: (error: unknown) => {
          this.showToast(
            "danger",
            this.extractErrorMessage(
              error,
              "Failed to send your message.",
            ),
          );
        },
      });
  }

  onTicketFilesSelected(
    event: Event,
  ): void {
    const input =
      event.target as HTMLInputElement;

    const files = Array.from(
      input.files ?? [],
    );

    this.selectedTicketFiles =
      this.filterValidFiles(files);

    input.value = "";

    this.cdr.detectChanges();
  }

  removeTicketFile(
    index: number,
  ): void {
    if (
      this.isAttachmentUploading
    ) {
      return;
    }

    this.selectedTicketFiles =
      this.selectedTicketFiles.filter(
        (_file, fileIndex) =>
          fileIndex !== index,
      );

    this.cdr.detectChanges();
  }

  uploadTicketAttachments(): void {
    if (
      !this.selectedTicket ||
      this.isSelectedTicketReadOnly ||
      !this.selectedTicketFiles.length ||
      this.isAttachmentUploading
    ) {
      return;
    }

    this.isAttachmentUploading =
      true;

    this.cdr.detectChanges();

    this.uploadEvidenceBatch(
      this.selectedTicket,
      this.selectedTicketFiles,
      this.selectedAttachmentType,
    )
      .pipe(
        finalize(() => {
          this.isAttachmentUploading =
            false;

          this.cdr.detectChanges();
        }),

        takeUntilDestroyed(
          this.destroyRef,
        ),
      )
      .subscribe({
        next: (result) => {
          this.selectedTicket =
            result.ticket;

          this.selectedTicketFiles =
            result.failedFiles;

          this.loadTickets(false);

          if (
            result.failedCount > 0
          ) {
            this.showToast(
              "danger",
              `${result.succeededCount} evidence image(s) uploaded and ${result.failedCount} failed. Please retry the failed files.`,
            );

            return;
          }

          this.showToast(
            "success",
            `${result.succeededCount} evidence image(s) uploaded successfully.`,
          );
        },

        error: (error: unknown) => {
          this.showToast(
            "danger",
            this.extractErrorMessage(
              error,
              "Failed to upload evidence.",
            ),
          );
        },
      });
  }

  goToTicketsPage(
    page: number,
  ): void {
    if (
      page < 1 ||
      page >
        this.ticketsTotalPages ||
      page === this.ticketsPage ||
      this.isTicketsLoading
    ) {
      return;
    }

    this.ticketsPage = page;

    this.loadTickets(false);
  }

  loadUserBookings(): void {
    if (
      this.isBookingsLoading
    ) {
      return;
    }

    this.isBookingsLoading = true;
    this.bookingLoadError = "";

    this.cdr.detectChanges();

    this.guestBookingsService
      .getMyBookings(1, 50)
      .pipe(
        finalize(() => {
          this.isBookingsLoading =
            false;

          this.cdr.detectChanges();
        }),

        takeUntilDestroyed(
          this.destroyRef,
        ),
      )
      .subscribe({
        next: (response) => {
          this.userBookings =
            response.items ?? [];
        },

        error: (error: unknown) => {
          this.userBookings = [];

          this.bookingLoadError =
            this.extractErrorMessage(
              error,
              "Failed to load your bookings. You can retry before submitting the report.",
            );
        },
      });
  }

  initWelcomeMessage(): void {
    this.chatMessages = [
      {
        text:
          "Welcome to SmartStay. It's your virtual assistant. How can I help you today?",

        isBot: true,

        timestamp:
          new Date(),
      },
    ];
  }

  sendUserMessage(
    messageText: string,
  ): void {
    const normalizedMessage =
      messageText.trim();

    if (
      !normalizedMessage ||
      this.isBotTyping
    ) {
      return;
    }

    this.chatMessages.push({
      text:
        normalizedMessage,

      isBot:
        false,

      timestamp:
        new Date(),
    });

    this.newMessage = "";
    this.isBotTyping = true;

    this.cdr.detectChanges();

    this.chatBot
      .sendMessage(
        normalizedMessage,
      )
      .pipe(
        finalize(() => {
          this.isBotTyping =
            false;

          this.cdr.detectChanges();
        }),

        takeUntilDestroyed(
          this.destroyRef,
        ),
      )
      .subscribe({
        next: (response) => {
          this.chatMessages.push({
            text:
              response.reply,

            isBot:
              true,

            timestamp:
              new Date(),
          });
        },

        error: () => {
          this.chatMessages.push({
            text:
              "Sorry, I'm having trouble connecting right now.",

            isBot:
              true,

            timestamp:
              new Date(),
          });
        },
      });
  }

  toggleChat(): void {
    this.isChatOpen =
      !this.isChatOpen;
  }

  formatStatus(
    status:
      string |
      null |
      undefined,
  ): string {
    return this.splitPascalCase(
      status,
    );
  }

  formatCategory(
    category:
      string |
      null |
      undefined,
  ): string {
    return this.splitPascalCase(
      category,
    );
  }

  formatUrgency(
    urgency:
      string |
      null |
      undefined,
  ): string {
    return this.splitPascalCase(
      urgency,
    );
  }

  formatAttachmentType(
    type:
      string |
      null |
      undefined,
  ): string {
    return this.splitPascalCase(
      type,
    );
  }

  getStatusBadgeClass(
    status:
      string |
      null |
      undefined,
  ): string {
    const normalizedStatus =
      status?.toLowerCase() ?? "";

    if (
      normalizedStatus ===
        "resolved" ||
      normalizedStatus ===
        "closed"
    ) {
      return "status-resolved";
    }

    if (
      normalizedStatus ===
      "inprogress"
    ) {
      return "status-progress";
    }

    return "status-open";
  }

  getUrgencyBadgeClass(
    urgency:
      string |
      null |
      undefined,
  ): string {
    const normalizedUrgency =
      urgency?.toLowerCase() ?? "";

    if (
      normalizedUrgency ===
      "critical"
    ) {
      return "urgency-critical";
    }

    if (
      normalizedUrgency ===
      "high"
    ) {
      return "urgency-high";
    }

    if (
      normalizedUrgency ===
      "medium"
    ) {
      return "urgency-medium";
    }

    return "urgency-low";
  }

  getAttachmentId(
    _index: number,
    attachment: {
      attachmentId: string;
    },
  ): string {
    return attachment.attachmentId;
  }

  getTicketPages(): number[] {
    const maximumVisiblePages = 7;

    const startPage = Math.max(
      1,
      Math.min(
        this.ticketsPage - 3,
        this.ticketsTotalPages -
          maximumVisiblePages +
          1,
      ),
    );

    const endPage = Math.min(
      this.ticketsTotalPages,
      startPage +
        maximumVisiblePages -
        1,
    );

    return Array.from(
      {
        length: Math.max(
          0,
          endPage -
            startPage +
            1,
        ),
      },

      (_value, index) =>
        startPage + index,
    );
  }

  formatFileSize(
    sizeInBytes:
      number |
      null |
      undefined,
  ): string {
    if (
      !sizeInBytes ||
      sizeInBytes <= 0
    ) {
      return "0 KB";
    }

    if (
      sizeInBytes <
      1024 * 1024
    ) {
      return `${Math.ceil(
        sizeInBytes / 1024,
      )} KB`;
    }

    return `${(
      sizeInBytes /
      (1024 * 1024)
    ).toFixed(1)} MB`;
  }

  getBookingOptionLabel(
    booking:
      GuestBookingListItem,
  ): string {
    return [
      booking.property.title,

      `${booking.checkInDate} → ${booking.checkOutDate}`,

      booking.status,
    ].join(" · ");
  }

  private uploadEvidenceBatch(
    ticket:
      SupportTicketResponse,

    files:
      File[],

    type:
      SupportTicketAttachmentType,
  ): Observable<EvidenceUploadResult> {
    if (!files.length) {
      return of({
        ticket,

        succeededCount:
          0,

        failedCount:
          0,

        failedFiles:
          [],
      });
    }

    const uploadRequests =
      files.map((file) =>
        this.feedBack
          .uploadAttachment(
            ticket.ticketId,
            file,
            type,
          )
          .pipe(
            map(
              (): EvidenceUploadAttempt => ({
                file,
                succeeded: true,
              }),
            ),

            catchError(
              (): Observable<EvidenceUploadAttempt> =>
                of({
                  file,
                  succeeded: false,
                }),
            ),
          ),
      );

    return forkJoin(
      uploadRequests,
    ).pipe(
      switchMap((attempts) =>
        this.feedBack
          .getTicketById(
            ticket.ticketId,
          )
          .pipe(
            catchError(() =>
              of(ticket),
            ),

            map(
              (updatedTicket) => ({
                ticket:
                  updatedTicket,

                succeededCount:
                  attempts.filter(
                    (attempt) =>
                      attempt.succeeded,
                  ).length,

                failedCount:
                  attempts.filter(
                    (attempt) =>
                      !attempt.succeeded,
                  ).length,

                failedFiles:
                  attempts
                    .filter(
                      (attempt) =>
                        !attempt.succeeded,
                    )
                    .map(
                      (attempt) =>
                        attempt.file,
                    ),
              }),
            ),
          ),
      ),
    );
  }

  private validateReportForm():
    boolean {
    this.reportFormError = "";

    const subject =
      this.reportSubject.trim();

    const description =
      this.reportDescription.trim();

    const bookingId =
      this.reportBookingId.trim();

    const propertyId =
      this.reportPropertyId.trim();

    if (subject.length < 3) {
      this.reportFormError =
        "Subject must contain at least 3 characters.";

      return false;
    }

    if (subject.length > 200) {
      this.reportFormError =
        "Subject cannot exceed 200 characters.";

      return false;
    }

    if (
      description.length < 10
    ) {
      this.reportFormError =
        "Description must contain at least 10 characters.";

      return false;
    }

    if (
      description.length > 4000
    ) {
      this.reportFormError =
        "Description cannot exceed 4000 characters.";

      return false;
    }

    if (
      this.reportCategoryRequiresBooking &&
      !bookingId
    ) {
      this.reportFormError =
        "A booking must be linked to payment, booking, and refund issues.";

      return false;
    }

    if (
      bookingId &&
      !this.selectedReportBooking
    ) {
      this.reportFormError =
        "Choose a booking from your SmartStay booking list instead of entering an unknown booking ID.";

      return false;
    }

    if (
      bookingId &&
      !this.isGuid(bookingId)
    ) {
      this.reportFormError =
        "Booking ID must be a valid identifier.";

      return false;
    }

    if (
      propertyId &&
      !this.isGuid(propertyId)
    ) {
      this.reportFormError =
        "Property ID must be a valid identifier.";

      return false;
    }

    return true;
  }

  private filterValidFiles(
    files: File[],
  ): File[] {
    const limitedFiles =
      files.slice(
        0,
        this.maximumFilesPerBatch,
      );

    if (
      files.length >
      this.maximumFilesPerBatch
    ) {
      this.showToast(
        "danger",
        `You can upload up to ${this.maximumFilesPerBatch} evidence images in one batch.`,
      );
    }

    const uniqueFiles =
      new Map<string, File>();

    for (
      const file of limitedFiles
    ) {
      const fileKey =
        `${file.name.toLowerCase()}-${file.size}`;

      if (
        uniqueFiles.has(fileKey)
      ) {
        continue;
      }

      if (file.size <= 0) {
        this.showToast(
          "danger",
          `${file.name} is empty and was not selected.`,
        );

        continue;
      }

      if (
        file.size >
        this.maxAttachmentSizeInBytes
      ) {
        this.showToast(
          "danger",
          `${file.name} is larger than 5 MB and was not selected.`,
        );

        continue;
      }

      if (
        !this.isSupportedImage(
          file,
        )
      ) {
        this.showToast(
          "danger",
          `${file.name} is not a supported image. Use JPG, JPEG, PNG, or WebP.`,
        );

        continue;
      }

      uniqueFiles.set(
        fileKey,
        file,
      );
    }

    return Array.from(
      uniqueFiles.values(),
    );
  }

  private isSupportedImage(
    file: File,
  ): boolean {
    const extension =
      `.${file.name
        .split(".")
        .pop()
        ?.toLowerCase() ?? ""}`;

    const allowedMimeTypes =
      new Set([
        "image/jpeg",
        "image/png",
        "image/webp",
      ]);

    const allowedExtensions =
      new Set([
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
      ]);

    return (
      allowedMimeTypes.has(
        file.type,
      ) &&
      allowedExtensions.has(
        extension,
      )
    );
  }

  private getDefaultAttachmentTypeForTicket(
    ticket:
      SupportTicketResponse,
  ): SupportTicketAttachmentType {
    if (
      ticket.category ===
        "PaymentIssue" ||
      ticket.category ===
        "RefundIssue"
    ) {
      return "PaymentEvidence";
    }

    if (
      ticket.category ===
        "PropertyIssue" ||
      ticket.category ===
        "HostIssue"
    ) {
      return "PropertyPhoto";
    }

    return "IssueEvidence";
  }

  private splitPascalCase(
    value:
      string |
      null |
      undefined,
  ): string {
    if (!value) {
      return "N/A";
    }

    return value.replace(
      /([a-z])([A-Z])/g,
      "$1 $2",
    );
  }

  private scrollToTicketsSection():
    void {
    setTimeout(() => {
      document
        .getElementById(
          "support-tickets-section",
        )
        ?.scrollIntoView({
          behavior: "smooth",
          block: "start",
        });
    }, 50);
  }

  private isGuid(
    value: string,
  ): boolean {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(
      value.trim(),
    );
  }

  private extractErrorMessage(
    error: unknown,
    fallbackMessage: string,
  ): string {
    const typedError =
      error as {
        error?: unknown;
        message?: string;
        status?: number;
      };

    if (
      typedError.error &&
      typeof typedError.error ===
        "object"
    ) {
      const errorBody =
        typedError.error as {
          detail?: string;
          message?: string;
          title?: string;
          errors?: Record<
            string,
            string[]
          >;
        };

      const firstValidationError =
        errorBody.errors
          ? Object.values(
              errorBody.errors,
            )[0]?.[0]
          : undefined;

      return (
        errorBody.detail ||
        errorBody.message ||
        firstValidationError ||
        errorBody.title ||
        typedError.message ||
        fallbackMessage
      );
    }

    if (
      typeof typedError.error ===
        "string" &&
      typedError.error.trim()
    ) {
      try {
        const parsedError =
          JSON.parse(
            typedError.error,
          ) as {
            detail?: string;
            message?: string;
            title?: string;
            errors?: Record<
              string,
              string[]
            >;
          };

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
          fallbackMessage
        );
      } catch {
        return typedError.error.trim();
      }
    }

    if (
      typedError.status === 0
    ) {
      return "Cannot reach the server. Check your internet connection and try again.";
    }

    return (
      typedError.message ||
      fallbackMessage
    );
  }
}