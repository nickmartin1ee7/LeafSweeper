#!/usr/bin/env node
// Generates all SVG art for LeafSweeper (debris, bugs, ground).
//
// Run from the repo root:  node tools/gen_art.mjs
// Outputs into assets/textures/ and assets/textures/bugs/.
// Style: soft cartoon, warm autumn palette (see docs/art-style.md).

import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, join, relative } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "..");
const TEX = join(ROOT, "assets", "textures");
const BUGS = join(TEX, "bugs");

function write(path, svg) {
  mkdirSync(dirname(path), { recursive: true });
  writeFileSync(path, svg.trim() + "\n");
  console.log("wrote", relative(ROOT, path));
}

// ---------------------------------------------------------------- debris ---

function leafSimple(name, top, bot, vein) {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <linearGradient id="g" x1="0" y1="0" x2="0.4" y2="1">
      <stop offset="0" stop-color="${top}"/>
      <stop offset="1" stop-color="${bot}"/>
    </linearGradient>
  </defs>
  <ellipse cx="50" cy="88" rx="24" ry="5" fill="#000" opacity="0.10"/>
  <path d="M50 8 C72 26 76 54 50 84 C24 54 28 26 50 8 Z"
        fill="url(#g)" stroke="${vein}" stroke-width="3" stroke-linejoin="round"/>
  <path d="M50 16 L50 78 M50 30 C56 34 60 38 63 44 M50 30 C44 34 40 38 37 44
           M50 46 C57 51 60 56 62 61 M50 46 C43 51 40 56 38 61"
        fill="none" stroke="${vein}" stroke-width="2.4" stroke-linecap="round"/>
</svg>`;
  write(join(TEX, name + ".svg"), svg);
}

function leafMaple(name, top, bot, line) {
  const pts =
    "50,8 57,22 72,15 68,31 84,33 72,45 83,58 66,57 68,74 55,63 " +
    "50,84 45,63 32,74 34,57 17,58 28,45 16,33 32,31 28,15 43,22";
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <linearGradient id="g" x1="0" y1="0" x2="0.3" y2="1">
      <stop offset="0" stop-color="${top}"/>
      <stop offset="1" stop-color="${bot}"/>
    </linearGradient>
  </defs>
  <ellipse cx="50" cy="90" rx="26" ry="5" fill="#000" opacity="0.10"/>
  <polygon points="${pts}" fill="url(#g)" stroke="${line}" stroke-width="3"
           stroke-linejoin="round"/>
  <path d="M50 18 L50 78 M50 34 L62 28 M50 34 L38 28 M50 50 L66 46 M50 50 L34 46"
        fill="none" stroke="${line}" stroke-width="2.2" stroke-linecap="round"/>
</svg>`;
  write(join(TEX, name + ".svg"), svg);
}

function leafOak(name, top, bot, line) {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <linearGradient id="g" x1="0" y1="0" x2="0.4" y2="1">
      <stop offset="0" stop-color="${top}"/>
      <stop offset="1" stop-color="${bot}"/>
    </linearGradient>
  </defs>
  <ellipse cx="50" cy="88" rx="22" ry="5" fill="#000" opacity="0.10"/>
  <path d="M50 9 C58 13 60 19 56 25 C63 25 67 31 61 36 C69 38 71 45 63 48
           C71 52 69 60 61 60 C65 66 59 72 53 69 C53 77 47 77 47 69
           C41 72 35 66 39 60 C31 60 29 52 37 48 C29 45 31 38 39 36
           C33 31 37 25 44 25 C40 19 42 13 50 9 Z"
        fill="url(#g)" stroke="${line}" stroke-width="3" stroke-linejoin="round"/>
  <path d="M50 16 L50 74 M50 28 L59 25 M50 28 L41 25 M50 44 L60 42 M50 44 L40 42
           M50 58 L57 56 M50 58 L43 56"
        fill="none" stroke="${line}" stroke-width="2.2" stroke-linecap="round"/>
</svg>`;
  write(join(TEX, name + ".svg"), svg);
}

function mossCluster(name, base, dark, light) {
  const blobs = [
    [50, 52, 26], [30, 60, 16], [70, 60, 16], [40, 40, 14], [62, 42, 13],
  ];
  const circles = blobs
    .map(([x, y, r]) => `<circle cx="${x}" cy="${y}" r="${r}"/>`)
    .join(" ");
  const dotPts = [
    [38, 44, 6], [60, 50, 5], [47, 62, 4], [70, 62, 4], [28, 58, 3],
  ];
  const dots = dotPts
    .map(([x, y, r]) => `<circle cx="${x}" cy="${y}" r="${r}" fill="${light}" opacity="0.85"/>`)
    .join("");
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <ellipse cx="50" cy="80" rx="34" ry="6" fill="#000" opacity="0.10"/>
  <g fill="${base}" stroke="${dark}" stroke-width="7" stroke-linejoin="round">${circles}</g>
  <g fill="${base}" stroke="none">${circles}</g>
  ${dots}
</svg>`;
  write(join(TEX, name + ".svg"), svg);
}

function stick() {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <ellipse cx="50" cy="74" rx="36" ry="5" fill="#000" opacity="0.10"/>
  <g stroke="#6d4c2a" fill="none" stroke-linecap="round">
    <path d="M12 66 C28 60 44 52 88 28" stroke-width="9"/>
    <path d="M46 54 C44 44 46 36 52 30" stroke-width="5"/>
    <path d="M68 40 C74 40 80 42 84 46" stroke-width="4"/>
  </g>
  <g stroke="#8a6538" fill="none" stroke-linecap="round" stroke-width="2">
    <path d="M20 63 C34 58 50 50 80 32"/>
    <path d="M49 50 C47 44 48 38 51 33"/>
  </g>
</svg>`;
  write(join(TEX, "stick.svg"), svg);
}

function rock(name, top, bot, line, hi) {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <linearGradient id="g" x1="0" y1="0" x2="0.3" y2="1">
      <stop offset="0" stop-color="${top}"/>
      <stop offset="1" stop-color="${bot}"/>
    </linearGradient>
  </defs>
  <ellipse cx="50" cy="74" rx="32" ry="6" fill="#000" opacity="0.12"/>
  <path d="M22 62 C16 48 26 32 44 28 C62 24 80 32 82 46 C84 58 74 68 58 70
           C42 74 28 72 22 62 Z"
        fill="url(#g)" stroke="${line}" stroke-width="3.5" stroke-linejoin="round"/>
  <path d="M34 42 C40 34 52 32 62 36 C58 40 52 40 46 44 C42 47 36 46 34 42 Z"
        fill="${hi}" opacity="0.8"/>
  <path d="M28 58 C40 62 60 62 74 56" fill="none" stroke="${line}"
        stroke-width="2" opacity="0.5"/>
</svg>`;
  write(join(TEX, name + ".svg"), svg);
}

