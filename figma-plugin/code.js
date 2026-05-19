// UstAldan Quiz — Figma UI Generator
// Plugins → Development → Import plugin from manifest → выбери figma-plugin/manifest.json

const W = 390;
const H = 844;
const GAP = 80;

// ── Цвета проекта (из GameSceneBuilder.cs) ───────────────────────────
function hex(h) {
  const n = parseInt(h, 16);
  return { r: ((n >> 16) & 255) / 255, g: ((n >> 8) & 255) / 255, b: (n & 255) / 255 };
}
const C = {
  bg:        hex('F5F0E8'),
  primary:   hex('2D6040'),
  secondary: hex('C8A84B'),
  text:      hex('1A2A1A'),
  text2:     hex('4A6A4A'),
  correct:   hex('4CAF50'),
  wrong:     hex('F44336'),
  white:     { r: 1, g: 1, b: 1 },
  black:     { r: 0, g: 0, b: 0 },
  gradTop:   hex('0F2961'),  // тёмно-синий верх градиента
  gradBot:   hex('2D85D1'),  // голубой низ градиента
  rowAlt:    { r: 0.97, g: 0.97, b: 0.97 },
};

// ── Базовые хелперы ───────────────────────────────────────────────────
function solid(color, opacity = 1) {
  return { type: 'SOLID', color, opacity };
}

function makeFrame(parent, name, x, y, w, h) {
  const f = figma.createFrame();
  f.name = name;
  f.x = x; f.y = y;
  f.resize(w, h);
  f.fills = [];
  f.clipsContent = true;
  if (parent) parent.appendChild(f);
  return f;
}

function makeRect(parent, name, x, y, w, h, color, opacity = 1, radius = 0) {
  const r = figma.createRectangle();
  r.name = name;
  r.x = x; r.y = y;
  r.resize(Math.max(w, 1), Math.max(h, 1));
  r.fills = [solid(color, opacity)];
  r.cornerRadius = radius;
  parent.appendChild(r);
  return r;
}

function makeGradient(parent, name, x, y, w, h, topColor, botColor) {
  const r = figma.createRectangle();
  r.name = name;
  r.x = x; r.y = y;
  r.resize(w, h);
  // gradientTransform для направления сверху вниз
  r.fills = [{
    type: 'GRADIENT_LINEAR',
    gradientTransform: [[0, 1, 0], [-1, 0, 1]],
    gradientStops: [
      { position: 0, color: { r: topColor.r, g: topColor.g, b: topColor.b, a: 1 } },
      { position: 1, color: { r: botColor.r, g: botColor.g, b: botColor.b, a: 1 } },
    ],
  }];
  parent.appendChild(r);
  return r;
}

async function makeText(parent, str, x, y, w, h, size, color, bold = false, align = 'LEFT', opacity = 1) {
  const t = figma.createText();
  t.fontName = { family: 'Inter', style: bold ? 'Bold' : 'Regular' };
  t.characters = str;
  t.fontSize = size;
  t.fills = [solid(color, opacity)];
  t.textAlignHorizontal = align;
  t.x = x; t.y = y;
  if (w > 0) t.resize(w, h > 0 ? h : Math.max(t.height, size + 4));
  parent.appendChild(t);
  return t;
}

function makeEllipse(parent, name, cx, cy, r, color, opacity = 1) {
  const e = figma.createEllipse();
  e.name = name;
  e.resize(r * 2, r * 2);
  e.x = cx - r; e.y = cy - r;
  e.fills = [solid(color, opacity)];
  parent.appendChild(e);
  return e;
}

// ── Переиспользуемые компоненты ───────────────────────────────────────

function addStatusBar(parent) {
  makeRect(parent, 'StatusBarCover', 0, 0, W, 44, C.black);
}

