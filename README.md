# Kalanga

**Learn Kalanga. Keep it alive.**

An offline-first, voice-enabled mobile app for learning the Kalanga language — spoken in Zimbabwe and neighbouring regions. Built with the community, for the community: structured lessons, native-speaker audio, and real-life scenarios that work even when the network does not.

Kalanga is under-documented and hard to study with modern tools. This project is both a **learning app** and a **living archive**: phrases, variations (formal vs everyday), and recordings that learners can practise with, and that speakers can contribute.

> Early development. Architecture is in place; implementation is starting. If you care about African languages, mobile learning, or high-quality .NET / Expo work — you are welcome here.

## Why it matters

- **Access** — many learners have phones, not textbooks, and often unreliable connectivity.
- **Voice** — pronunciation needs native audio, not only text.
- **Community** — the language belongs to its speakers. Contributors create lessons; reviewers keep quality high.
- **Scale later** — the data model is multitenant (`language_id`). Kalanga is first; other African languages can follow without a rewrite.

## What you can do in the app

| For learners | For the community |
|---|---|
| Browse lessons by level and category | Create lessons and attach native audio |
| Play recordings (including offline packs) | Review submissions before they go live |
| Practise: flashcards, quizzes, sentence builder, listening | Request topics that are missing |
| Pronunciation practice on-device | Formal vs everyday language variations |
| XP, streaks, and spaced repetition | Downloadable content packs for low connectivity |

Roles: **Learner** (default) · **Contributor** · **Reviewer** · **Admin**.

## Stack

| Layer | Choice |
|---|---|
| Mobile | [Expo](https://expo.dev) (React Native), TypeScript, Expo Router |
| API | [.NET 10](https://learn.microsoft.com/dotnet) / ASP.NET Core, Clean Architecture |
| Data | PostgreSQL, EF Core |
| Audio | Object storage + CDN (the API never streams files) |
| Local / offline | expo-sqlite, expo-file-system, expo-av, expo-secure-store |

Mobile UX follows the structure of [language-learning-app](https://github.com/NlisiMalaba/language-learning-app) (tabs, onboarding, practise, lesson exercises). Auth and data go to **our .NET API**, not Supabase.

```
Expo app  →  local sqlite + audio cache  →  delta sync over HTTPS
                                              ↓
                                    ASP.NET Core API (.NET 10)
                                              ↓
                                    PostgreSQL  ·  S3 / CDN
```

The API is **stateless** (JWT). Hot paths are built for a large launch: async I/O, connection pooling, cached published catalog, CDN for audio, idempotent sync.

## Repository layout (target)

```
src/
  Kalanga.Domain/          # entities, value objects, domain services
  Kalanga.Application/     # use cases and ports
  Kalanga.Infrastructure/  # EF Core, S3, JWT, notifications
  Kalanga.Api/             # HTTP adapters only — no business logic
mobile/                    # Expo app (app/, components/, lib/, providers/)
```

Business logic lives in use cases, never in controllers or screens.

## Status

The product and engineering direction are defined. Source folders will land as we implement backend foundation, then content APIs, then the Expo client.

If you are cloning today, expect a thin tree and a lot of room to help.

## How to contribute

You do not need to be a full-stack engineer.

| You know… | You can help with… |
|---|---|
| Kalanga (or TjiKalanga) | Phrases, translations, register labels, audio, cultural review |
| Linguistics / teaching | Lesson structure, beginner → advanced sequencing |
| C# / .NET | Domain, EF Core, auth, sync, performance |
| TypeScript / Expo | Screens, offline store, audio, exercises |
| QA / product | Edge cases, offline flows, accessibility |

### For engineers

1. Open an issue describing the change (or pick an existing one).
2. Keep PRs small and focused.
3. Match existing style: typed boundaries, tenant `language_id` on every query, no secrets in git.
4. Tests for domain rules (unique email, tenant isolation, SRS, sync last-write-wins).

### For speakers and teachers

Open an issue with **content**: a greeting set, a market scenario, a recording note, a correction. Native judgement is as valuable as code.

### Principles we hold to

- Offline-first for learners; the network is opportunistic.
- Speakers own quality — nothing published without review.
- Pronunciation recordings stay on-device unless the learner consents.
- No drive-by architecture. Prefer a clear modular monolith over extra moving parts.

## Getting started (when code lands)

**API** — .NET 10 SDK, PostgreSQL, then `dotnet run` on `Kalanga.Api`.

**Mobile** — Node LTS, then in `mobile/`: `npm install` and `npx expo start`.

Environment files stay local (`.env`, `appsettings.*.local.json`). Never commit keys.

## Community

- Issues: bugs, content gaps, and “good first” tasks
- Pull requests: welcome, including docs and tests
- Respect: this is a language community first. Be precise, kind, and credit speakers.

## License

License will be published with the first public source drop. Until then, treat the repo as source-available for collaboration — ask before reusing recordings or lesson text, which belong to contributors.

---

Come learn. Come teach. Come build.

[github.com/NlisiMalaba/KalangaLanguageApp](https://github.com/NlisiMalaba/KalangaLanguageApp)
