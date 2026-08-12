import {
  HttpClient,
  HttpParams,
} from "@angular/common/http";
import { Injectable } from "@angular/core";
import {
  Observable,
  map,
  timeout,
} from "rxjs";
import { environment } from "../../../../environments/environment";

export interface FeedbackModel {
  userName: string;
  email: string;
  rating: number;
  comments: string;
}

export interface ReportModel {
  subject: string;
  description: string;
  userEmail: string;
  bookingId?: string | null;
  propertyId?: string | null;
  category?: SupportTicketCategory;
  urgency?: SupportTicketUrgency;
}

export interface CreateSupportTicketRequest {
  subject: string;
  description: string;
  category: SupportTicketCategory;
  urgency: SupportTicketUrgency;
  bookingId?: string | null;
  propertyId?: string | null;
}

export interface CreateSupportTicketMessageRequest {
  message: string;
}

export interface SupportTicketSearchRequest {
  search?: string | null;
  status?:
    | SupportTicketStatus
    | string
    | null;

  category?:
    | SupportTicketCategory
    | string
    | null;

  urgency?:
    | SupportTicketUrgency
    | string
    | null;

  page?: number;
  pageSize?: number;
}

export type SupportTicketCategory =
  | "General"
  | "PaymentIssue"
  | "BookingIssue"
  | "PropertyIssue"
  | "HostIssue"
  | "AccountIssue"
  | "RefundIssue"
  | "TechnicalIssue"
  | "Other";

export type SupportTicketUrgency =
  | "Low"
  | "Medium"
  | "High"
  | "Critical";

export type SupportTicketStatus =
  | "Open"
  | "InProgress"
  | "Resolved"
  | "Closed";

export type SupportTicketDecisionStatus =
  | "NoDecision"
  | "ValidComplaint"
  | "InvalidComplaint"
  | "NeedsMoreEvidence";

export type SupportTicketDecisionAction =
  | "NoAction"
  | "PartialRefundRecommended"
  | "FullRefundRecommended"
  | "HostWarningRecommended"
  | "HidePropertyRecommended"
  | "HoldPayoutRecommended"
  | "ReleasePayoutRecommended";

export type SupportTicketAttachmentType =
  | "PropertyPhoto"
  | "SelfieAtProperty"
  | "IssueEvidence"
  | "PaymentEvidence"
  | "Other";

export interface SupportTicketMessageResponse {
  messageId: string;
  senderUserId: string;
  senderName: string;
  senderEmail?: string | null;
  isAdminMessage: boolean;
  message: string;
  createdAt: string;
}

export interface SupportTicketAttachmentResponse {
  attachmentId: string;
  uploadedByUserId: string;
  uploadedByName: string;
  uploadedByEmail?: string | null;
  type:
    | SupportTicketAttachmentType
    | string;

  url: string;
  fileName: string;
  contentType: string;
  fileSizeInBytes: number;
  createdAt: string;
}

export interface UploadSupportTicketAttachmentResponse {
  ticketId: string;
  attachmentId: string;
  type:
    | SupportTicketAttachmentType
    | string;

  url: string;
  fileName: string;
  contentType: string;
  fileSizeInBytes: number;
  createdAt: string;
  message: string;
}

export interface SupportTicketResponse {
  ticketId: string;
  referenceCode: string;

  createdByUserId: string;
  createdByName: string;
  createdByEmail?: string | null;

  bookingId?: string | null;
  propertyId?: string | null;
  propertyTitle?: string | null;

  subject: string;
  description: string;

  category:
    | SupportTicketCategory
    | string;

  urgency:
    | SupportTicketUrgency
    | string;

  status:
    | SupportTicketStatus
    | string;

  decisionStatus:
    | SupportTicketDecisionStatus
    | string;

  decisionAction:
    | SupportTicketDecisionAction
    | string;

  decisionNote?: string | null;

  decidedAt?: string | null;
  decidedByAdminId?: string | null;
  decidedByAdminName?: string | null;

  createdAt: string;
  updatedAt: string;

  resolvedAt?: string | null;
  resolutionNote?: string | null;

  messages:
    SupportTicketMessageResponse[];

  attachments:
    SupportTicketAttachmentResponse[];
}

