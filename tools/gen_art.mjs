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
  <path d="M50 30 L50 85" stroke="#5e1713" stroke-width="3"/>
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
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <ellipse cx="50" cy="82" rx="32" ry="5" fill="#000" opacity="0.13"/>
  <g stroke="#b5762e" stroke-width="2.6" stroke-linecap="round" fill="none">
    <path d="M24 44 L15 38 M22 54 L11 52 M24 64 L14 68 M30 72 L24 81 M40 77 L38 87
             M52 78 L54 88 M64 74 L70 82 M74 64 L84 68 M78 52 L89 52 M76 42 L86 36"/>
  </g>
  <g stroke="#8a5318" stroke-width="11" stroke-linecap="round" fill="none">
    <path d="M30 42 A24 24 0 1 1 26 58 A16 16 0 1 0 54 64 A10 10 0 1 1 44 50"/>
  </g>
  <g stroke="#d99a4e" stroke-width="5.4" stroke-linecap="round" fill="none">
    <path d="M30 42 A24 24 0 1 1 26 58 A16 16 0 1 0 54 64 A10 10 0 1 1 44 50"/>
  </g>
  <g stroke="#8a5318" stroke-width="1.8" opacity="0.75">
    <path d="M18 34 L28 40 M12 46 L24 48 M13 58 L25 58 M19 69 L30 65"/>
  </g>
  <circle cx="30" cy="42" r="9" fill="#d99a4e" stroke="#8a5318" stroke-width="3"/>
  <circle cx="27" cy="40" r="2" fill="#26201c"/>
  <path d="M24 34 C20 28 16 26 12 26 M30 33 C30 27 28 23 24 20"
        fill="none" stroke="#8a5318" stroke-width="2.2" stroke-linecap="round"/>
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
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <linearGradient id="b" x1="0" y1="0" x2="0.4" y2="1">
      <stop offset="0" stop-color="#8fbf4d"/>
      <stop offset="1" stop-color="#5c8a30"/>
    </linearGradient>
  </defs>
  <ellipse cx="50" cy="80" rx="30" ry="5" fill="#000" opacity="0.13"/>
  <g transform="rotate(38 50 50)">
    <ellipse cx="50" cy="52" rx="27" ry="11" fill="url(#b)"
             stroke="#3f6420" stroke-width="3"/>
    <ellipse cx="52" cy="49" rx="20" ry="5.6" fill="#a8d06e" opacity="0.85"/>
  </g>
  <path d="M62 52 C74 46 80 36 78 28 M78 28 C76 40 72 58 66 72"
        fill="none" stroke="#3f6420" stroke-width="5" stroke-linecap="round"/>
  <path d="M40 58 L34 68 M48 60 L46 70 M32 52 L22 58" fill="none"
        stroke="#3f6420" stroke-width="3.4" stroke-linecap="round"/>
  <circle cx="33" cy="38" r="10" fill="url(#b)" stroke="#3f6420" stroke-width="3"/>
  <circle cx="30" cy="36" r="2.6" fill="#26201c"/>
  <path d="M28 30 C22 20 16 16 8 16 M36 28 C36 18 34 12 30 6"
        fill="none" stroke="#3f6420" stroke-width="2.4" stroke-linecap="round"/>
</svg>`;
  write(join(BUGS, "grasshopper.svg"), svg);
}

function dragonfly() {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <ellipse cx="50" cy="88" rx="20" ry="3.6" fill="#000" opacity="0.12"/>
  <g fill="#cfe3ef" fill-opacity="0.75" stroke="#7ba3bd" stroke-width="2">
    <ellipse cx="28" cy="38" rx="24" ry="6.4" transform="rotate(-22 28 38)"/>
    <ellipse cx="72" cy="38" rx="24" ry="6.4" transform="rotate(22 72 38)"/>
    <ellipse cx="28" cy="50" rx="21" ry="5.6" transform="rotate(14 28 50)"/>
    <ellipse cx="72" cy="50" rx="21" ry="5.6" transform="rotate(-14 72 50)"/>
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
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <radialGradient id="s" cx="0.4" cy="0.35" r="0.9">
      <stop offset="0" stop-color="#b07a48"/>
      <stop offset="1" stop-color="#7c4f28"/>
    </radialGradient>
  </defs>
  <ellipse cx="52" cy="84" rx="34" ry="5" fill="#000" opacity="0.13"/>
  <path d="M22 76 C26 64 42 60 58 64 C74 62 84 70 80 78 C68 86 34 86 22 76 Z"
        fill="#c9a06a" stroke="#8a6538" stroke-width="3"/>
  <path d="M80 74 C84 66 82 58 76 54" fill="none" stroke="#8a6538"
        stroke-width="3" stroke-linecap="round"/>
  <circle cx="76" cy="50" r="2.6" fill="#26201c"/>
  <circle cx="84" cy="56" r="2.6" fill="#26201c"/>
  <circle cx="54" cy="46" r="25" fill="url(#s)" stroke="#5c3a1c" stroke-width="3.4"/>
  <path d="M54 46 m0 -16 a16 16 0 1 1 -13 25 a10 10 0 1 0 8 -15"
        fill="none" stroke="#5c3a1c" stroke-width="4" stroke-linecap="round"/>
  <ellipse cx="44" cy="36" rx="9" ry="6" fill="#d9a86a" opacity="0.55"
           transform="rotate(-30 44 36)"/>
</svg>`;
  write(join(BUGS, "snail.svg"), svg);
}

// ---------------------------------------------------------------- ground ---

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
