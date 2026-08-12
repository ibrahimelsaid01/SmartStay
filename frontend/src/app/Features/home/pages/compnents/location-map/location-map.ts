import {
  ChangeDetectionStrategy,
  Component,
  Input,
  inject,
} from '@angular/core';
import {
  DomSanitizer,
  SafeResourceUrl,
} from '@angular/platform-browser';

@Component({
  selector: 'app-location-map',
  standalone: true,
  imports: [],
  templateUrl: './location-map.html',
  styleUrl: './location-map.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LocationMap {
  private readonly sanitizer =
    inject(DomSanitizer);

  private latitudeValue:
    number | string | null | undefined;

  private longitudeValue:
    number | string | null | undefined;

  @Input()
  sectionTitle = "Where you'll be";

  @Input()
  address: string | null | undefined;

  @Input()
  set latitude(
    value: number | string | null | undefined,
  ) {
    this.latitudeValue = value;
    this.updateMapState();
  }

  @Input()
  set longitude(
    value: number | string | null | undefined,
  ) {
    this.longitudeValue = value;
    this.updateMapState();
  }

  mapUrl: SafeResourceUrl | null = null;

  openMapUrl = '';

  isMapLoading = false;

  hasValidCoordinates = false;

  get displayAddress(): string {
    return (
      this.address?.trim() ||
      'Location shown on the map'
    );
  }

  get mapTitle(): string {
    return `${this.sectionTitle}: ${this.displayAddress}`;
  }

  onMapLoaded(): void {
    this.isMapLoading = false;
  }

  private updateMapState(): void {
    const latitude =
      this.normalizeCoordinate(
        this.latitudeValue,
      );

    const longitude =
      this.normalizeCoordinate(
        this.longitudeValue,
      );

    if (
      latitude === null ||
      longitude === null ||
      latitude < -90 ||
      latitude > 90 ||
      longitude < -180 ||
      longitude > 180
    ) {
      this.hasValidCoordinates = false;
      this.mapUrl = null;
      this.openMapUrl = '';
      this.isMapLoading = false;
      return;
    }

    const formattedLatitude =
      latitude.toFixed(6);

    const formattedLongitude =
      longitude.toFixed(6);

    const latitudeOffset = 0.01;
    const longitudeOffset = 0.015;

    const boundingBox = [
      longitude - longitudeOffset,
      latitude - latitudeOffset,
      longitude + longitudeOffset,
      latitude + latitudeOffset,
    ]
      .map((coordinate) =>
        coordinate.toFixed(6),
      )
      .join(',');

    const embedQuery =
      new URLSearchParams({
        bbox: boundingBox,
        layer: 'mapnik',
        marker:
          `${formattedLatitude},${formattedLongitude}`,
      });

    const embedUrl =
      `https://www.openstreetmap.org/export/embed.html?${embedQuery.toString()}`;

    this.openMapUrl =
      `https://www.openstreetmap.org/?mlat=${formattedLatitude}` +
      `&mlon=${formattedLongitude}` +
      `#map=16/${formattedLatitude}/${formattedLongitude}`;

    this.mapUrl =
      this.sanitizer
        .bypassSecurityTrustResourceUrl(
          embedUrl,
        );

    this.hasValidCoordinates = true;
    this.isMapLoading = true;
  }

  private normalizeCoordinate(
    value: number | string | null | undefined,
  ): number | null {
    if (
      value === null ||
      value === undefined ||
      (
        typeof value === 'string' &&
        !value.trim()
      )
    ) {
      return null;
    }

    const coordinate = Number(value);

    return Number.isFinite(coordinate)
      ? coordinate
      : null;
  }
}