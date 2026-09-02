import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, UrlTree } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { UserRole } from '../models/user.model';

// Usage in routing: { path: 'users', canActivate: [RoleGuard], data: { roles: ['Admin'] }, ... }
@Injectable({ providedIn: 'root' })
export class RoleGuard implements CanActivate {
  constructor(private auth: AuthService, private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot): boolean | UrlTree {
    const allowedRoles = (route.data['roles'] as UserRole[] | undefined) ?? [];
    if (allowedRoles.length === 0 || this.auth.hasRole(...allowedRoles)) {
      return true;
    }
    // Authenticated but not permitted — send them to the dashboard rather
    // than back to login. The backend must still enforce this (§9); this
    // guard only hides the UI.
    return this.router.createUrlTree(['/dashboard']);
  }
}
