import { apiPost, apiPatch } from './client';
import { COMPANY_ID, signIn, stubFetch } from '../test/render';

describe('apiPost', () => {
  beforeEach(() => {
    signIn();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('sends POST with JSON body and correct headers', async () => {
    const fetchMock = stubFetch({ ok: true });
    await apiPost('/api/test', { key: 'value' });
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/test',
      expect.objectContaining({
        method: 'POST',
        headers: expect.objectContaining({
          'Content-Type': 'application/json',
          Accept: 'application/json',
          'X-Company-Id': COMPANY_ID,
          'X-Role': 'Cashier',
        }),
        body: JSON.stringify({ key: 'value' }),
      }),
    );
  });

  it('throws ApiError on non-2xx', async () => {
    stubFetch({}, 500);
    await expect(apiPost('/api/test', {})).rejects.toThrow(/API request failed with status 500/);
  });
});

describe('apiPatch', () => {
  beforeEach(() => {
    signIn();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('sends PATCH with JSON body and correct headers', async () => {
    const fetchMock = stubFetch({ ok: true });
    await apiPatch('/api/test/123', { key: 'value' });
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/test/123',
      expect.objectContaining({
        method: 'PATCH',
        headers: expect.objectContaining({
          'Content-Type': 'application/json',
          Accept: 'application/json',
          'X-Company-Id': COMPANY_ID,
          'X-Role': 'Cashier',
        }),
        body: JSON.stringify({ key: 'value' }),
      }),
    );
  });

  it('throws ApiError on non-2xx', async () => {
    stubFetch({}, 404);
    await expect(apiPatch('/api/test/123', {})).rejects.toThrow(/API request failed with status 404/);
  });
});
