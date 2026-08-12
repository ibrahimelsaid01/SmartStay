// host-application.models.ts

export type ApplicationStatus =
  | 'Draft'
  | 'Pending'
  | 'Approved'
  | 'Rejected'
  | string; // fallback in case backend sends other values

/** Matches the response schema returned by ALL host-application endpoints */
export interface HostApplication {
  id: string;
  displayName: string;
  bio: string;
  country: string;
  city: string;
  phoneNumber: string;
  profileImageUrl: string | null;
  status: ApplicationStatus;
  rejectionReason: string | null;
  hasProfileImage: boolean;
  hasIdentityDocument: boolean;
  createdAt: string;
  updatedAt: string;
  submittedAt: string | null;
  reviewedAt: string | null;
}

/** Body for POST /draft and PUT /current */
export interface HostApplicationBasicInfo {
  displayName: string;
  bio: string;
  country: string;
  city: string;
  phoneNumber: string;
}

/** Standard RFC7807 problem-details error shape returned on 400/401/404/409 */
export interface ApiProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  [key: string]: unknown;
}

/** Wizard step identifiers used by the UI (not sent to backend) */
export enum HostApplicationStep {
  Information = 1,
  ProfileImage = 2,
  IdDocument = 3,
  Review = 4,
}

/** Helper: derive which step the user should be on, based on the application state */
export function resolveStepFromApplication(app: HostApplication): HostApplicationStep {
  if (!app.displayName || !app.country || !app.city) {
    return HostApplicationStep.Information;
  }
  if (!app.hasProfileImage) {
    return HostApplicationStep.ProfileImage;
  }
  if (!app.hasIdentityDocument) {
    return HostApplicationStep.IdDocument;
  }
  return HostApplicationStep.Review;
}