export interface SupportTicketListItemResponse {
  ticketId: string;
  referenceCode: string;
  subject: string;

  category:
    | SupportTicketCategory
    | string;

  urgency:
    | SupportTicketUrgency
    | string;

  status:
    | SupportTicketStatus
    | string;

  createdByUserId: string;
  createdByName: string;
  createdByEmail?: string | null;

  bookingId?: string | null;
  propertyId?: string | null;
  propertyTitle?: string | null;

  messagesCount: number;

  createdAt: string;
  updatedAt: string;
  resolvedAt?: string | null;
}

export interface SupportTicketsResponse {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;

  items:
    SupportTicketListItemResponse[];
}

@Injectable({
  providedIn: "root",
})
export class Feedback {
  private readonly supportTicketsApiUrl =
    `${environment.baseApi}/api/support/tickets`;

  private readonly requestTimeoutMs =
    30000;

  constructor(
    private readonly http: HttpClient,
  ) {}

  sendFeedback(
    feedback: FeedbackModel,
  ): Observable<SupportTicketResponse> {
    const payload:
      CreateSupportTicketRequest = {
        subject:
          `User feedback - ${feedback.rating}/5`,

        description:
          this.buildFeedbackDescription(
            feedback,
          ),

        category:
          "General",

        urgency:
          this.getFeedbackUrgency(
            feedback.rating,
          ),

        bookingId:
          null,

        propertyId:
          null,
      };

    return this.createTicket(
      payload,
    );
  }

  sendReport(
    report: ReportModel,
  ): Observable<SupportTicketResponse> {
    const payload:
      CreateSupportTicketRequest = {
        subject:
          report.subject.trim(),

        description:
          this.buildReportDescription(
            report,
          ),

        category:
          report.category ??
          "TechnicalIssue",

        urgency:
          report.urgency ??
          "High",

        bookingId:
          this.toNullableGuid(
            report.bookingId,
          ),

        propertyId:
          this.toNullableGuid(
            report.propertyId,
          ),
      };

    return this.createTicket(
      payload,
    );
  }

  createTicket(
    request:
      CreateSupportTicketRequest,
  ): Observable<SupportTicketResponse> {
    const payload:
      CreateSupportTicketRequest = {
        subject:
          request.subject.trim(),

        description:
          request.description.trim(),

        category:
          request.category,

        urgency:
          request.urgency,

        bookingId:
          request.bookingId ??
          null,

        propertyId:
          request.propertyId ??
          null,
      };

    return this.http
      .post(
        this.supportTicketsApiUrl,
        payload,
        {
          responseType: "text",
          withCredentials: true,
        },
      )
      .pipe(
        timeout(
          this.requestTimeoutMs,
        ),

        map((responseText) =>
          this.mapTicketDetails(
            this.parseRequiredJson<unknown>(
              responseText,
            ),
          ),
        ),
      );
  }

  getMyTickets(
    request:
      SupportTicketSearchRequest = {},
  ): Observable<SupportTicketsResponse> {
    const page =
      request.page ?? 1;

    const pageSize =
      request.pageSize ?? 10;

    const params =
      this.buildSearchParams(
        request,
        page,
        pageSize,
      );

    return this.http
      .get(
        `${this.supportTicketsApiUrl}/my-tickets`,
        {
          params,
          responseType: "text",
          withCredentials: true,
        },
      )
      .pipe(
        timeout(
          this.requestTimeoutMs,
        ),

        map((responseText) => {
          const normalizedResponse =
            this.normalizeResponseText(
              responseText,
            );

          if (!normalizedResponse) {
            return this.createEmptyTicketsResponse(
              page,
              pageSize,
            );
          }

          const response =
            this.parseJson<any>(
              normalizedResponse,
            );

          const rawItems =
            response.items ??
            response.Items ??
            [];

          return {
            page:
              response.page ??
              response.Page ??
              page,

            pageSize:
              response.pageSize ??
              response.PageSize ??
              pageSize,

            totalCount:
              response.totalCount ??
              response.TotalCount ??
              0,

            totalPages:
              Math.max(
                1,
                response.totalPages ??
                  response.TotalPages ??
                  1,
              ),

            items:
              Array.isArray(
                rawItems,
              )
                ? rawItems.map(
                    (
                      item:
                        unknown,
                    ) =>
                      this.mapTicketSummary(
                        item,
                      ),
                  )
                : [],
          };
        }),
      );
  }

