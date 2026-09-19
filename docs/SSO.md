# Single sign-on: Microsoft Entra ID, Google Workspace, Okta

How a university lets its people sign in with the account they already have, what the register does
with that sign-in, and — because none of it counts until a real account in a real directory has
been through it — the checklist for proving each provider works.

## What it is

One OpenID Connect handler (authorization code with PKCE), configured per tenant from the setup
wizard. Entra, Google and Okta are all OpenID Connect providers; the difference between them is
which values the wizard asks for and which issuer the register accepts:

| Provider | The wizard asks for | Authority the register uses |
|---|---|---|
| Microsoft Entra ID | directory (tenant) id, application (client) id, client secret | `https://login.microsoftonline.com/{tenant}/v2.0` — pinned to that one directory, never `common` |
| Google Workspace | OAuth client id, client secret, Workspace domain | `https://accounts.google.com`, and the ID token's `hd` claim must equal the domain |
| Okta | org URL (authority), client id, client secret | the org URL as given (`https://{org}.okta.com` or `.../oauth2/default`) |

The provider authenticates the person. The register then issues **its own** RS256 access token and
refresh-token family, through the same function a password login uses
(`AegisCore.API/Authorization/SessionTokenIssuer.cs`). Nothing downstream — tenant claims, roles,
Casbin, session binding, the applicant-portal scheme — can tell which door the person came through.
The API never validates a provider's token on an ordinary request; `Auth:Authority` is untouched.

A tenant may enable several methods at once: Local (email and password) for a few administrator
accounts and Entra for everyone else is the expected shape. Local only and SSO only are both fine.
When Local is off, `POST /api/auth/login` answers `403 local_sign_in_disabled` with a sentence that
names the way in.

## Setting it up (wizard, step 3)

1. Tick the methods people will use. The step shows the **redirect URI** to register at each
   provider: `{APP_URL}/api/auth/sso/callback`. Register it exactly.
2. For each provider, enter its values, the **group → role mapping** and, if you want one, a
   **default role**. A provider that can give nobody a role is refused at "Save and continue",
   because the first person through it would land on an empty portal.
3. **Save and test connection.** The test fetches the provider's discovery document through the
   same client sign-in will use, and checks it names an authorization endpoint, a token endpoint and
   signing keys. A provider that has not passed since it last changed cannot be saved.
4. Go live. The tested list becomes rows in `IdentityProviderConfigurations`; each client secret
   stays in the installation's secret vault under `/tenants/{tenant}/identity/{provider}/client-secret`
   — the row holds the name, never the value — so it is encrypted at rest by whichever vault the
   installation chose (`AEGISCORE_SECRET_PROVIDER`).

If Local is off, the administrator account from the last wizard step cannot use its password. Map
the administrator's group to **Admin**, or keep Local on.

## What happens at sign-in

```
browser ─GET /api/auth/sso/entra/start─► API: state + PKCE verifier + nonce → Redis (10 min)
        ◄─302 to the provider's authorization endpoint (code, S256, response_mode=query)
        …the person signs in at the provider…
        ─GET /api/auth/sso/callback?code&state─► API: state consumed (one use), code exchanged
                                                    (client secret sent by the API, never the browser),
                                                    ID token validated against the provider's JWKS:
                                                    issuer, audience = client id, lifetime, nonce,
                                                    Entra tid / Google hd, then the person is matched
                                                    or created and the role mapping applied
        ◄─302 to {APP_URL}/login/sso?code=<one-time code, 60 s>
        ─POST /api/auth/sso/exchange {code}─► API: same tokens as a password login
                                              (the portal's proxy turns them into HttpOnly cookies)
```

Errors never surface as a raw exception: the callback redirects to `/login?sso_error=<code>&message=`
and the login screen shows the sentence. The codes are in `AegisCore.Application/Identity/ExternalIdentity.cs`
and `AegisCore.Infrastructure/Identity/Oidc/OidcClient.cs`.

### Matching a person

An external identity is **(tenant, issuer, subject)** — `Users.ExternalIssuer` + `Users.ExternalId`,
under a unique index. Never the subject alone: two Entra directories can issue the same `oid`. For
Entra the subject is `oid` (directory-wide) rather than `sub` (pairwise per application).

