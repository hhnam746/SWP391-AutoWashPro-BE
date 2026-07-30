import http from "k6/http";
import { Counter } from "k6/metrics";

const successfulRequests = new Counter("successful_requests");
const rateLimitedRequests = new Counter("rate_limited_requests");
const unexpectedResponses = new Counter("unexpected_responses");
const tokens = (__ENV.TOKENS || "")
    .split(";")
    .map((token) => token.trim())
    .filter(Boolean);

export const options = {
    vus: Number(__ENV.VUS || 20),
    duration: __ENV.DURATION || "30s",
};

export default function () {
    const baseUrl = __ENV.BASE_URL || "http://localhost:5207";
    const path = __ENV.PATH || "/api/v1/vouchers";
    const token = tokens[(__VU - 1) % tokens.length];
    const params = token
        ? { headers: { Authorization: `Bearer ${token}` } }
        : {};
    const response = http.get(`${baseUrl}${path}`, params);

    if (response.status >= 200 && response.status < 300) {
        successfulRequests.add(1);
    } else if (response.status === 429) {
        rateLimitedRequests.add(1);
    } else {
        unexpectedResponses.add(1);
    }
}
