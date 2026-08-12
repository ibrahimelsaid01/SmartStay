import { Injectable } from '@angular/core';

interface JwtPayload {
  [claim: string]: unknown;
}

@Injectable({
  providedIn: 'root',
})
export class AuthState {
  private readonly tokenKey = 'token';
  private readonly userNameKey = 'userName';
  private readonly profileKey = 'current-user-profile';

  isLoggedIn(): boolean {
    /*
     * Do not reject an otherwise readable token only because
     * its access-token expiry has passed. SmartStay uses an
     * HttpOnly refresh-token cookie, and the HTTP interceptor
     * may still renew the access token on the next API call.
     *
     * This method only confirms that a JWT exists and that its
     * payload can be decoded safely.
     */
    return this.readTokenPayload() !== null;
  }

  getUserName(): string {
    return localStorage.getItem(this.userNameKey) ?? '';
  }

  getFirstLetter(): string {
    const name = this.getUserName().trim();

    return name
      ? name.charAt(0).toUpperCase()
      : '';
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userNameKey);
    localStorage.removeItem(this.profileKey);
  }

  getUserId(): string {
    const payload = this.readTokenPayload();

    if (!payload) {
      return '';
    }

    return this.toStringValue(
      payload[
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'
      ] ?? payload['sub'],
    );
  }

  getRole(): string {
    const roles = this.getRoles();

    if (roles.length === 0) {
      return '';
    }

    /*
     * Preserve the existing Admin priority while also making
     * the result deterministic for accounts that contain more
     * than one role.
     */
    return (
      roles.find((role) => this.isRole(role, 'Admin')) ??
      roles.find((role) => this.isRole(role, 'Host')) ??
      roles.find((role) => this.isRole(role, 'User')) ??
      roles[0]
    );
  }

  getRoles(): string[] {
    const payload = this.readTokenPayload();

    if (!payload) {
      return [];
    }

    const roleClaim =
      payload[
        'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
      ] ?? payload['role'] ?? payload['roles'];

    const rawRoles = Array.isArray(roleClaim)
      ? roleClaim
      : [roleClaim];

    const uniqueRoles = new Map<string, string>();

    for (const roleValue of rawRoles) {
      const role = this.toStringValue(roleValue);

      if (!role) {
        continue;
      }

      const normalizedRole = role.toLowerCase();

      if (!uniqueRoles.has(normalizedRole)) {
        uniqueRoles.set(normalizedRole, role);
      }
    }

    return Array.from(uniqueRoles.values());
  }

  hasRole(expectedRole: string): boolean {
    const normalizedExpectedRole = expectedRole.trim();

    if (!normalizedExpectedRole) {
      return false;
    }

    return this.getRoles().some((role) =>
      this.isRole(role, normalizedExpectedRole),
    );
  }

  isAdmin(): boolean {
    return this.hasRole('Admin');
  }

  isHost(): boolean {
    return this.hasRole('Host');
  }

  isUser(): boolean {
    return this.hasRole('User');
  }

  private readTokenPayload(): JwtPayload | null {
    const token = localStorage.getItem(this.tokenKey);

    if (!token) {
      return null;
    }

    const tokenParts = token.split('.');

    if (tokenParts.length !== 3 || !tokenParts[1]) {
      return null;
    }

    try {
      const parsedPayload = JSON.parse(
        this.decodeBase64Url(tokenParts[1]),
      ) as unknown;

      return parsedPayload &&
        typeof parsedPayload === 'object' &&
        !Array.isArray(parsedPayload)
        ? (parsedPayload as JwtPayload)
        : null;
    } catch {
      return null;
    }
  }

  private decodeBase64Url(value: string): string {
    const normalizedValue = value
      .replace(/-/g, '+')
      .replace(/_/g, '/');

    const paddingLength =
      (4 - (normalizedValue.length % 4)) % 4;

    const decodedValue = atob(
      normalizedValue.padEnd(
        normalizedValue.length + paddingLength,
        '=',
      ),
    );

    const bytes = Uint8Array.from(
      decodedValue,
      (character) => character.charCodeAt(0),
    );

    return new TextDecoder().decode(bytes);
  }

  private toStringValue(value: unknown): string {
    return typeof value === 'string'
      ? value.trim()
      : '';
  }

  private isRole(
    role: string,
    expectedRole: string,
  ): boolean {
    return role.toLowerCase() === expectedRole.toLowerCase();
  }
}