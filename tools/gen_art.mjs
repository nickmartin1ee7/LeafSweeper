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

function ladybug() {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="b" cx="0.35" cy="0.3" r="0.9">
      <stop offset="0" stop-color="#e8453c"/>
      <stop offset="1" stop-color="#b02a24"/>
    </radialGradient>
  </defs>
  <ellipse cx="50" cy="88" rx="30" ry="5" fill="#000" opacity="0.14"/>
  <g stroke="#26201c" stroke-width="3.4" stroke-linecap="round" fill="none">
    <path d="M26 48 L14 40 M24 62 L12 62 M28 74 L18 82"/>
    <path d="M74 48 L86 40 M76 62 L88 62 M72 74 L82 82"/>
  </g>
  <path d="M50 24 C72 24 82 42 82 58 C82 76 68 86 50 86 C32 86 18 76 18 58
           C18 42 28 24 50 24 Z"
        fill="url(#b)" stroke="#5e1713" stroke-width="3.4"/>
  <circle cx="50" cy="26" r="13" fill="#26201c" stroke="#0f0d0b" stroke-width="2.4"/>
  <circle cx="45" cy="23" r="2.6" fill="#fff"/>
  <circle cx="55" cy="23" r="2.6" fill="#fff"/>
  <path d="M43 15 C40 10 36 8 32 8 M57 15 C60 10 64 8 68 8"
        fill="none" stroke="#26201c" stroke-width="2.6" stroke-linecap="round"/>
  <path d="M50 30 L50 85" stroke="#26201c" stroke-width="3.4"/>
  <g fill="#26201c">
    <circle cx="36" cy="44" r="5"/><circle cx="64" cy="44" r="5"/>
    <circle cx="30" cy="60" r="4"/><circle cx="70" cy="60" r="4"/>
    <circle cx="42" cy="72" r="4.6"/><circle cx="58" cy="72" r="4.6"/>
    <circle cx="50" cy="52" r="3.4"/>
  </g>
</svg>`;
  write(join(BUGS, "ladybug.svg"), svg);
}

function butterfly() {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="w" cx="0.4" cy="0.35" r="0.95">
      <stop offset="0" stop-color="#9db8d6"/>
      <stop offset="1" stop-color="#5f7ea6"/>
    </radialGradient>
  </defs>
  <ellipse cx="50" cy="86" rx="26" ry="4.6" fill="#000" opacity="0.13"/>
  <g stroke="#3a4658" stroke-width="3" stroke-linejoin="round">
    <path d="M46 46 C30 18 8 20 12 40 C15 56 32 58 46 54 Z" fill="url(#w)"/>
    <path d="M54 46 C70 18 92 20 88 40 C85 56 68 58 54 54 Z" fill="url(#w)"/>
    <path d="M46 58 C34 66 26 80 36 84 C44 87 48 74 48 62 Z" fill="url(#w)"/>
    <path d="M54 58 C66 66 74 80 64 84 C56 87 52 74 52 62 Z" fill="url(#w)"/>
  </g>
  <g fill="#e08b3e" opacity="0.9">
    <circle cx="22" cy="34" r="5"/><circle cx="78" cy="34" r="5"/>
  </g>
  <g fill="#f4ede0">
    <circle cx="30" cy="44" r="2.4"/><circle cx="70" cy="44" r="2.4"/>
    <circle cx="38" cy="76" r="2"/><circle cx="62" cy="76" r="2"/>
  </g>
  <ellipse cx="50" cy="56" rx="5.4" ry="18" fill="#3a4658" stroke="#232b38" stroke-width="2.4"/>
  <circle cx="50" cy="36" r="7" fill="#3a4658" stroke="#232b38" stroke-width="2.4"/>
  <path d="M46 30 C42 22 36 18 30 18 M54 30 C58 22 64 18 70 18"
        fill="none" stroke="#232b38" stroke-width="2.4" stroke-linecap="round"/>
  <circle cx="30" cy="18" r="2.2" fill="#232b38"/>
  <circle cx="70" cy="18" r="2.2" fill="#232b38"/>
</svg>`;
  write(join(BUGS, "butterfly.svg"), svg);
}

