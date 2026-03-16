#!/usr/bin/env python3
"""
Patch smart-cicd.yml to add keychain bypass BEFORE setup-buildx.
The setup-buildx action pulls moby/buildkit from Docker Hub, which triggers
the macOS Keychain on self-hosted runners. We must set DOCKER_CONFIG=/tmp/.docker.ci
(pointing to a config.json with credsStore="") BEFORE that pull happens.
"""
path = '/Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/.github/workflows/smart-cicd.yml'

with open(path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

# Find the line with "Set up Docker Buildx"
target_idx = None
for i, line in enumerate(lines):
    if 'Set up Docker Buildx' in line and '- name:' in line:
        target_idx = i
        print(f"Found 'Set up Docker Buildx' at line {i+1}: {repr(line[:80])}")
        break

if target_idx is None:
    print("ERROR: Could not find 'Set up Docker Buildx' step")
    exit(1)

# The step we want to INSERT before line target_idx
new_step = '''      - name: "Disable Docker credential helper (macOS keychain fix)"
        if: github.ref == 'refs/heads/main'
        run: |
          mkdir -p /tmp/.docker.ci
          # Setting credsStore to empty string disables osxkeychain for ALL docker ops,
          # including setup-buildx pulling moby/buildkit:buildx-stable-1 from Docker Hub.
          printf '{"auths":{},"credsStore":""}' > /tmp/.docker.ci/config.json
          echo "DOCKER_CONFIG=/tmp/.docker.ci" >> "$GITHUB_ENV"
          echo "osxkeychain bypassed — DOCKER_CONFIG=/tmp/.docker.ci"

'''

# Also clean up the corrupted emoji in the buildx step name (replace with clean name)
clean_buildx_line = '      - name: "Set up Docker Buildx"\n'
clean_login_line  = '      - name: "Docker login to GHCR (credential-helper-free)"\n'

new_lines = lines[:target_idx]
new_lines.append(new_step)

# Clean and add the remaining lines (starting from buildx step)
for i, line in enumerate(lines[target_idx:]):
    # Fix the corrupted buildx name line
    if 'Set up Docker Buildx' in line and '- name:' in line:
        new_lines.append(clean_buildx_line)
    # Fix the corrupted Docker login name line (has the corrupted emoji too)
    elif 'Docker login to GHCR (credential-helper-free)' in line and '- name:' in line:
        new_lines.append(clean_login_line)
    # Remove the now-redundant 'mkdir -p /tmp/.docker.ci' inside Docker login step
    # (the new bypass step above already creates it)
    elif line.strip() == 'mkdir -p /tmp/.docker.ci':
        new_lines.append('          # DOCKER_CONFIG=/tmp/.docker.ci already set by keychain fix step above\n')
    else:
        new_lines.append(line)

with open(path, 'w', encoding='utf-8') as f:
    f.writelines(new_lines)

print(f"SUCCESS: Inserted credential bypass step before line {target_idx+1}")
print("Verifying...")

# Verify
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

if 'Disable Docker credential helper' in content:
    print("✓ Bypass step present")
if 'setup-buildx-action@v3' in content:
    print("✓ Buildx action still present")
if 'credential-helper-free' in content:
    print("✓ Docker login step still present")

# Show the patched section
with open(path, 'r', encoding='utf-8') as f:
    all_lines = f.readlines()
for i, line in enumerate(all_lines):
    if 'Disable Docker credential helper' in line:
        print("\n--- Patched section (lines {} - {}) ---".format(i+1, i+20))
        print(''.join(all_lines[i:i+22]))
        break
