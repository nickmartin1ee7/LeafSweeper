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