- Known identity → profile refreshed, signed in.
- Unknown identity, and an account in the tenant with the same email that has **never** been linked
  to an issuer (the wizard's administrator, or a person added under Identity) → that account is
  linked. It keeps its password if it had one.
- Same email already linked to a different issuer → refused (`identity_conflict`), not merged.
- Otherwise → created just in time, with **no password**. `AuthProvider` is `AzureAd`, `Google` or
  `Oidc`, and `POST /api/identity/users/{id}/password` refuses such accounts.

### Roles

The mapping is per tenant, per provider: a provider group (object id **or** display name; Entra app
role values count too) → an AegisCore role name. On every sign-in the roles the mapping controls
(its targets plus the default) are brought into line with the person's current groups; roles an
administrator gave by hand, outside the mapping, are left alone. A mapped role name that does not
exist in the tenant is logged and skipped.

Where the groups come from:

- **Entra**: the `groups` claim in the ID token (turn it on under *Token configuration → Add groups
  claim*). A person in more than ~200 groups gets no claim, only `_claim_names`; the register then
  asks Microsoft Graph `me/memberOf` with the access token, which needs a Graph scope in *Scopes*
  (`https://graph.microsoft.com/GroupMember.Read.All`). Without it, that person signs in with no groups.
- **Okta**: the `groups` scope (requested by default) and a *groups* claim on the authorization
  server with a filter that releases the groups you map.
- **Google**: no groups in an ID token. Add `https://www.googleapis.com/auth/admin.directory.group.readonly`
  to *Scopes* and the register lists the person's groups through the Admin SDK; it answers 403 for a
  person who may not read groups, who then gets the default role.

### The licence lock

`/api/auth` is exempt from the licence write-lock so people can sign in to read their records. A
first SSO sign-in creates a person, which is a write, so it is refused explicitly when the licence
is locked — with the same sentence the rest of the system uses (`LicenseLockDescription`), the
licence page named, and no half-created account. A person who already exists signs in as normal.

## Proving it with a real account — the checklist

Nothing above is done until each row of this table has been filled in from a real sign-in. The unit
tests (`AegisCore.Tests/SsoSignInTests.cs`) prove the handler against a fake provider that signs
real RS256 tokens; they cannot stand in for a directory's actual claim shape, which is what this
records. Fill it in and keep it with the release.

For each provider, in a fresh installation:

1. Register the app with the redirect URI the wizard shows. Entra: *App registrations → New →
   Web → redirect URI*, then *Certificates & secrets → New client secret* (copy the **value**), then
   *Token configuration → Add groups claim → Security groups*. Google: *APIs & Services →
   Credentials → OAuth client ID → Web application → Authorised redirect URIs*. Okta: *Applications
   → Create App Integration → OIDC, Web Application → Sign-in redirect URIs*; on the authorization
   server, add a `groups` claim (filter: matches regex `.*`) to the ID token.
2. Run the wizard with that provider (and once each with Local on and Local off), map one real
   group to a role that is **not** the default, and confirm: a wrong secret is refused at "Save and
   test" with a sentence; the right one passes and shows the provider's authorization endpoint.
3. Sign in with a real account that is in the mapped group. Then record, from the API log at
   `Information` (`SSO sign-in completed via …`) and from `Users.ExternalClaimsJson`:

| Provider | Issuer (`iss`) exactly as received | Subject used | Email claim that carried the address | Groups claim shape (array? ids or names? overage?) | Role received | Local on + SSO | SSO only |
|---|---|---|---|---|---|---|---|
| Entra | | `oid` | `email` / `preferred_username` / `upn` | | | | |
| Google | | `sub` | `email` (+ `hd`) | | | | |
| Okta | | `sub` | `email` | | | | |

4. Then the negative cases, each of which must end on the login screen with a sentence, not a
   500: the same account after its group was removed (role goes); a consumer Gmail account against
   the Google provider (`wrong_domain`); an account from a different Entra directory
   (`wrong_directory`); the callback URL opened twice (`state_invalid`); a locked licence
   (`license_read_only`, licence page named) for a brand-new account, and a normal sign-in for a
   known one.

Notes an operator will want:

- The state, nonce and PKCE verifier live in Redis for ten minutes; the one-time exchange code for
  sixty seconds. Both are consumed on first use (`StringGetDelete`).
- Discovery documents and signing keys are cached per authority (`ConfigurationManager`) and
  re-read once when a token arrives signed with an unknown key.
- `SessionBindingMiddleware` only checks that an `X-Session-Binding` header, when present, matches
  the `__Host-session-id` cookie. Nothing in the API or portal sets that cookie today, for password
  or SSO sign-ins alike; the SSO path is at parity with the local one.
- The provider's redirect and the portal's own pages are the same origin (`APP_URL`), and
  `/api/auth/sso/*` passes through the portal's `/api` proxy, so no CORS entry is needed.
