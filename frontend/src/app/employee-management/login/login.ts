import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.html',
  styleUrls: ['./login.css']
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  username = '';
  password = '';
  loading = false;
  error = '';
  success = '';

  login(): void {
    this.error = '';
    this.success = '';
    if (this.username.trim().length < 3) { this.error = 'Please enter a valid username.'; return; }
    if (this.password.length < 6) { this.error = 'Password must be at least 6 characters.'; return; }
    if (this.loading) return;
    this.loading = true;
    this.auth.login({ username: this.username.trim(), password: this.password }).subscribe({
      next: (response) => {
        this.loading = false;
        this.success = response?.message || 'Login successful.';
        const role = String(response?.user?.role || '').toLowerCase();
        this.router.navigate([['admin', 'administrator'].includes(role) ? '/admin' : '/dashboard']);
      },
      error: (err: HttpErrorResponse) => {
        this.loading = false;
        this.error = err.error?.message || (err.status === 0
          ? 'Cannot connect to backend. Start the .NET API on port 5103.'
          : 'Login failed. Please check your username and password.');
      }
    });
  }

  goToRegister(): void { this.router.navigate(['/register']); }
}
