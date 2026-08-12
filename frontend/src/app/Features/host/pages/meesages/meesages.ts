import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChatSidebar } from "./chat-sidebar/chat-sidebar";
import { ChatWindow } from "./chat-window/chat-window";
import { ChatDetails } from "./chat-details/chat-details";
import { ChatService } from './services/chat-service';
@Component({
  selector: 'app-meesages',
  imports: [CommonModule, ChatSidebar, ChatWindow, ChatDetails],
  templateUrl: './meesages.html',
  styleUrl: './meesages.css',
})
export class Meesages {
  protected readonly chatService = inject(ChatService);

  constructor() {
    this.chatService.clearActiveThread();
  }

  // ميثودز مساعدة لو حابب تخليهم في الـ service
  get isMobileDetailsOpen() { return this.chatService.isMobileDetailsOpen; }
  closeMobileDetails() { this.chatService.isMobileDetailsOpen.set(false); }
}
