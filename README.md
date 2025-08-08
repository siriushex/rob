# Outline Token Verification Server

This project provides a simple Express server with endpoints for verifying
Google and Apple tokens and managing Outline VPN access keys.

## Endpoints

- `POST /verify` – Verify Google or Apple ID tokens. Body should include
  `{ "provider": "google|apple", "token": "<ID_TOKEN>" }`.
- `GET /servers` – List configured Outline servers.
- `GET /config` – Return Outline parameters from `config.json`.
- `POST /servers/:id/keys` – Create a new Outline access key for the given server ID.
- `DELETE /servers/:serverId/keys/:keyId` – Delete an Outline access key.

## Setup

1. Install dependencies:
   ```sh
   npm install
   ```
2. Set environment variables for token verification:
   - `GOOGLE_CLIENT_ID`
   - `APPLE_CLIENT_ID`
3. Update `config.json` with your Outline server details.
4. Start the server:
   ```sh
   npm start
   ```

The server listens on port `3000` by default.
