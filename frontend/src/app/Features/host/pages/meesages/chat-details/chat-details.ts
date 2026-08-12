import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChatService } from '../services/chat-service';
@Component({
  selector: 'app-chat-details',
  imports: [CommonModule],
  templateUrl: './chat-details.html',
  styleUrl: './chat-details.css',
})
export class ChatDetails {
  protected readonly chatService = inject(ChatService);
  resendCode() {
    const thread = this.chatService.activeThread();
    if (thread) {
      console.log(`Resending access code to ${thread.name}`);
    }
  }
}
