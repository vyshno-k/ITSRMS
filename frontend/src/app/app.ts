import { Component, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from './employee-management/services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthService);
  protected readonly title = signal('IT Service Desk');
  protected readonly isAuthPage = signal(this.router.url === '/login' || this.router.url === '/register' || this.router.url === '/');
  protected readonly mobileMenuOpen = signal(false);
  protected get isAdmin(): boolean { return this.auth.isAdmin(); }

  constructor() {
    this.router.events.pipe(filter(event => event instanceof NavigationEnd)).subscribe(event => {
      const url = (event as NavigationEnd).urlAfterRedirects;
      this.isAuthPage.set(url === '/login' || url === '/register' || !this.auth.isLoggedIn());
      this.mobileMenuOpen.set(false);
    });
  }

  toggleMobileMenu(): void { this.mobileMenuOpen.update(open => !open); }
  closeMobileMenu(): void { this.mobileMenuOpen.set(false); }
  logout(): void { this.auth.logout(); this.router.navigate(['/login']); }
}
