import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChatSidebar } from '../../host/pages/meesages/chat-sidebar/chat-sidebar';
import { ChatWindow } from '../../host/pages/meesages/chat-window/chat-window';
import { ChatDetails } from '../../host/pages/meesages/chat-details/chat-details';
import { ChatService } from '../../host/pages/meesages/services/chat-service';

@Component({
  selector: 'app-admin-messages',
  standalone: true,
  imports: [CommonModule, ChatSidebar, ChatWindow, ChatDetails],
  templateUrl: './admin-messages.html'
})
export class AdminMessages {
  protected readonly chatService = inject(ChatService);

  constructor() {
    this.chatService.clearActiveThread();
  }
}