async function addHeader(parent, y, title, showBack = true, showRight = '') {
  const h = makeFrame(parent, 'Header', 0, y, W, 56);
  h.fills = [solid(C.primary)];
  if (showBack) {
    makeRect(h, 'BtnBack_bg', 10, 10, 36, 36, C.white, 0.2, 8);
    await makeText(h, '‹', 18, 8, 20, 40, 26, C.white, true);
  }
  const titleX = showBack ? 58 : 0;
  const titleW = showBack ? W - 70 - (showRight ? 80 : 0) : W;
  await makeText(h, title, titleX, 14, titleW, 28, 17, C.white, true, showBack ? 'LEFT' : 'CENTER');
  if (showRight) {
    await makeText(h, showRight, W - 90, 14, 80, 28, 12, C.white, false, 'RIGHT');
  }
  return h;
}

async function addNavBar(parent, activeIndex) {
  const navH = 84;
  const nav = makeFrame(parent, 'NavBar', 0, H - navH, W, navH);
  nav.fills = [solid(C.white)];
  nav.effects = [{
    type: 'DROP_SHADOW',
    color: { r: 0, g: 0, b: 0, a: 0.1 },
    offset: { x: 0, y: -2 },
    radius: 8, spread: 0, visible: true, blendMode: 'NORMAL',
  }];
  const tabs = ['Профиль', 'Рекорды', 'Главная', 'О нас', 'Настройки'];
  const tabW = W / 5;
  for (let i = 0; i < tabs.length; i++) {
    const active = i === activeIndex;
    const color = active ? C.primary : { r: 0.6, g: 0.6, b: 0.6 };
    const tx = i * tabW;
    makeRect(nav, `Icon_${i}`, tx + tabW / 2 - 12, 12, 24, 24, color, 1, 12);
    await makeText(nav, tabs[i], tx, 42, tabW, 18, 9, color, active, 'CENTER');
  }
  return nav;
}

// ── Сцена 0: Intro ────────────────────────────────────────────────────
async function buildIntro(page, x) {
  const f = makeFrame(page, '0 — Intro', x, 0, W, H);
  f.fills = [solid(C.black)];
  makeRect(f, 'VideoBg', 0, 0, W, H, { r: 0.08, g: 0.08, b: 0.08 });
  makeEllipse(f, 'PlayIcon', W / 2, H / 2, 44, C.white, 0.15);
  makeRect(f, 'PlayTriangle', W / 2 - 10, H / 2 - 14, 28, 28, C.white, 0.5, 4);
  addStatusBar(f);
  await makeText(f, 'Нажмите, чтобы пропустить', 0, H - 52, W, 28, 13, C.white, false, 'CENTER', 0.55);
}

// ── Сцена 1: MainMenu — Главная ───────────────────────────────────────
async function buildMainMenu(page, x) {
  const f = makeFrame(page, '1a — MainMenu / Главная', x, 0, W, H);
  makeGradient(f, 'Background', 0, 0, W, H, C.gradTop, C.gradBot);
  addStatusBar(f);

  await makeText(f, 'Усть-Алданский улус', 0, 56, W, 22, 12, C.white, false, 'CENTER', 0.75);
  await makeText(f, 'Викторина', 0, 80, W, 60, 36, C.white, true, 'CENTER');
  await makeText(f, '100 лет', 0, 144, W, 46, 30, C.secondary, true, 'CENTER');

  // Сетка категорий 2×3
  const cats = ['История', 'Культура', 'Природа', 'Спорт', 'Люди', 'События'];
  const cW = 174, cH = 58, cGapX = 12, cGapY = 10;
  const gridX = (W - cW * 2 - cGapX) / 2;
  for (let i = 0; i < cats.length; i++) {
    const col = i % 2, row = Math.floor(i / 2);
    const bx = gridX + col * (cW + cGapX);
    const by = 204 + row * (cH + cGapY);
    const btn = makeFrame(f, `CategoryBtn_${cats[i]}`, bx, by, cW, cH);
    btn.fills = [solid(C.white, 0.88)];
    btn.cornerRadius = 16;
    await makeText(btn, cats[i], 0, 0, cW, cH, 14, C.primary, true, 'CENTER');
  }

  await makeText(f, 'Сыграно: 0  •  Рекорд: 0/0  •  Вопросов: 0', 0, 412, W, 22, 11, C.white, false, 'CENTER', 0.65);

  // BtnPlay
  const play = makeFrame(f, 'BtnPlay', W / 2 - 120, 444, 240, 58);
  play.fills = [solid(C.primary)]; play.cornerRadius = 29;
  await makeText(play, 'Начать игру', 0, 0, 240, 58, 16, C.white, true, 'CENTER');

  // BtnArcade
  const arcade = makeFrame(f, 'BtnArcade', W / 2 - 94, 514, 188, 46);
  arcade.fills = [solid(C.white, 0.15)]; arcade.cornerRadius = 23;
  await makeText(arcade, 'Аркада', 0, 0, 188, 46, 14, C.white, false, 'CENTER');

  await addNavBar(f, 2);
}

