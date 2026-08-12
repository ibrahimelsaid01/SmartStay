import { CommonModule } from '@angular/common';
import { Component, OnInit, ViewChild, ElementRef, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AiChatbotService } from '../../../../Shared/services/ai-chatbot.service';

interface ChatMessage {
  id: string;
  text: string;
  isUser: boolean;
  timestamp: Date;
  isLoading?: boolean;
}

@Component({
  selector: 'app-ai-chatbot',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ai-chatbot.html',
  styleUrl: './ai-chatbot.css',
})
export class AiChatbotComponent implements OnInit {
  @ViewChild('messagesContainer') messagesContainer!: ElementRef;

  private chatbotService = inject(AiChatbotService);

  messages: ChatMessage[] = [];
  userMessage: string = '';
  isLoading: boolean = false;
  userId: string = '';
  private messageCounter: number = 0;

  ngOnInit(): void {
    this.initializeUser();
    this.addWelcomeMessage();
  }

  private generateId(): string {
    return `${Date.now()}-${++this.messageCounter}`;
  }

  private initializeUser(): void {
    // Generate a unique user ID based on timestamp and random number
    this.userId = `user-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  private addWelcomeMessage(): void {
    this.messages.push({
      id: this.generateId(),
      text: "👋 Welcome to SmartStay AI! I can help you find the perfect property. Tell me what you're looking for (location, budget, number of guests, dates, etc.)",
      isUser: false,
      timestamp: new Date(),
    });
  }

  sendMessage(): void {
    if (!this.userMessage.trim() || this.isLoading) {
      return;
    }

    // Add user message to chat
    this.messages.push({
      id: this.generateId(),
      text: this.userMessage,
      isUser: true,
      timestamp: new Date(),
    });

    const messageToSend = this.userMessage;
    this.userMessage = '';
    this.isLoading = true;

    // Add loading indicator
    const loadingMessageId = this.generateId();
    this.messages.push({
      id: loadingMessageId,
      text: '',
      isUser: false,
      timestamp: new Date(),
      isLoading: true,
    });

    this.scrollToBottom();

    // Send message to chatbot API
    this.chatbotService.sendMessage(this.userId, messageToSend).subscribe({
      next: (response) => {
        // Remove loading message
        this.messages = this.messages.filter((m) => m.id !== loadingMessageId);

        // Add bot response
        this.messages.push({
          id: this.generateId(),
          text: response.reply,
          isUser: false,
          timestamp: new Date(),
        });

        this.isLoading = false;
        this.scrollToBottom();
      },
      error: (error) => {
        console.error('Chatbot error:', error);

        // Remove loading message
        this.messages = this.messages.filter((m) => m.id !== loadingMessageId);

        // Add error message
        this.messages.push({
          id: this.generateId(),
          text: '❌ Sorry, I encountered an error. Please try again.',
          isUser: false,
          timestamp: new Date(),
        });

        this.isLoading = false;
        this.scrollToBottom();
      },
    });
  }

  clearChat(): void {
    this.messages = [];
    this.addWelcomeMessage();
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      if (this.messagesContainer) {
        this.messagesContainer.nativeElement.scrollTop =
          this.messagesContainer.nativeElement.scrollHeight;
      }
    }, 0);
  }
}
