import { Component } from '@angular/core';
import { AiChatbotComponent } from '../../components/ai-chatbot/ai-chatbot';

@Component({
  selector: 'app-ai-chatbot-page',
  standalone: true,
  imports: [AiChatbotComponent],
  templateUrl: './ai-chatbot-page.html',
  styleUrl: './ai-chatbot-page.css',
})
export class AiChatbotPageComponent {}
