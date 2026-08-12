import { Component, inject, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { AiChatbotService, ChatbotResponse } from '../../services/ai-chatbot.service';

interface ChatMessage {
  id: string;
  text: string;
  sender: 'user' | 'bot';
  timestamp: Date;
  isPropertyMessage?: boolean;
}

interface SearchContext {
  location?: string;
  budget?: number;
  guests?: number;
  checkInDate?: string;
  checkOutDate?: string;
}

@Component({
  selector: 'app-chatbot-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="chatbot-modal-content">
      <div class="modal-header border-bottom">
        <div class="d-flex align-items-center gap-2">
          <i class="bi bi-chat-dots" style="font-size: 20px; color: #0dabb2;"></i>
          <div>
            <h5 class="modal-title mb-0">AI Assistant</h5>
            <small class="text-muted">SmartStay Support</small>
          </div>
        </div>
        <button type="button" class="btn-close" (click)="closeModal()"></button>
      </div>

      <div class="messages-container" #messagesContainer>
        @for (message of messages; track message.id) {
          <div
            [class]="
              'message-wrapper ' + (message.sender === 'user' ? 'user-message' : 'bot-message')
            "
          >
            @if (message.isPropertyMessage) {
              <div class="property-card">
                {{ message.text }}
              </div>
            } @else {
              <div class="message-bubble">
                {{ message.text }}
              </div>
            }
            <small class="message-time">{{ message.timestamp | date: 'short' }}</small>
          </div>
        }

        @if (isLoading) {
          <div class="message-wrapper bot-message">
            <div class="message-bubble typing-indicator">
              <span></span>
              <span></span>
              <span></span>
            </div>
          </div>
        }
      </div>

      <div class="modal-footer border-top">
        <div class="input-group w-100">
          <input
            type="text"
            class="form-control"
            placeholder="Type your message..."
            [(ngModel)]="userMessage"
            (keydown.enter)="sendMessage()"
            [disabled]="isLoading"
            #messageInput
            autofocus
          />
          <button
            class="btn btn-info"
            type="button"
            (click)="sendMessage()"
            [disabled]="!userMessage.trim() || isLoading"
          >
            <i class="bi bi-send"></i>
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .chatbot-modal-content {
        display: flex;
        flex-direction: column;
        height: 500px;
        max-height: 80vh;
      }

      .modal-header {
        background: #f8fafc;
        padding: 16px;
      }

      .modal-header h5 {
        color: #172b3a;
        font-weight: 700;
      }

      .messages-container {
        flex: 1;
        overflow-y: auto;
        padding: 16px;
        background: #ffffff;
        display: flex;
        flex-direction: column;
        gap: 12px;
      }

      .message-wrapper {
        display: flex;
        flex-direction: column;
        gap: 4px;
        animation: slideIn 0.3s ease-out;
      }

      @keyframes slideIn {
        from {
          opacity: 0;
          transform: translateY(10px);
        }
        to {
          opacity: 1;
          transform: translateY(0);
        }
      }

      .user-message {
        align-items: flex-end;
      }

      .bot-message {
        align-items: flex-start;
      }

      .message-bubble {
        max-width: 70%;
        padding: 10px 14px;
        border-radius: 14px;
        word-wrap: break-word;
        line-height: 1.4;
      }

      .user-message .message-bubble {
        background: #0dabb2;
        color: white;
        border-bottom-right-radius: 4px;
      }

      .bot-message .message-bubble {
        background: #e8f5f7;
        color: #172b3a;
        border-bottom-left-radius: 4px;
      }

      .message-time {
        color: #94a3b8;
        font-size: 11px;
        padding: 0 4px;
      }

      .typing-indicator {
        display: flex;
        gap: 4px;
        padding: 12px;
      }

      .typing-indicator span {
        width: 8px;
        height: 8px;
        background: #0dabb2;
        border-radius: 50%;
        animation: typing 1.4s infinite;
      }

      .typing-indicator span:nth-child(2) {
        animation-delay: 0.2s;
      }

      .typing-indicator span:nth-child(3) {
        animation-delay: 0.4s;
      }

      @keyframes typing {
        0%,
        60%,
        100% {
          opacity: 0.5;
          transform: translateY(0);
        }
        30% {
          opacity: 1;
          transform: translateY(-8px);
        }
      }

      .modal-footer {
        padding: 12px;
        background: #f8fafc;
      }

      .input-group .form-control {
        border: 1px solid #e2e8f0;
        border-radius: 8px 0 0 8px;
        padding: 10px 12px;
      }

      .input-group .btn {
        border-radius: 0 8px 8px 0;
        border: 1px solid #0dabb2;
        background: #0dabb2;
        color: white;
        transition: all 0.2s;
      }

      .input-group .btn:hover:not(:disabled) {
        background: #09959a;
        border-color: #09959a;
      }

      .input-group .btn:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }

      @media (max-width: 576px) {
        .chatbot-modal-content {
          height: 400px;
        }

        .message-bubble {
          max-width: 85%;
        }
      }

      .property-card {
        background: linear-gradient(135deg, #f8f9fa 0%, #ffffff 100%);
        border-left: 4px solid #0dabb2;
        padding: 12px 14px;
        border-radius: 8px;
        margin: 8px 0;
        font-size: 13px;
        line-height: 1.5;
        color: #172b3a;
        white-space: pre-wrap;
        word-break: break-word;
      }

      .property-card strong {
        color: #0dabb2;
        font-weight: 600;
      }
    `,
  ],
})
export class ChatbotModalComponent implements AfterViewChecked {
  @ViewChild('messagesContainer') messagesContainer!: ElementRef<HTMLDivElement>;
  @ViewChild('messageInput') messageInput!: ElementRef<HTMLInputElement>;

  activeModal = inject(NgbActiveModal);
  private chatbotService = inject(AiChatbotService);

  messages: ChatMessage[] = [
    {
      id: '0',
      text: "Hi! 👋 I'm your SmartStay AI Assistant. How can I help you today? I can help you find properties, answer questions about bookings, or assist with any other needs.",
      sender: 'bot',
      timestamp: new Date(),
    },
  ];

  userMessage: string = '';
  isLoading: boolean = false;
  private messageCounter: number = 1;
  private userId: string = '';
  private shouldScroll: boolean = false;
  private searchContext: SearchContext = {};

  constructor() {
    this.initializeUser();
  }

  ngAfterViewChecked(): void {
    if (this.shouldScroll) {
      this.scrollToBottom();
      this.shouldScroll = false;
    }
  }

  private initializeUser(): void {
    const storedUserId = sessionStorage.getItem('chatbot_user_id');
    if (storedUserId) {
      this.userId = storedUserId;
    } else {
      this.userId = `user_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
      sessionStorage.setItem('chatbot_user_id', this.userId);
    }
  }

  private scrollToBottom(): void {
    try {
      if (this.messagesContainer) {
        this.messagesContainer.nativeElement.scrollTop =
          this.messagesContainer.nativeElement.scrollHeight;
      }
    } catch (err) {
      console.warn('Auto-scroll failed:', err);
    }
  }

  closeModal(): void {
    this.activeModal.dismiss();
  }

  sendMessage(): void {
    if (!this.userMessage.trim() || this.isLoading) return;

    // Add user message
    const userMsg: ChatMessage = {
      id: this.generateId(),
      text: this.userMessage,
      sender: 'user',
      timestamp: new Date(),
    };

    this.messages.push(userMsg);
    const messageText = this.userMessage;

    // Extract search context from message
    this.extractSearchContext(messageText);

    this.userMessage = '';
    this.isLoading = true;
    this.shouldScroll = true;

    // Call chatbot service with context
    this.chatbotService.sendMessage(this.userId, messageText, this.searchContext).subscribe({
      next: (response: ChatbotResponse) => {
        const botMsg: ChatMessage = {
          id: this.generateId(),
          text: response.reply,
          sender: 'bot',
          timestamp: new Date(),
          isPropertyMessage: response.isPropertyResponse || false,
        };
        this.messages.push(botMsg);
        this.shouldScroll = true;
      },
      error: (error) => {
        console.error('Chatbot error:', error);
        const errorMsg: ChatMessage = {
          id: this.generateId(),
          text: 'Sorry, I encountered an error. Please try again later.',
          sender: 'bot',
          timestamp: new Date(),
        };
        this.messages.push(errorMsg);
        this.shouldScroll = true;
      },
      complete: () => {
        this.isLoading = false;
      },
    });
  }

  private extractSearchContext(message: string): void {
    const lowerMsg = message.toLowerCase();

    // Extract location
    const locationMatch = message.match(/(?:in|cairo|giza|alexandria|hurghada|sharm)/i);
    if (locationMatch) {
      this.searchContext.location = locationMatch[0];
    }

    // Extract guest count
    const guestMatch = message.match(/(\d+)\s*(?:guest|people|person)/i);
    if (guestMatch) {
      this.searchContext.guests = parseInt(guestMatch[1], 10);
    }

    // Extract budget
    const budgetMatch = message.match(
      /(?:under|budget|max|up to|less than)?\s*(\d+)\s*(?:egp|pounds|£|le|eg)/i,
    );
    if (budgetMatch) {
      this.searchContext.budget = parseInt(budgetMatch[1], 10);
    }

    // Extract check-in date
    const checkInMatch = message.match(
      /(?:check\s*in|arrival).*?(\d{1,2})\s*(?:july|june|august|september|july|august)/i,
    );
    if (checkInMatch) {
      this.searchContext.checkInDate = checkInMatch[1];
    }

    // Extract check-out date
    const checkOutMatch = message.match(
      /(?:check\s*out|departure).*?(\d{1,2})\s*(?:july|june|august|september)/i,
    );
    if (checkOutMatch) {
      this.searchContext.checkOutDate = checkOutMatch[1];
    }
  }

  private generateId(): string {
    return `msg_${this.messageCounter++}`;
  }
}
