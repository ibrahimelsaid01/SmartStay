import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ChatbotRequest {
  userId: string;
  message: string;
  context?: {
    location?: string;
    budget?: number;
    guests?: number;
    checkInDate?: string;
    checkOutDate?: string;
  };
}

export interface Property {
  id: string;
  name: string;
  location: string;
  price: number;
  currency: string;
  image?: string;
  rating?: number;
  description?: string;
}

export interface ChatbotResponse {
  reply: string;
  properties?: Property[];
  isPropertyResponse?: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class AiChatbotService {
  private readonly API_URL =
    'https://smartstayaifeatures-production-123.up.railway.app/api/chatbot/message';

  constructor(private http: HttpClient) {}

  sendMessage(
    userId: string,
    message: string,
    context?: ChatbotRequest['context'],
  ): Observable<ChatbotResponse> {
    const payload: ChatbotRequest = {
      userId,
      message,
      context,
    };
    return this.http.post<ChatbotResponse>(this.API_URL, payload);
  }
}
