import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatService } from '../services/chat-service';
@Component({
  selector: 'app-chat-sidebar',
  imports: [CommonModule, FormsModule],
  templateUrl: './chat-sidebar.html',
  styleUrl: './chat-sidebar.css',
})
export class ChatSidebar {
  protected readonly chatService = inject(ChatService);

  onSearchChange(query: string) {
    this.chatService.searchQuery.set(query);
}
}