function petal(name, top, bot, line) {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <linearGradient id="g" x1="0" y1="0" x2="0.5" y2="1">
      <stop offset="0" stop-color="${top}"/>
      <stop offset="1" stop-color="${bot}"/>
    </linearGradient>
  </defs>
  <ellipse cx="50" cy="84" rx="20" ry="4" fill="#000" opacity="0.08"/>
  <path d="M50 12 C68 30 70 56 50 80 C30 56 32 30 50 12 Z"
        fill="url(#g)" stroke="${line}" stroke-width="3" stroke-linejoin="round"/>
  <path d="M44 34 C40 44 40 56 44 64" fill="none" stroke="${line}"
        stroke-width="2" opacity="0.55" stroke-linecap="round"/>
</svg>`;
  write(join(TEX, name + ".svg"), svg);
}

// ------------------------------------------------------------------ bugs ---

function ladybug(p = {}, name = "ladybug") {
  const {
    b1 = "#e8453c", b2 = "#b02a24",
    outline = "#5e1713", dark = "#26201c", headStroke = "#0f0d0b",
    spots = [[36, 44, 5], [64, 44, 5], [30, 60, 4], [70, 60, 4],
            [42, 72, 4.6], [58, 72, 4.6], [50, 52, 3.4]],
  } = p;
  const dot = ([cx, cy, r]) => `<circle cx="${cx}" cy="${cy}" r="${r}"/>`;
  const spotDots = spots
    .map((s, i) =>
      i % 2 === 1 ? dot(s) : (i === 0 ? dot(s) : "\n    " + dot(s)))
    .join("");
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="b" cx="0.35" cy="0.3" r="0.9">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </radialGradient>
  </defs>
  <ellipse cx="50" cy="88" rx="30" ry="5" fill="#000" opacity="0.14"/>
  <g stroke="${dark}" stroke-width="3.4" stroke-linecap="round" fill="none">
    <path d="M26 48 L14 40 M24 62 L12 62 M28 74 L18 82"/>
    <path d="M74 48 L86 40 M76 62 L88 62 M72 74 L82 82"/>
  </g>
  <path d="M50 24 C72 24 82 42 82 58 C82 76 68 86 50 86 C32 86 18 76 18 58
           C18 42 28 24 50 24 Z"
        fill="url(#b)" stroke="${outline}" stroke-width="3.4"/>
  <circle cx="50" cy="26" r="13" fill="${dark}" stroke="${headStroke}" stroke-width="2.4"/>
  <circle cx="45" cy="23" r="2.6" fill="#fff"/>
  <circle cx="55" cy="23" r="2.6" fill="#fff"/>
  <path d="M43 15 C40 10 36 8 32 8 M57 15 C60 10 64 8 68 8"
        fill="none" stroke="${dark}" stroke-width="2.6" stroke-linecap="round"/>
  <path d="M50 30 L50 85" stroke="${dark}" stroke-width="3.4"/>
  <g fill="${dark}">
    ${spotDots}
  </g>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function butterfly(p = {}, name = "butterfly") {
  const {
    w1 = "#9db8d6", w2 = "#5f7ea6",
    limb = "#3a4658", limbDark = "#232b38",
    wingSpots = [[22, 34, 5], [78, 34, 5]], wingSpotsFill = "#e08b3e",
    wingDots = [[30, 44, 2.4], [70, 44, 2.4], [38, 76, 2], [62, 76, 2]],
    wingDotsFill = "#f4ede0",
  } = p;
  const dot = ([cx, cy, r]) => `<circle cx="${cx}" cy="${cy}" r="${r}"/>`;
  const pairs = (arr) => arr
    .map((s, i) =>
      i % 2 === 1 ? dot(s) : (i === 0 ? dot(s) : "\n    " + dot(s)))
    .join("");
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="w" cx="0.4" cy="0.35" r="0.95">
      <stop offset="0" stop-color="${w1}"/>
      <stop offset="1" stop-color="${w2}"/>
    </radialGradient>
  </defs>
  <ellipse cx="50" cy="86" rx="26" ry="4.6" fill="#000" opacity="0.13"/>
  <g stroke="${limb}" stroke-width="3" stroke-linejoin="round">
    <path d="M46 46 C30 18 8 20 12 40 C15 56 32 58 46 54 Z" fill="url(#w)"/>
    <path d="M54 46 C70 18 92 20 88 40 C85 56 68 58 54 54 Z" fill="url(#w)"/>
    <path d="M46 58 C34 66 26 80 36 84 C44 87 48 74 48 62 Z" fill="url(#w)"/>
    <path d="M54 58 C66 66 74 80 64 84 C56 87 52 74 52 62 Z" fill="url(#w)"/>
  </g>
  <g fill="${wingSpotsFill}" opacity="0.9">
    ${pairs(wingSpots)}
  </g>
  <g fill="${wingDotsFill}">
    ${pairs(wingDots)}
  </g>
  <ellipse cx="50" cy="56" rx="5.4" ry="18" fill="${limb}" stroke="${limbDark}" stroke-width="2.4"/>
  <circle cx="50" cy="36" r="7" fill="${limb}" stroke="${limbDark}" stroke-width="2.4"/>
  <path d="M46 30 C42 22 36 18 30 18 M54 30 C58 22 64 18 70 18"
        fill="none" stroke="${limbDark}" stroke-width="2.4" stroke-linecap="round"/>
  <circle cx="30" cy="18" r="2.2" fill="${limbDark}"/>
  <circle cx="70" cy="18" r="2.2" fill="${limbDark}"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function centipede(p = {}, name = "centipede") {
  const {
    body = "#d99a4e", outline = "#8a5318", spot = "#f0bd7e",
    legs = "#6e3f10", dark = "#26201c",
    // null hides the dorsal spots; a number N spots every Nth segment.
    spotEvery = 2,
  } = p;
  // Flatter, elongated segmented body with many long splayed legs.
  // Use ellipses for a low profile and 7 pairs of long thin legs alternating.
  const segs = [
    // x, y, rx, ry, rotation
    [18, 48, 9.5, 5.2, -6], // head/first
    [30, 48, 8.6, 4.4, -4],
    [40, 48, 8.0, 4.0, -2],
    [50, 49, 7.2, 3.6, 0],
    [60, 50, 6.2, 3.2, 2],
    [70, 51, 5.4, 2.8, 4],
    [80, 52, 4.6, 2.4, 6],
  ];

  // legs: long, thin, alternately angled. Produce a pair per segment except head.
  const legLines = segs
    .slice(1) // skip head for leg pairing start
    .map(([x, y, rx, ry], i) => {
      const baseY = y + ry - 1; // emerge from just under ellipse
      const out = 14 + (i % 3); // length 14-16
      const spread = 10 + (i % 2) * 4; // how far sideways they go
      // alternate angles: even index -> left leg steeper, odd -> right leg steeper
      const leftEndX = x - spread - (i % 2 === 0 ? 2 : 0);
      const rightEndX = x + spread + (i % 2 === 1 ? 2 : 0);
      const endY = baseY + out;
      // give each leg a two-segment feel (joint) with a mid point for a slight kink
      const leftMidX = x - (spread * 0.5);
      const leftMidY = baseY + out * 0.45;
      const rightMidX = x + (spread * 0.5);
      const rightMidY = baseY + out * 0.45;
      return `
        <path d="M${x - 2} ${baseY} L${leftMidX.toFixed(1)} ${leftMidY.toFixed(1)} L${leftEndX} ${endY}" />
        <path d="M${x + 2} ${baseY} L${rightMidX.toFixed(1)} ${rightMidY.toFixed(1)} L${rightEndX} ${endY}" />`;
    })
    .join("\n      ");

  const spots = spotEvery == null ? "" : segs
    .filter((_, i) => i % spotEvery === 0)
    .map(([x, y]) => `<circle cx="${x + 2}" cy="${y - 2}" r="1.9" fill="${spot}"/>`)
    .join("");

  const ellipses = segs
    .map(([x, y, rx, ry, rot]) =>
      `<ellipse cx="${x}" cy="${y}" rx="${rx}" ry="${ry}" transform="rotate(${rot} ${x} ${y})"/>`
    )
    .join(" ");

  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <ellipse cx="52" cy="80" rx="36" ry="5" fill="#000" opacity="0.13"/>
  <!-- body stroke pass -->
  <g stroke="${outline}" stroke-width="3" stroke-linejoin="round" fill="none">${ellipses}</g>
  <!-- body fill pass to hide inner strokes -->
  <g fill="${body}">${ellipses}</g>
  ${spots}
  <!-- long splayed legs drawn on top so they read clearly -->
  <g stroke="${legs}" stroke-width="2.4" stroke-linecap="round" fill="none">
      ${legLines}
  </g>
  <!-- head details -->
  <ellipse cx="12" cy="48" rx="6.8" ry="4.6" fill="${body}" stroke="${outline}" stroke-width="3"/>
  <circle cx="10.2" cy="46.6" r="1.9" fill="${dark}"/>
  <path d="M10.5 42 C8.5 36 6 33 4 31 M14 42 C15.8 36 16 31 14 27"
        fill="none" stroke="${outline}" stroke-width="2.4" stroke-linecap="round"/>
  <path d="M7 54 C9 56 12 56 14 54" fill="none" stroke="${outline}"
        stroke-width="1.6" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function moth(p = {}, name = "moth") {
  const {
    w1 = "#9c7248", w2 = "#6d4c2c",
    outline = "#4a3018", eyespotsFill = "#3d2712", eyespotsHi = "#e8d9c0",
    body = "#7a5631", bands = "#8a6538",
    // Each entry: [cx, cy, bigR, hiR]; rendered in the original order.
    eyespots = [[22, 42, 4.4, 1.8], [78, 42, 4.4, 1.8]],
  } = p;
  const eyeBigs = eyespots
    .map(([cx, cy, r]) => `<circle cx="${cx}" cy="${cy}" r="${r}"/>`)
    .join("");
  const eyeHis = eyespots
    .map(([cx, cy, , hr]) =>
      `<circle cx="${cx}" cy="${cy}" r="${hr}" fill="${eyespotsHi}"/>`)
    .join("\n  ");
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <linearGradient id="w" x1="0" y1="0" x2="0.5" y2="1">
      <stop offset="0" stop-color="${w1}"/>
      <stop offset="1" stop-color="${w2}"/>
    </linearGradient>
  </defs>
  <ellipse cx="50" cy="86" rx="26" ry="4.6" fill="#000" opacity="0.13"/>
  <g stroke="${outline}" stroke-width="3" stroke-linejoin="round">
    <path d="M45 42 C26 16 8 26 14 46 C19 62 36 62 45 54 Z" fill="url(#w)"/>
    <path d="M55 42 C74 16 92 26 86 46 C81 62 64 62 55 54 Z" fill="url(#w)"/>
    <path d="M46 56 C36 64 30 78 40 82 C47 84 49 70 49 60 Z" fill="url(#w)"/>
    <path d="M54 56 C64 64 70 78 60 82 C53 84 51 70 51 60 Z" fill="url(#w)"/>
  </g>
  <g stroke="${outline}" stroke-width="2" opacity="0.65" fill="none">
    <path d="M24 30 C30 38 34 46 36 54 M76 30 C70 38 66 46 64 54"/>
  </g>
  <g fill="${eyespotsFill}">
    ${eyeBigs}
  </g>
  ${eyeHis}
  <ellipse cx="50" cy="58" rx="9.4" ry="21" fill="${body}" stroke="${outline}" stroke-width="2.8"/>
  <g stroke="${bands}" stroke-width="1.6" opacity="0.8">
    <path d="M42 50 L58 50 M42 58 L58 58 M43 66 L57 66"/>
  </g>
  <circle cx="50" cy="34" r="7.6" fill="${body}" stroke="${outline}" stroke-width="2.6"/>
  <path d="M46 28 C42 20 38 16 30 14 M54 28 C58 20 62 16 70 14"
        fill="none" stroke="${outline}" stroke-width="2.2" stroke-linecap="round"/>
  <g stroke="${outline}" stroke-width="1.4" stroke-linecap="round">
    <path d="M44 24 L38 20 M46 22 L42 17 M56 24 L62 20 M54 22 L58 17"/>
  </g>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function grasshopper(p = {}, name = "grasshopper") {
  // Side-profile hopper: head left, body angled up to the right, folded
  // hind leg (thick femur arc, thin tibia) clear of the body.
  const {
    b1 = "#8fbf4d", b2 = "#5c8a30",
    outline = "#3f6420", hi = "#a8d06e", dark = "#26201c",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <linearGradient id="b" x1="0" y1="0" x2="0.4" y2="1">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </linearGradient>
  </defs>
  <ellipse cx="50" cy="80" rx="30" ry="5" fill="#000" opacity="0.13"/>
  <path d="M60 58 C72 54 80 44 79 34" fill="none" stroke="${outline}"
        stroke-width="6.5" stroke-linecap="round"/>
  <path d="M79 34 C82 48 78 64 70 76" fill="none" stroke="${outline}"
        stroke-width="3.6" stroke-linecap="round"/>
  <g transform="rotate(-22 57 55)">
    <ellipse cx="57" cy="55" rx="24" ry="10" fill="url(#b)"
             stroke="${outline}" stroke-width="3"/>
    <ellipse cx="59" cy="52" rx="15" ry="4.5" fill="${hi}" opacity="0.85"/>
  </g>
  <path d="M42 64 L36 76 M50 66 L47 78" fill="none" stroke="${outline}"
        stroke-width="3.2" stroke-linecap="round"/>
  <circle cx="31" cy="57" r="9.5" fill="url(#b)" stroke="${outline}" stroke-width="3"/>
  <circle cx="28" cy="55" r="2.4" fill="${dark}"/>
  <path d="M27 50 C22 42 16 38 10 37 M34 48 C32 40 30 34 26 29"
        fill="none" stroke="${outline}" stroke-width="2.4" stroke-linecap="round"/>
  <path d="M24 62 C26 64 29 64 31 62" fill="none" stroke="${outline}"
        stroke-width="1.6" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function dragonfly(p = {}, name = "dragonfly") {
  const {
    wingFill = "#cfe3ef", wingStroke = "#7ba3bd",
    body = "#5f8aa8", bodyHi = "#8fb6cd", bands = "#4a708c",
    headStroke = "#3c5f78", eye = "#26333d", eyeHi = "#dfeaf2",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <ellipse cx="50" cy="88" rx="20" ry="3.6" fill="#000" opacity="0.12"/>
  <g fill="${wingFill}" fill-opacity="0.75" stroke="${wingStroke}" stroke-width="2">
    <!-- Forewings anchored to trunk at (50,36), theta=30deg, rx=24, ry=6 -->
    <ellipse cx="29.2" cy="24" rx="24" ry="6" transform="rotate(30 29.2 24)"/>
    <ellipse cx="70.8" cy="24" rx="24" ry="6" transform="rotate(-30 70.8 24)"/>
    <!-- Hindwings anchored to trunk at (50,41), theta=-16deg, rx=21, ry=5.4 -->
    <ellipse cx="29.8" cy="46.8" rx="21" ry="5.4" transform="rotate(-16 29.8 46.8)"/>
    <ellipse cx="70.2" cy="46.8" rx="21" ry="5.4" transform="rotate(16 70.2 46.8)"/>
  </g>
  <path d="M50 34 L50 84" stroke="${body}" stroke-width="6.4" stroke-linecap="round"/>
  <path d="M50 34 L50 84" stroke="${bodyHi}" stroke-width="3" stroke-linecap="round"/>
  <g stroke="${bands}" stroke-width="1.6" opacity="0.8">
    <path d="M46 58 L54 58 M46 66 L54 66 M46.5 74 L53.5 74"/>
  </g>
  <circle cx="50" cy="26" r="9.6" fill="${body}" stroke="${headStroke}" stroke-width="2.8"/>
  <circle cx="44" cy="24" r="4.6" fill="${eye}"/>
  <circle cx="56" cy="24" r="4.6" fill="${eye}"/>
  <circle cx="45" cy="22.6" r="1.4" fill="${eyeHi}"/>
  <circle cx="57" cy="22.6" r="1.4" fill="${eyeHi}"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function beetle(p = {}, name = "beetle") {
  const {
    b1 = "#5a6b8c", b2 = "#33405c",
    elytraOutline = "#1c2434", limb = "#20283a", legHi = "#8ea2c4",
    headStroke = "#121722",
    // Optional glossy dots along the elytra: [cx, cy, r, fill].
    dots = [],
  } = p;
  const dotEls = dots.length === 0 ? "" : "\n  " + dots
    .map(([cx, cy, r, fill]) =>
      `<circle cx="${cx}" cy="${cy}" r="${r}" fill="${fill}"/>`)
    .join("");
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="b" cx="0.35" cy="0.3" r="0.95">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </radialGradient>
  </defs>
  <ellipse cx="50" cy="86" rx="26" ry="5" fill="#000" opacity="0.14"/>
  <g stroke="${limb}" stroke-width="3.2" stroke-linecap="round" fill="none">
    <path d="M30 46 L16 38 M28 60 L13 60 M32 72 L20 80"/>
    <path d="M70 46 L84 38 M72 60 L87 60 M68 72 L80 80"/>
  </g>
  <ellipse cx="50" cy="56" rx="24" ry="28" fill="url(#b)" stroke="${elytraOutline}" stroke-width="3.2"/>
  <path d="M50 30 L50 83" stroke="${elytraOutline}" stroke-width="2.6"/>${dotEls}
  <g stroke="${legHi}" stroke-width="1.6" opacity="0.7">
    <path d="M36 44 C40 46 44 47 47 47 M64 44 C60 46 56 47 53 47
             M34 58 C39 60 44 61 47 61 M66 58 C61 60 56 61 53 61"/>
  </g>
  <circle cx="50" cy="26" r="10" fill="${limb}" stroke="${headStroke}" stroke-width="2.6"/>
  <path d="M44 18 C40 10 44 6 48 8 M56 18 C60 10 56 6 52 8"
        fill="none" stroke="${headStroke}" stroke-width="3.4" stroke-linecap="round"/>
  <path d="M45 20 C42 14 42 10 45 8 M55 20 C58 14 58 10 55 8"
        fill="none" stroke="${headStroke}" stroke-width="2.2" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function snail(p = {}, name = "snail") {
  // Shell sits clear of the head so the neck, eyestalks and eyes read.
  const {
    s1 = "#b07a48", s2 = "#7c4f28",
    shellStroke = "#5c3a1c",
    bodyFill = "#c9a06a", bodyStroke = "#8a6538", bodyHi = "#d9a86a",
    // Optional extra spiral band element (prefix it with a newline+indent).
    extraBand = "",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="s" cx="0.4" cy="0.35" r="0.9">
      <stop offset="0" stop-color="${s1}"/>
      <stop offset="1" stop-color="${s2}"/>
    </radialGradient>
  </defs>
  <ellipse cx="52" cy="84" rx="34" ry="5" fill="#000" opacity="0.13"/>
  <path d="M18 76 C24 64 40 60 56 62 C68 63 76 68 80 76 C80 79 76 80 70 80
           L28 80 C21 80 16 79 18 76 Z"
        fill="${bodyFill}" stroke="${bodyStroke}" stroke-width="3" stroke-linejoin="round"/>
  <path d="M70 73 C74 66 76 60 82 56 C88 52 94 57 93 63 C92 69 86 74 78 75
           C75 75.4 72 74.6 70 73 Z"
        fill="${bodyFill}" stroke="${bodyStroke}" stroke-width="3" stroke-linejoin="round"/>
  <g fill="none" stroke="${bodyStroke}" stroke-width="2.6" stroke-linecap="round">
    <path d="M89 54 C91 47 92 42 92 38"/>
    <path d="M80 53 C79 46 77 41 74 37"/>
  </g>
  <circle cx="92.5" cy="36" r="2.7" fill="#26201c"/>
  <circle cx="73.5" cy="35" r="2.7" fill="#26201c"/>
  <circle cx="44" cy="44" r="22" fill="url(#s)" stroke="${shellStroke}" stroke-width="3.4"/>
  <path d="M44 44 m0 -13 a13 13 0 1 1 -11 20 a8 8 0 1 0 7 -12"
        fill="none" stroke="${shellStroke}" stroke-width="3.6" stroke-linecap="round"/>${extraBand}
  <ellipse cx="36" cy="36" rx="8" ry="5" fill="${bodyHi}" opacity="0.55"
           transform="rotate(-30 36 36)"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function firefly(p = {}, name = "firefly") {
  const {
    glow1 = "#fff6c8", glow2 = "#f7e28a",
    lamp1 = "#fff3b0", lamp2 = "#f2c94c", lampStroke = "#a3771c",
    wingFill = "#cfe3ef", wingStroke = "#7ba3bd",
    dark = "#2e241a", body = "#4a3b2a", headStroke = "#1c150f",
    eyeHi = "#f5e8cd",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="glow" cx="0.5" cy="0.5" r="0.5">
      <stop offset="0" stop-color="${glow1}" stop-opacity="0.85"/>
      <stop offset="0.55" stop-color="${glow2}" stop-opacity="0.35"/>
      <stop offset="1" stop-color="${glow2}" stop-opacity="0"/>
    </radialGradient>
    <radialGradient id="lamp" cx="0.4" cy="0.35" r="0.9">
      <stop offset="0" stop-color="${lamp1}"/>
      <stop offset="1" stop-color="${lamp2}"/>
    </radialGradient>
  </defs>
  <ellipse cx="50" cy="88" rx="18" ry="3.6" fill="#000" opacity="0.12"/>
  <circle cx="50" cy="62" r="30" fill="url(#glow)"/>
  <ellipse cx="36" cy="46" rx="9" ry="15" fill="${wingFill}" fill-opacity="0.75"
           stroke="${wingStroke}" stroke-width="2" transform="rotate(-30 36 46)"/>
  <ellipse cx="64" cy="46" rx="9" ry="15" fill="${wingFill}" fill-opacity="0.75"
           stroke="${wingStroke}" stroke-width="2" transform="rotate(30 64 46)"/>
  <path d="M41 45 L33 51 M41 50 L33 57 M59 45 L67 51 M59 50 L67 57"
        fill="none" stroke="${dark}" stroke-width="2.2" stroke-linecap="round"/>
  <ellipse cx="50" cy="62" rx="13" ry="16" fill="url(#lamp)"
           stroke="${lampStroke}" stroke-width="3"/>
  <ellipse cx="50" cy="42" rx="10" ry="9" fill="${body}" stroke="${dark}"
           stroke-width="2.8"/>
  <circle cx="50" cy="30" r="7" fill="${dark}" stroke="${headStroke}" stroke-width="2.2"/>
  <circle cx="47.4" cy="28.4" r="1.7" fill="${eyeHi}"/>
  <circle cx="52.6" cy="28.4" r="1.7" fill="${eyeHi}"/>
  <path d="M47 24 C44 18 40 15 36 14 M53 24 C56 18 60 15 64 14"
        fill="none" stroke="${dark}" stroke-width="2" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function bumblebee(p = {}, name = "bumblebee") {
  const {
    b1 = "#f6c04e", b2 = "#d9942e",
    outline = "#8a5b14", band = "#26201c", dark = "#26201c",
    headStroke = "#141210", eyeHi = "#f5e8cd",
    wingFill = "#dfeaf2", wingStroke = "#8fb6cd",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="b" cx="0.35" cy="0.3" r="0.95">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </radialGradient>
  </defs>
  <ellipse cx="50" cy="88" rx="22" ry="4" fill="#000" opacity="0.13"/>
  <ellipse cx="35" cy="46" rx="13" ry="7.5" fill="${wingFill}" fill-opacity="0.85"
           stroke="${wingStroke}" stroke-width="2.2" transform="rotate(-32 35 46)"/>
  <ellipse cx="65" cy="46" rx="13" ry="7.5" fill="${wingFill}" fill-opacity="0.85"
           stroke="${wingStroke}" stroke-width="2.2" transform="rotate(32 65 46)"/>
  <ellipse cx="50" cy="60" rx="20" ry="24" fill="url(#b)"
           stroke="${outline}" stroke-width="3.4"/>
  <ellipse cx="50" cy="52" rx="18.5" ry="5.2" fill="${band}"/>
  <ellipse cx="50" cy="66" rx="17" ry="5" fill="${band}"/>
  <path d="M50 90 L45.5 80 L54.5 80 Z" fill="${band}" stroke="${band}"
        stroke-width="1.5" stroke-linejoin="round"/>
  <circle cx="50" cy="28" r="8.5" fill="${dark}" stroke="${headStroke}" stroke-width="2.2"/>
  <circle cx="46.8" cy="26.5" r="2" fill="${eyeHi}"/>
  <circle cx="53.2" cy="26.5" r="2" fill="${eyeHi}"/>
  <path d="M46 21 C43 15 39 12 34 11 M54 21 C57 15 61 12 66 11"
        fill="none" stroke="${dark}" stroke-width="2.2" stroke-linecap="round"/>
  <circle cx="33" cy="10.5" r="1.8" fill="${dark}"/>
  <circle cx="67" cy="10.5" r="1.8" fill="${dark}"/>
  <path d="M38 74 L32 82 M62 74 L68 82" fill="none" stroke="${dark}"
        stroke-width="2.6" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function caterpillar(p = {}, name = "caterpillar") {
  const {
    body = "#7fae4e", outline = "#3e6323", spot = "#a8d06e",
    head = "#8fbf4e", dark = "#26201c",
  } = p;
  // Arched segments, stub legs, friendly face — clearly softer than
  // the long-legged centipede.
  const segs = [
    [36, 56, 7.5], [48, 52, 7.5], [60, 56, 7.5], [71, 61, 7.0], [80, 67, 5.5],
  ];
  const circles = segs
    .map(([x, y, r]) => `<circle cx="${x}" cy="${y}" r="${r}"/>`)
    .join(" ");

  // Build legs BEFORE drawing the body so the body fill covers the
  // upper part of each leg. Give every segment one pair of short stub
  // legs (down-and-out). Ensure legs end above the ground shadow (<= y=79).
  const legLength = 9; // visible extent below the body
  const legs = segs
    .map(([x, y, r]) => {
      const startY = Math.round(y + r - 2);
      const endY = Math.min(79, Math.round(y + r + legLength));
      const leftStartX = Math.round(x - r * 0.5);
      const leftEndX = Math.round(leftStartX - 7);
      const rightStartX = Math.round(x + r * 0.5);
      const rightEndX = Math.round(rightStartX + 7);
      return `<path d="M${leftStartX} ${startY} L${leftEndX} ${endY} M${rightStartX} ${startY} L${rightEndX} ${endY}"/>`;
    })
    .join(" ");

  // Tail prolegs (2-3 little vertical stubs under the last segment), end by y=79
  const [tx, ty, tr] = segs[segs.length - 1];
  const prolegs = [-3, 0, 3]
    .map(off => {
      const sx = tx + off;
      const sy0 = Math.round(ty + tr * 0.7);
      const sy1 = Math.min(79, sy0 + legLength);
      return `<path d="M${sx} ${sy0} L${sx} ${sy1}"/>`;
    })
    .join(" ");

  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <ellipse cx="50" cy="84" rx="34" ry="5" fill="#000" opacity="0.13"/>
  <!-- legs drawn BEFORE the body so the body fill covers their tops and only the lower
       portions of the legs peek out beneath the silhouette -->
  <g stroke="${outline}" stroke-width="2.6" stroke-linecap="round" fill="none">${legs} ${prolegs}</g>
  <g fill="${body}" stroke="${outline}" stroke-width="3">${circles}</g>
  <g fill="${body}">${circles}</g>
  <circle cx="48" cy="50" r="2.1" fill="${spot}"/>
  <circle cx="60" cy="54" r="2.1" fill="${spot}"/>
  <circle cx="22" cy="60" r="9.5" fill="${head}" stroke="${outline}" stroke-width="3"/>
  <circle cx="19" cy="58" r="2.2" fill="${dark}"/>
  <path d="M15 64 C17 66 20 66 22 64" fill="none" stroke="${outline}"
        stroke-width="1.6" stroke-linecap="round"/>
  <path d="M17 52 C14 46 10 43 6 42 M23 51 C22 44 20 39 16 35"
        fill="none" stroke="${outline}" stroke-width="2.2" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function mantis(p = {}, name = "mantis") {
  // Triangular head with big eyes + folded forelegs carry the silhouette.
  const {
    m1 = "#8fbf4d", m2 = "#5c8a30",
    outline = "#3e6323", hi = "#a8d06e", head = "#7fae4e", dark = "#26201c",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <linearGradient id="m" x1="0" y1="0" x2="0.4" y2="1">
      <stop offset="0" stop-color="${m1}"/>
      <stop offset="1" stop-color="${m2}"/>
    </linearGradient>
  </defs>
  <ellipse cx="52" cy="86" rx="26" ry="4.5" fill="#000" opacity="0.12"/>
  <g stroke="${outline}" stroke-width="2.6" fill="none" stroke-linecap="round"
     stroke-linejoin="round">
    <path d="M44 52 L34 66 L40 70"/>
    <path d="M54 60 L46 76 L52 80"/>
    <path d="M66 64 L62 78 L68 82"/>
  </g>
  <g stroke="${outline}" stroke-width="3.2" fill="none" stroke-linecap="round"
     stroke-linejoin="round">
    <!-- Single clean folded foreleg PAIR (praying pose). Each arm folds up-forward then sharply down. -->
    <path d="M44 44 L50 30 L56 44"/>
    <path d="M46 46 L52 32 L58 46"/>
  </g>
  <g transform="rotate(38 58 62)">
    <ellipse cx="58" cy="62" rx="24" ry="9" fill="url(#m)"
             stroke="${outline}" stroke-width="3"/>
    <ellipse cx="60" cy="59.5" rx="15" ry="4" fill="${hi}" opacity="0.8"/>
  </g>
  <path d="M34 26 L46 48" stroke="${outline}" stroke-width="6" stroke-linecap="round"/>
  <path d="M34 26 L46 48" stroke="${head}" stroke-width="3" stroke-linecap="round"/>
  <path d="M26 20 C33 16 40 18 40 25 C40 32 33 34 26 30 C22 27 22 23 26 20 Z"
        fill="${head}" stroke="${outline}" stroke-width="2.6" stroke-linejoin="round"/>
  <circle cx="27" cy="25" r="3" fill="${dark}"/>
  <circle cx="36" cy="24" r="3" fill="${dark}"/>
  <path d="M32 18 C30 12 27 8 23 6 M38 17 C38 11 37 7 35 4"
        fill="none" stroke="${outline}" stroke-width="2" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function stickInsect(p = {}, name = "stick_insect") {
  // A long thin stick: dark under-stroke + light over-stroke fake an outline.
  const { dark = "#55603a", body = "#8a9455" } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <ellipse cx="48" cy="88" rx="30" ry="3.6" fill="#000" opacity="0.1"/>
  <g stroke="${dark}" stroke-width="2.4" fill="none" stroke-linecap="round"
     stroke-linejoin="round">
    <path d="M64 46 L56 60 L62 72"/>
    <path d="M52 54 L44 66 L50 78"/>
    <path d="M40 62 L32 74 L38 84"/>
  </g>
  <path d="M16 86 L78 30" stroke="${dark}" stroke-width="9" stroke-linecap="round"/>
  <path d="M16 86 L78 30" stroke="${body}" stroke-width="5.4" stroke-linecap="round"/>
  <g stroke="${dark}" stroke-width="1.4" opacity="0.7">
    <path d="M30 74 L36 80 M42 64 L48 70 M54 54 L60 60 M66 44 L72 50"/>
  </g>
  <circle cx="82" cy="27" r="6" fill="${body}" stroke="${dark}" stroke-width="2.4"/>
  <circle cx="84" cy="26" r="1.8" fill="#26201c"/>
  <path d="M86 22 C90 16 94 12 97 10 M84 20 C86 14 88 9 92 6"
        fill="none" stroke="${dark}" stroke-width="2" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function weevil(p = {}, name = "weevil") {
  const {
    w1 = "#a8794a", w2 = "#7a5426",
    outline = "#4a3418", spot = "#c49a63", head = "#8a6538",
    // Dorsal speckles: [cx, cy, r].
    spots = [[50, 48, 2.2], [66, 52, 2.2], [56, 64, 2.2]],
  } = p;
  const dot = ([cx, cy, r]) => `<circle cx="${cx}" cy="${cy}" r="${r}"/>`;
  const spotEls = spots
    .map((s, i) =>
      i % 2 === 1 ? dot(s) : (i === 0 ? dot(s) : "\n    " + dot(s)))
    .join("");
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="w" cx="0.35" cy="0.3" r="0.95">
      <stop offset="0" stop-color="${w1}"/>
      <stop offset="1" stop-color="${w2}"/>
    </radialGradient>
  </defs>
  <ellipse cx="52" cy="80" rx="26" ry="4.5" fill="#000" opacity="0.12"/>
  <g stroke="${outline}" stroke-width="2.8" fill="none" stroke-linecap="round">
    <path d="M40 62 L28 72 M44 68 L36 79 M58 68 L60 80 M66 60 L74 70"/>
  </g>
  <ellipse cx="58" cy="56" rx="22" ry="19" fill="url(#w)"
           stroke="${outline}" stroke-width="3.2"/>
  <path d="M58 38 L58 74" stroke="${outline}" stroke-width="2.2"/>
  <g fill="${spot}" opacity="0.9">
    ${spotEls}
  </g>
  <circle cx="32" cy="44" r="7" fill="${head}" stroke="${outline}" stroke-width="2.8"/>
  <!-- tapered snout: dark under-shape then lighter inner fill -->
  <path d="M28 42 L11 46 L14 44 Z" fill="${outline}" stroke="none"/>
  <path d="M28 42 L13 45 L13 44 Z" fill="${w1}" stroke="none"/>
  <!-- tip dot -->
  <circle cx="11" cy="46" r="1.6" fill="#26201c"/>
  <!-- small antenna along snout -->
  <path d="M20 43 C19 41 17 40 16 41" fill="none" stroke="${outline}" stroke-width="1.6" stroke-linecap="round"/>
  <circle cx="16.2" cy="41.1" r="0.9" fill="#26201c"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function pillbug(p = {}, name = "pillbug") {
  const {
    p1 = "#8a93a3", p2 = "#57606d",
    outline = "#3c424c", hi = "#9aa3b2",
    head = "#57606d",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="p" cx="0.4" cy="0.3" r="0.95">
      <stop offset="0" stop-color="${p1}"/>
      <stop offset="1" stop-color="${p2}"/>
    </radialGradient>
  </defs>
  <ellipse cx="50" cy="84" rx="26" ry="4.5" fill="#000" opacity="0.12"/>
  <g stroke="${outline}" stroke-width="2.4" stroke-linecap="round">
    <path d="M36 74 L32 82 M46 76 L44 84 M56 76 L56 84 M66 74 L70 82"/>
  </g>
  <path d="M24 76 C18 56 32 40 52 40 C72 40 84 56 78 76 C74 80 28 80 24 76 Z"
        fill="url(#p)" stroke="${outline}" stroke-width="3.2" stroke-linejoin="round"/>
  <g fill="none" stroke="${outline}" stroke-width="2.2" stroke-linecap="round" opacity="0.85">
    <path d="M36 44 C32 56 32 66 36 76"/>
    <path d="M46 41 C43 54 43 66 46 78"/>
    <path d="M56 40 C54 54 54 66 56 78"/>
    <path d="M66 42 C65 54 65 66 66 77"/>
    <path d="M74 48 C75 58 75 66 73 75"/>
  </g>
  <ellipse cx="44" cy="52" rx="10" ry="5" fill="${hi}" opacity="0.6"
           transform="rotate(-16 44 52)"/>
  <circle cx="26" cy="60" r="7" fill="${head}" stroke="${outline}" stroke-width="2.6"/>
  <circle cx="23" cy="58" r="1.8" fill="#26201c"/>
  <path d="M21 54 C18 49 15 46 11 44 M24 53 C23 48 21 44 18 41"
        fill="none" stroke="${outline}" stroke-width="2" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function ant(p = {}, name = "ant") {
  const {
    body = "#3a2c1c", outline = "#1c150f", legDark = "#26201c",
    eyeHi = "#f5e8cd",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <ellipse cx="50" cy="80" rx="24" ry="4" fill="#000" opacity="0.12"/>
  <g stroke="${legDark}" stroke-width="2.4" fill="none" stroke-linecap="round"
     stroke-linejoin="round">
    <path d="M38 58 L28 68 L30 74 M42 60 L38 72 L42 78 M48 60 L52 72 L58 76"/>
  </g>
  <ellipse cx="68" cy="58" rx="15" ry="12" fill="${body}" stroke="${outline}"
           stroke-width="2.8" transform="rotate(-18 68 58)"/>
  <circle cx="57" cy="55" r="3.6" fill="${body}" stroke="${outline}" stroke-width="2"/>
  <ellipse cx="45" cy="53" rx="9" ry="7" fill="${body}" stroke="${outline}"
           stroke-width="2.6"/>
  <circle cx="29" cy="47" r="8" fill="${body}" stroke="${outline}" stroke-width="2.6"/>
  <circle cx="26" cy="45" r="1.8" fill="${eyeHi}"/>
  <path d="M27 40 L22 32 L26 26 M33 40 L33 31 L38 25"
        stroke="${outline}" stroke-width="2.2" fill="none" stroke-linecap="round"
        stroke-linejoin="round"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function fly(p = {}, name = "fly") {
  const {
    f1 = "#99a3b2", f2 = "#6a7380",
    outline = "#3c424c", thorax = "#7a8391",
    eye1 = "#a8442e", eyeStroke = "#6e2417", eyeHi = "#e8d9c0",
    wingFill = "#dfeaf2", wingStroke = "#8fb6cd",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="f" cx="0.35" cy="0.3" r="0.95">
      <stop offset="0" stop-color="${f1}"/>
      <stop offset="1" stop-color="${f2}"/>
    </radialGradient>
  </defs>
  <ellipse cx="50" cy="82" rx="22" ry="4" fill="#000" opacity="0.12"/>
  <ellipse cx="66" cy="44" rx="15" ry="5.5" fill="${wingFill}" fill-opacity="0.8"
           stroke="${wingStroke}" stroke-width="2" transform="rotate(-18 66 44)"/>
  <ellipse cx="64" cy="54" rx="13" ry="4.8" fill="${wingFill}" fill-opacity="0.8"
           stroke="${wingStroke}" stroke-width="2" transform="rotate(10 64 54)"/>
  <g stroke="${outline}" stroke-width="2.2" fill="none" stroke-linecap="round">
    <path d="M40 58 L32 70 M46 60 L42 74 M56 60 L60 74"/>
  </g>
  <ellipse cx="60" cy="62" rx="13" ry="10.5" fill="url(#f)" stroke="${outline}"
           stroke-width="2.8" transform="rotate(28 60 62)"/>
  <ellipse cx="46" cy="50" rx="12" ry="10" fill="${thorax}" stroke="${outline}"
           stroke-width="2.8"/>
  <circle cx="39" cy="38" r="5.4" fill="${eye1}" stroke="${eyeStroke}" stroke-width="2.2"/>
  <circle cx="51" cy="37" r="5.4" fill="${eye1}" stroke="${eyeStroke}" stroke-width="2.2"/>
  <circle cx="37.5" cy="36.5" r="1.5" fill="${eyeHi}"/>
  <circle cx="49.5" cy="35.5" r="1.5" fill="${eyeHi}"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

// ---- New species (slice: 22 additional bugs, 4 variants each) ----
function aphid(p = {}, name = "aphid") {
  // Tiny teardrop with two splayed legs and twin cornicles on the rear.
  const {
    b1 = "#a4c464", b2 = "#6e8a3c", outline = "#42551c",
    hi = "#c2d88a", cornicle = "#3e5218", eye = "#26201c",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <ellipse cx="46" cy="78" rx="20" ry="4" fill="#000" opacity="0.12"/>
  <g stroke="${outline}" stroke-width="2.4" fill="none" stroke-linecap="round">
    <path d="M40 64 L28 74 M46 66 L42 78"/>
  </g>
  <path d="M46 30 C64 30 72 48 68 62 C64 72 30 72 26 60 C22 46 32 30 46 30 Z"
        fill="url(#a)" stroke="${outline}" stroke-width="3"/>
  <ellipse cx="42" cy="42" rx="9" ry="6" fill="${hi}" opacity="0.7"
           transform="rotate(-22 42 42)"/>
  <path d="M60 54 L64 46 M66 56 L71 49" stroke="${cornicle}"
        stroke-width="2.4" fill="none" stroke-linecap="round"/>
  <circle cx="38" cy="36" r="2.4" fill="${eye}"/>
  <path d="M34 30 C30 24 26 21 22 20 M40 28 C40 22 42 17 45 14"
        fill="none" stroke="${outline}" stroke-width="1.8" stroke-linecap="round"/>
  <defs>
    <radialGradient id="a" cx="0.35" cy="0.3" r="0.95">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </radialGradient>
  </defs>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function barklice(p = {}, name = "barklice") {
  // Small oval with roof-like patterned wings folded flat over the back.
  const {
    b1 = "#c9ab7c", b2 = "#96794e", outline = "#5c4628",
    wingMark = "#7a6240", hi = "#e0c8a0", eye = "#26201c",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="bk" cx="0.4" cy="0.3" r="0.95">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </radialGradient>
  </defs>
  <ellipse cx="50" cy="72" rx="24" ry="4.4" fill="#000" opacity="0.12"/>
  <g stroke="${outline}" stroke-width="2.2" fill="none" stroke-linecap="round">
    <path d="M36 58 L26 68 M46 62 L42 72 M58 60 L64 70 M64 54 L74 62"/>
  </g>
  <path d="M50 34 C68 34 78 46 76 58 C74 66 26 66 24 58 C22 46 32 34 50 34 Z"
        fill="url(#bk)" stroke="${outline}" stroke-width="2.8"/>
  <path d="M50 34 L50 64" stroke="${outline}" stroke-width="1.8"/>
  <g stroke="${wingMark}" stroke-width="1.8" fill="none" opacity="0.8">
    <path d="M38 42 C34 50 34 56 38 62 M62 42 C66 50 66 56 62 62"/>
    <path d="M44 38 C42 46 42 56 44 64 M56 38 C58 46 58 56 56 64"/>
  </g>
  <ellipse cx="42" cy="42" rx="8" ry="4" fill="${hi}" opacity="0.6"
           transform="rotate(-16 42 42)"/>
  <circle cx="40" cy="38" r="2.2" fill="${eye}"/>
  <circle cx="46" cy="37" r="2.2" fill="${eye}"/>
  <path d="M37 34 C34 29 31 26 28 25 M43 33 C44 28 47 24 51 22"
        fill="none" stroke="${outline}" stroke-width="1.6" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function cicada(p = {}, name = "cicada") {
  // Wide blunt body, stubby clear wings angled back, two big wide-set eyes.
  const {
    b1 = "#a8823c", b2 = "#6e521e", outline = "#42300e",
    wingFill = "#e2e8d8", wingStroke = "#9aa884",
    hi = "#c4a058", eye = "#2a2418", eyeHi = "#e8d9c0",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="cc" cx="0.4" cy="0.3" r="0.95">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </radialGradient>
  </defs>
  <ellipse cx="50" cy="82" rx="26" ry="4.6" fill="#000" opacity="0.13"/>
  <path d="M30 42 C36 34 64 34 70 42 L80 64 C74 72 26 72 20 64 Z"
        fill="${wingFill}" fill-opacity="0.8" stroke="${wingStroke}" stroke-width="2.4"/>
  <path d="M40 44 L46 66 M50 42 L50 68 M60 44 L54 66"
        stroke="${wingStroke}" stroke-width="1.4" fill="none" opacity="0.7"/>
  <path d="M32 38 C34 26 44 22 50 22 C56 22 66 26 68 38 C70 52 64 66 50 68
           C36 66 30 52 32 38 Z"
        fill="url(#cc)" stroke="${outline}" stroke-width="3"/>
  <path d="M36 44 L34 62 M44 46 L43 66 M56 46 L57 66 M64 44 L66 62"
        stroke="${outline}" stroke-width="1.6" opacity="0.65" fill="none"/>
  <ellipse cx="42" cy="34" rx="9" ry="4.6" fill="${hi}" opacity="0.7"
           transform="rotate(-14 42 34)"/>
  <path d="M38 68 L30 78 M46 70 L44 80 M58 70 L60 80 M64 68 L72 78"
        stroke="${outline}" stroke-width="2.2" fill="none" stroke-linecap="round"/>
  <circle cx="30" cy="34" r="6.4" fill="${eye}" stroke="${outline}" stroke-width="1.8"/>
  <circle cx="70" cy="34" r="6.4" fill="${eye}" stroke="${outline}" stroke-width="1.8"/>
  <circle cx="28" cy="32" r="1.8" fill="${eyeHi}"/>
  <circle cx="68" cy="32" r="1.8" fill="${eyeHi}"/>
  <path d="M44 24 C46 20 54 20 56 24" stroke="${outline}" stroke-width="2"
        fill="none" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function clickBeetle(p = {}, name = "click_beetle") {
  // Elongated flat body, squared pronotum with pinched rear corners, tiny head.
  const {
    b1 = "#8a6a3c", b2 = "#54401e", outline = "#33250e",
    hi = "#a8895a", eye = "#26201c", feathery = "#33250e",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <linearGradient id="ck" x1="0" y1="0" x2="0.3" y2="1">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </linearGradient>
  </defs>
  <ellipse cx="50" cy="82" rx="24" ry="4.4" fill="#000" opacity="0.12"/>
  <g stroke="${outline}" stroke-width="2.4" fill="none" stroke-linecap="round">
    <path d="M34 56 L24 68 M38 60 L30 72 M48 62 L46 74 M58 62 L60 74 M66 58 L74 70"/>
  </g>
  <ellipse cx="50" cy="50" rx="17" ry="27" fill="url(#ck)"
           stroke="${outline}" stroke-width="3"/>
  <path d="M50 26 L50 76" stroke="${outline}" stroke-width="2"/>
  <path d="M35 32 C40 24 60 24 65 32 L64 38 C56 34 44 34 36 38 Z"
        fill="${b1}" stroke="${outline}" stroke-width="2.6" stroke-linejoin="round"/>
  <ellipse cx="43" cy="44" rx="7" ry="9" fill="${hi}" opacity="0.5"
           transform="rotate(14 43 44)"/>
  <g stroke="${feathery}" stroke-width="1.4" opacity="0.5" fill="none">
    <path d="M38 66 C40 62 44 60 48 60"/>
  </g>
  <circle cx="50" cy="19" r="5" fill="${b1}" stroke="${outline}" stroke-width="2.4"/>
  <g fill="${eye}"><circle cx="48" cy="18" r="1.4"/><circle cx="52" cy="18" r="1.4"/></g>
  <path d="M46 15 C44 11 41 9 38 8 M54 15 C56 11 59 9 62 8"
        fill="none" stroke="${outline}" stroke-width="1.6" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function damselfly(p = {}, name = "damselfly") {
  // Slender percher: narrow wings held together over the back, thin long tail.
  const {
    b1 = "#5aa8b8", b2 = "#2e7488", outline = "#1c4a58",
    wingFill = "#dcecf4", wingStroke = "#8ab4c8",
    hi = "#84c8d4", eye = "#1e3444", eyeHi = "#dfeaf2",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <ellipse cx="48" cy="80" rx="24" ry="4" fill="#000" opacity="0.12"/>
  <ellipse cx="58" cy="34" rx="24" ry="5" fill="${wingFill}" fill-opacity="0.85"
           stroke="${wingStroke}" stroke-width="2" transform="rotate(-24 58 34)"/>
  <ellipse cx="60" cy="42" rx="22" ry="4.6" fill="${wingFill}" fill-opacity="0.85"
           stroke="${wingStroke}" stroke-width="2" transform="rotate(-12 60 42)"/>
  <path d="M48 34 L74 28 M50 42 L74 39" stroke="${wingStroke}" stroke-width="1"
        opacity="0.6"/>
  <g stroke="${outline}" stroke-width="2.2" fill="none" stroke-linecap="round">
    <path d="M38 50 L26 62 M42 52 L36 66"/>
  </g>
  <path d="M30 56 C34 50 42 46 48 48 L76 64 C78 66 76 70 72 69 L44 58
           C38 58 34 60 30 60 Z"
        fill="url(#dm)" stroke="${outline}" stroke-width="2.6" stroke-linejoin="round"/>
  <circle cx="30" cy="50" r="8" fill="${b1}" stroke="${outline}" stroke-width="2.4"/>
  <circle cx="26" cy="48" r="4.6" fill="${eye}"/>
  <circle cx="34" cy="47" r="4.6" fill="${eye}"/>
  <circle cx="24.6" cy="46.6" r="1.4" fill="${eyeHi}"/>
  <circle cx="32.6" cy="45.6" r="1.4" fill="${eyeHi}"/>
  <ellipse cx="42" cy="50" rx="7" ry="3.4" fill="${hi}" opacity="0.7"
           transform="rotate(16 42 50)"/>
  <defs>
    <linearGradient id="dm" x1="0" y1="0" x2="1" y2="0.4">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </linearGradient>
  </defs>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function earwig(p = {}, name = "earwig") {
  // Elongated dark body with folded wings and pincer forceps at the rear.
  const {
    b1 = "#8a6238", b2 = "#5c3e1e", outline = "#33220e",
    hi = "#a8804e", pincer = "#4a3216", eye = "#26201c", eyeHi = "#f5e8cd",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <linearGradient id="ew" x1="0" y1="0" x2="0.3" y2="1">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </linearGradient>
  </defs>
  <ellipse cx="50" cy="78" rx="28" ry="4" fill="#000" opacity="0.12"/>
  <g stroke="${outline}" stroke-width="2" fill="none" stroke-linecap="round">
    <path d="M40 58 L30 66 M46 62 L40 72 M56 62 L62 72 M64 56 L74 64"/>
  </g>
  <path d="M28 46 C28 38 72 38 72 46 L70 58 C66 64 34 64 30 58 Z"
        fill="url(#ew)" stroke="${outline}" stroke-width="2.6" stroke-linejoin="round"/>
  <path d="M28 46 C34 56 40 60 46 62 C54 64 62 62 70 54"
        fill="none" stroke="${outline}" stroke-width="1.8" opacity="0.6"/>
  <ellipse cx="28" cy="42" rx="12" ry="9" fill="${b1}" stroke="${outline}" stroke-width="2.6"/>
  <ellipse cx="25" cy="40" rx="5" ry="3.4" fill="${hi}" opacity="0.6"
           transform="rotate(-18 25 40)"/>
  <path d="M46 60 L36 74 M52 61 L48 76 M58 60 L62 74"
        stroke="${outline}" stroke-width="1.8" fill="none" stroke-linecap="round" opacity="0.8"/>
  <path d="M70 56 C80 58 86 64 84 72" fill="none" stroke="${pincer}"
        stroke-width="3.4" stroke-linecap="round"/>
  <path d="M74 54 C82 50 90 52 93 58" fill="none" stroke="${pincer}"
        stroke-width="3.4" stroke-linecap="round"/>
  <circle cx="21" cy="40" r="1.8" fill="${eyeHi}"/>
  <path d="M18 34 C14 28 10 25 6 24 M22 33 C22 27 24 21 28 17"
        fill="none" stroke="${outline}" stroke-width="1.6" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function earthworm(p = {}, name = "earthworm") {
  // Legless segmented tube in an S-curve; clitellum as a pale saddle band.
  const {
    b1 = "#d99a8a", b2 = "#a86458", outline = "#6e3a34",
    hi = "#eab8a8", saddle = "#c47a6e", seg = "#8a4c42",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <linearGradient id="wm" x1="0" y1="0" x2="0.3" y2="1">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </linearGradient>
  </defs>
  <ellipse cx="50" cy="82" rx="32" ry="4.4" fill="#000" opacity="0.11"/>
  <path d="M12 66 C20 52 40 50 50 60 C60 70 76 68 84 56 L88 60 C82 74 62 78 48 70
           C36 64 24 66 16 72 Z"
        fill="url(#wm)" stroke="${outline}" stroke-width="2.8" stroke-linejoin="round"/>
  <path d="M28 63 C30 58 36 55 42 56 L40 64 C36 64 31 64 28 63 Z" fill="${saddle}" opacity="0.9"/>
  <g stroke="${seg}" stroke-width="1.5" opacity="0.55" fill="none">
    <path d="M50 62 L52 70 M58 66 L60 72 M66 66 L68 72 M74 64 L76 70"/>
  </g>
  <path d="M14 64 C18 58 24 55 30 56" stroke="${hi}" stroke-width="2"
        fill="none" stroke-linecap="round" opacity="0.7"/>
  <circle cx="13" cy="64" r="1.4" fill="${outline}"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function froghopper(p = {}, name = "froghopper") {
  // Wide squat hopper: blunt frog-like face, folded wings, chunky hind legs.
  const {
    b1 = "#8a9455", b2 = "#57622c", outline = "#33400f",
    hi = "#a8b06e", eye = "#26201c", eyeHi = "#f5e8cd",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="fh" cx="0.4" cy="0.3" r="0.95">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </radialGradient>
  </defs>
  <ellipse cx="50" cy="72" rx="26" ry="4.4" fill="#000" opacity="0.12"/>
  <g stroke="${outline}" stroke-width="2.2" fill="none" stroke-linecap="round">
    <path d="M32 56 L22 64 M68 56 L78 64"/>
    <path d="M42 64 C50 68 60 66 66 58 C72 48 66 40 58 42 C52 28 34 30 34 42
             C26 42 22 52 28 58 C32 62 38 62 42 64 Z" fill="none" stroke-width="0"/>
  </g>
  <path d="M42 64 C50 68 60 66 66 58 C72 48 66 40 58 42 C56 30 36 28 34 42
           C26 42 22 52 28 58 C34 63 38 62 42 64 Z"
        fill="url(#fh)" stroke="${outline}" stroke-width="2.8" stroke-linejoin="round"/>
  <path d="M34 46 L66 46 L64 58 C56 62 44 62 36 58 Z" fill="${b2}" opacity="0.55"/>
  <path d="M36 42 C40 38 46 38 50 42" stroke="${hi}" stroke-width="2"
        fill="none" stroke-linecap="round" opacity="0.7"/>
  <g stroke="${outline}" stroke-width="3" fill="none" stroke-linecap="round">
    <path d="M30 58 C24 62 22 68 24 72 M70 58 C76 62 78 68 76 72"/>
  </g>
  <circle cx="34" cy="36" r="4.4" fill="${eye}" stroke="${outline}" stroke-width="1.4"/>
  <circle cx="48" cy="33" r="4.4" fill="${eye}" stroke="${outline}" stroke-width="1.4"/>
  <circle cx="32.8" cy="34.6" r="1.4" fill="${eyeHi}"/>
  <circle cx="46.8" cy="31.6" r="1.4" fill="${eyeHi}"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function glowworm(p = {}, name = "glowworm") {
  // Dark segmented larva whose rear segments carry a soft glow.
  const {
    b1 = "#6a5a44", b2 = "#3e3226", outline = "#241c12",
    hi = "#8a7658", glow1 = "#d8f0a0", glow2 = "#a8d858", eye = "#26201c",
  } = p;
  const segs = [
    [26, 58, 9], [38, 60, 9.5], [50, 61, 9.5], [62, 61, 9], [73, 59, 8],
  ];
  const segEls = segs
    .map(([x, y, r]) => `<circle cx="${x}" cy="${y}" r="${r}"/>`)
    .join(" ");
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <linearGradient id="gw" x1="0" y1="0" x2="0.4" y2="1">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </linearGradient>
    <radialGradient id="gwglow" cx="0.5" cy="0.5" r="0.5">
      <stop offset="0" stop-color="${glow1}" stop-opacity="0.9"/>
      <stop offset="0.6" stop-color="${glow2}" stop-opacity="0.45"/>
      <stop offset="1" stop-color="${glow2}" stop-opacity="0"/>
    </radialGradient>
  </defs>
  <ellipse cx="48" cy="74" rx="30" ry="4.4" fill="#000" opacity="0.12"/>
  <circle cx="76" cy="59" r="22" fill="url(#gwglow)"/>
  <g fill="url(#gw)" stroke="${outline}" stroke-width="2.6">${segEls}</g>
  <g fill="${glow2}" opacity="0.85">
    <circle cx="62" cy="61" r="3.4"/><circle cx="73" cy="59" r="3.8"/>
  </g>
  <ellipse cx="24" cy="55" rx="4" ry="2.4" fill="${hi}" opacity="0.7"
           transform="rotate(-24 24 55)"/>
  <g stroke="${outline}" stroke-width="1.8" fill="none" stroke-linecap="round">
    <path d="M30 67 L26 72 M46 70 L44 74 M60 70 L62 74"/>
  </g>
  <circle cx="18" cy="55" r="6.4" fill="${b1}" stroke="${outline}" stroke-width="2.4"/>
  <circle cx="15.6" cy="53.6" r="1.7" fill="${eye}"/>
  <path d="M13 50 C10 45 7 42 4 41 M16 49 C15 44 16 39 19 35"
        fill="none" stroke="${outline}" stroke-width="1.6" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function jewelBeetle(p = {}, name = "jewel_beetle") {
  // Streamlined metallic body tapering to a point; pronotum wider than head.
  const {
    b1 = "#3ca88a", b2 = "#166a54", outline = "#0c3a2c",
    hi = "#7ad8b8", limb = "#0e4234", legHi = "#5ab898", head = "#1a5a46",
    eye = "#26201c",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <linearGradient id="jb" x1="0" y1="0" x2="0.35" y2="1">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </linearGradient>
  </defs>
  <ellipse cx="48" cy="84" rx="24" ry="4.4" fill="#000" opacity="0.13"/>
  <g stroke="${limb}" stroke-width="2.4" fill="none" stroke-linecap="round">
    <path d="M36 52 L24 62 M40 56 L32 68 M52 58 L56 70 M60 54 L70 66 M64 48 L76 56"/>
  </g>
  <path d="M48 18 C64 18 72 32 70 48 C68 64 58 78 48 82 C38 78 28 64 26 48
           C24 32 32 18 48 18 Z"
        fill="url(#jb)" stroke="${outline}" stroke-width="3"/>
  <path d="M48 22 L48 78" stroke="${outline}" stroke-width="1.8"/>
  <path d="M32 30 C36 24 42 22 48 22 C54 22 60 24 64 30 C60 26 54 24 48 24
           C42 24 36 26 32 30 Z" fill="${hi}" opacity="0.55"/>
  <ellipse cx="40" cy="40" rx="6" ry="12" fill="${hi}" opacity="0.4"
           transform="rotate(12 40 40)"/>
  <path d="M36 30 C40 27 44 26 48 26 C52 26 56 27 60 30 L58 34 C54 31 51 30 48 30
           C45 30 42 31 38 34 Z"
        fill="${head}" stroke="${outline}" stroke-width="2.2" stroke-linejoin="round"/>
  <g fill="${eye}"><circle cx="35" cy="27" r="1.8"/><circle cx="61" cy="27" r="1.8"/></g>
  <ellipse cx="52" cy="46" rx="4" ry="10" fill="${legHi}" opacity="0.35"
           transform="rotate(-10 52 46)"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function lacewing(p = {}, name = "lacewing") {
  // Pale green body under broad rounded net-veined wings.
  const {
    b1 = "#a8c46e", b2 = "#6e8a3c", outline = "#42551c",
    wingFill = "#eef4e0", wingStroke = "#a8b888",
    vein = "#8aa066", eye = "#e8b84a", eyeStroke = "#8a651c",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <linearGradient id="lw" x1="0" y1="0" x2="0.3" y2="1">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </linearGradient>
  </defs>
  <ellipse cx="50" cy="80" rx="24" ry="4.2" fill="#000" opacity="0.11"/>
  <ellipse cx="34" cy="40" rx="22" ry="13" fill="${wingFill}" fill-opacity="0.92"
           stroke="${wingStroke}" stroke-width="2" transform="rotate(-34 34 40)"/>
  <ellipse cx="66" cy="40" rx="22" ry="13" fill="${wingFill}" fill-opacity="0.92"
           stroke="${wingStroke}" stroke-width="2" transform="rotate(34 66 40)"/>
  <g stroke="${vein}" stroke-width="1.1" opacity="0.8" fill="none">
    <path d="M22 52 L44 30 M28 56 L50 32 M36 58 L58 34"/>
    <path d="M78 52 L56 30 M72 56 L50 32 M64 58 L42 34"/>
  </g>
  <g stroke="${outline}" stroke-width="2" fill="none" stroke-linecap="round">
    <path d="M38 58 L28 68 M46 62 L40 72 M56 62 L62 72 M62 58 L72 66"/>
  </g>
  <ellipse cx="50" cy="52" rx="9" ry="17" fill="url(#lw)" stroke="${outline}" stroke-width="2.6"/>
  <circle cx="50" cy="32" r="8" fill="${b1}" stroke="${outline}" stroke-width="2.6"/>
  <circle cx="46.4" cy="30" r="3" fill="${eye}" stroke="${eyeStroke}" stroke-width="1.4"/>
  <circle cx="53.6" cy="30" r="3" fill="${eye}" stroke="${eyeStroke}" stroke-width="1.4"/>
  <path d="M46 25 C42 19 38 16 33 15 M54 25 C58 19 62 16 67 15"
        fill="none" stroke="${outline}" stroke-width="1.8" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function lanternfly(p = {}, name = "lanternfly") {
  // Moth-like hopper with a long up-curled snout and spotted wings.
  const {
    b1 = "#d9c9a0", b2 = "#a8905c", outline = "#5c4a24",
    wingSpot = "#8a6a34", snout = "#c9a86a", hi = "#ece0c0",
    eye = "#26201c", eyeHi = "#f5e8cd",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="lf" cx="0.4" cy="0.3" r="0.95">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </radialGradient>
  </defs>
  <ellipse cx="50" cy="76" rx="26" ry="4.4" fill="#000" opacity="0.12"/>
  <path d="M42 40 C30 34 18 36 12 46 C20 48 34 50 44 48 Z" fill="${b2}"
        stroke="${outline}" stroke-width="2.2" stroke-linejoin="round"/>
  <path d="M58 40 C70 34 82 36 88 46 C80 48 66 50 56 48 Z" fill="${b2}"
        stroke="${outline}" stroke-width="2.2" stroke-linejoin="round"/>
  <path d="M44 42 C48 34 52 34 56 42 L62 62 C58 68 42 68 38 62 Z"
        fill="url(#lf)" stroke="${outline}" stroke-width="2.6" stroke-linejoin="round"/>
  <g fill="${wingSpot}" opacity="0.85">
    <circle cx="46" cy="50" r="2.6"/><circle cx="55" cy="52" r="2.2"/>
    <circle cx="50" cy="59" r="2.2"/>
  </g>
  <ellipse cx="47" cy="44" rx="4" ry="3" fill="${hi}" opacity="0.8"/>
  <path d="M34 44 C26 42 18 38 14 30 C20 28 28 32 36 40 Z" fill="${snout}"
        stroke="${outline}" stroke-width="2.4" stroke-linejoin="round"/>
  <circle cx="15" cy="30" r="1.6" fill="${outline}"/>
  <g stroke="${outline}" stroke-width="2" fill="none" stroke-linecap="round">
    <path d="M40 66 L34 74 M48 68 L46 76 M56 66 L62 74"/>
  </g>
  <circle cx="40" cy="41" r="3" fill="${eye}" stroke="${outline}" stroke-width="1.2"/>
  <circle cx="60" cy="41" r="3" fill="${eye}" stroke="${outline}" stroke-width="1.2"/>
  <circle cx="39" cy="40" r="1" fill="${eyeHi}"/>
  <circle cx="59" cy="40" r="1" fill="${eyeHi}"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function leafhopper(p = {}, name = "leafhopper") {
  // Wedge-shaped hopper tilted forward on its hind legs.
  const {
    b1 = "#8ab86a", b2 = "#4e7a34", outline = "#2c4a18",
    hi = "#a8d088", wingMark = "#5e8a40", eye = "#26201c", eyeHi = "#f5e8cd",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <linearGradient id="lh" x1="0" y1="0" x2="0.3" y2="1">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </linearGradient>
  </defs>
  <ellipse cx="48" cy="74" rx="24" ry="4.2" fill="#000" opacity="0.12"/>
  <g stroke="${outline}" stroke-width="2.4" fill="none" stroke-linecap="round">
    <path d="M30 58 C24 62 22 68 23 72 M70 44 C76 44 80 48 80 52"/>
  </g>
  <path d="M14 46 C22 40 36 34 48 34 C62 34 76 40 84 50 C76 56 62 60 48 60
           C36 60 24 56 14 46 Z"
        fill="url(#lh)" stroke="${outline}" stroke-width="2.8" stroke-linejoin="round"/>
  <path d="M40 38 C50 42 62 44 74 46 M34 44 C46 48 58 50 70 52"
        stroke="${wingMark}" stroke-width="1.8" fill="none" opacity="0.75"/>
  <ellipse cx="30" cy="44" rx="8" ry="3.4" fill="${hi}" opacity="0.7"
           transform="rotate(-14 30 44)"/>
  <g stroke="${outline}" stroke-width="2" fill="none" stroke-linecap="round">
    <path d="M34 60 L28 70 M42 62 L40 72 M50 61 L54 72"/>
  </g>
  <circle cx="18" cy="46" r="4.4" fill="${eye}" stroke="${outline}" stroke-width="1.4"/>
  <circle cx="16.6" cy="44.6" r="1.4" fill="${eyeHi}"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function mayfly(p = {}, name = "mayfly") {
  // Dainty upwinger: tiny body, sail wings, two long thread tails.
  const {
    b1 = "#b8a888", b2 = "#7e6e50", outline = "#4a3e28",
    wingFill = "#f0ece0", wingStroke = "#b0a888",
    eye = "#3a3226", eyeHi = "#f5e8cd",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <linearGradient id="mf" x1="0" y1="0" x2="0.3" y2="1">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </linearGradient>
  </defs>
  <ellipse cx="46" cy="70" rx="18" ry="3.6" fill="#000" opacity="0.1"/>
  <path d="M42 40 C34 22 40 14 52 14 C64 14 68 24 60 42 C66 26 76 22 82 28
           C86 34 80 44 62 48 Z"
        fill="${wingFill}" fill-opacity="0.9" stroke="${wingStroke}" stroke-width="2"
        stroke-linejoin="round"/>
  <path d="M50 18 L48 40 M66 28 L58 44" stroke="${wingStroke}" stroke-width="1"
        opacity="0.7"/>
  <ellipse cx="46" cy="44" rx="10" ry="6.4" fill="url(#mf)" stroke="${outline}" stroke-width="2.4"/>
  <path d="M54 46 C64 50 72 58 78 70 M55 48 C62 56 66 64 68 74"
        fill="none" stroke="${outline}" stroke-width="1.4" stroke-linecap="round"/>
  <g stroke="${outline}" stroke-width="1.8" fill="none" stroke-linecap="round">
    <path d="M38 48 L30 56 M42 50 L36 58 M46 50 L44 58"/>
  </g>
  <circle cx="38" cy="42" r="5" fill="${b1}" stroke="${outline}" stroke-width="2.2"/>
  <g fill="${eye}"><circle cx="35.6" cy="41" r="1.7"/><circle cx="41.4" cy="41" r="1.7"/></g>
  <circle cx="35" cy="40.2" r="0.8" fill="${eyeHi}"/>
  <path d="M34 38 C30 34 26 32 22 32 M39 37 C38 33 38 29 40 25"
        fill="none" stroke="${outline}" stroke-width="1.4" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function rhinocerosBeetle(p = {}, name = "rhinoceros_beetle") {
  // Heavy rounded tank with a big forward horn and deep gloss.
  const {
    b1 = "#5c4630", b2 = "#2e2010", outline = "#1a1006",
    hi = "#8a6e4a", limb = "#241608", legHi = "#6a5236",
    eye = "#0e0a04", head = "#42321e",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="rb" cx="0.35" cy="0.25" r="1">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </radialGradient>
  </defs>
  <ellipse cx="52" cy="82" rx="30" ry="5" fill="#000" opacity="0.14"/>
  <g stroke="${limb}" stroke-width="3.4" fill="none" stroke-linecap="round">
    <path d="M32 58 L20 68 M36 64 L28 76 M60 66 L62 78 M70 60 L82 70"/>
  </g>
  <path d="M54 40 C76 40 84 54 82 66 C80 76 66 82 52 82 C38 82 24 76 22 66
           C20 54 32 40 54 40 Z"
        fill="url(#rb)" stroke="${outline}" stroke-width="3"/>
  <path d="M54 40 L54 82" stroke="${outline}" stroke-width="2"/>
  <ellipse cx="40" cy="52" rx="10" ry="4.6" fill="${hi}" opacity="0.45"
           transform="rotate(-18 40 52)"/>
  <path d="M30 44 C34 38 44 34 54 34 C64 34 74 38 78 44 C74 40 66 38 54 38
           C44 38 36 40 30 44 Z" fill="${head}" stroke="${outline}" stroke-width="2.4"/>
  <path d="M34 40 C28 32 24 22 26 12 C30 14 34 20 38 28 L42 38 Z"
        fill="${head}" stroke="${outline}" stroke-width="2.6" stroke-linejoin="round"/>
  <path d="M30 16 C33 17 36 20 37 24" fill="none" stroke="${hi}" stroke-width="1.6"
        opacity="0.6" stroke-linecap="round"/>
  <g fill="${eye}"><circle cx="40" cy="38" r="1.8"/><circle cx="66" cy="40" r="1.8"/></g>
  <ellipse cx="48" cy="56" rx="6" ry="12" fill="${legHi}" opacity="0.3"
           transform="rotate(8 48 56)"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function shieldBug(p = {}, name = "shield_bug") {
  // Classic pentagonal shield with a big central scutellum triangle.
  const {
    b1 = "#7aa84e", b2 = "#4a7a2c", outline = "#2c4a18",
    scutellum = "#5e8a38", hi = "#a8d078", band = "#3e6a24",
    eye = "#26201c", eyeHi = "#f5e8cd",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="sb" cx="0.4" cy="0.3" r="0.95">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </radialGradient>
  </defs>
  <ellipse cx="50" cy="78" rx="26" ry="4.4" fill="#000" opacity="0.12"/>
  <g stroke="${outline}" stroke-width="2.2" fill="none" stroke-linecap="round">
    <path d="M28 54 L18 64 M34 60 L28 72 M50 64 L50 76 M66 60 L72 72 M72 54 L82 64"/>
  </g>
  <path d="M50 22 L70 38 L66 66 C58 72 42 72 34 66 L30 38 Z"
        fill="url(#sb)" stroke="${outline}" stroke-width="2.8" stroke-linejoin="round"/>
  <path d="M50 34 L62 44 L58 62 C55 64 45 64 42 62 L38 44 Z"
        fill="${scutellum}" stroke="${outline}" stroke-width="2.2" stroke-linejoin="round"/>
  <path d="M50 22 L50 34" stroke="${outline}" stroke-width="2"/>
  <path d="M36 40 L64 40" stroke="${band}" stroke-width="2.2" opacity="0.7"/>
  <path d="M38 60 L62 60" stroke="${band}" stroke-width="2.2" opacity="0.7"/>
  <ellipse cx="42" cy="30" rx="7" ry="3.4" fill="${hi}" opacity="0.7"
           transform="rotate(-24 42 30)"/>
  <g fill="${eye}"><circle cx="40" cy="27" r="1.9"/><circle cx="60" cy="27" r="1.9"/></g>
  <circle cx="39.2" cy="26.2" r="0.8" fill="${eyeHi}"/>
  <circle cx="59.2" cy="26.2" r="0.8" fill="${eyeHi}"/>
  <path d="M42 22 C44 18 56 18 58 22" fill="none" stroke="${outline}" stroke-width="1.8"
        stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function silverfish(p = {}, name = "silverfish") {
  // Wingless tapering teardrop, long antennae, three bristle tails.
  const {
    b1 = "#c2c8cc", b2 = "#8a949c", outline = "#4e565e",
    hi = "#e4e9ec", bristle = "#6a747c", eye = "#26201c",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <linearGradient id="sf" x1="0" y1="0" x2="0.3" y2="1">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </linearGradient>
  </defs>
  <ellipse cx="48" cy="72" rx="24" ry="4" fill="#000" opacity="0.1"/>
  <g stroke="${outline}" stroke-width="1.8" fill="none" stroke-linecap="round">
    <path d="M36 62 L26 68 M44 64 L40 72 M54 63 L58 71"/>
  </g>
  <path d="M28 40 C36 32 56 32 62 40 C70 50 74 58 76 66 C68 66 60 62 54 62
           C46 62 36 60 30 54 C26 50 25 44 28 40 Z"
        fill="url(#sf)" stroke="${outline}" stroke-width="2.6" stroke-linejoin="round"/>
  <g stroke="${outline}" stroke-width="1.2" opacity="0.5" fill="none">
    <path d="M38 38 L40 56 M46 34 L48 60 M54 36 L56 62"/>
  </g>
  <path d="M76 66 C84 64 90 68 92 74 M76 66 C82 70 84 76 83 82 M76 66 C74 74 76 80 80 84"
        fill="none" stroke="${bristle}" stroke-width="1.8" stroke-linecap="round"/>
  <ellipse cx="36" cy="44" rx="7" ry="3" fill="${hi}" opacity="0.75"
           transform="rotate(-18 36 44)"/>
  <circle cx="30" cy="42" r="2" fill="${eye}"/>
  <path d="M24 38 C16 32 10 28 4 27 M26 36 C22 29 20 24 20 18"
        fill="none" stroke="${outline}" stroke-width="1.6" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function slug(p = {}, name = "slug") {
  // Soft blob: mantle saddle over a tail, two eyestalks, feeler low front.
  const {
    b1 = "#c9a86e", b2 = "#967444", outline = "#5e4626",
    mantle = "#b0945c", hi = "#e0c898", eye = "#26201c",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <linearGradient id="sl" x1="0" y1="0" x2="0.25" y2="1">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </linearGradient>
  </defs>
  <ellipse cx="52" cy="80" rx="32" ry="5" fill="#000" opacity="0.12"/>
  <path d="M18 76 C16 66 26 58 40 58 L64 58 C76 58 84 66 84 74 C84 78 80 80 74 80
           L24 80 C20 80 18 78 18 76 Z"
        fill="url(#sl)" stroke="${outline}" stroke-width="2.8" stroke-linejoin="round"/>
  <path d="M34 58 C32 48 40 40 50 40 C60 40 66 48 64 58 Z"
        fill="${mantle}" stroke="${outline}" stroke-width="2.6" stroke-linejoin="round"/>
  <ellipse cx="46" cy="48" rx="6" ry="3.4" fill="${hi}" opacity="0.65"
           transform="rotate(-18 46 48)"/>
  <g stroke="${outline}" stroke-width="2.4" fill="none" stroke-linecap="round">
    <path d="M28 56 C26 48 27 42 30 38"/>
    <path d="M36 55 C35 49 36 45 38 42"/>
  </g>
  <circle cx="30.6" cy="36.6" r="3" fill="${eye}" stroke="${outline}" stroke-width="1.2"/>
  <circle cx="38.6" cy="40.6" r="2.4" fill="${eye}" stroke="${outline}" stroke-width="1.2"/>
  <circle cx="29.6" cy="35.6" r="1" fill="#f5f0e6"/>
  <circle cx="37.8" cy="39.8" r="0.9" fill="#f5f0e6"/>
  <path d="M22 62 C20 60 18 59 16 59" fill="none" stroke="${outline}" stroke-width="1.8"
        stroke-linecap="round"/>
  <path d="M20 74 C34 77 62 77 78 74" stroke="${hi}" stroke-width="1.6"
        fill="none" opacity="0.5" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function stagBeetle(p = {}, name = "stag_beetle") {
  // Broad dark beetle whose mandibles fork like antlers.
  const {
    b1 = "#4e3a26", b2 = "#2a1c0e", outline = "#150d04",
    hi = "#7a5e3e", mandible = "#3a2a16", limb = "#201406",
    eye = "#0e0a04", eyeHi = "#c9b088",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="sg" cx="0.35" cy="0.28" r="1">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </radialGradient>
  </defs>
  <ellipse cx="54" cy="82" rx="28" ry="5" fill="#000" opacity="0.14"/>
  <g stroke="${limb}" stroke-width="3" fill="none" stroke-linecap="round">
    <path d="M34 56 L22 64 M38 62 L30 74 M60 62 L66 74 M66 56 L78 64"/>
  </g>
  <path d="M56 34 C74 34 82 48 80 62 C78 74 68 80 54 80 C40 80 30 74 28 62
           C26 48 38 34 56 34 Z"
        fill="url(#sg)" stroke="${outline}" stroke-width="3"/>
  <path d="M56 36 L56 80" stroke="${outline}" stroke-width="2"/>
  <ellipse cx="44" cy="48" rx="9" ry="4.4" fill="${hi}" opacity="0.5"
           transform="rotate(-16 44 48)"/>
  <path d="M34 40 C36 34 46 30 56 30 C66 30 76 34 78 40 C72 36 64 34 56 34
           C48 34 40 36 34 40 Z" fill="${b1}" stroke="${outline}" stroke-width="2.4"/>
  <path d="M36 38 C28 30 22 20 22 10 C28 12 34 20 38 30 Z"
        fill="${mandible}" stroke="${outline}" stroke-width="2.4" stroke-linejoin="round"/>
  <path d="M42 36 C40 26 42 16 48 8 C52 12 52 22 50 34 Z"
        fill="${mandible}" stroke="${outline}" stroke-width="2.4" stroke-linejoin="round"/>
  <path d="M28 18 C31 18 34 21 35 24 M46 14 C48 16 49 19 49 23"
        fill="none" stroke="${hi}" stroke-width="1.4" opacity="0.6" stroke-linecap="round"/>
  <g fill="${eye}"><circle cx="46" cy="35" r="1.8"/><circle cx="66" cy="36" r="1.8"/></g>
  <circle cx="45.4" cy="34.4" r="0.7" fill="${eyeHi}"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function tigerBeetle(p = {}, name = "tiger_beetle") {
  // Long-legged runner: slim body, white spotting, prominent eyes.
  const {
    b1 = "#8a9450", b2 = "#545e26", outline = "#2c3410",
    spot = "#efe8d0", hi = "#a8b06e", limb = "#3a4214", legHi = "#6e7a3c",
    eye = "#26201c", eyeHi = "#f5e8cd",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <linearGradient id="tb" x1="0" y1="0" x2="0.3" y2="1">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </linearGradient>
  </defs>
  <ellipse cx="50" cy="80" rx="27" ry="4.4" fill="#000" opacity="0.12"/>
  <g stroke="${limb}" stroke-width="2.6" fill="none" stroke-linecap="round">
    <path d="M34 54 L18 60 M36 60 L20 70 M50 62 L50 76 M64 60 L72 72 M68 52 L84 58"/>
  </g>
  <path d="M30 42 C30 34 68 32 72 42 C76 52 72 62 62 66 C50 70 36 66 32 58
           C29 52 29 47 30 42 Z"
        fill="url(#tb)" stroke="${outline}" stroke-width="2.8"/>
  <path d="M52 34 L52 68" stroke="${outline}" stroke-width="1.6"/>
  <g fill="${spot}" opacity="0.9">
    <ellipse cx="40" cy="46" rx="3.4" ry="2.2" transform="rotate(-20 40 46)"/>
    <ellipse cx="62" cy="48" rx="3" ry="2" transform="rotate(14 62 48)"/>
    <ellipse cx="46" cy="58" rx="2.8" ry="1.9"/>
    <ellipse cx="60" cy="60" rx="2.4" ry="1.7"/>
  </g>
  <ellipse cx="38" cy="40" rx="6" ry="3" fill="${hi}" opacity="0.6"
           transform="rotate(-10 38 40)"/>
  <circle cx="27" cy="42" r="7.4" fill="${b1}" stroke="${outline}" stroke-width="2.4"/>
  <circle cx="24" cy="40" r="3.4" fill="${eye}" stroke="${outline}" stroke-width="1.2"/>
  <circle cx="23" cy="38.8" r="1.1" fill="${eyeHi}"/>
  <path d="M22 36 C18 30 14 27 10 26 M28 35 C28 29 30 23 34 19"
        fill="none" stroke="${outline}" stroke-width="1.8" stroke-linecap="round"/>
  <ellipse cx="52" cy="50" rx="5" ry="9" fill="${legHi}" opacity="0.25"/>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function tortoiseBeetle(p = {}, name = "tortoise_beetle") {
  // Round flattened body with a flared transparent skirt, like a tiny turtle.
  const {
    b1 = "#e0b04c", b2 = "#b07e1e", outline = "#6e4c10",
    skirt = "#f2d88a", skirtStroke = "#c9a050", hi = "#f4dc98",
    spot = "#8a5e14", eye = "#26201c", head = "#a8781e",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="tb2" cx="0.4" cy="0.3" r="0.95">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </radialGradient>
  </defs>
  <ellipse cx="50" cy="74" rx="27" ry="4.4" fill="#000" opacity="0.12"/>
  <g stroke="${outline}" stroke-width="2" fill="none" stroke-linecap="round">
    <path d="M30 60 L22 66 M38 66 L34 72 M62 66 L66 72 M70 60 L78 66"/>
  </g>
  <path d="M50 24 C68 24 80 36 80 52 C80 62 72 68 50 68 C28 68 20 62 20 52
           C20 36 32 24 50 24 Z"
        fill="${skirt}" fill-opacity="0.85" stroke="${skirtStroke}" stroke-width="2.4"/>
  <path d="M50 28 C64 28 74 38 74 50 C74 58 66 64 50 64 C34 64 26 58 26 50
           C26 38 36 28 50 28 Z"
        fill="url(#tb2)" stroke="${outline}" stroke-width="2.8"/>
  <path d="M50 30 L50 62" stroke="${outline}" stroke-width="1.5" opacity="0.6"/>
  <g fill="${spot}" opacity="0.75">
    <circle cx="38" cy="42" r="2.4"/><circle cx="62" cy="42" r="2.4"/>
    <circle cx="44" cy="54" r="2"/><circle cx="56" cy="54" r="2"/>
  </g>
  <ellipse cx="40" cy="36" rx="7" ry="3.4" fill="${hi}" opacity="0.8"
           transform="rotate(-16 40 36)"/>
  <circle cx="50" cy="24" r="5.6" fill="${head}" stroke="${outline}" stroke-width="2"/>
  <g fill="${eye}"><circle cx="47.6" cy="23" r="1.3"/><circle cx="52.4" cy="23" r="1.3"/></g>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function waterStrider(p = {}, name = "water_strider") {
  // Perched on its ripple: slim body, splayed long legs, dimple shadow.
  const {
    b1 = "#6e6258", b2 = "#42382e", outline = "#26201a",
    hi = "#8a7e70", limb = "#332c24", ripple = "#e8e2d2",
    eye = "#1a1612", eyeHi = "#f5e8cd",
  } = p;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <ellipse cx="50" cy="78" rx="26" ry="6" fill="${ripple}" opacity="0.55"/>
  <ellipse cx="50" cy="78" rx="40" ry="8" fill="none" stroke="${ripple}"
           stroke-width="1.6" opacity="0.5"/>
  <g stroke="${limb}" stroke-width="2.2" fill="none" stroke-linecap="round">
    <path d="M40 52 L14 42 M42 54 L12 58"/>
    <path d="M52 52 C66 46 78 40 90 42 M54 54 C68 54 82 58 92 64"/>
    <path d="M48 54 C46 66 42 74 36 80 M50 54 C52 66 56 74 62 80"/>
  </g>
  <ellipse cx="52" cy="50" rx="16" ry="5.4" fill="url(#ws)" stroke="${outline}" stroke-width="2.4"
           transform="rotate(-8 52 50)"/>
  <ellipse cx="40" cy="48" rx="6.4" ry="4.6" fill="${b1}" stroke="${outline}" stroke-width="2.2"/>
  <ellipse cx="36" cy="46" rx="3" ry="1.8" fill="${hi}" opacity="0.7"
           transform="rotate(-16 36 46)"/>
  <circle cx="33" cy="46" r="2" fill="${eye}"/>
  <circle cx="32.4" cy="45.4" r="0.7" fill="${eyeHi}"/>
  <path d="M30 43 C26 39 22 37 18 36" fill="none" stroke="${outline}" stroke-width="1.4"
        stroke-linecap="round"/>
  <defs>
    <linearGradient id="ws" x1="0" y1="0" x2="0.3" y2="1">
      <stop offset="0" stop-color="${b1}"/>
      <stop offset="1" stop-color="${b2}"/>
    </linearGradient>
  </defs>
</svg>`;
  write(join(BUGS, name + ".svg"), svg);
}

