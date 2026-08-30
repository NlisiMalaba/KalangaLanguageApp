import { SessionExpiredError } from '@/domain/auth/errors';
import { apiRequest } from '@/utils/api';
import { resetTokenSessionForTests, setTokenSession } from '@/utils/tokenSession';

describe('apiRequest', () => {
  const originalFetch = global.fetch;

  beforeEach(() => {
    resetTokenSessionForTests();
    global.fetch = jest.fn();
  });

  afterEach(() => {
    global.fetch = originalFetch;
  });

  it('injects a Bearer access token', async () => {
    setTokenSession({
      languageId: 'lang-1',
      accessToken: 'access-1',
      refreshToken: 'refresh-1',
    });
    (global.fetch as jest.Mock).mockResolvedValue({
      status: 200,
      ok: true,
      text: async () => JSON.stringify({ ok: true }),
    });

    await apiRequest('/lessons', { method: 'GET' });

    expect(global.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/lessons'),
      expect.objectContaining({
        headers: expect.objectContaining({ Authorization: 'Bearer access-1' }),
      }),
    );
  });

  it('refreshes on 401 and retries the original request', async () => {
    setTokenSession({
      languageId: 'lang-1',
      accessToken: 'expired',
      refreshToken: 'refresh-1',
    });
    (global.fetch as jest.Mock)
      .mockResolvedValueOnce({
        status: 401,
        ok: false,
        text: async () => JSON.stringify({ title: 'Unauthorized' }),
      })
      .mockResolvedValueOnce({
        status: 200,
        ok: true,
        text: async () =>
          JSON.stringify({
            accessToken: 'access-2',
            refreshToken: 'refresh-2',
            accessTokenExpiresAt: '2026-08-24T12:00:00.000Z',
            refreshTokenExpiresAt: '2026-09-23T12:00:00.000Z',
          }),
      })
      .mockResolvedValueOnce({
        status: 200,
        ok: true,
        text: async () => JSON.stringify({ items: [] }),
      });

    const result = await apiRequest<{ items: unknown[] }>('/lessons', { method: 'GET' });

    expect(result.items).toEqual([]);
    expect(global.fetch).toHaveBeenCalledTimes(3);
    expect(global.fetch).toHaveBeenNthCalledWith(
      2,
      expect.stringContaining('/auth/refresh'),
      expect.any(Object),
    );
    expect(global.fetch).toHaveBeenNthCalledWith(
      3,
      expect.stringContaining('/lessons'),
      expect.objectContaining({
        headers: expect.objectContaining({ Authorization: 'Bearer access-2' }),
      }),
    );
  });

  it('throws SessionExpiredError when refresh fails', async () => {
    setTokenSession({
      languageId: 'lang-1',
      accessToken: 'expired',
      refreshToken: 'refresh-1',
    });
    (global.fetch as jest.Mock)
      .mockResolvedValueOnce({
        status: 401,
        ok: false,
        text: async () => '{}',
      })
      .mockResolvedValueOnce({
        status: 401,
        ok: false,
        text: async () => '{}',
      });

    await expect(apiRequest('/lessons', { method: 'GET' })).rejects.toBeInstanceOf(SessionExpiredError);
  });
});
