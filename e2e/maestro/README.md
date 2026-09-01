# Maestro E2E (tasks 21.2–21.4)

Requires a running .NET API, seeded Kalanga language (`EXPO_PUBLIC_LANGUAGE_ID`), Expo app (`com.kalanga.languageapp` or Expo Go `host.exp.exponent`), and Docker only if you use Testcontainers to seed data.

## Accounts and catalog

Override emails if you re-register. Defaults in `config.yaml`:

- Learner: `learner.e2e@example.test` / `Password1!`
- Contributor / reviewer: same password; roles must already be set in the API
- Content pack name: `Beginner Everyday`
- Published lesson title: `Greetings`
- Flashcard English: `Hello`

## Run

```bash
maestro test e2e/maestro/learner-first-lesson.yaml
maestro test e2e/maestro/offline-mode.yaml
maestro test e2e/maestro/contributor-review.yaml
```

Expo Go:

```bash
maestro test --app-id host.exp.exponent e2e/maestro/learner-first-lesson.yaml
```

Airplane mode (21.3) needs a device or emulator with telephony. Env overrides: `maestro test --env LEARNER_EMAIL=you@example.test ...`.