function centipede() {
  // Flatter, elongated segmented body with many long splayed legs.
  // Keep orange body (#d99a4e) with darker outline (#8a5318), head at left.
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

  const spots = segs
    .filter((_, i) => i % 2 === 0)
    .map(([x, y]) => `<circle cx="${x + 2}" cy="${y - 2}" r="1.9" fill="#f0bd7e"/>`)
    .join("");

  const ellipses = segs
    .map(([x, y, rx, ry, rot]) =>
      `<ellipse cx="${x}" cy="${y}" rx="${rx}" ry="${ry}" transform="rotate(${rot} ${x} ${y})"/>`
    )
    .join(" ");

  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <ellipse cx="52" cy="80" rx="36" ry="5" fill="#000" opacity="0.13"/>
  <!-- body stroke pass -->
  <g stroke="#8a5318" stroke-width="3" stroke-linejoin="round" fill="none">${ellipses}</g>
  <!-- body fill pass to hide inner strokes -->
  <g fill="#d99a4e">${ellipses}</g>
  ${spots}
  <!-- long splayed legs drawn on top so they read clearly -->
  <g stroke="#6e3f10" stroke-width="2.4" stroke-linecap="round" fill="none">
      ${legLines}
  </g>
  <!-- head details -->
  <ellipse cx="12" cy="48" rx="6.8" ry="4.6" fill="#d99a4e" stroke="#8a5318" stroke-width="3"/>
  <circle cx="10.2" cy="46.6" r="1.9" fill="#26201c"/>
  <path d="M10.5 42 C8.5 36 6 33 4 31 M14 42 C15.8 36 16 31 14 27"
        fill="none" stroke="#8a5318" stroke-width="2.4" stroke-linecap="round"/>
  <path d="M7 54 C9 56 12 56 14 54" fill="none" stroke="#8a5318"
        stroke-width="1.6" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, "centipede.svg"), svg);
}

