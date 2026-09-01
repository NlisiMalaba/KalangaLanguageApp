# Kalanga mobile

Expo SDK 54 + Expo Router client. Talks to the .NET API, not Supabase.

```bash
npm install
npx expo start
npm test
```

Set `EXPO_PUBLIC_API_BASE_URL` and `EXPO_PUBLIC_LANGUAGE_ID` (see `.env.example`). The language id must match the Development seed (`WellKnownLanguages.KalangaId`). JWTs and password hashes go in expo-secure-store only.

Registration needs `Kalanga.Api` running. From a phone, the client rewrites `localhost` to the Expo bundler host and talks HTTP on port 5077 (the HTTPS dev certificate is not trusted on device). Allow inbound TCP 5077 on the Windows firewall if create-account still cannot reach the API.