  getTicketById(
    ticketId: string,
  ): Observable<SupportTicketResponse> {
    return this.http
      .get(
        `${this.supportTicketsApiUrl}/${ticketId}`,
        {
          responseType: "text",
          withCredentials: true,
        },
      )
      .pipe(
        timeout(
          this.requestTimeoutMs,
        ),

        map((responseText) =>
          this.mapTicketDetails(
            this.parseRequiredJson<unknown>(
              responseText,
            ),
          ),
        ),
      );
  }

  addMessage(
    ticketId: string,
    message: string,
  ): Observable<SupportTicketResponse> {
    const payload:
      CreateSupportTicketMessageRequest = {
        message:
          message.trim(),
      };

    return this.http
      .post(
        `${this.supportTicketsApiUrl}/${ticketId}/messages`,
        payload,
        {
          responseType: "text",
          withCredentials: true,
        },
      )
      .pipe(
        timeout(
          this.requestTimeoutMs,
        ),

        map((responseText) =>
          this.mapTicketDetails(
            this.parseRequiredJson<unknown>(
              responseText,
            ),
          ),
        ),
      );
  }

  uploadAttachment(
    ticketId: string,
    file: File,
    type:
      SupportTicketAttachmentType =
        "IssueEvidence",
  ): Observable<UploadSupportTicketAttachmentResponse> {
    const formData =
      new FormData();

    formData.append(
      "file",
      file,
      file.name,
    );

    formData.append(
      "type",
      type,
    );

    return this.http
      .post(
        `${this.supportTicketsApiUrl}/${ticketId}/attachments`,
        formData,
        {
          responseType: "text",
          withCredentials: true,
        },
      )
      .pipe(
        timeout(
          this.requestTimeoutMs,
        ),

        map((responseText) =>
          this.mapUploadResponse(
            this.parseRequiredJson<unknown>(
              responseText,
            ),
          ),
        ),
      );
  }

  private buildSearchParams(
    request:
      SupportTicketSearchRequest,

    page: number,
    pageSize: number,
  ): HttpParams {
    let params =
      new HttpParams()
        .set(
          "page",
          String(page),
        )
        .set(
          "pageSize",
          String(pageSize),
        )
        .set(
          "_ts",
          String(Date.now()),
        );

    if (
      request.search?.trim()
    ) {
      params =
        params.set(
          "search",
          request.search.trim(),
        );
    }

    if (
      request.status?.trim()
    ) {
      params =
        params.set(
          "status",
          request.status.trim(),
        );
    }

    if (
      request.category?.trim()
    ) {
      params =
        params.set(
          "category",
          request.category.trim(),
        );
    }

    if (
      request.urgency?.trim()
    ) {
      params =
        params.set(
          "urgency",
          request.urgency.trim(),
        );
    }

    return params;
  }

  private mapTicketSummary(
    value: unknown,
  ): SupportTicketListItemResponse {
    const item =
      value as Record<
        string,
        any
      >;

    return {
      ticketId:
        item["ticketId"] ??
        item["TicketId"] ??
        "",

      referenceCode:
        item["referenceCode"] ??
        item["ReferenceCode"] ??
        "",

      subject:
        item["subject"] ??
        item["Subject"] ??
        "",

      category:
        item["category"] ??
        item["Category"] ??
        "General",

      urgency:
        item["urgency"] ??
        item["Urgency"] ??
        "Medium",

      status:
        item["status"] ??
        item["Status"] ??
        "Open",

      createdByUserId:
        item["createdByUserId"] ??
        item["CreatedByUserId"] ??
        "",

      createdByName:
        item["createdByName"] ??
        item["CreatedByName"] ??
        "Unknown User",

      createdByEmail:
        item["createdByEmail"] ??
        item["CreatedByEmail"] ??
        null,

      bookingId:
        item["bookingId"] ??
        item["BookingId"] ??
        null,

      propertyId:
        item["propertyId"] ??
        item["PropertyId"] ??
        null,

      propertyTitle:
        item["propertyTitle"] ??
        item["PropertyTitle"] ??
        null,

      messagesCount:
        item["messagesCount"] ??
        item["MessagesCount"] ??
        0,

      createdAt:
        item["createdAt"] ??
        item["CreatedAt"] ??
        new Date().toISOString(),

      updatedAt:
        item["updatedAt"] ??
        item["UpdatedAt"] ??
        new Date().toISOString(),

      resolvedAt:
        item["resolvedAt"] ??
        item["ResolvedAt"] ??
        null,
    };
  }