// ── Сцена 1b: MainMenu — Рекорды ─────────────────────────────────────
async function buildRecords(page, x) {
  const f = makeFrame(page, '1b — MainMenu / Рекорды', x, 0, W, H);
  makeGradient(f, 'Background', 0, 0, W, H, C.gradTop, C.gradBot);
  addStatusBar(f);

  const card = makeFrame(f, 'RecordsCard', 0, 44, W, H - 44 - 84);
  card.fills = [solid(C.white)];
  await makeText(card, 'Рекорды', 0, 20, W, 36, 20, C.primary, true, 'CENTER');

  const cats = ['История', 'Культура', 'Природа', 'Спорт', 'Люди', 'События'];
  const scores = ['15 / 20', '8 / 15', '0 / 12', '12 / 20', '0 / 10', '5 / 18'];
  for (let i = 0; i < cats.length; i++) {
    const ry = 70 + i * 68;
    if (i % 2 === 0) makeRect(card, `RowBg_${i}`, 0, ry, W, 64, C.rowAlt);
    await makeText(card, cats[i], 20, ry + 20, 220, 24, 15, C.text);
    await makeText(card, scores[i], W - 120, ry + 20, 100, 24, 15, C.primary, true, 'RIGHT');
  }

  await addNavBar(f, 1);
}

// ── Сцена 1c: MainMenu — Настройки ───────────────────────────────────
async function buildSettings(page, x) {
  const f = makeFrame(page, '1c — MainMenu / Настройки', x, 0, W, H);
  makeGradient(f, 'Background', 0, 0, W, H, C.gradTop, C.gradBot);
  addStatusBar(f);

  const card = makeFrame(f, 'SettingsCard', 0, 44, W, H - 44 - 84);
  card.fills = [solid(C.white)];
  await makeText(card, 'Настройки', 0, 20, W, 36, 20, C.primary, true, 'CENTER');

  const rows = [
    { label: 'Музыка', type: 'slider', value: 0.75 },
    { label: 'Звуки', type: 'slider', value: 0.5 },
    { label: 'Вибрация', type: 'toggle', on: true },
    { label: 'Язык', type: 'lang' },
  ];
  let ry = 74;
  for (const row of rows) {
    makeRect(card, `Row_${row.label}`, 16, ry, W - 32, 62, { r: 0.96, g: 0.97, b: 0.96 }, 1, 12);
    await makeText(card, row.label, 32, ry + 18, 140, 26, 15, C.text);
    if (row.type === 'slider') {
      const trackW = 160, trackX = 180, trackY = ry + 27;
      makeRect(card, 'Track', trackX, trackY, trackW, 8, { r: 0.82, g: 0.82, b: 0.82 }, 1, 4);
      makeRect(card, 'Fill', trackX, trackY, Math.round(trackW * row.value), 8, C.primary, 1, 4);
      makeEllipse(card, 'Thumb', trackX + Math.round(trackW * row.value), trackY + 4, 11, C.primary);
    } else if (row.type === 'toggle') {
      makeRect(card, 'ToggleBg', W - 72, ry + 19, 46, 24, row.on ? C.primary : { r: 0.8, g: 0.8, b: 0.8 }, 1, 12);
      makeEllipse(card, 'Knob', row.on ? W - 34 : W - 58, ry + 31, 10, C.white);
    } else {
      await makeText(card, 'RU  /  SAH', W - 110, ry + 18, 94, 26, 13, C.primary, true, 'RIGHT');
    }
    ry += 76;
  }

  await addNavBar(f, 4);
}