function moth() {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <linearGradient id="w" x1="0" y1="0" x2="0.5" y2="1">
      <stop offset="0" stop-color="#9c7248"/>
      <stop offset="1" stop-color="#6d4c2c"/>
    </linearGradient>
  </defs>
  <ellipse cx="50" cy="86" rx="26" ry="4.6" fill="#000" opacity="0.13"/>
  <g stroke="#4a3018" stroke-width="3" stroke-linejoin="round">
    <path d="M45 42 C26 16 8 26 14 46 C19 62 36 62 45 54 Z" fill="url(#w)"/>
    <path d="M55 42 C74 16 92 26 86 46 C81 62 64 62 55 54 Z" fill="url(#w)"/>
    <path d="M46 56 C36 64 30 78 40 82 C47 84 49 70 49 60 Z" fill="url(#w)"/>
    <path d="M54 56 C64 64 70 78 60 82 C53 84 51 70 51 60 Z" fill="url(#w)"/>
  </g>
  <g stroke="#4a3018" stroke-width="2" opacity="0.65" fill="none">
    <path d="M24 30 C30 38 34 46 36 54 M76 30 C70 38 66 46 64 54"/>
  </g>
  <g fill="#3d2712">
    <circle cx="22" cy="42" r="4.4"/><circle cx="78" cy="42" r="4.4"/>
  </g>
  <circle cx="22" cy="42" r="1.8" fill="#e8d9c0"/>
  <circle cx="78" cy="42" r="1.8" fill="#e8d9c0"/>
  <ellipse cx="50" cy="58" rx="9.4" ry="21" fill="#7a5631" stroke="#4a3018" stroke-width="2.8"/>
  <g stroke="#8a6538" stroke-width="1.6" opacity="0.8">
    <path d="M42 50 L58 50 M42 58 L58 58 M43 66 L57 66"/>
  </g>
  <circle cx="50" cy="34" r="7.6" fill="#7a5631" stroke="#4a3018" stroke-width="2.6"/>
  <path d="M46 28 C42 20 38 16 30 14 M54 28 C58 20 62 16 70 14"
        fill="none" stroke="#4a3018" stroke-width="2.2" stroke-linecap="round"/>
  <g stroke="#4a3018" stroke-width="1.4" stroke-linecap="round">
    <path d="M44 24 L38 20 M46 22 L42 17 M56 24 L62 20 M54 22 L58 17"/>
  </g>
</svg>`;
  write(join(BUGS, "moth.svg"), svg);
}

function grasshopper() {
  // Side-profile hopper: head left, body angled up to the right, folded
  // hind leg (thick femur arc, thin tibia) clear of the body.
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <linearGradient id="b" x1="0" y1="0" x2="0.4" y2="1">
      <stop offset="0" stop-color="#8fbf4d"/>
      <stop offset="1" stop-color="#5c8a30"/>
    </linearGradient>
  </defs>
  <ellipse cx="50" cy="80" rx="30" ry="5" fill="#000" opacity="0.13"/>
  <path d="M60 58 C72 54 80 44 79 34" fill="none" stroke="#3f6420"
        stroke-width="6.5" stroke-linecap="round"/>
  <path d="M79 34 C82 48 78 64 70 76" fill="none" stroke="#3f6420"
        stroke-width="3.6" stroke-linecap="round"/>
  <g transform="rotate(-22 57 55)">
    <ellipse cx="57" cy="55" rx="24" ry="10" fill="url(#b)"
             stroke="#3f6420" stroke-width="3"/>
    <ellipse cx="59" cy="52" rx="15" ry="4.5" fill="#a8d06e" opacity="0.85"/>
  </g>
  <path d="M42 64 L36 76 M50 66 L47 78" fill="none" stroke="#3f6420"
        stroke-width="3.2" stroke-linecap="round"/>
  <circle cx="31" cy="57" r="9.5" fill="url(#b)" stroke="#3f6420" stroke-width="3"/>
  <circle cx="28" cy="55" r="2.4" fill="#26201c"/>
  <path d="M27 50 C22 42 16 38 10 37 M34 48 C32 40 30 34 26 29"
        fill="none" stroke="#3f6420" stroke-width="2.4" stroke-linecap="round"/>
  <path d="M24 62 C26 64 29 64 31 62" fill="none" stroke="#3f6420"
        stroke-width="1.6" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, "grasshopper.svg"), svg);
}

function dragonfly() {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <ellipse cx="50" cy="88" rx="20" ry="3.6" fill="#000" opacity="0.12"/>
  <g fill="#cfe3ef" fill-opacity="0.75" stroke="#7ba3bd" stroke-width="2">
    <!-- Forewings: steeper, pulled slightly outward and upward -->
    <ellipse cx="24" cy="34" rx="26" ry="6.4" transform="rotate(-32 24 34)"/>
    <ellipse cx="76" cy="34" rx="26" ry="6.4" transform="rotate(32 76 34)"/>
    <!-- Hindwings: shallower, pulled outward and down to create a visible gap -->
    <ellipse cx="24" cy="54" rx="20" ry="5.6" transform="rotate(28 24 54)"/>
    <ellipse cx="76" cy="54" rx="20" ry="5.6" transform="rotate(-28 76 54)"/>
  </g>
  <path d="M50 34 L50 84" stroke="#5f8aa8" stroke-width="6.4" stroke-linecap="round"/>
  <path d="M50 34 L50 84" stroke="#8fb6cd" stroke-width="3" stroke-linecap="round"/>
  <g stroke="#4a708c" stroke-width="1.6" opacity="0.8">
    <path d="M46 58 L54 58 M46 66 L54 66 M46.5 74 L53.5 74"/>
  </g>
  <circle cx="50" cy="26" r="9.6" fill="#5f8aa8" stroke="#3c5f78" stroke-width="2.8"/>
  <circle cx="44" cy="24" r="4.6" fill="#26333d"/>
  <circle cx="56" cy="24" r="4.6" fill="#26333d"/>
  <circle cx="45" cy="22.6" r="1.4" fill="#dfeaf2"/>
  <circle cx="57" cy="22.6" r="1.4" fill="#dfeaf2"/>
</svg>`;
  write(join(BUGS, "dragonfly.svg"), svg);
}

