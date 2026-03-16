#!/usr/bin/env python3
"""Patch the workflow to add keychain bypass before setup-buildx."""
import re

path = '/Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/.github/workflows/smart-cicd.yml'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Find the section with setup-buildx
idx = content.find('Set up Docker Buildx')
if idx == -1:
    print("ERROR: 'Set up Docker Buildx' not found in file")
    exit(1)

# Find the line start
line_start = content.rfind('\n', 0, idx) + 1

# Find the end of the Docker login step (after mkdir -p line)
search_from = idx
end_marker = '          # Build base64 auth token'
end_idx = content.find(end_marker, search_from)
if end_idx == -1:
    print("ERROR: end marker not found")
    exit(1)

old_block = content[line_start:end_idx]
print("OLD BLOCK:")
print(repr(old_block[:200]))
print("...")

new_block = '''      - name: "Key Disable Docker credential helper (macOS keychain fix)"
        if: github.ref == 'refs/heads/main'
        run: |
          mkdir -p /tmp/.docker.ci
          # Empty credsStore bypasses osxkeychain for ALL docker ops, including
          # setup-buildx pulling moby/buildkit:buildx-stable-1 from Docker Hub
          printf '{"auths":{},"credsStore":""}' > /tmp/.docker.ci/config.json
          echo "DOCKER_CONFIG=/tmp/.docker.ci" >> "$GITHUB_ENV"
          echo "osxkeychain bypassed — DOCKER_CONFIG=/tmp/.docker.ci"

      - name: "Docker Set up Docker Buildx"
        if: github.ref == 'refs/heads/main'
        uses: docker/setup-buildx-action@v3

      - name: "Lock Docker login to GHCR (credential-helper-free)"
        if: github.ref == 'refs/heads/main'
        env:
          GHCR_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: |
          # DOCKER_CONFIG=/tmp/.docker.ci already set (no keychain, no mkdir needed)
          # Build base64 auth token'''

new_content = content[:line_start] + new_block + content[end_idx:]
with open(path, 'w', encoding='utf-8') as f:
    f.write(new_content)
print("\nSUCCESS: File patched")
print("New block inserted before old setup-buildx step")
