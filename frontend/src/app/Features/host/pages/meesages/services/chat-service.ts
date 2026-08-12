import { Injectable, signal, computed } from '@angular/core';

export interface ChatThread {
  id: string;
  otherUserId: string;
  name: string;
  avatar: string;
  propertyName: string;
  bookingDates: string;
  lastMessage: string;
  lastMessageTime: string;
  isUnread: boolean;
  status: 'Online now' | 'Offline';
}

export interface Message {
  id: string;
  threadId: string;
  senderId: string;
  senderName: string;
  text: string;
  timestamp: string;
  isMe: boolean;
}

interface StoredChatState {
  threads: ChatThread[];
  messages: Message[];
}

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly localStorageKey = 'smartstay-local-chat-state';

  readonly threads = signal<ChatThread[]>([]);
  readonly messages = signal<Message[]>([]);
  readonly selectedThreadId = signal<string | null>(null);
  readonly searchQuery = signal<string>('');
  readonly isMobileDetailsOpen = signal<boolean>(false);

  readonly filteredThreads = computed(() => {
    const query = this.searchQuery().toLowerCase().trim();
    let result = this.threads();

    if (query) {
      result = result.filter(
        thread =>
          thread.name.toLowerCase().includes(query) ||
          thread.propertyName.toLowerCase().includes(query)
      );
    }

    return result;
  });

  readonly activeThread = computed(() => {
    return (
      this.threads().find(
        thread => thread.id === this.selectedThreadId()
      ) || null
    );
  });

  readonly activeMessages = computed(() => {
    return this.messages().filter(
      message => message.threadId === this.selectedThreadId()
    );
  });

  constructor() {
    this.loadLocalChatState();
  }

  loadUserThreads(): void {
    /*
     * ملاحظة:
     * الباك الحالي لا يحتوي على ChatController.
     * لذلك لا نرسل request إلى /api/chat/threads.
     */
    this.loadLocalChatState();
  }

  loadThreadMessages(threadId: string): void {
    /*
     * الرسائل محفوظة مؤقتًا في localStorage فقط.
     * عندما يتم تنفيذ chat backend لاحقًا، هذه الدالة ستنادي:
     * GET /api/chat/threads/{threadId}/messages
     */
    this.selectedThreadId.set(threadId);
  }

  clearActiveThread(): void {
    this.selectedThreadId.set(null);
  }

  selectThread(id: string): void {
    const threadId = id.toString();

    this.selectedThreadId.set(threadId);

    this.threads.update(oldThreads =>
      oldThreads.map(thread =>
        thread.id === threadId
          ? { ...thread, isUnread: false }
          : thread
      )
    );

    this.saveLocalChatState();
  }

  sendMessage(text: string): void {
    const currentThreadId = this.selectedThreadId();
    const trimmedText = text.trim();

    if (!currentThreadId || !trimmedText) {
      return;
    }

    const now = new Date();

    const newMessage: Message = {
      id: this.createId('message'),
      threadId: currentThreadId,
      senderId: 'me',
      senderName: 'Me',
      text: trimmedText,
      timestamp: this.formatTime(now),
      isMe: true,
    };

    this.messages.update(oldMessages => [...oldMessages, newMessage]);

    this.threads.update(oldThreads =>
      oldThreads.map(thread =>
        thread.id === currentThreadId
          ? {
              ...thread,
              lastMessage: trimmedText,
              lastMessageTime: 'Now',
              isUnread: false,
            }
          : thread
      )
    );

    this.saveLocalChatState();
  }

  startNewThread(targetUserId: string): void {
    if (!targetUserId.trim()) {
      return;
    }

    const newThreadId = this.createId('thread');

    const newThread: ChatThread = {
      id: newThreadId,
      otherUserId: targetUserId,
      name: 'New Guest',
      avatar: '',
      propertyName: 'Property conversation',
      bookingDates: 'No booking dates',
      lastMessage: 'New conversation started',
      lastMessageTime: 'Now',
      isUnread: false,
      status: 'Offline',
    };

    this.threads.update(oldThreads => [newThread, ...oldThreads]);
    this.selectedThreadId.set(newThreadId);

    this.saveLocalChatState();
  }

  private loadLocalChatState(): void {
    const storedValue = localStorage.getItem(this.localStorageKey);

    if (!storedValue) {
      this.threads.set([]);
      this.messages.set([]);
      return;
    }

    try {
      const parsedValue = JSON.parse(storedValue) as StoredChatState;

      this.threads.set(parsedValue.threads ?? []);
      this.messages.set(parsedValue.messages ?? []);

      if (parsedValue.threads?.length && !this.selectedThreadId()) {
        this.selectedThreadId.set(parsedValue.threads[0].id);
      }
    } catch {
      this.threads.set([]);
      this.messages.set([]);
      localStorage.removeItem(this.localStorageKey);
    }
  }

  private saveLocalChatState(): void {
    const state: StoredChatState = {
      threads: this.threads(),
      messages: this.messages(),
    };

    localStorage.setItem(this.localStorageKey, JSON.stringify(state));
  }

  private createId(prefix: string): string {
    if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
      return `${prefix}-${crypto.randomUUID()}`;
    }

    return `${prefix}-${Date.now()}-${Math.random().toString(16).slice(2)}`;
  }

  private formatTime(date: Date): string {
    return date.toLocaleTimeString([], {
      hour: '2-digit',
      minute: '2-digit',
    });
  }
}