function beetle() {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="b" cx="0.35" cy="0.3" r="0.95">
      <stop offset="0" stop-color="#5a6b8c"/>
      <stop offset="1" stop-color="#33405c"/>
    </radialGradient>
  </defs>
  <ellipse cx="50" cy="86" rx="26" ry="5" fill="#000" opacity="0.14"/>
  <g stroke="#20283a" stroke-width="3.2" stroke-linecap="round" fill="none">
    <path d="M30 46 L16 38 M28 60 L13 60 M32 72 L20 80"/>
    <path d="M70 46 L84 38 M72 60 L87 60 M68 72 L80 80"/>
  </g>
  <ellipse cx="50" cy="56" rx="24" ry="28" fill="url(#b)" stroke="#1c2434" stroke-width="3.2"/>
  <path d="M50 30 L50 83" stroke="#1c2434" stroke-width="2.6"/>
  <g stroke="#8ea2c4" stroke-width="1.6" opacity="0.7">
    <path d="M36 44 C40 46 44 47 47 47 M64 44 C60 46 56 47 53 47
             M34 58 C39 60 44 61 47 61 M66 58 C61 60 56 61 53 61"/>
  </g>
  <circle cx="50" cy="26" r="10" fill="#20283a" stroke="#121722" stroke-width="2.6"/>
  <path d="M44 18 C40 10 44 6 48 8 M56 18 C60 10 56 6 52 8"
        fill="none" stroke="#121722" stroke-width="3.4" stroke-linecap="round"/>
  <path d="M45 20 C42 14 42 10 45 8 M55 20 C58 14 58 10 55 8"
        fill="none" stroke="#121722" stroke-width="2.2" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, "beetle.svg"), svg);
}