// ── Сцена 1d: MainMenu — О приложении ────────────────────────────────
async function buildAbout(page, x) {
  const f = makeFrame(page, '1d — MainMenu / О нас', x, 0, W, H);
  makeGradient(f, 'Background', 0, 0, W, H, C.gradTop, C.gradBot);
  addStatusBar(f);

  const card = makeFrame(f, 'AboutCard', 0, 44, W, H - 44 - 84);
  card.fills = [solid(C.white)];
  await makeText(card, 'О приложении', 0, 20, W, 36, 20, C.primary, true, 'CENTER');

  await makeText(card, 'Викторина к 100-летию Усть-Алданского улуса.\nВопросы об истории, культуре и природе района.', 24, 72, W - 48, 72, 14, C.text, false, 'LEFT');
  await makeText(card, 'Разработчик: ...', 24, 160, W - 48, 28, 13, C.text2);
  await makeText(card, 'v1.0  •  2025', 24, 192, W - 48, 24, 12, C.text2);
  await makeText(card, 'Контакты: info@uacbs.ru', 24, 232, W - 48, 24, 13, C.text2);

  // Кнопка Предложить вопрос
  const btnSuggest = makeFrame(card, 'BtnSuggest', 24, 300, W - 48, 52);
  btnSuggest.fills = [];
  btnSuggest.strokes = [solid(C.primary)];
  btnSuggest.strokeWeight = 1.5;
  btnSuggest.cornerRadius = 26;
  await makeText(btnSuggest, 'Предложить вопрос', 0, 0, W - 48, 52, 15, C.primary, true, 'CENTER');

  // Кнопка Посетить сайт
  const btnSite = makeFrame(card, 'BtnVisitSite', 24, 364, W - 48, 52);
  btnSite.fills = [solid(C.primary)];
  btnSite.cornerRadius = 26;
  await makeText(btnSite, 'Посетить сайт', 0, 0, W - 48, 52, 15, C.white, true, 'CENTER');

  await addNavBar(f, 3);
}

// ── Сцена 2: QuestionMap ──────────────────────────────────────────────
async function buildQuestionMap(page, x) {
  const f = makeFrame(page, '2 — QuestionMap', x, 0, W, H);
  f.fills = [solid(C.bg)];
  addStatusBar(f);
  await addHeader(f, 44, 'История', true, 'Правильных: 0/15');

  const tW = 98, tH = 68, cols = 3, gx = 14, gy = 14;
  const startX = (W - cols * tW - (cols - 1) * gx) / 2;
  const startY = 116;
  const stateColors = [C.correct, C.correct, C.wrong, C.secondary];

  for (let i = 0; i < 15; i++) {
    const col = i % cols, row = Math.floor(i / cols);
    const tx = startX + col * (tW + gx);
    const ty = startY + row * (tH + gy);
    const color = i < stateColors.length ? stateColors[i] : { r: 0.88, g: 0.88, b: 0.88 };
    const textColor = i < stateColors.length ? C.white : C.text;
    const tile = makeFrame(f, `Tile_${i + 1}`, tx, ty, tW, tH);
    tile.fills = [solid(color)];
    tile.cornerRadius = 14;
    await makeText(tile, String(i + 1), 0, 0, tW, tH, 18, textColor, true, 'CENTER');
  }
}

