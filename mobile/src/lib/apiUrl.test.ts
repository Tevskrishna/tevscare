import assert from "node:assert/strict";
import test from "node:test";
import { isPublicHttpsApiUrl } from "./apiUrl.ts";

test("staging urls must be public https", () => {
  assert.equal(isPublicHttpsApiUrl("https://tevscare-api.onrender.com"), true);
  assert.equal(isPublicHttpsApiUrl("http://localhost:5080"), false);
  assert.equal(isPublicHttpsApiUrl("https://127.0.0.1:5080"), false);
  assert.equal(isPublicHttpsApiUrl("https://10.21.127.96:5080"), false);
  assert.equal(isPublicHttpsApiUrl("https://192.168.1.20:5080"), false);
  assert.equal(isPublicHttpsApiUrl(undefined), false);
});