function snail() {
  // Shell sits clear of the head so the neck, eyestalks and eyes read.
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="s" cx="0.4" cy="0.35" r="0.9">
      <stop offset="0" stop-color="#b07a48"/>
      <stop offset="1" stop-color="#7c4f28"/>
    </radialGradient>
  </defs>
  <ellipse cx="52" cy="84" rx="34" ry="5" fill="#000" opacity="0.13"/>
  <path d="M18 76 C24 64 40 60 56 62 C68 63 76 68 80 76 C80 79 76 80 70 80
           L28 80 C21 80 16 79 18 76 Z"
        fill="#c9a06a" stroke="#8a6538" stroke-width="3" stroke-linejoin="round"/>
  <path d="M70 73 C74 66 76 60 82 56 C88 52 94 57 93 63 C92 69 86 74 78 75
           C75 75.4 72 74.6 70 73 Z"
        fill="#c9a06a" stroke="#8a6538" stroke-width="3" stroke-linejoin="round"/>
  <g fill="none" stroke="#8a6538" stroke-width="2.6" stroke-linecap="round">
    <path d="M89 54 C91 47 92 42 92 38"/>
    <path d="M80 53 C79 46 77 41 74 37"/>
  </g>
  <circle cx="92.5" cy="36" r="2.7" fill="#26201c"/>
  <circle cx="73.5" cy="35" r="2.7" fill="#26201c"/>
  <circle cx="44" cy="44" r="22" fill="url(#s)" stroke="#5c3a1c" stroke-width="3.4"/>
  <path d="M44 44 m0 -13 a13 13 0 1 1 -11 20 a8 8 0 1 0 7 -12"
        fill="none" stroke="#5c3a1c" stroke-width="3.6" stroke-linecap="round"/>
  <ellipse cx="36" cy="36" rx="8" ry="5" fill="#d9a86a" opacity="0.55"
           transform="rotate(-30 36 36)"/>
</svg>`;
  write(join(BUGS, "snail.svg"), svg);
}

function firefly() {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="glow" cx="0.5" cy="0.5" r="0.5">
      <stop offset="0" stop-color="#fff6c8" stop-opacity="0.85"/>
      <stop offset="0.55" stop-color="#f7e28a" stop-opacity="0.35"/>
      <stop offset="1" stop-color="#f7e28a" stop-opacity="0"/>
    </radialGradient>
    <radialGradient id="lamp" cx="0.4" cy="0.35" r="0.9">
      <stop offset="0" stop-color="#fff3b0"/>
      <stop offset="1" stop-color="#f2c94c"/>
    </radialGradient>
  </defs>
  <ellipse cx="50" cy="88" rx="18" ry="3.6" fill="#000" opacity="0.12"/>
  <circle cx="50" cy="62" r="30" fill="url(#glow)"/>
  <ellipse cx="36" cy="46" rx="9" ry="15" fill="#cfe3ef" fill-opacity="0.75"
           stroke="#7ba3bd" stroke-width="2" transform="rotate(-30 36 46)"/>
  <ellipse cx="64" cy="46" rx="9" ry="15" fill="#cfe3ef" fill-opacity="0.75"
           stroke="#7ba3bd" stroke-width="2" transform="rotate(30 64 46)"/>
  <path d="M41 45 L33 51 M41 50 L33 57 M59 45 L67 51 M59 50 L67 57"
        fill="none" stroke="#2e241a" stroke-width="2.2" stroke-linecap="round"/>
  <ellipse cx="50" cy="62" rx="13" ry="16" fill="url(#lamp)"
           stroke="#a3771c" stroke-width="3"/>
  <ellipse cx="50" cy="42" rx="10" ry="9" fill="#4a3b2a" stroke="#2e241a"
           stroke-width="2.8"/>
  <circle cx="50" cy="30" r="7" fill="#2e241a" stroke="#1c150f" stroke-width="2.2"/>
  <circle cx="47.4" cy="28.4" r="1.7" fill="#f5e8cd"/>
  <circle cx="52.6" cy="28.4" r="1.7" fill="#f5e8cd"/>
  <path d="M47 24 C44 18 40 15 36 14 M53 24 C56 18 60 15 64 14"
        fill="none" stroke="#2e241a" stroke-width="2" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, "firefly.svg"), svg);
}

function bumblebee() {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="b" cx="0.35" cy="0.3" r="0.95">
      <stop offset="0" stop-color="#f6c04e"/>
      <stop offset="1" stop-color="#d9942e"/>
    </radialGradient>
  </defs>
  <ellipse cx="50" cy="88" rx="22" ry="4" fill="#000" opacity="0.13"/>
  <ellipse cx="35" cy="46" rx="13" ry="7.5" fill="#dfeaf2" fill-opacity="0.85"
           stroke="#8fb6cd" stroke-width="2.2" transform="rotate(-32 35 46)"/>
  <ellipse cx="65" cy="46" rx="13" ry="7.5" fill="#dfeaf2" fill-opacity="0.85"
           stroke="#8fb6cd" stroke-width="2.2" transform="rotate(32 65 46)"/>
  <ellipse cx="50" cy="60" rx="20" ry="24" fill="url(#b)"
           stroke="#8a5b14" stroke-width="3.4"/>
  <ellipse cx="50" cy="52" rx="18.5" ry="5.2" fill="#26201c"/>
  <ellipse cx="50" cy="66" rx="17" ry="5" fill="#26201c"/>
  <path d="M50 90 L45.5 80 L54.5 80 Z" fill="#26201c" stroke="#26201c"
        stroke-width="1.5" stroke-linejoin="round"/>
  <circle cx="50" cy="28" r="8.5" fill="#26201c" stroke="#141210" stroke-width="2.2"/>
  <circle cx="46.8" cy="26.5" r="2" fill="#f5e8cd"/>
  <circle cx="53.2" cy="26.5" r="2" fill="#f5e8cd"/>
  <path d="M46 21 C43 15 39 12 34 11 M54 21 C57 15 61 12 66 11"
        fill="none" stroke="#26201c" stroke-width="2.2" stroke-linecap="round"/>
  <circle cx="33" cy="10.5" r="1.8" fill="#26201c"/>
  <circle cx="67" cy="10.5" r="1.8" fill="#26201c"/>
  <path d="M38 74 L32 82 M62 74 L68 82" fill="none" stroke="#26201c"
        stroke-width="2.6" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, "bumblebee.svg"), svg);
}

function caterpillar() {
  // Arched green segments, stub legs, friendly face — clearly softer than
  // the long-legged centipede.
  const segs = [
    [36, 56, 7.5], [48, 52, 7.5], [60, 56, 7.5], [71, 61, 7.0], [80, 67, 5.5],
  ];
  const circles = segs
    .map(([x, y, r]) => `<circle cx="${x}" cy="${y}" r="${r}"/>`)
    .join(" ");

  // Build visible legs after the body so they appear on top of the circles.
  // Pick the two rearmost segments (by vertical position) and give them
  // chunky stub legs that extend below the body's lowest edge. Keep legs
  // short (around 9-12px) and angled down-and-out.
  const sortedByBottom = segs.map((s, i) => [s[1] + s[2], i]).sort((a, b) => a[0] - b[0]);
  const tailPairs = sortedByBottom.slice(-2).map(([_, i]) => i); // indices of the 2 lowest segments
  const legLength = 11; // within requested 9-12px
  const legs = tailPairs
    .map(i => {
      const [x, y, r] = segs[i];
      const lx0 = Math.round(x - r * 0.6);
      const ly0 = Math.round(y + r * 0.6);
      const lx1 = Math.round(x - (r * 0.9) - Math.round(legLength * 0.9));
      const ly1 = Math.round(y + r + legLength);
      const rx0 = Math.round(x + r * 0.6);
      const rx1 = Math.round(x + (r * 0.9) + Math.round(legLength * 0.9));
      const ry0 = ly0;
      const ry1 = ly1;
      return `<path d="M${lx0} ${ly0} L${lx1} ${ly1} M${rx0} ${ry0} L${rx1} ${ry1}"/>`;
    })
    .join(" ");

  // Tail prolegs (2-3 little stubs centered under the last segment)
  const [tx, ty, tr] = segs[segs.length - 1];
  const prolegs = [-4, 0, 4]
    .map(off => {
      const sx = tx + off;
      const sy0 = Math.round(ty + tr * 0.7);
      const sy1 = Math.round(ty + tr + 11);
      return `<path d="M${sx} ${sy0} L${sx} ${sy1}"/>`;
    })
    .join(" ");

  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <ellipse cx="50" cy="84" rx="34" ry="5" fill="#000" opacity="0.13"/>
  <g fill="#7fae4e" stroke="#3e6323" stroke-width="3">${circles}</g>
  <g fill="#7fae4e">${circles}</g>
  <!-- legs drawn after the body so they show up clearly -->
  <g stroke="#3e6323" stroke-width="2.6" stroke-linecap="round" fill="none">${legs} ${prolegs}</g>
  <circle cx="48" cy="50" r="2.1" fill="#a8d06e"/>
  <circle cx="60" cy="54" r="2.1" fill="#a8d06e"/>
  <circle cx="22" cy="60" r="9.5" fill="#8fbf4e" stroke="#3e6323" stroke-width="3"/>
  <circle cx="19" cy="58" r="2.2" fill="#26201c"/>
  <path d="M15 64 C17 66 20 66 22 64" fill="none" stroke="#3e6323"
        stroke-width="1.6" stroke-linecap="round"/>
  <path d="M17 52 C14 46 10 43 6 42 M23 51 C22 44 20 39 16 35"
        fill="none" stroke="#3e6323" stroke-width="2.2" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, "caterpillar.svg"), svg);
}

function mantis() {
  // Triangular head with big eyes + folded forelegs carry the silhouette.
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <linearGradient id="m" x1="0" y1="0" x2="0.4" y2="1">
      <stop offset="0" stop-color="#8fbf4d"/>
      <stop offset="1" stop-color="#5c8a30"/>
    </linearGradient>
  </defs>
  <ellipse cx="52" cy="86" rx="26" ry="4.5" fill="#000" opacity="0.12"/>
  <g stroke="#3e6323" stroke-width="2.6" fill="none" stroke-linecap="round"
     stroke-linejoin="round">
    <path d="M44 52 L34 66 L40 70"/>
    <path d="M54 60 L46 76 L52 80"/>
    <path d="M66 64 L62 78 L68 82"/>
  </g>
  <g stroke="#3e6323" stroke-width="3.2" fill="none" stroke-linecap="round"
     stroke-linejoin="round">
    <!-- Single clean folded foreleg PAIR (praying pose). Each arm folds up-forward then sharply down. -->
    <path d="M44 44 L50 30 L56 44"/>
    <path d="M46 46 L52 32 L58 46"/>
  </g>
  <g transform="rotate(38 58 62)">
    <ellipse cx="58" cy="62" rx="24" ry="9" fill="url(#m)"
             stroke="#3e6323" stroke-width="3"/>
    <ellipse cx="60" cy="59.5" rx="15" ry="4" fill="#a8d06e" opacity="0.8"/>
  </g>
  <path d="M34 26 L46 48" stroke="#3e6323" stroke-width="6" stroke-linecap="round"/>
  <path d="M34 26 L46 48" stroke="#7fae4e" stroke-width="3" stroke-linecap="round"/>
  <path d="M26 20 C33 16 40 18 40 25 C40 32 33 34 26 30 C22 27 22 23 26 20 Z"
        fill="#7fae4e" stroke="#3e6323" stroke-width="2.6" stroke-linejoin="round"/>
  <circle cx="27" cy="25" r="3" fill="#26201c"/>
  <circle cx="36" cy="24" r="3" fill="#26201c"/>
  <path d="M32 18 C30 12 27 8 23 6 M38 17 C38 11 37 7 35 4"
        fill="none" stroke="#3e6323" stroke-width="2" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, "mantis.svg"), svg);
}

function stickInsect() {
  // A long thin stick: dark under-stroke + light over-stroke fake an outline.
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <ellipse cx="48" cy="88" rx="30" ry="3.6" fill="#000" opacity="0.1"/>
  <g stroke="#55603a" stroke-width="2.4" fill="none" stroke-linecap="round"
     stroke-linejoin="round">
    <path d="M64 46 L56 60 L62 72"/>
    <path d="M52 54 L44 66 L50 78"/>
    <path d="M40 62 L32 74 L38 84"/>
  </g>
  <path d="M16 86 L78 30" stroke="#55603a" stroke-width="9" stroke-linecap="round"/>
  <path d="M16 86 L78 30" stroke="#8a9455" stroke-width="5.4" stroke-linecap="round"/>
  <g stroke="#55603a" stroke-width="1.4" opacity="0.7">
    <path d="M30 74 L36 80 M42 64 L48 70 M54 54 L60 60 M66 44 L72 50"/>
  </g>
  <circle cx="82" cy="27" r="6" fill="#8a9455" stroke="#55603a" stroke-width="2.4"/>
  <circle cx="84" cy="26" r="1.8" fill="#26201c"/>
  <path d="M86 22 C90 16 94 12 97 10 M84 20 C86 14 88 9 92 6"
        fill="none" stroke="#55603a" stroke-width="2" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, "stick_insect.svg"), svg);
}

function weevil() {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="w" cx="0.35" cy="0.3" r="0.95">
      <stop offset="0" stop-color="#a8794a"/>
      <stop offset="1" stop-color="#7a5426"/>
    </radialGradient>
  </defs>
  <ellipse cx="52" cy="80" rx="26" ry="4.5" fill="#000" opacity="0.12"/>
  <g stroke="#4a3418" stroke-width="2.8" fill="none" stroke-linecap="round">
    <path d="M40 62 L28 72 M44 68 L36 79 M58 68 L60 80 M66 60 L74 70"/>
  </g>
  <ellipse cx="58" cy="56" rx="22" ry="19" fill="url(#w)"
           stroke="#4a3418" stroke-width="3.2"/>
  <path d="M58 38 L58 74" stroke="#4a3418" stroke-width="2.2"/>
  <g fill="#c49a63" opacity="0.9">
    <circle cx="50" cy="48" r="2.2"/><circle cx="66" cy="52" r="2.2"/>
    <circle cx="56" cy="64" r="2.2"/>
  </g>
  <circle cx="32" cy="44" r="7" fill="#8a6538" stroke="#4a3418" stroke-width="2.8"/>
  <!-- tapered snout: dark under-shape then lighter inner fill -->
  <path d="M28 42 L11 46 L14 44 Z" fill="#4a3418" stroke="none"/>
  <path d="M28 42 L13 45 L13 44 Z" fill="#a8794a" stroke="none"/>
  <!-- tip dot -->
  <circle cx="11" cy="46" r="1.6" fill="#26201c"/>
  <!-- small antenna along snout -->
  <path d="M20 43 C19 41 17 40 16 41" fill="none" stroke="#4a3418" stroke-width="1.6" stroke-linecap="round"/>
  <circle cx="16.2" cy="41.1" r="0.9" fill="#26201c"/>
</svg>`;
  write(join(BUGS, "weevil.svg"), svg);
}

