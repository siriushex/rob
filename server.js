const express = require('express');
const axios = require('axios');
const {OAuth2Client} = require('google-auth-library');
const appleSignin = require('apple-signin-auth');
const fs = require('fs');

const app = express();
app.use(express.json());

// Load configuration
const config = JSON.parse(fs.readFileSync('./config.json', 'utf8'));

// Google client for verifying ID tokens
const googleClient = new OAuth2Client();

async function verifyGoogleToken(token) {
  const ticket = await googleClient.verifyIdToken({
    idToken: token,
    audience: process.env.GOOGLE_CLIENT_ID
  });
  return ticket.getPayload();
}

async function verifyAppleToken(token) {
  const response = await appleSignin.verifyIdToken(token, {
    audience: process.env.APPLE_CLIENT_ID,
    ignoreExpiration: false
  });
  return response;
}

function getServer(serverId) {
  const server = config.servers.find(s => s.id === serverId);
  if (!server) {
    throw new Error('Server not found');
  }
  return server;
}

async function createOutlineKey(serverId) {
  const server = getServer(serverId);
  const res = await axios.post(`${server.apiUrl}/access-keys`, null, {
    headers: { Authorization: `Bearer ${server.apiKey}` }
  });
  return res.data;
}

async function deleteOutlineKey(serverId, keyId) {
  const server = getServer(serverId);
  const res = await axios.delete(`${server.apiUrl}/access-keys/${keyId}`, {
    headers: { Authorization: `Bearer ${server.apiKey}` }
  });
  return res.data;
}

app.post('/verify', async (req, res) => {
  const { provider, token } = req.body;
  try {
    let payload;
    if (provider === 'google') {
      payload = await verifyGoogleToken(token);
    } else if (provider === 'apple') {
      payload = await verifyAppleToken(token);
    } else {
      return res.status(400).json({ error: 'Unsupported provider' });
    }
    res.json({ valid: true, payload });
  } catch (err) {
    res.status(401).json({ valid: false, error: err.message });
  }
});

app.get('/servers', (req, res) => {
  res.json(config.servers);
});

app.get('/config', (req, res) => {
  res.json(config.outline);
});

app.post('/servers/:id/keys', async (req, res) => {
  try {
    const data = await createOutlineKey(req.params.id);
    res.json(data);
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

app.delete('/servers/:serverId/keys/:keyId', async (req, res) => {
  try {
    await deleteOutlineKey(req.params.serverId, req.params.keyId);
    res.json({ success: true });
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

const port = process.env.PORT || 3000;
app.listen(port, () => {
  console.log(`Server listening on port ${port}`);
});
