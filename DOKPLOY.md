# Dokploy deployment

Create a **Docker Compose** service (Compose, not Stack) using this GitHub repository,
branch `main`, Compose path `compose.dokploy.yml`, and repository root as the build path.
This deployment builds frontend, API and OCR images and creates its own MongoDB.
Do not create separate PostgreSQL or Redis services.

Generate a key on Ubuntu with `openssl rand -hex 32`. In Dokploy Environment save:

```dotenv
API_KEY=replace-with-generated-key
OCR_MODE=mock
```

Deploy. Add a Dokploy domain with host `ocr.baosat.dpdns.org`, service `frontend`,
container port `80`, path `/`, and HTTPS disabled when using an HTTP Cloudflare Tunnel origin.
The frontend must be attached to `dokploy-network` for the proxy.
In Cloudflare's healthy tunnel add a published hostname `ocr.baosat.dpdns.org`
with HTTP origin `127.0.0.1:80` (cloudflared runs on the Ubuntu host).
Visit `https://ocr.baosat.dpdns.org` and test with a synthetic document image.

MongoDB, API and Python OCR have no published host ports. Nginx serves the built
React site and proxies `/api/` to the API. Named volumes retain MongoDB, uploaded
files and downloaded PaddleX models across container recreation. Back up the MongoDB
and upload volumes; restart policies do not prevent downtime during host reboots.
Enable VM Start at boot in Proxmox and enable Docker/cloudflared on Ubuntu.

`mock` returns canned QID fields: it proves the pipeline, not real extraction.
Set `OCR_MODE=paddle` and redeploy for real OCR; models need internet downloads,
additional disk/RAM and cold-start time. The real OCR path requires separate validation.

The MVP embeds its shared API key in the browser bundle; it is not user authentication.
Protect the hostname with Cloudflare Access before uploading real identity documents.
Use synthetic examples for a public portfolio demo. Changing API_KEY requires rebuilding
the frontend as well as recreating the API. Do not commit keys or uploaded documents.

Validation on the preparation machine covers the frontend build and Compose structure;
a full Docker build and end-to-end test must be completed on the server.