function pillbug() {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="p" cx="0.4" cy="0.3" r="0.95">
      <stop offset="0" stop-color="#8a93a3"/>
      <stop offset="1" stop-color="#57606d"/>
    </radialGradient>
  </defs>
  <ellipse cx="50" cy="84" rx="26" ry="4.5" fill="#000" opacity="0.12"/>
  <g stroke="#3c424c" stroke-width="2.4" stroke-linecap="round">
    <path d="M36 74 L32 82 M46 76 L44 84 M56 76 L56 84 M66 74 L70 82"/>
  </g>
  <path d="M24 76 C18 56 32 40 52 40 C72 40 84 56 78 76 C74 80 28 80 24 76 Z"
        fill="url(#p)" stroke="#3c424c" stroke-width="3.2" stroke-linejoin="round"/>
  <g fill="none" stroke="#3c424c" stroke-width="2.2" stroke-linecap="round" opacity="0.85">
    <path d="M36 44 C32 56 32 66 36 76"/>
    <path d="M46 41 C43 54 43 66 46 78"/>
    <path d="M56 40 C54 54 54 66 56 78"/>
    <path d="M66 42 C65 54 65 66 66 77"/>
    <path d="M74 48 C75 58 75 66 73 75"/>
  </g>
  <ellipse cx="44" cy="52" rx="10" ry="5" fill="#9aa3b2" opacity="0.6"
           transform="rotate(-16 44 52)"/>
  <circle cx="26" cy="60" r="7" fill="#57606d" stroke="#3c424c" stroke-width="2.6"/>
  <circle cx="23" cy="58" r="1.8" fill="#26201c"/>
  <path d="M21 54 C18 49 15 46 11 44 M24 53 C23 48 21 44 18 41"
        fill="none" stroke="#3c424c" stroke-width="2" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, "pillbug.svg"), svg);
}

