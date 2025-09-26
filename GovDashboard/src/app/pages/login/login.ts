import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms'; // ✅ needed for [(ngModel)]
import { CommonModule } from '@angular/common'; // ✅ needed for *ngIf
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true, // ✅ mark component standalone
  imports: [FormsModule, CommonModule], // ✅ add required modules
  templateUrl: './login.html',
  styleUrls: ['./login.scss'],
})
export class LoginComponent {
  email = '';
  password = '';
  errorMessage = '';

  constructor(private authService: AuthService, private router: Router) {}

  onLogin(): void {
    this.errorMessage = '';

    this.authService.login(this.email, this.password).subscribe({
      next: () => {
        this.authService.getMe().subscribe(console.log);
        this.router.navigate(['/dashboard']); // redirect after login
      },
      error: (err) => {
        this.errorMessage = err.error || 'Login failed. Please try again.';
      },
    });
  }
}