function ground() {
  // Deterministic PRNG (mulberry32) so output is stable across runs.
  let seed = 7;
  const rand = () => {
    seed |= 0; seed = (seed + 0x6d2b79f5) | 0;
    let t = Math.imul(seed ^ (seed >>> 15), 1 | seed);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
  const uni = (a, b) => a + rand() * (b - a);
  const pick = (arr) => arr[Math.floor(rand() * arr.length)];

  const w = 1080, h = 2340;
  const bits = [];
  for (let i = 0; i < 220; i++) {
    const x = uni(0, w), y = uni(0, h), r = uni(2.5, 12);
    const c = pick(["#5f5340", "#8d7f63", "#94866a", "#6b5e45", "#7a6c50"]);
    bits.push(`<circle cx="${x.toFixed(0)}" cy="${y.toFixed(0)}" r="${r.toFixed(1)}" fill="${c}" opacity="${uni(0.18, 0.45).toFixed(2)}"/>`);
  }
  for (let i = 0; i < 26; i++) {
    const x = uni(0, w), y = uni(0, h), rx = uni(10, 26), ry = uni(7, 16);
    bits.push(
      `<ellipse cx="${x.toFixed(0)}" cy="${y.toFixed(0)}" rx="${rx.toFixed(0)}" ry="${ry.toFixed(0)}" fill="#8a8272" opacity="0.35"/>` +
      `<ellipse cx="${(x - rx * 0.2).toFixed(0)}" cy="${(y - ry * 0.25).toFixed(0)}" rx="${(rx * 0.5).toFixed(0)}" ry="${(ry * 0.45).toFixed(0)}" fill="#a29a89" opacity="0.4"/>`);
  }
  for (let i = 0; i < 40; i++) {
    const x = uni(0, w), y = uni(0, h), n = 2 + Math.floor(rand() * 2);
    const blades = Array.from({ length: n }, (_, k) => {
      const dx = k;
      return `M${(x + dx).toFixed(0)} ${y.toFixed(0)} C${(x + dx + uni(-6, 6)).toFixed(0)} ${(y - uni(8, 16)).toFixed(0)} ${(x + dx + uni(-4, 4)).toFixed(0)} ${(y - uni(14, 26)).toFixed(0)} ${(x + dx + uni(-10, 10)).toFixed(0)} ${(y - uni(20, 34)).toFixed(0)}`;
    }).join(" ");
    bits.push(`<g stroke="#6d7a44" stroke-width="2.6" fill="none" opacity="0.4" stroke-linecap="round">${blades}</g>`);
  }
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${w} ${h}">
  <defs>
    <radialGradient id="gg" cx="0.5" cy="0.42" r="0.85">
      <stop offset="0" stop-color="#857659"/>
      <stop offset="1" stop-color="#6a5c43"/>
    </radialGradient>
  </defs>
  <rect width="${w}" height="${h}" fill="url(#gg)"/>
  ${bits.join("\n  ")}
</svg>`;
  write(join(TEX, "ground.svg"), svg);
}

// ------------------------------------------------------------------ main ---

leafMaple("leaf_red", "#d9583b", "#a93a26", "#7c2818");
leafOak("leaf_yellow", "#e8b64c", "#c78f2e", "#96691e");
leafSimple("leaf_green", "#7fae4e", "#557f31", "#3e6323");
leafSimple("leaf_red2", "#c9503a", "#96321f", "#701f10");
mossCluster("moss", "#6f9a44", "#48682a", "#93bd63");
stick();
rock("rock", "#9a948a", "#6e675c", "#4f483e", "#b5afa4");
rock("rock2", "#a3896d", "#79604a", "#57432f", "#bfa488");
petal("petal_pink", "#f2b7c6", "#d97e97", "#b05a74");
petal("petal_white", "#f7f2e8", "#e4d9c6", "#c2b49c");
petal("petal_purple", "#b39bd6", "#8a6cb3", "#67498c");
ground();
// ---- Existing species (base "classic" variant keeps the legacy id) ----
ladybug();
ladybug({ b1: "#f2c53d", b2: "#d19a1f", outline: "#7a5a10" }, "ladybug_yellow");
ladybug({ b1: "#f08a3c", b2: "#cf6620", outline: "#7a3a12" }, "ladybug_orange");
ladybug({ b1: "#e884a8", b2: "#c25579", outline: "#7a2c48" }, "ladybug_pink");

butterfly();
butterfly({ w1: "#7fa8e0", w2: "#3f68a0", limb: "#2e3e58", limbDark: "#1d2942" },
  "butterfly_blue");
butterfly({ w1: "#f6d75c", w2: "#d9a82e", limb: "#6a5220", limbDark: "#4a3a12",
  wingSpotsFill: "#e0743e" }, "butterfly_yellow");
butterfly({ w1: "#f5f0e6", w2: "#cfc4ae", limb: "#7a7466", limbDark: "#565248",
  wingSpotsFill: "#d9803e", wingDotsFill: "#565048" }, "butterfly_white");

centipede();
centipede({ body: "#e0803c", outline: "#96501a", spot: "#f2b070", legs: "#7a3e12" },
  "centipede_orange");
centipede({ body: "#6a5a4c", outline: "#3e342c", spot: "#8a7666", legs: "#332c24" },
  "centipede_dark");
centipede({ body: "#e8c04c", outline: "#9a7418", spot: "#f4d878", legs: "#7a5a10" },
  "centipede_yellow");

moth();
moth({ w1: "#8fbf6a", w2: "#558a3c", outline: "#35602a", eyespotsFill: "#24401e",
  body: "#5c8a44", bands: "#7aa65a" }, "moth_green");
moth({ w1: "#e8a8c8", w2: "#b26a90", outline: "#6e3a56", eyespotsFill: "#4e2440",
  body: "#9a5a78", bands: "#b47a96" }, "moth_pink");
moth({ w1: "#f2ede0", w2: "#c6bda6", outline: "#6e6650", eyespotsFill: "#3e3a2c",
  body: "#a89c80", bands: "#c2b698" }, "moth_white");

grasshopper();
grasshopper({ b1: "#b08a5a", b2: "#7a5c34", outline: "#4e3a1c", hi: "#c9a678" },
  "grasshopper_brown");
grasshopper({ b1: "#e0a0b8", b2: "#b06a86", outline: "#6e3a4e", hi: "#f0bcd0" },
  "grasshopper_pink");
grasshopper({ b1: "#e0c050", b2: "#a8882a", outline: "#6a5414", hi: "#f0d870" },
  "grasshopper_yellow");

dragonfly();
dragonfly({ wingFill: "#c8e0f2", wingStroke: "#6a9ec4", body: "#4a78b0",
  bodyHi: "#7aa8d4", bands: "#35608e", headStroke: "#2c4a70" }, "dragonfly_blue");
dragonfly({ wingFill: "#f2d8c8", wingStroke: "#c48a6a", body: "#b0503a",
  bodyHi: "#d47a5e", bands: "#8e3a28", headStroke: "#702c1c" }, "dragonfly_red");
dragonfly({ wingFill: "#d0ecd8", wingStroke: "#6aa882", body: "#3c8a52",
  bodyHi: "#6ab07e", bands: "#2a6a3c", headStroke: "#1f5230" }, "dragonfly_green");

beetle();
beetle({ b1: "#5a8c4a", b2: "#33602c", elytraOutline: "#1c3a18", limb: "#1a2e18",
  legHi: "#8eb87a", headStroke: "#101f0e",
  dots: [[42, 58, 3.2, "#a8d88a"], [50, 64, 2.8, "#a8d88a"]] }, "beetle_green");
beetle({ b1: "#4a6aa0", b2: "#2a4068", elytraOutline: "#182842", limb: "#16223a",
  legHi: "#7a9ac8", headStroke: "#0e1626" }, "beetle_blue");
beetle({ b1: "#c07a3a", b2: "#8a4e1e", elytraOutline: "#5c2e0e", limb: "#4a2408",
  legHi: "#e8a860", headStroke: "#3a1c06",
  dots: [[44, 56, 3, "#f2c078"], [54, 66, 2.6, "#f2c078"]] }, "beetle_copper");

snail();
snail({ s1: "#e0b050", s2: "#b08020", shellStroke: "#7a5a10",
  bodyFill: "#d9b078", bodyStroke: "#9a7040", bodyHi: "#e8c890" }, "snail_golden");
snail({ s1: "#8aa860", s2: "#5a7a3a", shellStroke: "#3c5224",
  bodyFill: "#b0c48a", bodyStroke: "#7a8a54", bodyHi: "#c4d49e" }, "snail_green");
snail({ s1: "#d9a878", s2: "#8a5a34", shellStroke: "#5a3a20",
  bodyFill: "#c9a06a", bodyStroke: "#8a6538", bodyHi: "#d9a86a",
  extraBand: `\n  <path d="M27 36 C31 42 31 50 27 55" fill="none" stroke="#5a3a20" stroke-width="4" stroke-linecap="round" opacity="0.75"/>
  <path d="M60 32 C64 40 64 50 60 57" fill="none" stroke="#5a3a20" stroke-width="4" stroke-linecap="round" opacity="0.75"/>` },
  "snail_banded");

firefly();
firefly({ glow1: "#ffe0b0", glow2: "#f7b860", lamp1: "#ffdda0", lamp2: "#f2952e",
  lampStroke: "#a3621c" }, "firefly_orange");
firefly({ glow1: "#c8e8ff", glow2: "#7ab8e8", lamp1: "#c8ecff", lamp2: "#5aa0d8",
  lampStroke: "#2c6a94", wingFill: "#cfe3ef", wingStroke: "#6a94ad" }, "firefly_blue");
firefly({ glow1: "#e0ffc0", glow2: "#a8e07a", lamp1: "#d8ffae", lamp2: "#a0d85a",
  lampStroke: "#6a8a34" }, "firefly_green");

bumblebee();
bumblebee({ b1: "#4a4240", b2: "#2a2624", outline: "#14100e", band: "#f5f0e6",
  dark: "#14100e", headStroke: "#0a0908" }, "bumblebee_black");
bumblebee({ b1: "#f0923c", b2: "#c8661e", outline: "#8a4410", band: "#3a2408" },
  "bumblebee_orange");
bumblebee({ b1: "#b8b4ac", b2: "#848078", outline: "#4e4a42", band: "#3a3630",
  dark: "#3a3630", headStroke: "#201c18" }, "bumblebee_grey");

caterpillar();
caterpillar({ body: "#e0884c", outline: "#96501a", spot: "#f2b878", head: "#f09a5a" },
  "caterpillar_orange");
caterpillar({ body: "#e8c850", outline: "#9a7a14", spot: "#f4e080", head: "#f0d060" },
  "caterpillar_yellow");
caterpillar({ body: "#4a4440", outline: "#26211e", spot: "#6a605a", head: "#6a5f58",
  dark: "#1a1614" }, "caterpillar_black");

mantis();
mantis({ m1: "#b08a5a", m2: "#7a5c34", outline: "#4e3a1c", hi: "#c9a678",
  head: "#9a7448" }, "mantis_brown");
mantis({ m1: "#e0a8b8", m2: "#b06a80", outline: "#6e3a4c", hi: "#f0c4d0",
  head: "#cf8a9c" }, "mantis_pink");
mantis({ m1: "#e8cc5c", m2: "#b89a28", outline: "#6e5a10", hi: "#f4e07c",
  head: "#d4b83e" }, "mantis_yellow");

stickInsect();
stickInsect({ dark: "#3e5a2a", body: "#6a9448" }, "stick_insect_green");
stickInsect({ dark: "#4e4e4a", body: "#8a8a84" }, "stick_insect_grey");
stickInsect({ dark: "#3a2a1a", body: "#6a4c30" }, "stick_insect_darkbrown");

weevil();
weevil({ w1: "#7aa050", w2: "#4e7030", outline: "#2e4a1a", spot: "#a4c478",
  head: "#5e823c" }, "weevil_green");
weevil({ w1: "#b04a38", w2: "#7a2c1e", outline: "#4a160e", spot: "#d47a5e",
  head: "#8e3826" }, "weevil_red");
weevil({ w1: "#9a9a94", w2: "#6a6a64", outline: "#3e3e3a", spot: "#c2c2ba",
  head: "#7a7a74" }, "weevil_grey");

pillbug();
pillbug({ p1: "#6a8aa8", p2: "#3c5470", outline: "#263448", hi: "#8aa8c2",
  head: "#46607a" }, "pillbug_blue");
pillbug({ p1: "#5a5650", p2: "#34302c", outline: "#201c1a", hi: "#6e6a62",
  head: "#443e3a" }, "pillbug_dark");
pillbug({ p1: "#d9904c", p2: "#a05e24", outline: "#6a3a10", hi: "#e8ac70",
  head: "#b4702e" }, "pillbug_orange");

ant();
ant({ body: "#b04a30", outline: "#6e2814", legDark: "#4a1a0e" }, "ant_red");
ant({ body: "#c9a040", outline: "#7a5e14", legDark: "#5a4610" }, "ant_gold");
ant({ body: "#6e5238", outline: "#3e2c1a", legDark: "#2a1e10" }, "ant_brown");

fly();
fly({ f1: "#8aa4c2", f2: "#587090", outline: "#2c3a4a", thorax: "#6a839e",
  eye1: "#3a6ea0", eyeStroke: "#24486a", wingFill: "#d8e6f0", wingStroke: "#8fb0c8" },
  "fly_blue");
fly({ f1: "#5a564e", f2: "#343028", outline: "#1e1c18", thorax: "#443e38",
  eye1: "#8a2a1e", eyeStroke: "#5a180e", wingFill: "#d8d4cc", wingStroke: "#a09a90" },
  "fly_black");
fly({ f1: "#d9a05c", f2: "#a86e30", outline: "#6a4414", thorax: "#b4814a",
  eye1: "#a8442e", eyeStroke: "#6e2417", wingFill: "#f2e2cc", wingStroke: "#c4a680" },
  "fly_orange");

// ---- New species (base + 3 natural variants each) ----
aphid();
aphid({ b1: "#e0a8b8", b2: "#b06a80", outline: "#6e3a4c", hi: "#f0c4d0",
  cornicle: "#5a2c3c" }, "aphid_pink");
aphid({ b1: "#b09468", b2: "#7a6238", outline: "#4a3a1c", hi: "#c9b088",
  cornicle: "#3e3218" }, "aphid_brown");
aphid({ b1: "#e0cc6a", b2: "#a89434", outline: "#6a5a14", hi: "#f0e090",
  cornicle: "#4e4210" }, "aphid_yellow");

barklice();
barklice({ b1: "#b8b4ac", b2: "#84807a", outline: "#4a4640", wingMark: "#6a665e",
  hi: "#d4d0c8" }, "barklice_grey");
barklice({ b1: "#6e6455", b2: "#46402f", outline: "#2a2418", wingMark: "#585040",
  hi: "#8a8070" }, "barklice_dark");
barklice({ b1: "#c08a5a", b2: "#8a5a30", outline: "#54331a", wingMark: "#6e4423",
  hi: "#dca878" }, "barklice_rust");

cicada();
cicada({ b1: "#9aa858", b2: "#5e6e30", outline: "#38421a", wingFill: "#e8ecd8",
  wingStroke: "#a0ac84", hi: "#b8c478" }, "cicada_green");
cicada({ b1: "#d9944c", b2: "#96601e", outline: "#5c3a10", hi: "#f0b878" },
  "cicada_orange");
cicada({ b1: "#6a5a44", b2: "#3a3022", outline: "#241c12", hi: "#8a7a60" },
  "cicada_dark");

clickBeetle();
clickBeetle({ b1: "#55483c", b2: "#2e2620", outline: "#1a1410", hi: "#6e5e4e",
  feathery: "#1a1410" }, "click_beetle_dark");
clickBeetle({ b1: "#b06038", b2: "#6e3418", outline: "#421c0a", hi: "#c9825a",
  feathery: "#421c0a" }, "click_beetle_red");
clickBeetle({ b1: "#9a958c", b2: "#615c54", outline: "#3a362f", hi: "#b8b3a8",
  feathery: "#3a362f" }, "click_beetle_grey");

damselfly();
damselfly({ b1: "#d05a48", b2: "#94281c", outline: "#5c140c", wingFill: "#f4ded8",
  wingStroke: "#c89890", hi: "#e88878", eye: "#40100a" }, "damselfly_red");
damselfly({ b1: "#7ab86a", b2: "#3e7a34", outline: "#234a1a", hi: "#9ad088",
  eye: "#1a3414" }, "damselfly_green");
damselfly({ b1: "#a88ad0", b2: "#6a4e96", outline: "#3c2a58", hi: "#c8b0e4",
  eye: "#2c1e42" }, "damselfly_purple");

earwig();
earwig({ b1: "#55483c", b2: "#2e2620", outline: "#1a1410", hi: "#6e5e4e",
  pincer: "#241c14" }, "earwig_dark");
earwig({ b1: "#a85a3c", b2: "#6e3018", outline: "#421a0a", hi: "#c47e5e",
  pincer: "#4a2010" }, "earwig_red");
earwig({ b1: "#c9ab88", b2: "#967450", outline: "#54381e", hi: "#e0c8a8",
  pincer: "#5c3e22" }, "earwig_pale");

earthworm();
earthworm({ b1: "#e8c8bc", b2: "#c49a8c", outline: "#8a6258", hi: "#f4e0d8",
  saddle: "#d9ac9e", seg: "#a8786c" }, "earthworm_pale");
earthworm({ b1: "#c9705e", b2: "#964234", outline: "#64241a", hi: "#e09484",
  saddle: "#b25a4a", seg: "#7a3226" }, "earthworm_red");
earthworm({ b1: "#a8b878", b2: "#6e8248", outline: "#44521e", hi: "#c4d494",
  saddle: "#94a462", seg: "#566430" }, "earthworm_green");

froghopper();
froghopper({ b1: "#a88a5c", b2: "#6e562e", outline: "#423014", hi: "#c4a878" },
  "froghopper_brown");
froghopper({ b1: "#a8a49c", b2: "#6a665e", outline: "#3e3a32", hi: "#c4c0b6" },
  "froghopper_grey");
froghopper({ b1: "#e0cc60", b2: "#a8942c", outline: "#665610", hi: "#f0e088" },
  "froghopper_yellow");

glowworm();
glowworm({ glow1: "#ffe0b0", glow2: "#f7a848" }, "glowworm_orange");
glowworm({ glow1: "#c8e8ff", glow2: "#58a8e8" }, "glowworm_blue");
glowworm({ glow1: "#ffd8ec", glow2: "#e87ab8" }, "glowworm_pink");

jewelBeetle();
jewelBeetle({ b1: "#4a7ac8", b2: "#1e4488", outline: "#10285a", hi: "#88b0e8",
  limb: "#122a56", legHi: "#6e98d0", head: "#1c3a72" }, "jewel_beetle_blue");
jewelBeetle({ b1: "#d08040", b2: "#8a4416", outline: "#54240a", hi: "#f0ac68",
  limb: "#5e2c0e", legHi: "#d09050", head: "#6e3412" }, "jewel_beetle_copper");
jewelBeetle({ b1: "#9a5ac0", b2: "#5a2a80", outline: "#341450", hi: "#c898e4",
  limb: "#3c1858", legHi: "#9668bc", head: "#4a2068" }, "jewel_beetle_purple");

lacewing();
lacewing({ b1: "#b09468", b2: "#7a5e38", outline: "#4a381c", wingFill: "#f0e8d8",
  wingStroke: "#bca884", vein: "#9a8458" }, "lacewing_brown");
lacewing({ b1: "#b0aca4", b2: "#767268", outline: "#444036", wingFill: "#f0efe8",
  wingStroke: "#b0aca0", vein: "#928e80" }, "lacewing_grey");
lacewing({ b1: "#d9b860", b2: "#a88830", outline: "#665012", wingFill: "#f6efdc",
  wingStroke: "#ccb478", vein: "#ab8f4e" }, "lacewing_gold");

lanternfly();
lanternfly({ b1: "#b8b2a8", b2: "#847e72", outline: "#4a4438", wingSpot: "#6a6458",
  snout: "#a49e90", hi: "#d4cec2" }, "lanternfly_grey");
lanternfly({ b1: "#d98868", b2: "#a44c2e", outline: "#5e2410", wingSpot: "#7e3418",
  snout: "#c47858", hi: "#eeb098" }, "lanternfly_red");
lanternfly({ b1: "#7a6e5c", b2: "#463c2c", outline: "#2a2418", wingSpot: "#5c5240",
  snout: "#6a5e4a", hi: "#948872" }, "lanternfly_dark");

leafhopper();
leafhopper({ b1: "#d07050", b2: "#963e24", outline: "#5a200e", hi: "#e89878",
  wingMark: "#b05032" }, "leafhopper_red");
leafhopper({ b1: "#7a9ec8", b2: "#46648e", outline: "#24385a", hi: "#9cbcd8",
  wingMark: "#567aa4" }, "leafhopper_blue");
leafhopper({ b1: "#e0c858", b2: "#a8902a", outline: "#665410", hi: "#f0dc80",
  wingMark: "#c0a83c" }, "leafhopper_yellow");

mayfly();
mayfly({ b1: "#b0aaa0", b2: "#767066", outline: "#46403a", wingFill: "#f0ece4",
  wingStroke: "#b0a89c" }, "mayfly_grey");
mayfly({ b1: "#a8a468", b2: "#6e6a38", outline: "#403e1c", wingFill: "#ececdc",
  wingStroke: "#aaa678" }, "mayfly_olive");
mayfly({ b1: "#e8dcc0", b2: "#b8a880", outline: "#6e5e40", wingFill: "#f6f2e6",
  wingStroke: "#ccbca0" }, "mayfly_cream");

rhinocerosBeetle();
rhinocerosBeetle({ b1: "#3a3630", b2: "#1c1a16", outline: "#0e0c0a", hi: "#565248",
  limb: "#12100e", legHi: "#443e36", head: "#2a2723" }, "rhinoceros_beetle_black");
rhinocerosBeetle({ b1: "#a85038", b2: "#64241a", outline: "#38120a", hi: "#c8785e",
  limb: "#4a180e", legHi: "#8a4430", head: "#8a3826" }, "rhinoceros_beetle_red");
rhinocerosBeetle({ b1: "#5e7a44", b2: "#324a20", outline: "#1a2a0e", hi: "#82a062",
  limb: "#223a12", legHi: "#5a7a3c", head: "#48642e" }, "rhinoceros_beetle_green");

shieldBug();
shieldBug({ b1: "#a8894e", b2: "#6e5626", outline: "#423012", scutellum: "#8a6e38",
  hi: "#c4a870", band: "#584520" }, "shield_bug_brown");
shieldBug({ b1: "#c86848", b2: "#8e3c24", outline: "#56200f", scutellum: "#aa5438",
  hi: "#e28c70", band: "#722e18" }, "shield_bug_red");
shieldBug({ b1: "#6a94b8", b2: "#3a6084", outline: "#1e3a52", scutellum: "#50789c",
  hi: "#8ab4d0", band: "#2c4a66" }, "shield_bug_blue");

silverfish();
silverfish({ b1: "#c9a878", b2: "#8a6c46", outline: "#4e3c24", hi: "#e0c8a0",
  bristle: "#6e5636" }, "silverfish_bronze");
silverfish({ b1: "#8a929c", b2: "#565e68", outline: "#2c343c", hi: "#aab2bc",
  bristle: "#3e4650" }, "silverfish_slate");
silverfish({ b1: "#d9c060", b2: "#a08834", outline: "#64520e", hi: "#f0dc8c",
  bristle: "#7a6420" }, "silverfish_gold");

slug();
slug({ b1: "#e0c868", b2: "#b09a3c", outline: "#6a5a14", mantle: "#d4bc58",
  hi: "#f0e090" }, "slug_yellow");
slug({ b1: "#b0aa9e", b2: "#7a7468", outline: "#46413a", mantle: "#a09a8c",
  hi: "#ccc6ba" }, "slug_grey");
slug({ b1: "#55483c", b2: "#2e2620", outline: "#1a1410", mantle: "#463a2e",
  hi: "#6e5e4e" }, "slug_black");

stagBeetle();
stagBeetle({ b1: "#35302a", b2: "#191612", outline: "#0c0a08", hi: "#4e463e",
  mandible: "#211d18", limb: "#12100e" }, "stag_beetle_black");
stagBeetle({ b1: "#96482e", b2: "#5c2412", outline: "#33120a", hi: "#b46646",
  mandible: "#4c1e0e", limb: "#381408" }, "stag_beetle_red");
stagBeetle({ b1: "#6e3a22", b2: "#3e1c0e", outline: "#221006", hi: "#8e5638",
  mandible: "#36180a", limb: "#260e06" }, "stag_beetle_mahogany");

tigerBeetle();
tigerBeetle({ b1: "#5a7ac0", b2: "#2c447e", outline: "#16264a", hi: "#82a0d8",
  limb: "#1c3054", legHi: "#4e6aa4" }, "tiger_beetle_blue");
tigerBeetle({ b1: "#b08248", b2: "#6e4c22", outline: "#3e2a0e", hi: "#cca266",
  limb: "#4c3414", legHi: "#8e6a3c" }, "tiger_beetle_bronze");
tigerBeetle({ b1: "#8a62b0", b2: "#54367e", outline: "#2c1a4a", hi: "#ac86cc",
  limb: "#3a2458", legHi: "#7a54a0" }, "tiger_beetle_purple");

tortoiseBeetle();
tortoiseBeetle({ b1: "#8ab050", b2: "#527a24", outline: "#2c4a0e", skirt: "#b8d47c",
  skirtStroke: "#88a850", hi: "#ccd898", spot: "#3e6414", head: "#6a9430" },
  "tortoise_beetle_green");
tortoiseBeetle({ b1: "#d07048", b2: "#963e1e", outline: "#56200c", skirt: "#eeb098",
  skirtStroke: "#c88060", hi: "#f4ccae", spot: "#6e2a10", head: "#aa4e2c" },
  "tortoise_beetle_red");
tortoiseBeetle({ b1: "#b8bec4", b2: "#7e868e", outline: "#464e56", skirt: "#dde2e6",
  skirtStroke: "#aab2b8", hi: "#eef2f4", spot: "#565e66", head: "#969ea6" },
  "tortoise_beetle_silver");

waterStrider();
waterStrider({ b1: "#463e36", b2: "#262019", outline: "#14100c", hi: "#5e564c",
  limb: "#1a1612" }, "water_strider_dark");
waterStrider({ b1: "#8e887e", b2: "#5e584e", outline: "#332e26", hi: "#aaa498",
  limb: "#443e34" }, "water_strider_grey");
waterStrider({ b1: "#9a6844", b2: "#643e24", outline: "#3a2210", hi: "#b88660",
  limb: "#4e2e16" }, "water_strider_rust");
