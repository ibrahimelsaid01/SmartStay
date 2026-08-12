import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ChatbotModalComponent } from '../chatbot-modal/chatbot-modal';

@Component({
  selector: 'app-floating-chatbot-button',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button
      class="floating-chatbot-btn"
      (click)="openChatbot()"
      title="AI Assistant"
      aria-label="Open AI Assistant chatbot"
    >
      <i class="bi bi-chat-dots"></i>
    </button>
  `,
  styles: [
    `
      .floating-chatbot-btn {
        position: fixed;
        bottom: 24px;
        right: 24px;
        width: 56px;
        height: 56px;
        border-radius: 50%;
        background: linear-gradient(135deg, #0dabb2 0%, #09959a 100%);
        border: none;
        color: white;
        font-size: 24px;
        cursor: pointer;
        box-shadow: 0 4px 16px rgba(13, 171, 178, 0.4);
        display: flex;
        align-items: center;
        justify-content: center;
        transition: all 0.3s ease;
        z-index: 999;
      }

      .floating-chatbot-btn:hover {
        transform: scale(1.1);
        box-shadow: 0 8px 24px rgba(13, 171, 178, 0.6);
      }

      .floating-chatbot-btn:active {
        transform: scale(0.95);
      }

      @media (max-width: 576px) {
        .floating-chatbot-btn {
          bottom: 16px;
          right: 16px;
          width: 48px;
          height: 48px;
          font-size: 20px;
        }
      }
    `,
  ],
})
export class FloatingChatbotButtonComponent {
  private modalService = inject(NgbModal);

  openChatbot(): void {
    this.modalService.open(ChatbotModalComponent, {
      size: 'lg',
      backdrop: 'static',
      centered: true,
      windowClass: 'chatbot-modal-window',
    });
  }
}