function ant() {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <ellipse cx="50" cy="80" rx="24" ry="4" fill="#000" opacity="0.12"/>
  <g stroke="#26201c" stroke-width="2.4" fill="none" stroke-linecap="round"
     stroke-linejoin="round">
    <path d="M38 58 L28 68 L30 74 M42 60 L38 72 L42 78 M48 60 L52 72 L58 76"/>
  </g>
  <ellipse cx="68" cy="58" rx="15" ry="12" fill="#3a2c1c" stroke="#1c150f"
           stroke-width="2.8" transform="rotate(-18 68 58)"/>
  <circle cx="57" cy="55" r="3.6" fill="#3a2c1c" stroke="#1c150f" stroke-width="2"/>
  <ellipse cx="45" cy="53" rx="9" ry="7" fill="#3a2c1c" stroke="#1c150f"
           stroke-width="2.6"/>
  <circle cx="29" cy="47" r="8" fill="#3a2c1c" stroke="#1c150f" stroke-width="2.6"/>
  <circle cx="26" cy="45" r="1.8" fill="#f5e8cd"/>
  <path d="M27 40 L22 32 L26 26 M33 40 L33 31 L38 25"
        stroke="#1c150f" stroke-width="2.2" fill="none" stroke-linecap="round"
        stroke-linejoin="round"/>
</svg>`;
  write(join(BUGS, "ant.svg"), svg);
}

function fly() {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="f" cx="0.35" cy="0.3" r="0.95">
      <stop offset="0" stop-color="#99a3b2"/>
      <stop offset="1" stop-color="#6a7380"/>
    </radialGradient>
  </defs>
  <ellipse cx="50" cy="82" rx="22" ry="4" fill="#000" opacity="0.12"/>
  <ellipse cx="66" cy="44" rx="15" ry="5.5" fill="#dfeaf2" fill-opacity="0.8"
           stroke="#8fb6cd" stroke-width="2" transform="rotate(-18 66 44)"/>
  <ellipse cx="64" cy="54" rx="13" ry="4.8" fill="#dfeaf2" fill-opacity="0.8"
           stroke="#8fb6cd" stroke-width="2" transform="rotate(10 64 54)"/>
  <g stroke="#3c424c" stroke-width="2.2" fill="none" stroke-linecap="round">
    <path d="M40 58 L32 70 M46 60 L42 74 M56 60 L60 74"/>
  </g>
  <ellipse cx="60" cy="62" rx="13" ry="10.5" fill="url(#f)" stroke="#3c424c"
           stroke-width="2.8" transform="rotate(28 60 62)"/>
  <ellipse cx="46" cy="50" rx="12" ry="10" fill="#7a8391" stroke="#3c424c"
           stroke-width="2.8"/>
  <circle cx="39" cy="38" r="5.4" fill="#a8442e" stroke="#6e2417" stroke-width="2.2"/>
  <circle cx="51" cy="37" r="5.4" fill="#a8442e" stroke="#6e2417" stroke-width="2.2"/>
  <circle cx="37.5" cy="36.5" r="1.5" fill="#e8d9c0"/>
  <circle cx="49.5" cy="35.5" r="1.5" fill="#e8d9c0"/>
</svg>`;
  write(join(BUGS, "fly.svg"), svg);
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
ladybug();
butterfly();
centipede();
moth();
grasshopper();
dragonfly();
beetle();
snail();
firefly();
bumblebee();
caterpillar();
mantis();
stickInsect();
weevil();
pillbug();
ant();
fly();
