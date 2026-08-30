import { applyRangeChunk, downloadPercent } from '@/domain/contentPacks/bytes';

describe('content pack download bytes', () => {
  it('appends a range chunk at the existing offset', () => {
    const merged = applyRangeChunk(new Uint8Array([1, 2]), new Uint8Array([3, 4]), 2);
    expect([...merged]).toEqual([1, 2, 3, 4]);
    expect(downloadPercent(2, 4)).toBe(50);
  });
});
