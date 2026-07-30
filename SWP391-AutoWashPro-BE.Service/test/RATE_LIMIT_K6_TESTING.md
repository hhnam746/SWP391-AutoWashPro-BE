# K6 Rate Limit Testing

## Preconditions

1. Start the API with `dotnet run --project SWP391-AutoWashPro-BE.Api`.
2. Use the HTTP URL and port shown by the API startup output. The commands below assume `http://localhost:5207`.
3. Run the commands from this folder: `SWP391-AutoWashPro-BE.Service/test`.

The global limiter permits 250 requests per second for each partition. An anonymous client is partitioned by remote IP. An authenticated client is partitioned by user ID. A rejected request returns `429 Too Many Requests`, `Retry-After: 60`, and the message `Too many requests. Please retry later.`

## 1. Verify rate limiting for one client/IP

Run a burst of 500 requests per second for five seconds against a public endpoint:

```powershell
k6 run -e BASE_URL=http://localhost:5207 .\rate-limit-burst.js
```

Expected result:

- `accepted_requests` is greater than zero.
- `rate_limited_requests` is greater than zero.
- `unexpected_responses` is zero.

The default request rate is intentionally above 250 requests per second. To tune the load, set `RATE` and `DURATION`:

```powershell
k6 run -e BASE_URL=http://localhost:5207 -e RATE=300 -e DURATION=10s .\rate-limit-burst.js
```

## 2. Test concurrent authenticated users

This test uses one virtual user (VU) per simulated user. Supply valid JWTs, separated by semicolons. Use at least as many tokens as `VUS` when each VU must represent a distinct account.

```powershell
$env:TOKENS = "JWT_FOR_USER_1;JWT_FOR_USER_2;JWT_FOR_USER_3"
k6 run -e BASE_URL=http://localhost:5207 -e VUS=3 -e DURATION=30s .\concurrent-users.js
```

The default endpoint is the authenticated `GET /api/v1/vouchers`. Change `PATH` if testing another protected endpoint:

```powershell
k6 run -e BASE_URL=http://localhost:5207 -e PATH=/api/v1/user -e VUS=3 -e DURATION=30s .\concurrent-users.js
```

Expected result with distinct users and ordinary traffic:

- `successful_requests` is greater than zero.
- `rate_limited_requests` is zero or very low.
- `unexpected_responses` is zero.

If the same JWT is reused by many VUs, they deliberately share one user quota. At a high enough combined request rate, `rate_limited_requests` must increase.

## Interpreting results

- `429` responses prove the application rate limiter rejected requests.
- No `429` during the burst usually means k6 did not reach the configured rate, the target URL is wrong, or the API process was not restarted after configuration changes.
- Local public-endpoint tests use one IP, so all VUs share the same anonymous quota. They do not simulate different IP addresses.
- Do not trust `X-Forwarded-For` alone to simulate IPs. The API uses the connection remote IP unless trusted proxy forwarding is explicitly configured.