  private mapTicketDetails(
    value: unknown,
  ): SupportTicketResponse {
    const item =
      value as Record<
        string,
        any
      >;

    const rawMessages =
      item["messages"] ??
      item["Messages"] ??
      [];

    const rawAttachments =
      item["attachments"] ??
      item["Attachments"] ??
      [];

    return {
      ticketId:
        item["ticketId"] ??
        item["TicketId"] ??
        "",

      referenceCode:
        item["referenceCode"] ??
        item["ReferenceCode"] ??
        "",

      createdByUserId:
        item["createdByUserId"] ??
        item["CreatedByUserId"] ??
        "",

      createdByName:
        item["createdByName"] ??
        item["CreatedByName"] ??
        "Unknown User",

      createdByEmail:
        item["createdByEmail"] ??
        item["CreatedByEmail"] ??
        null,

      bookingId:
        item["bookingId"] ??
        item["BookingId"] ??
        null,

      propertyId:
        item["propertyId"] ??
        item["PropertyId"] ??
        null,

      propertyTitle:
        item["propertyTitle"] ??
        item["PropertyTitle"] ??
        null,

      subject:
        item["subject"] ??
        item["Subject"] ??
        "",

      description:
        item["description"] ??
        item["Description"] ??
        "",

      category:
        item["category"] ??
        item["Category"] ??
        "General",

      urgency:
        item["urgency"] ??
        item["Urgency"] ??
        "Medium",

      status:
        item["status"] ??
        item["Status"] ??
        "Open",

      decisionStatus:
        item["decisionStatus"] ??
        item["DecisionStatus"] ??
        "NoDecision",

      decisionAction:
        item["decisionAction"] ??
        item["DecisionAction"] ??
        "NoAction",

      decisionNote:
        item["decisionNote"] ??
        item["DecisionNote"] ??
        null,

      decidedAt:
        item["decidedAt"] ??
        item["DecidedAt"] ??
        null,

      decidedByAdminId:
        item["decidedByAdminId"] ??
        item["DecidedByAdminId"] ??
        null,

      decidedByAdminName:
        item["decidedByAdminName"] ??
        item["DecidedByAdminName"] ??
        null,

      createdAt:
        item["createdAt"] ??
        item["CreatedAt"] ??
        new Date().toISOString(),

      updatedAt:
        item["updatedAt"] ??
        item["UpdatedAt"] ??
        new Date().toISOString(),

      resolvedAt:
        item["resolvedAt"] ??
        item["ResolvedAt"] ??
        null,

      resolutionNote:
        item["resolutionNote"] ??
        item["ResolutionNote"] ??
        null,

      messages:
        Array.isArray(
          rawMessages,
        )
          ? rawMessages.map(
              (
                message:
                  unknown,
              ) =>
                this.mapMessage(
                  message,
                ),
            )
          : [],

      attachments:
        Array.isArray(
          rawAttachments,
        )
          ? rawAttachments.map(
              (
                attachment:
                  unknown,
              ) =>
                this.mapAttachment(
                  attachment,
                ),
            )
          : [],
    };
  }

  private mapMessage(
    value: unknown,
  ): SupportTicketMessageResponse {
    const item =
      value as Record<
        string,
        any
      >;

    return {
      messageId:
        item["messageId"] ??
        item["MessageId"] ??
        "",

      senderUserId:
        item["senderUserId"] ??
        item["SenderUserId"] ??
        "",

      senderName:
        item["senderName"] ??
        item["SenderName"] ??
        "Unknown User",

      senderEmail:
        item["senderEmail"] ??
        item["SenderEmail"] ??
        null,

      isAdminMessage:
        item["isAdminMessage"] ??
        item["IsAdminMessage"] ??
        false,

      message:
        item["message"] ??
        item["Message"] ??
        "",

      createdAt:
        item["createdAt"] ??
        item["CreatedAt"] ??
        new Date().toISOString(),
    };
  }

