// src/app/services/notification.service.ts
import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private messageSubject = new BehaviorSubject<string | null>(null);
  message$ = this.messageSubject.asObservable();

  showMessage(message: string) {
    this.messageSubject.next(message);

    // Auto-hide after 3s
    setTimeout(() => this.clearMessage(), 3000);
  }

  clearMessage() {
    this.messageSubject.next(null);
  }
}