// ── Сцена 2b: QuestionWindow (попап) ─────────────────────────────────
async function buildQuestionWindow(page, x) {
  const f = makeFrame(page, '2b — QuestionWindow (popup)', x, 0, W, H);
  f.fills = [solid(C.bg)];
  addStatusBar(f);
  await addHeader(f, 44, 'История', true, 'Правильных: 2/15');

  // Затемнение
  makeRect(f, 'Overlay', 0, 0, W, H, C.black, 0.45);

  // Нижний лист
  const sheetH = 640;
  const sheet = makeFrame(f, 'QuestionSheet', 0, H - sheetH, W, sheetH);
  sheet.fills = [solid(C.white)];
  sheet.cornerRadius = 24;

  // Зона вопроса
  const qZone = makeFrame(sheet, 'Zone_Question', 16, 20, W - 32, 112);
  qZone.fills = [solid({ r: 0.97, g: 0.97, b: 0.97 })];
  qZone.cornerRadius = 16;
  await makeText(qZone, 'В каком году был основан\nУсть-Алданский район?', 16, 16, W - 64, 80, 16, C.text, false, 'LEFT');

  // Ответы
  const answers = ['1930', '1935', '1940', '1925'];
  const aColors = [C.correct, { r: 0.93, g: 0.93, b: 0.93 }, { r: 0.93, g: 0.93, b: 0.93 }, { r: 0.93, g: 0.93, b: 0.93 }];
  const aTextColors = [C.white, C.text, C.text, C.text];
  for (let i = 0; i < 4; i++) {
    const ay = 148 + i * 74;
    const btn = makeFrame(sheet, `Answer_${i}`, 16, ay, W - 32, 62);
    btn.fills = [solid(aColors[i])];
    btn.cornerRadius = 16;
    await makeText(btn, answers[i], 0, 0, W - 32, 62, 17, aTextColors[i], true, 'CENTER');
  }

  // Результат и кнопка продолжить (в Zone_Bottom — фиксированная зона 200px)
  const zBottom = makeFrame(sheet, 'Zone_Bottom', 0, sheetH - 200, W, 200);
  zBottom.fills = [];
  makeRect(zBottom, 'ResultFeedback', 16, 8, W - 32, 56, { r: 0.88, g: 0.97, b: 0.89 }, 1, 12);
  await makeText(zBottom, '✓  Правильно!', 32, 22, 200, 28, 15, C.correct, true);
  const cont = makeFrame(zBottom, 'BtnContinue', 16, 80, W - 32, 56);
  cont.fills = [solid(C.primary)]; cont.cornerRadius = 28;
  await makeText(cont, 'Продолжить', 0, 0, W - 32, 56, 16, C.white, true, 'CENTER');
}

// ── Сцена 3: Results ──────────────────────────────────────────────────
async function buildResults(page, x) {
  const f = makeFrame(page, '3 — Results', x, 0, W, H);
  f.fills = [solid(C.bg)];
  addStatusBar(f);
  await addHeader(f, 44, 'Результаты', false);

  await makeText(f, 'Отлично!', 0, 114, W, 54, 36, C.primary, true, 'CENTER');

  // Круг счёта
  makeEllipse(f, 'ScoreCircle', W / 2, 258, 78, C.primary);
  await makeText(f, '15/20', W / 2 - 78, 232, 156, 52, 28, C.white, true, 'CENTER');

  // Звёзды
  for (let i = 0; i < 3; i++) {
    makeRect(f, `Star_${i}`, W / 2 - 64 + i * 54, 356, 44, 44, C.secondary, 1, 8);
  }

  await makeText(f, 'Вы ответили правильно на 15 из 20 вопросов', 24, 418, W - 48, 48, 14, C.text, false, 'CENTER');
  await makeText(f, 'Лучший результат: 18 / 20', 0, 472, W, 28, 13, C.text2, false, 'CENTER');

  // Кнопки
  const b1 = makeFrame(f, 'BtnPlayAgain', 24, 528, W - 48, 56);
  b1.fills = [solid(C.primary)]; b1.cornerRadius = 28;
  await makeText(b1, 'Играть снова', 0, 0, W - 48, 56, 16, C.white, true, 'CENTER');

  const b2 = makeFrame(f, 'BtnMainMenu', 24, 596, W - 48, 52);
  b2.fills = []; b2.strokes = [solid(C.primary)]; b2.strokeWeight = 1.5; b2.cornerRadius = 26;
  await makeText(b2, 'Главное меню', 0, 0, W - 48, 52, 15, C.primary, true, 'CENTER');

  const b3 = makeFrame(f, 'BtnShare', 24, 660, W - 48, 52);
  b3.fills = []; b3.strokes = [solid(C.primary)]; b3.strokeWeight = 1.5; b3.cornerRadius = 26;
  await makeText(b3, 'Поделиться', 0, 0, W - 48, 52, 15, C.primary, false, 'CENTER');
}

