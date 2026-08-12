import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, catchError, of } from 'rxjs';
import { delay } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';

export interface ChatMessage {
  text: string;
  isBot: boolean;
  timestamp: Date;
}

export interface ChatbotResponse {
  reply: string;
}

@Injectable({
  providedIn: 'root',
})
export class Chatbot {
  private readonly mockEndpoint = 'http://localhost:3001/api/chatbot/message';
  private readonly productionEndpoint =
    'https://smartstayaifeatures-production.up.railway.app/api/chatbot/message';

  constructor(private http: HttpClient) {}

  sendMessage(userMessage: string): Observable<ChatbotResponse> {
    const requestBody = {
      userId: this.getUserId(),
      message: userMessage,
    };

    return this.http
      .post<ChatbotResponse>(this.getEndpoint(), requestBody)
      .pipe(catchError(() => this.getFallbackReply(userMessage)));
  }

  private getEndpoint(): string {
    return environment.production ? this.productionEndpoint : this.mockEndpoint;
  }

  private getUserId(): string {
    return `smartstay-${Date.now()}`;
  }

  private getFallbackReply(userMessage: string): Observable<ChatbotResponse> {
    const lowerMessage = userMessage.toLowerCase().trim();

    let botReply = "I'm here to help! Could you please provide more details about your request?";

    if (lowerMessage.includes('booking') || lowerMessage.includes('hajj')) {
      botReply =
        "To manage your booking, please go to the 'My Booking' section in your profile sidebar.";
    } else if (lowerMessage.includes('refund') || lowerMessage.includes('cancel')) {
      botReply =
        'Refunds typically take 3-5 business days to process. Would you like me to guide you through cancellation?';
    } else if (lowerMessage.includes('hi') || lowerMessage.includes('hello')) {
      botReply = 'Hi there! Welcome to SmartStayBot. How can I assist you today?';
    }

    return of({ reply: botReply }).pipe(delay(900));
  }
}
