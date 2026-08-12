import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { Chatbot } from './chatbot';

describe('Chatbot', () => {
  let service: Chatbot;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });
    service = TestBed.inject(Chatbot);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fall back to a local reply when the endpoint errors', () => {
    service.sendMessage('hello').subscribe({
      next: (response) => {
        expect(response.reply).toContain('SmartStayBot');
      },
    });

    const req = httpMock.expectOne('http://localhost:3001/api/chatbot/message');
    expect(req.request.method).toBe('POST');
    req.flush('server error', { status: 500, statusText: 'Server Error' });
  });
});