// ── Сцена 4: Roadmap ──────────────────────────────────────────────────
async function buildRoadmap(page, x) {
  const f = makeFrame(page, '4 — Roadmap / Аркада', x, 0, W, H);
  f.fills = [solid(C.bg)];
  addStatusBar(f);
  await addHeader(f, 44, 'Аркада');

  // Змейка узлов
  const NR = 26;
  const nodes = [
    { x: 60,  y: 130 }, { x: 150, y: 130 }, { x: 240, y: 130 }, { x: 320, y: 200 },
    { x: 240, y: 270 }, { x: 150, y: 270 }, { x: 60,  y: 270 }, { x: 30,  y: 340 },
    { x: 60,  y: 410 }, { x: 150, y: 410 }, { x: 240, y: 410 }, { x: 320, y: 480 },
    { x: 240, y: 550 }, { x: 150, y: 550 }, { x: 60,  y: 550 },
  ];

  // Линии пути
  for (let i = 0; i < nodes.length - 1; i++) {
    const a = nodes[i], b = nodes[i + 1];
    const lx = Math.min(a.x, b.x) - NR / 2;
    const ly = Math.min(a.y, b.y) - NR / 2;
    const lw = Math.max(Math.abs(b.x - a.x) + NR, 6);
    const lh = Math.max(Math.abs(b.y - a.y) + NR, 6);
    makeRect(f, `PathLine_${i}`, lx, ly, lw, lh, { r: 0.72, g: 0.72, b: 0.72 }, 1, 3);
  }

  // Узлы
  for (let i = 0; i < nodes.length; i++) {
    const { x, y } = nodes[i];
    const color =
      i < 4  ? C.correct   :
      i === 4 ? C.wrong     :
      i < 7  ? C.secondary :
               { r: 0.86, g: 0.86, b: 0.86 };
    const textColor = i < 7 ? C.white : C.text;
    makeEllipse(f, `Node_${i + 1}`, x, y, NR, color);
    await makeText(f, String(i + 1), x - NR, y - 10, NR * 2, 20, 13, textColor, true, 'CENTER');
  }

  // Кнопка Завершить
  const fin = makeFrame(f, 'BtnFinish', W / 2 - 110, 700, 220, 56);
  fin.fills = [solid(C.secondary)]; fin.cornerRadius = 28;
  await makeText(fin, 'Завершить', 0, 0, 220, 56, 16, C.white, true, 'CENTER');
}

// ── Точка входа ───────────────────────────────────────────────────────
async function main() {
  // Предзагрузка шрифтов (обязательно до createText)
  await figma.loadFontAsync({ family: 'Inter', style: 'Regular' });
  await figma.loadFontAsync({ family: 'Inter', style: 'Bold' });

  // Страница
  let page = figma.root.children.find(p => p.name === 'UstAldan Quiz — UI');
  if (!page) {
    page = figma.createPage();
    page.name = 'UstAldan Quiz — UI';
  } else {
    page.children.slice().forEach(c => c.remove());
  }
  figma.currentPage = page;

  let x = 0;
  const step = W + GAP;

  await buildIntro(page, x);           x += step;
  await buildMainMenu(page, x);        x += step;
  await buildRecords(page, x);         x += step;
  await buildSettings(page, x);        x += step;
  await buildAbout(page, x);           x += step;
  await buildQuestionMap(page, x);     x += step;
  await buildQuestionWindow(page, x);  x += step;
  await buildResults(page, x);         x += step;
  await buildRoadmap(page, x);

  figma.viewport.scrollAndZoomIntoView(page.children);
  figma.closePlugin('✅ UstAldan Quiz UI создан! Страница: «UstAldan Quiz — UI»');
}

main().catch(err => figma.closePlugin('❌ Ошибка: ' + err.message));
