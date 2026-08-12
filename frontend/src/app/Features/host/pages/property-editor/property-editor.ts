import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize, forkJoin, Observable } from 'rxjs';

import {
  AmenityResponse,
  CreatePropertyDraftRequest,
  ListingsService,
  PropertyEditorResponse,
  PropertyImageResponse,
  PropertyVerificationDocumentPageResponse,
  UpdatePropertyBasicInformationRequest,
  UpdatePropertyCapacityRequest,
  UpdatePropertyHouseRulesRequest,
  UpdatePropertyLocationRequest,
  UpdatePropertyPricingAndPoliciesRequest,
} from '../../services/listings.service';

type EditorMode = 'add' | 'edit';

interface EditorStep {
  key:
    | 'basic'
    | 'location'
    | 'capacity'
    | 'pricing'
    | 'rules'
    | 'amenities'
    | 'images'
    | 'document'
    | 'submit';
  label: string;
  icon: string;
}

interface BasicInformationForm {
  title: string;
  description: string;
  propertyType: number;
  spaceType: number;
}

interface LocationForm {
  country: string;
  city: string;
  streetAddress: string;
  buildingNumber: string;
  floor: string;
  apartmentNumber: string;
  postalCode: string;
  latitude: number;
  longitude: number;
}

interface CapacityForm {
  maxGuests: number;
  bedrooms: number;
  beds: number;
  bathrooms: number;
}

interface PricingForm {
  pricePerNight: number;
  currency: string;
  checkInTime: string;
  checkOutTime: string;
  cancellationPolicy: number;
}

interface HouseRulesForm {
  allowsSmoking: boolean;
  allowsPets: boolean;
  allowsParties: boolean;
  allowsChildren: boolean;
  additionalHouseRules: string;
}

