import { expect, test } from "@playwright/test";

const api = process.env.E2E_API_BASE_URL ?? "http://localhost:8080/api/v1";

test("@security authentication abuse is rate-limited with a stable safe error", async ({ request }) => {
  const responses = [];
  for (let attempt = 0; attempt < 7; attempt += 1) {
    responses.push(await request.post(`${api}/auth/login`, {
      data: { email: `rate-limit-${attempt}@example.invalid`, password: "IncorrectPassword123!" },
      failOnStatusCode: false,
    }));
  }
  const rejected = responses.find((response) => response.status() === 429);
  expect(rejected, `Statuses: ${responses.map((response) => response.status()).join(", ")}`).toBeTruthy();
  expect(rejected!.headers()["content-type"]).toContain("application/json");
  const body = await rejected!.json();
  expect(body.code).toBeTruthy();
  expect(body.correlationId).toBeTruthy();
  expect(JSON.stringify(body)).not.toMatch(/password|token|stack|System\.|Npgsql/i);
});