  private mapAttachment(
    value: unknown,
  ): SupportTicketAttachmentResponse {
    const item =
      value as Record<
        string,
        any
      >;

    return {
      attachmentId:
        item["attachmentId"] ??
        item["AttachmentId"] ??
        "",

      uploadedByUserId:
        item["uploadedByUserId"] ??
        item["UploadedByUserId"] ??
        "",

      uploadedByName:
        item["uploadedByName"] ??
        item["UploadedByName"] ??
        "Unknown User",

      uploadedByEmail:
        item["uploadedByEmail"] ??
        item["UploadedByEmail"] ??
        null,

      type:
        item["type"] ??
        item["Type"] ??
        "IssueEvidence",

      url:
        item["url"] ??
        item["Url"] ??
        "",

      fileName:
        item["fileName"] ??
        item["FileName"] ??
        "Evidence image",

      contentType:
        item["contentType"] ??
        item["ContentType"] ??
        "",

      fileSizeInBytes:
        item["fileSizeInBytes"] ??
        item["FileSizeInBytes"] ??
        0,

      createdAt:
        item["createdAt"] ??
        item["CreatedAt"] ??
        new Date().toISOString(),
    };
  }

  private mapUploadResponse(
    value: unknown,
  ): UploadSupportTicketAttachmentResponse {
    const item =
      value as Record<
        string,
        any
      >;

    return {
      ticketId:
        item["ticketId"] ??
        item["TicketId"] ??
        "",

      attachmentId:
        item["attachmentId"] ??
        item["AttachmentId"] ??
        "",

      type:
        item["type"] ??
        item["Type"] ??
        "IssueEvidence",

      url:
        item["url"] ??
        item["Url"] ??
        "",

      fileName:
        item["fileName"] ??
        item["FileName"] ??
        "Evidence image",

      contentType:
        item["contentType"] ??
        item["ContentType"] ??
        "",

      fileSizeInBytes:
        item["fileSizeInBytes"] ??
        item["FileSizeInBytes"] ??
        0,

      createdAt:
        item["createdAt"] ??
        item["CreatedAt"] ??
        new Date().toISOString(),

      message:
        item["message"] ??
        item["Message"] ??
        "The evidence image was uploaded successfully.",
    };
  }

  private buildFeedbackDescription(
    feedback: FeedbackModel,
  ): string {
    const comments =
      feedback.comments.trim() ||
      "No additional comments.";

    return [
      `Rating: ${feedback.rating}/5`,

      `Submitted by: ${
        feedback.userName ||
        "SmartStay user"
      }`,

      `Email: ${
        feedback.email ||
        "Not available"
      }`,

      "",

      comments,
    ].join("\n");
  }

  private buildReportDescription(
    report: ReportModel,
  ): string {
    return report.description.trim();
  }

  private getFeedbackUrgency(
    rating: number,
  ): SupportTicketUrgency {
    if (rating <= 1) {
      return "High";
    }

    if (rating <= 3) {
      return "Medium";
    }

    return "Low";
  }

  private toNullableGuid(
    value?: string | null,
  ): string | null {
    const normalizedValue =
      value?.trim();

    return (
      normalizedValue ||
      null
    );
  }

  private createEmptyTicketsResponse(
    page: number,
    pageSize: number,
  ): SupportTicketsResponse {
    return {
      page,
      pageSize,
      totalCount: 0,
      totalPages: 1,
      items: [],
    };
  }

  private parseRequiredJson<T>(
    responseText: string,
  ): T {
    const normalizedResponse =
      this.normalizeResponseText(
        responseText,
      );

    if (!normalizedResponse) {
      throw new Error(
        "The server returned an empty response.",
      );
    }

    return this.parseJson<T>(
      normalizedResponse,
    );
  }

  private normalizeResponseText(
    responseText: string,
  ): string {
    return (
      responseText ?? ""
    )
      .replace(
        /^\uFEFF/,
        "",
      )
      .trim();
  }

  private parseJson<T>(
    responseText: string,
  ): T {
    try {
      return JSON.parse(
        responseText,
      ) as T;
    } catch {
      throw new Error(
        "The server returned an invalid JSON response.",
      );
    }
  }
}