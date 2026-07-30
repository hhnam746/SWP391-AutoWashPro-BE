import http from "k6/http";
import { Counter } from "k6/metrics";

const acceptedRequests = new Counter("accepted_requests");
const rateLimitedRequests = new Counter("rate_limited_requests");
const unexpectedResponses = new Counter("unexpected_responses");

export const options = {
    scenarios: {
        single_client_burst: {
            executor: "constant-arrival-rate",
            rate: Number(__ENV.RATE || 500),
            timeUnit: "1s",
            duration: __ENV.DURATION || "5s",
            preAllocatedVUs: Number(__ENV.PRE_ALLOCATED_VUS || 100),
            maxVUs: Number(__ENV.MAX_VUS || 500),
        },
    },
};

export default function () {
    const baseUrl = __ENV.BASE_URL || "http://localhost:5207";
    const path = __ENV.PATH || "/WeatherForecast";
    const response = http.get(`${baseUrl}${path}`);

    if (response.status === 200) {
        acceptedRequests.add(1);
    } else if (response.status === 429) {
        rateLimitedRequests.add(1);

        if (response.headers["Retry-After"] !== "60") {
            unexpectedResponses.add(1);
        }
    } else {
        unexpectedResponses.add(1);
    }
}
