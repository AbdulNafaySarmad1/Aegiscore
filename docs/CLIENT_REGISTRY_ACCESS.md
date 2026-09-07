# Per-client private registry access

Nocturne owns `ghcr.io/nocturne-systems/aegiscore`. Keep visibility **Private**.
Disable inherited repository permissions and grant the publishing repository Actions
access to this package. Clients receive no repository access or organization-wide package access.
The release workflow refuses to publish unless the existing package is private.
Before the first tagged release, a Nocturne maintainer must bootstrap the package
by pushing the reviewed image with a maintainer credential, confirm Private visibility,
and configure the package's granular access settings. Never send that credential to clients.

GitHub Container Registry uses a **personal access token (classic)**. Classic PATs
cannot be narrowed to a selected package directly: `read:packages` covers packages
the token owner can read. To achieve package-only isolation, create a dedicated
GitHub machine identity per client, with **Read** access only to the `aegiscore`
package. Do not reuse employee accounts, organization-wide service accounts, or tokens
that have access to other private packages. One active pull token per client.

In that identity's GitHub Settings → Developer settings → Personal access tokens →
Tokens (classic) → Generate new token, give it a client-specific name and expiry,
select **read:packages only**, and leave `repo`, write/delete packages, and admin
scopes unchecked. Authorize SSO if the organization requires it. Deliver the token
through the agreed secret channel and record its owner, client, expiry and rotation
date in Nocturne's credential inventory; never record the token in this repository.

The exact client command (run alongside the supplied compose file):

```sh
mkdir -p .registry-auth
chmod 700 .registry-auth
printf '%s' "$GHCR_PULL_TOKEN" | docker --config .registry-auth login ghcr.io --username "$GHCR_USERNAME" --password-stdin
chmod 600 .registry-auth/config.json
```

Use the same identity/token for the initial pull and Watchtower. `.registry-auth`
is a dedicated Docker config directory without credential helpers. Compose mounts
the directory read-only, so atomically replaced login credentials remain visible
to Watchtower. Set `DOCKER_CONFIG="$PWD/.registry-auth"` for Docker Compose pulls.
Never mount a developer's Docker config, which may contain unrelated registry credentials.

Rotation: issue the replacement under the same package-only identity, deliver securely,
repeat login, then run `docker --config .registry-auth pull ghcr.io/nocturne-systems/aegiscore:latest`.
Confirm a successful Watchtower authenticated registry check before revoking the old
token. Keep overlap brief; for compromise, revoke immediately and replace.
Verify the revoked token can no longer pull and record completion. For offboarding,
revoke the token and remove the identity's package access without deleting client data.

## Release automation

The tag-triggered private publish is `.github/workflows/client-release.yml` — this is
the Task 19A deliverable. It fires on `v*.*.*` tag pushes (and `workflow_dispatch`),
builds the single combined image from the root `Dockerfile`, and pushes
`ghcr.io/nocturne-systems/aegiscore:{tag}` and `:latest` to the private package.

`.github/workflows/release.yml` is a separate, pre-existing internal build
(push-to-`main` → `aegiscore-api` / `aegiscore-portal` images + GitHub Release) and is
intentionally left unchanged.

The same workflow also has a `publish-clip` job (P20) that builds
`services/aegiscore-clip` and pushes `ghcr.io/nocturne-systems/aegiscore-clip:{tag}`
and `:latest` to a second private package. `docker-compose.client.yml` references
`aegiscore-clip:latest` by image only (the client compose forbids `build:`
directives), so this internal photo-filter / OCR sidecar ships through the same
tag-triggered pipeline and Watchtower keeps it current alongside the application
image. The first-publish bootstrap (`allow_bootstrap=true`) covers both packages.

First publish, before the package exists: a maintainer pushes the initial reviewed
image manually and sets Private visibility, then runs `client-release.yml` via
**Run workflow** (`workflow_dispatch`) with `tag=vX.Y.Z` and `allow_bootstrap=true`,
which skips the private-package precondition for that one run. All later runs — tag
pushes or dispatches without `allow_bootstrap` — enforce that the package is private
before pushing.

References: [GitHub GHCR authentication](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry),
[package access controls](https://docs.github.com/en/packages/learn-github-packages/configuring-a-packages-access-control-and-visibility),
[Watchtower registry credentials](https://containrrr.dev/watchtower/private-registries/).
