import * as fc from 'fast-check';

import { takeLastWriteWins } from '@/domain/sync/lastWriteWins';
import type { SyncGamificationWire, SyncProgressWire, SyncSrsWire } from '@/domain/sync/types';
import { Level } from '@/domain/enums';

const epoch = Date.parse('2026-08-01T12:00:00.000Z');

function instant(offsetSeconds: number): string {
  return new Date(epoch + offsetSeconds * 1000).toISOString();
}

const sampleArb = fc.record({
  firstOffset: fc.integer({ min: 0, max: 10_000 }),
  secondOffset: fc.integer({ min: 0, max: 10_000 }),
  firstScore: fc.integer({ min: 0, max: 100 }),
  secondScore: fc.integer({ min: 0, max: 100 }),
  firstInterval: fc.integer({ min: 1, max: 20 }),
  secondInterval: fc.integer({ min: 1, max: 20 }),
  firstXp: fc.integer({ min: 0, max: 50 }),
  secondXp: fc.integer({ min: 0, max: 50 }),
  firstStreak: fc.integer({ min: 0, max: 30 }),
  secondStreak: fc.integer({ min: 0, max: 30 }),
});

function progress(score: number, updatedAt: string): SyncProgressWire {
  return {
    lessonId: 'lesson-1',
    completedAt: updatedAt,
    score,
    xpAwarded: 10,
    updatedAt,
  };
}

function srs(intervalDays: number, updatedAt: string): SyncSrsWire {
  return {
    phraseId: 'phrase-1',
    variationId: null,
    easeFactor: 2.5,
    intervalDays,
    repetitions: 0,
    nextReviewAt: updatedAt.slice(0, 10),
    lastReviewedAt: null,
    updatedAt,
  };
}

function gamification(totalXp: number, currentStreak: number, updatedAt: string): SyncGamificationWire {
  return {
    totalXp,
    currentStreak,
    longestStreak: currentStreak,
    lastActivityDate: updatedAt.slice(0, 10),
    progressLevel: Level.Beginner,
    xpDelta: 0,
    updatedAt,
  };
}

describe('last-write-wins conflict resolution', () => {
  // Feature: kalanga-language-app, Property 26: Last-Write-Wins Conflict Resolution
  it('keeps the newer progress, SRS, and gamification row regardless of apply order when timestamps differ', async () => {
    await fc.assert(
      fc.asyncProperty(sampleArb, async (sample) => {
        const firstAt = instant(sample.firstOffset);
        const secondAt = instant(sample.secondOffset);
        const firstProgress = progress(sample.firstScore, firstAt);
        const secondProgress = progress(sample.secondScore, secondAt);
        const firstSrs = srs(sample.firstInterval, firstAt);
        const secondSrs = srs(sample.secondInterval, secondAt);
        const firstXp = gamification(sample.firstXp, sample.firstStreak, firstAt);
        const secondXp = gamification(sample.secondXp, sample.secondStreak, secondAt);

        const forwardProgress = takeLastWriteWins(
          takeLastWriteWins(null, firstProgress),
          secondProgress,
        );
        const reverseProgress = takeLastWriteWins(
          takeLastWriteWins(null, secondProgress),
          firstProgress,
        );
        const forwardSrs = takeLastWriteWins(takeLastWriteWins(null, firstSrs), secondSrs);
        const reverseSrs = takeLastWriteWins(takeLastWriteWins(null, secondSrs), firstSrs);
        const forwardXp = takeLastWriteWins(takeLastWriteWins(null, firstXp), secondXp);
        const reverseXp = takeLastWriteWins(takeLastWriteWins(null, secondXp), firstXp);

        if (firstAt === secondAt) {
          expect(forwardProgress).toEqual(secondProgress);
          expect(reverseProgress).toEqual(firstProgress);
          expect(forwardSrs).toEqual(secondSrs);
          expect(reverseSrs).toEqual(firstSrs);
          expect(forwardXp).toEqual(secondXp);
          expect(reverseXp).toEqual(firstXp);
          return;
        }

        const winnerProgress = firstAt > secondAt ? firstProgress : secondProgress;
        const winnerSrs = firstAt > secondAt ? firstSrs : secondSrs;
        const winnerXp = firstAt > secondAt ? firstXp : secondXp;
        expect(forwardProgress).toEqual(winnerProgress);
        expect(reverseProgress).toEqual(winnerProgress);
        expect(forwardSrs).toEqual(winnerSrs);
        expect(reverseSrs).toEqual(winnerSrs);
        expect(forwardXp).toEqual(winnerXp);
        expect(reverseXp).toEqual(winnerXp);
      }),
      { numRuns: 100 },
    );
  });
});
