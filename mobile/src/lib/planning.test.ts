import assert from "node:assert/strict";
import test from "node:test";
import { budgetRemaining, isQuietTime, mealLabel, suggestWaterMl, waterPercent } from "./planning.ts";

test("water percent stays inside 0 to 100", () => {
  assert.equal(waterPercent(2000, 500), 25);
  assert.equal(waterPercent(2000, 4000), 100);
  assert.equal(waterPercent(0, 100), 0);
});

test("water suggestion stays in the planning range", () => {
  assert.equal(suggestWaterMl(70), 2450);
  assert.equal(suggestWaterMl(40), 2000);
  assert.equal(suggestWaterMl(120), 3500);
});

test("budget remaining can be negative when the day goes over", () => {
  assert.equal(budgetRemaining(675, 200), 475);
  assert.equal(budgetRemaining(675, 700), -25);
});

test("quiet hours wrap past midnight", () => {
  assert.equal(isQuietTime(6, 30, "22:00", "07:00", true), true);
  assert.equal(isQuietTime(8, 0, "22:00", "07:00", true), false);
  assert.equal(isQuietTime(23, 0, "22:00", "07:00", true), true);
  assert.equal(isQuietTime(6, 30, "22:00", "07:00", false), false);
});

test("meal labels stay human", () => {
  assert.equal(mealLabel("MidMorning"), "Mid-morning");
  assert.equal(mealLabel("AfternoonSnack"), "Afternoon");
});
