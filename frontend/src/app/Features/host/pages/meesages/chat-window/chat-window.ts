import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatService } from '../../meesages/services/chat-service';
import { Component, inject, signal, ElementRef, ViewChild, effect } from '@angular/core';
@Component({
  selector: 'app-chat-window',
  imports: [CommonModule, FormsModule],
  templateUrl: './chat-window.html',
  styleUrl: './chat-window.css',
})
export class ChatWindow {
  protected readonly chatService = inject(ChatService);
  @ViewChild('scrollContainer') private scrollContainer!: ElementRef;

  typedMessage = signal<string>('');

  constructor() {
    effect(() => {
      this.chatService.activeMessages();
      setTimeout(() => this.scrollToBottom(), 50);
    });
  }

  onSend() {
    if (!this.typedMessage().trim()) return;
    this.chatService.sendMessage(this.typedMessage());
    this.typedMessage.set('');
  }

  private scrollToBottom(): void {
    try {
      this.scrollContainer.nativeElement.scrollTop = this.scrollContainer.nativeElement.scrollHeight;
    } catch (err) {}
  }
  onFileSelected(event: any) {
  const file = event.target.files[0];
  if (file) {
    console.log("Selected file to upload later:", file.name);
  }
}
}