@Component({
  selector: 'app-property-editor',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './property-editor.html',
  styleUrl: './property-editor.css',
})
export class PropertyEditorComponent implements OnInit {
  private readonly listingsService = inject(ListingsService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly mode = signal<EditorMode>('add');
  readonly propertyId = signal<string | null>(null);

  readonly editor = signal<PropertyEditorResponse | null>(null);
  readonly allAmenities = signal<AmenityResponse[]>([]);
  readonly selectedAmenityIds = signal<string[]>([]);

  readonly selectedImageFiles = signal<File[]>([]);
  readonly selectedDocumentFiles = signal<File[]>([]);

  readonly loading = signal<boolean>(false);
  readonly saving = signal<boolean>(false);
  readonly submitting = signal<boolean>(false);

  readonly errorMessage = signal<string>('');
  readonly successMessage = signal<string>('');

  readonly activeStepIndex = signal<number>(0);

  readonly steps: EditorStep[] = [
    { key: 'basic', label: 'Basic Info', icon: 'bi-info-circle' },
    { key: 'location', label: 'Location', icon: 'bi-geo-alt' },
    { key: 'capacity', label: 'Capacity', icon: 'bi-people' },
    { key: 'pricing', label: 'Pricing', icon: 'bi-cash-stack' },
    { key: 'rules', label: 'House Rules', icon: 'bi-shield-check' },
    { key: 'amenities', label: 'Amenities', icon: 'bi-grid' },
    { key: 'images', label: 'Images', icon: 'bi-images' },
    { key: 'document', label: 'Verification', icon: 'bi-file-earmark-check' },
    { key: 'submit', label: 'Submit', icon: 'bi-send-check' },
  ];

  readonly propertyTypeOptions = [
    { value: 1, label: 'Apartment' },
    { value: 2, label: 'House' },
    { value: 3, label: 'Villa' },
    { value: 4, label: 'Studio' },
    { value: 5, label: 'Chalet' },
  ];

  readonly spaceTypeOptions = [
    { value: 1, label: 'Entire Place' },
    { value: 2, label: 'Private Room' },
  ];

  readonly cancellationPolicyOptions = [
    { value: 1, label: 'Flexible' },
    { value: 2, label: 'Moderate' },
    { value: 3, label: 'Strict' },
  ];

  readonly documentTypeOptions = [
    { value: 1, label: 'Ownership Contract' },
    { value: 2, label: 'Lease Agreement' },
    { value: 3, label: 'Hosting Authorization' },
  ];

  readonly documentType = signal<number>(1);

  readonly basicForm = signal<BasicInformationForm>({
    title: '',
    description: '',
    propertyType: 1,
    spaceType: 1,
  });

  readonly locationForm = signal<LocationForm>({
    country: '',
    city: '',
    streetAddress: '',
    buildingNumber: '',
    floor: '',
    apartmentNumber: '',
    postalCode: '',
    latitude: 0,
    longitude: 0,
  });

  readonly capacityForm = signal<CapacityForm>({
    maxGuests: 1,
    bedrooms: 1,
    beds: 1,
    bathrooms: 1,
  });

  readonly pricingForm = signal<PricingForm>({
    pricePerNight: 1,
    currency: 'EGP',
    checkInTime: '14:00',
    checkOutTime: '11:00',
    cancellationPolicy: 2,
  });

  readonly houseRulesForm = signal<HouseRulesForm>({
    allowsSmoking: false,
    allowsPets: false,
    allowsParties: false,
    allowsChildren: true,
    additionalHouseRules: '',
  });

  readonly currentStep = computed(() => {
    return this.steps[this.activeStepIndex()];
  });

  readonly isAddMode = computed(() => this.mode() === 'add');

  readonly canGoPrevious = computed(() => this.activeStepIndex() > 0);

  readonly canGoNext = computed(() => this.activeStepIndex() < this.steps.length - 1);

  readonly completion = computed(() => {
    return this.editor()?.completion ?? null;
  });

  readonly propertyImages = computed<PropertyImageResponse[]>(() => {
    return this.editor()?.images?.images ?? [];
  });

  readonly verificationPages = computed<PropertyVerificationDocumentPageResponse[]>(() => {
    return this.editor()?.verificationDocument?.pages ?? [];
  });

  readonly groupedAmenities = computed(() => {
    const grouped = new Map<string, AmenityResponse[]>();

    this.allAmenities().forEach(amenity => {
      const category = amenity.category || 'Other';

      if (!grouped.has(category)) {
        grouped.set(category, []);
      }

      grouped.get(category)?.push(amenity);
    });

    return Array.from(grouped.entries()).map(([category, amenities]) => ({
      category,
      amenities,
    }));
  });

  ngOnInit(): void {
    const routePropertyId = this.route.snapshot.paramMap.get('propertyId');

    if (routePropertyId) {
      this.mode.set('edit');
      this.propertyId.set(routePropertyId);
      this.loadEditor(routePropertyId);
      return;
    }

    this.mode.set('add');
    this.loadAmenitiesOnly();
  }

  loadEditor(propertyId: string): void {
    this.loading.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    forkJoin({
      editor: this.listingsService.getEditor(propertyId),
      amenities: this.listingsService.getAllAmenities(),
    })
      .pipe(
        finalize(() => {
          this.loading.set(false);
        })
      )
      .subscribe({
        next: ({ editor, amenities }) => {
          this.editor.set(editor);
          this.allAmenities.set(amenities ?? []);
          this.patchFormsFromEditor(editor);
        },
        error: error => {
          this.errorMessage.set(this.getErrorMessage(error, 'Failed to load property editor.'));
        },
      });
  }

  loadAmenitiesOnly(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.listingsService
      .getAllAmenities()
      .pipe(
        finalize(() => {
          this.loading.set(false);
        })
      )
      .subscribe({
        next: amenities => {
          this.allAmenities.set(amenities ?? []);
        },
        error: error => {
          this.errorMessage.set(this.getErrorMessage(error, 'Failed to load amenities.'));
        },
      });
  }

  saveCurrentStep(): void {
    if (!this.isPropertyEditableForMutation()) {
      this.errorMessage.set(this.getReadOnlyMessage());
      return;
    }

    const stepKey = this.currentStep().key;

    if (stepKey === 'basic') {
      this.saveBasicInformation();
      return;
    }

    const currentPropertyId = this.propertyId();

    if (!currentPropertyId) {
      this.errorMessage.set('Please save basic information first to create a draft.');
      return;
    }

    if (stepKey === 'location') {
      this.saveLocation(currentPropertyId);
      return;
    }

    if (stepKey === 'capacity') {
      this.saveCapacity(currentPropertyId);
      return;
    }

    if (stepKey === 'pricing') {
      this.savePricing(currentPropertyId);
      return;
    }

    if (stepKey === 'rules') {
      this.saveHouseRules(currentPropertyId);
      return;
    }

    if (stepKey === 'amenities') {
      this.saveAmenities(currentPropertyId);
      return;
    }

    if (stepKey === 'images') {
      this.uploadSelectedImages(currentPropertyId);
      return;
    }

    if (stepKey === 'document') {
      this.uploadSelectedDocument(currentPropertyId);
      return;
    }

    if (stepKey === 'submit') {
      this.submitProperty();
    }
  }

  saveBasicInformation(): void {
    const request: CreatePropertyDraftRequest | UpdatePropertyBasicInformationRequest = {
      title: this.basicForm().title.trim(),
      description: this.basicForm().description.trim(),
      propertyType: Number(this.basicForm().propertyType),
      spaceType: Number(this.basicForm().spaceType),
    };

    if (!request.title || !request.description) {
      this.errorMessage.set('Title and description are required.');
      return;
    }

    this.saving.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    const currentPropertyId = this.propertyId();

    const saveRequest = currentPropertyId
      ? this.listingsService.updateBasicInformation(currentPropertyId, request)
      : this.listingsService.createDraft(request);

    saveRequest
      .pipe(
        finalize(() => {
          this.saving.set(false);
        })
      )
      .subscribe({
        next: response => {
          this.successMessage.set('Basic information saved successfully.');

          if (!currentPropertyId) {
            this.propertyId.set(response.id);
            this.mode.set('edit');

            this.router.navigate(['/host/listings', response.id, 'edit'], {
              replaceUrl: true,
            });
          }

          this.reloadEditorAfterSave(response.id);
        },
        error: error => {
          this.errorMessage.set(this.getErrorMessage(error, 'Failed to save basic information.'));
        },
      });
  }

  saveLocation(propertyId: string): void {
    const form = this.locationForm();

    const request: UpdatePropertyLocationRequest = {
      country: form.country.trim(),
      city: form.city.trim(),
      streetAddress: form.streetAddress.trim(),
      buildingNumber: form.buildingNumber?.trim() || null,
      floor: form.floor?.trim() || null,
      apartmentNumber: form.apartmentNumber?.trim() || null,
      postalCode: form.postalCode?.trim() || null,
      latitude: Number(form.latitude),
      longitude: Number(form.longitude),
    };

    if (!request.country || !request.city || !request.streetAddress) {
      this.errorMessage.set('Country, city, and street address are required.');
      return;
    }

    this.runSaveRequest(
      this.listingsService.updateLocation(propertyId, request),
      'Location saved successfully.',
      'Failed to save location.'
    );
  }

  saveCapacity(propertyId: string): void {
    const form = this.capacityForm();

    const request: UpdatePropertyCapacityRequest = {
      maxGuests: Number(form.maxGuests),
      bedrooms: Number(form.bedrooms),
      beds: Number(form.beds),
      bathrooms: Number(form.bathrooms),
    };

    if (
      request.maxGuests < 1 ||
      request.bedrooms < 0 ||
      request.beds < 1 ||
      request.bathrooms < 0.5
    ) {
      this.errorMessage.set('Please enter valid capacity values.');
      return;
    }

    this.runSaveRequest(
      this.listingsService.updateCapacity(propertyId, request),
      'Capacity saved successfully.',
      'Failed to save capacity.'
    );
  }

  savePricing(propertyId: string): void {
    const form = this.pricingForm();

    const request: UpdatePropertyPricingAndPoliciesRequest = {
      pricePerNight: Number(form.pricePerNight),
      currency: form.currency.trim() || 'EGP',
      checkInTime: form.checkInTime,
      checkOutTime: form.checkOutTime,
      cancellationPolicy: Number(form.cancellationPolicy),
    };

    if (request.pricePerNight <= 0) {
      this.errorMessage.set('Price per night must be greater than zero.');
      return;
    }

    this.runSaveRequest(
      this.listingsService.updatePricingAndPolicies(propertyId, request),
      'Pricing and policies saved successfully.',
      'Failed to save pricing and policies.'
    );
  }

  saveHouseRules(propertyId: string): void {
    const form = this.houseRulesForm();

    const request: UpdatePropertyHouseRulesRequest = {
      allowsSmoking: Boolean(form.allowsSmoking),
      allowsPets: Boolean(form.allowsPets),
      allowsParties: Boolean(form.allowsParties),
      allowsChildren: Boolean(form.allowsChildren),
      additionalHouseRules: form.additionalHouseRules?.trim() || null,
    };

    this.runSaveRequest(
      this.listingsService.updateHouseRules(propertyId, request),
      'House rules saved successfully.',
      'Failed to save house rules.'
    );
  }

  saveAmenities(propertyId: string): void {
    this.runSaveRequest(
      this.listingsService.updateAmenities(propertyId, this.selectedAmenityIds()),
      'Amenities saved successfully.',
      'Failed to save amenities.'
    );
  }

  uploadSelectedImages(propertyId: string): void {
    const files = this.selectedImageFiles();

    if (files.length === 0) {
      this.errorMessage.set('Please select at least one image before uploading.');
      return;
    }

    this.runSaveRequest(
      this.listingsService.uploadImages(propertyId, files),
      'Images uploaded successfully.',
      'Failed to upload images.',
      () => {
        this.selectedImageFiles.set([]);
      }
    );
  }

  uploadSelectedDocument(propertyId: string): void {
    const files = this.selectedDocumentFiles();

    if (files.length === 0) {
      this.errorMessage.set('Please select at least one verification document file.');
      return;
    }

    this.runSaveRequest(
      this.listingsService.uploadVerificationDocument(
        propertyId,
        Number(this.documentType()),
        files
      ),
      'Verification document uploaded successfully.',
      'Failed to upload verification document.',
      () => {
        this.selectedDocumentFiles.set([]);
      }
    );
  }

  submitProperty(): void {
    const currentPropertyId = this.propertyId();

    if (!currentPropertyId) {
      this.errorMessage.set('Please create and complete the listing first.');
      return;
    }

    if (this.isPropertyPendingReview()) {
      this.errorMessage.set('This property is already submitted and waiting for admin review.');
      return;
    }

    if (!this.canSubmitProperty()) {
      this.errorMessage.set('Please complete all required sections before submission.');
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.listingsService
      .submitProperty(currentPropertyId)
      .pipe(
        finalize(() => {
          this.submitting.set(false);
        })
      )
      .subscribe({
        next: response => {
          this.successMessage.set(response.message || 'Property submitted successfully.');
          this.reloadEditorAfterSave(currentPropertyId);
        },
        error: error => {
          this.errorMessage.set(this.getErrorMessage(error, 'Failed to submit property.'));
        },
      });
  }

  deleteImage(image: PropertyImageResponse): void {
    if (!this.isPropertyEditableForMutation()) {
      this.errorMessage.set(this.getReadOnlyMessage());
      return;
    }

    const currentPropertyId = this.propertyId();

    if (!currentPropertyId) {
      return;
    }

    const confirmed = confirm('Are you sure you want to delete this image?');

    if (!confirmed) {
      return;
    }

    this.runSaveRequest(
      this.listingsService.deleteImage(currentPropertyId, image.id),
      'Image deleted successfully.',
      'Failed to delete image.'
    );
  }

  setCoverImage(image: PropertyImageResponse): void {
    if (!this.isPropertyEditableForMutation()) {
      this.errorMessage.set(this.getReadOnlyMessage());
      return;
    }

    const currentPropertyId = this.propertyId();

    if (!currentPropertyId) {
      return;
    }

    this.runSaveRequest(
      this.listingsService.setCoverImage(currentPropertyId, image.id),
      'Cover image updated successfully.',
      'Failed to set cover image.'
    );
  }

  deleteVerificationDocument(): void {
    if (!this.isPropertyEditableForMutation()) {
      this.errorMessage.set(this.getReadOnlyMessage());
      return;
    }

    const currentPropertyId = this.propertyId();

    if (!currentPropertyId) {
      return;
    }

    const confirmed = confirm('Are you sure you want to delete the verification document?');

    if (!confirmed) {
      return;
    }

    this.runSaveRequest(
      this.listingsService.deleteVerificationDocument(currentPropertyId),
      'Verification document deleted successfully.',
      'Failed to delete verification document.'
    );
  }

  onImageFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);

    this.selectedImageFiles.set(files);
  }

  onDocumentFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);

    this.selectedDocumentFiles.set(files);
  }

  toggleAmenity(amenityId: string): void {
    if (!this.isPropertyEditableForMutation()) {
      this.errorMessage.set(this.getReadOnlyMessage());
      return;
    }

    const currentIds = this.selectedAmenityIds();

    if (currentIds.includes(amenityId)) {
      this.selectedAmenityIds.set(currentIds.filter(id => id !== amenityId));
      return;
    }

    this.selectedAmenityIds.set([...currentIds, amenityId]);
  }

  isAmenitySelected(amenityId: string): boolean {
    return this.selectedAmenityIds().includes(amenityId);
  }

  goToStep(index: number): void {
    if (index < 0 || index >= this.steps.length) {
      return;
    }

    if (!this.propertyId() && index > 0) {
      this.errorMessage.set('Save basic information first before moving to the next step.');
      return;
    }

    this.activeStepIndex.set(index);
    this.errorMessage.set('');
    this.successMessage.set('');
  }

  nextStep(): void {
    if (!this.canGoNext()) {
      return;
    }

    this.goToStep(this.activeStepIndex() + 1);
  }

  previousStep(): void {
    if (!this.canGoPrevious()) {
      return;
    }

    this.goToStep(this.activeStepIndex() - 1);
  }

  backToListings(): void {
    this.router.navigate(['/host/my-listings']);
  }

  getStepStatus(stepKey: EditorStep['key']): boolean {
    const completion = this.completion();

    if (!completion) {
      return false;
    }

    if (stepKey === 'basic') {
      return completion.basicInformation;
    }

    if (stepKey === 'location') {
      return completion.location;
    }

    if (stepKey === 'capacity') {
      return completion.capacity;
    }

    if (stepKey === 'pricing') {
      return completion.pricingAndPolicies;
    }

    if (stepKey === 'rules') {
      return completion.houseRules;
    }

    if (stepKey === 'amenities') {
      return this.selectedAmenityIds().length > 0;
    }

    if (stepKey === 'images') {
      return completion.images;
    }

    if (stepKey === 'document') {
      return completion.verificationDocument;
    }

    if (stepKey === 'submit') {
      return this.isPropertyPendingReview() || this.canSubmitProperty();
    }

    return false;
  }

  canSubmitProperty(): boolean {
    const completion = this.completion();

    if (!completion) {
      return false;
    }

    if (this.isPropertyPendingReview()) {
      return false;
    }

    if (completion.isEditable === false) {
      return false;
    }

    if (completion.canSubmit) {
      return true;
    }

    const hasNoSubmissionErrors = this.getSubmissionErrors().length === 0;

    const allRequiredSectionsAreDone =
      completion.basicInformation &&
      completion.location &&
      completion.capacity &&
      completion.pricingAndPolicies &&
      completion.houseRules &&
      completion.images &&
      completion.verificationDocument;

    return Boolean(hasNoSubmissionErrors && allRequiredSectionsAreDone);
  }

  getSubmissionErrors(): string[] {
    const completion = this.completion() as
      | {
          submissionErrors?: string[];
          SubmissionErrors?: string[];
        }
      | null;

    return completion?.submissionErrors ?? completion?.SubmissionErrors ?? [];
  }

  isPropertyPendingReview(): boolean {
    const status = this.normalizeEnumValue(this.editor()?.status);

    return status === 'pending' || status === 'underreview';
  }

  isPropertyEditableForMutation(): boolean {
    const editor = this.editor();

    if (!editor) {
      return true;
    }

    if (this.isPropertyPendingReview()) {
      return false;
    }

    return editor.completion?.isEditable !== false;
  }

  getReadOnlyMessage(): string {
    if (this.isPropertyPendingReview()) {
      return 'This property is already submitted and waiting for admin review.';
    }

    return 'This property cannot be edited right now.';
  }

  private reloadEditorAfterSave(propertyId: string): void {
    this.listingsService.getEditor(propertyId).subscribe({
      next: editor => {
        this.editor.set(editor);
        this.patchFormsFromEditor(editor);
      },
      error: error => {
        this.errorMessage.set(this.getErrorMessage(error, 'Saved, but failed to reload editor data.'));
      },
    });
  }

  private runSaveRequest<T>(
    request$: Observable<T>,
    successMessage: string,
    fallbackErrorMessage: string,
    afterSuccess?: () => void
  ): void {
    if (!this.isPropertyEditableForMutation()) {
      this.errorMessage.set(this.getReadOnlyMessage());
      return;
    }

    this.saving.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    request$
      .pipe(
        finalize(() => {
          this.saving.set(false);
        })
      )
      .subscribe({
        next: () => {
          this.successMessage.set(successMessage);
          afterSuccess?.();

          const currentPropertyId = this.propertyId();

          if (currentPropertyId) {
            this.reloadEditorAfterSave(currentPropertyId);
          }
        },
        error: (error: unknown) => {
          this.errorMessage.set(this.getErrorMessage(error, fallbackErrorMessage));
        },
      });
  }

  private patchFormsFromEditor(editor: PropertyEditorResponse): void {
    this.basicForm.set({
      title: editor.basicInformation?.title ?? '',
      description: editor.basicInformation?.description ?? '',
      propertyType: this.mapPropertyTypeToNumber(editor.basicInformation?.propertyType),
      spaceType: this.mapSpaceTypeToNumber(editor.basicInformation?.spaceType),
    });

    this.locationForm.set({
      country: editor.location?.country ?? '',
      city: editor.location?.city ?? '',
      streetAddress: editor.location?.streetAddress ?? '',
      buildingNumber: editor.location?.buildingNumber ?? '',
      floor: editor.location?.floor ?? '',
      apartmentNumber: editor.location?.apartmentNumber ?? '',
      postalCode: editor.location?.postalCode ?? '',
      latitude: editor.location?.latitude ?? 0,
      longitude: editor.location?.longitude ?? 0,
    });

    this.capacityForm.set({
      maxGuests: editor.capacity?.maxGuests ?? 1,
      bedrooms: editor.capacity?.bedrooms ?? 1,
      beds: editor.capacity?.beds ?? 1,
      bathrooms: editor.capacity?.bathrooms ?? 1,
    });

    this.pricingForm.set({
      pricePerNight: editor.pricingAndPolicies?.pricePerNight ?? 1,
      currency: editor.pricingAndPolicies?.currency ?? 'EGP',
      checkInTime: editor.pricingAndPolicies?.checkInTime ?? '14:00',
      checkOutTime: editor.pricingAndPolicies?.checkOutTime ?? '11:00',
      cancellationPolicy: this.mapCancellationPolicyToNumber(
        editor.pricingAndPolicies?.cancellationPolicy
      ),
    });

    this.houseRulesForm.set({
      allowsSmoking: editor.houseRules?.allowsSmoking ?? false,
      allowsPets: editor.houseRules?.allowsPets ?? false,
      allowsParties: editor.houseRules?.allowsParties ?? false,
      allowsChildren: editor.houseRules?.allowsChildren ?? true,
      additionalHouseRules: editor.houseRules?.additionalHouseRules ?? '',
    });

    this.selectedAmenityIds.set(
      editor.amenities?.amenities?.map(amenity => amenity.id) ?? []
    );
  }

  private mapPropertyTypeToNumber(value?: string | null): number {
    const normalized = this.normalizeEnumValue(value);

    if (normalized === 'house') {
      return 2;
    }

    if (normalized === 'villa') {
      return 3;
    }

    if (normalized === 'studio') {
      return 4;
    }

    if (normalized === 'chalet') {
      return 5;
    }

    return 1;
  }

  private mapSpaceTypeToNumber(value?: string | null): number {
    const normalized = this.normalizeEnumValue(value);

    if (normalized === 'privateroom') {
      return 2;
    }

    return 1;
  }

  private mapCancellationPolicyToNumber(value?: string | null): number {
    const normalized = this.normalizeEnumValue(value);

    if (normalized === 'moderate') {
      return 2;
    }

    if (normalized === 'strict') {
      return 3;
    }

    return 1;
  }

  private normalizeEnumValue(value?: string | null): string {
    return (value ?? '').toLowerCase().replace(/\s|_/g, '');
  }

  private getErrorMessage(error: unknown, fallbackMessage: string): string {
    const possibleError = error as {
      error?: unknown;
      message?: string;
    };

    if (typeof possibleError.error === 'string') {
      try {
        const parsedError = JSON.parse(possibleError.error) as {
          message?: string;
          title?: string;
          errors?: Record<string, string[]>;
        };

        if (parsedError.message) {
          return parsedError.message;
        }

        if (parsedError.title) {
          return parsedError.title;
        }

        if (parsedError.errors) {
          const firstError = Object.values(parsedError.errors)[0]?.[0];

          if (firstError) {
            return firstError;
          }
        }
      } catch {
        return possibleError.error;
      }
    }

    if (
      possibleError.error &&
      typeof possibleError.error === 'object' &&
      'message' in possibleError.error
    ) {
      return String((possibleError.error as { message: string }).message);
    }

    return possibleError.message || fallbackMessage;
  }
}