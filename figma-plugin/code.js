// UstAldan Quiz — Figma UI Generator
// Plugins → Development → Import plugin from manifest → выбери figma-plugin/manifest.json

const W = 390;
const H = 844;
const GAP = 80;

// ── Цвета ────────────────────────────────────────────────────────────
function hex(h) {
  const n = parseInt(h, 16);
  return { r: ((n >> 16) & 255) / 255, g: ((n >> 8) & 255) / 255, b: (n & 255) / 255 };
}
const C = {
  bg:       hex('F5F0E8'),
  primary:  hex('2D6040'),
  secondary:hex('C8A84B'),
  text:     hex('1A2A1A'),
  text2:    hex('4A6A4A'),
  correct:  hex('4CAF50'),
  wrong:    hex('F44336'),
  white:    { r:1, g:1, b:1 },
  black:    { r:0, g:0, b:0 },
  gradTop:  hex('0F2961'),
  gradBot:  hex('2D85D1'),
  rowAlt:   { r:0.97, g:0.97, b:0.97 },
};

// ── Базовые хелперы ───────────────────────────────────────────────────
function solid(color, opacity = 1) {
  return { type:'SOLID', color, opacity };
}

function makeFrame(parent, name, x, y, w, h, radius = 0) {
  const f = figma.createFrame();
  f.name = name; f.x = x; f.y = y;
  f.resize(Math.max(w, 1), Math.max(h, 1));
  f.fills = []; f.clipsContent = true;
  if (radius) f.cornerRadius = radius;
  if (parent) parent.appendChild(f);
  return f;
}

function makeRect(parent, name, x, y, w, h, color, opacity = 1, radius = 0) {
  const r = figma.createRectangle();
  r.name = name; r.x = x; r.y = y;
  r.resize(Math.max(w, 1), Math.max(h, 1));
  r.fills = [solid(color, opacity)];
  r.cornerRadius = radius;
  parent.appendChild(r);
  return r;
}

function makeGradient(parent, name, x, y, w, h, top, bot) {
  const r = figma.createRectangle();
  r.name = name; r.x = x; r.y = y; r.resize(w, h);
  r.fills = [{
    type: 'GRADIENT_LINEAR',
    gradientTransform: [[0,1,0],[-1,0,1]],
    gradientStops: [
      { position:0, color:{ r:top.r, g:top.g, b:top.b, a:1 } },
      { position:1, color:{ r:bot.r, g:bot.g, b:bot.b, a:1 } },
    ],
  }];
  parent.appendChild(r);
  return r;
}

async function txt(parent, str, x, y, w, h, size, color, bold=false, align='LEFT', opacity=1) {
  const t = figma.createText();
  t.fontName = { family:'Inter', style: bold ? 'Bold' : 'Regular' };
  t.characters = String(str);
  t.fontSize = size;
  t.fills = [solid(color, opacity)];
  t.textAlignHorizontal = align;
  t.x = x; t.y = y;
  if (w > 0) t.resize(w, h > 0 ? h : Math.max(t.height, size + 4));
  parent.appendChild(t);
  return t;
}

function ellipse(parent, name, cx, cy, r, color, opacity=1) {
  const e = figma.createEllipse();
  e.name = name; e.resize(r*2, r*2); e.x = cx-r; e.y = cy-r;
  e.fills = [solid(color, opacity)];
  parent.appendChild(e);
  return e;
}

// ── Переиспользуемые блоки ────────────────────────────────────────────

function statusBar(parent) {
  makeRect(parent, 'StatusBarCover', 0, 0, W, 44, C.black);
}

async function header(parent, y, title, back=true, rightText='', hh=56) {
  const h = makeFrame(parent, 'Header', 0, y, W, hh);
  h.fills = [solid(C.primary)];
  if (back) {
    makeRect(h, 'BtnBack', 10, (hh-36)/2, 36, 36, C.white, 0.2, 8);
    await txt(h, '‹', 18, (hh-40)/2, 20, 40, 26, C.white, true);
  }
  const tx = back ? 58 : 0;
  const tw = back ? W - 70 - (rightText ? 90 : 0) : W;
  await txt(h, title, tx, (hh-20)/2, tw, 20, 16, C.white, true, back ? 'LEFT' : 'CENTER');
  if (rightText) await txt(h, rightText, W-90, (hh-20)/2, 80, 20, 10, C.white, false, 'RIGHT');
  return h;
}

async function navBar(parent, activeIndex) {
  const navH = 84;
  const nav = makeFrame(parent, 'NavBar', 0, H-navH, W, navH);
  nav.fills = [solid(C.white)];
  nav.effects = [{type:'DROP_SHADOW',color:{r:0,g:0,b:0,a:0.1},offset:{x:0,y:-2},radius:8,spread:0,visible:true,blendMode:'NORMAL'}];
  const tabs = ['Профиль','Рекорды','Главная','О нас','Настройки'];
  const tabW = W/5;
  for (let i = 0; i < 5; i++) {
    const active = i === activeIndex;
    const col = active ? C.primary : {r:0.6,g:0.6,b:0.6};
    makeRect(nav, `Icon_${i}`, i*tabW + tabW/2-12, 10, 24, 24, col, 1, 12);
    await txt(nav, tabs[i], i*tabW, 40, tabW, 18, 9, col, active, 'CENTER');
  }
}

async function primaryBtn(parent, label, x, y, w, h) {
  const f = makeFrame(parent, `Btn_${label}`, x, y, w, h, Math.round(h/2));
  f.fills = [solid(C.primary)];
  await txt(f, label, 0, 0, w, h, 14, C.white, true, 'CENTER');
  return f;
}

async function outlineBtn(parent, label, x, y, w, h) {
  const f = makeFrame(parent, `Btn_${label}`, x, y, w, h, Math.round(h/2));
  f.fills = []; f.strokes = [solid(C.primary)]; f.strokeWeight = 1.5;
  await txt(f, label, 0, 0, w, h, 13, C.primary, true, 'CENTER');
  return f;
}

// Попап поверх градиентного фона
async function overlayPopup(page, name, x, cardW, cardH, buildCard) {
  const f = makeFrame(page, name, x, 0, W, H);
  makeGradient(f, 'Background', 0, 0, W, H, C.gradTop, C.gradBot);
  makeRect(f, 'Overlay', 0, 0, W, H, C.black, 0.5);
  const cx = (W - cardW) / 2, cy = (H - cardH) / 2;
  const card = makeFrame(f, 'Card', cx, cy, cardW, cardH, 20);
  card.fills = [solid(C.white)];
  await buildCard(card, cardW, cardH);
}

// ── 0 — Intro ─────────────────────────────────────────────────────────
async function buildIntro(page, x) {
  const f = makeFrame(page, '0 — Intro', x, 0, W, H);
  f.fills = [solid(C.black)];
  makeRect(f, 'VideoBg', 0, 0, W, H, {r:0.08,g:0.08,b:0.08});
  ellipse(f, 'PlayIcon', W/2, H/2, 44, C.white, 0.15);
  makeRect(f, 'PlayTriangle', W/2-10, H/2-14, 28, 28, C.white, 0.5, 4);
  statusBar(f);
  await txt(f, 'Нажмите, чтобы пропустить', 0, H-52, W, 28, 12, C.white, false, 'CENTER', 0.5);
}

// ── 1a — MainMenu / Главная ───────────────────────────────────────────
async function buildMainMenu(page, x) {
  const f = makeFrame(page, '1a — MainMenu / Главная', x, 0, W, H);
  makeGradient(f, 'Background', 0, 0, W, H, C.gradTop, C.gradBot);
  statusBar(f);

  // Сетка категорий (logo block убран — пустое пространство)
  const cats = ['История','Культура','Природа','Спорт','Люди','События'];
  const cW=172, cH=58, cGapX=12, cGapY=10;
  const gridX=(W - cW*2 - cGapX)/2, gridY=56;
  for (let i=0; i<cats.length; i++) {
    const col=i%2, row=Math.floor(i/2);
    const btn = makeFrame(f, `Cat_${cats[i]}`, gridX+col*(cW+cGapX), gridY+row*(cH+cGapY), cW, cH, 14);
    btn.fills = [solid(C.white, 0.88)];
    await txt(btn, cats[i], 0, 0, cW, cH, 13, C.primary, true, 'CENTER');
  }

  const statsY = gridY + 3*(cH+cGapY) + 10;
  await txt(f, 'Сыграно: 0  •  Рекорд: 0/0  •  Вопросов: 0', 0, statsY, W, 20, 10, C.white, false, 'CENTER', 0.6);
  await primaryBtn(f, 'Начать игру', W/2-120, statsY+28, 240, 56);
  const arc = makeFrame(f, 'BtnArcade', W/2-90, statsY+94, 180, 44, 22);
  arc.fills = [solid(C.white, 0.15)];
  await txt(arc, 'Аркада', 0, 0, 180, 44, 12, C.white, false, 'CENTER');

  await navBar(f, 2);
}

// ── 1b — MainMenu / Рекорды ───────────────────────────────────────────
async function buildRecords(page, x) {
  const f = makeFrame(page, '1b — MainMenu / Рекорды', x, 0, W, H);
  makeGradient(f, 'Background', 0, 0, W, H, C.gradTop, C.gradBot);
  statusBar(f);
  await header(f, 44, 'Рекорды', false);

  const card = makeFrame(f, 'RecordsCard', 0, 100, W, H-100-84);
  card.fills = [solid(C.white)];
  const cats=['История','Культура','Природа','Спорт','Люди','События'];
  const scores=['15 / 20','8 / 15','0 / 12','12 / 20','0 / 10','5 / 18'];
  for (let i=0; i<cats.length; i++) {
    const ry = i*58;
    if (i%2===0) makeRect(card, `RowBg_${i}`, 0, ry, W, 56, C.rowAlt);
    await txt(card, cats[i], 20, ry+17, 200, 22, 13, C.text);
    await txt(card, scores[i], W-110, ry+17, 90, 22, 13, C.primary, true, 'RIGHT');
  }
  await navBar(f, 1);
}

// ── 1c — MainMenu / Настройки ─────────────────────────────────────────
async function buildSettings(page, x) {
  const f = makeFrame(page, '1c — MainMenu / Настройки', x, 0, W, H);
  makeGradient(f, 'Background', 0, 0, W, H, C.gradTop, C.gradBot);
  statusBar(f);
  await header(f, 44, 'Настройки', false);

  // App info
  const appSec = makeFrame(f, 'AppInfo', 0, 100, W, 44);
  appSec.fills = [solid(C.primary)];
  await txt(appSec, 'Викторина', 0, 4, W, 20, 13, C.white, true, 'CENTER');
  await txt(appSec, 'v1.0', 0, 26, W, 14, 9, C.white, false, 'CENTER', 0.6);

  // TabBar: Настройки / Игра / Безопасность
  const tabBarY = 144;
  const tabBar = makeFrame(f, 'TabBar', 0, tabBarY, W, 40);
  tabBar.fills = [solid(C.primary)];
  const tabLabels = ['Настройки','Игра','Безопасность'];
  for (let i=0; i<3; i++) {
    const active = i===0;
    const tw = W/3;
    if (active) makeRect(tabBar, `TabActive`, i*tw, 34, tw, 4, C.secondary);
    await txt(tabBar, tabLabels[i], i*tw, 8, tw, 22, 11, C.white, active, 'CENTER', active ? 1 : 0.6);
  }

  // Cards grid (2 cols × 2 rows)
  const cardW = (W - 48 - 16) / 2;  // ≈ 163
  const cardH = 88;
  const cards = [
    { label:'Музыка',   color:hex('F0A33B') },
    { label:'Звук',     color:hex('4A8FE2') },
    { label:'Вибрация', color:hex('8C5CF5') },
    { label:'Язык',     color:C.primary     },
  ];
  const gridTop = tabBarY + 40 + 14;
  for (let i=0; i<4; i++) {
    const col=i%2, row=Math.floor(i/2);
    const cx = 24 + col*(cardW+16);
    const cy = gridTop + row*(cardH+12);
    const card = makeFrame(f, `Card_${cards[i].label}`, cx, cy, cardW, cardH, 16);
    card.fills = [solid(cards[i].color)];
    makeRect(card, 'Icon', (cardW-28)/2, 12, 28, 28, C.white, 0.25, 8);
    await txt(card, cards[i].label, 0, 48, cardW, 18, 11, C.white, true, 'CENTER');
    if (i===3) {
      await txt(card, 'RU / SAH', 0, 66, cardW, 16, 9, C.white, false, 'CENTER', 0.8);
    } else {
      makeRect(card, 'Toggle', cardW-38, 64, 28, 16, C.white, 0.85, 8);
    }
  }

  // Sliders
  const sliderTop = gridTop + 2*(cardH+12) + 12;
  for (let i=0; i<2; i++) {
    const sy = sliderTop + i*52;
    makeRect(f, `SliderRow_${i}`, 24, sy, W-48, 44, {r:0.96,g:0.97,b:0.96}, 1, 10);
    await txt(f, i===0 ? 'Музыка' : 'Звук', 36, sy+13, 100, 18, 12, C.text);
    const val = i===0 ? 0.75 : 0.5;
    const trackX=148, trackY=sy+21, trackW=W-48-148-16;
    makeRect(f, 'Track', trackX, trackY, trackW, 6, {r:0.82,g:0.82,b:0.82}, 1, 3);
    makeRect(f, 'Fill', trackX, trackY, Math.round(trackW*val), 6, C.primary, 1, 3);
    ellipse(f, 'Thumb', trackX+Math.round(trackW*val), trackY+3, 8, C.primary);
  }

  await navBar(f, 4);
}

// ── 1d — MainMenu / О приложении ─────────────────────────────────────
async function buildAbout(page, x) {
  const f = makeFrame(page, '1d — MainMenu / О приложении', x, 0, W, H);
  makeGradient(f, 'Background', 0, 0, W, H, C.gradTop, C.gradBot);
  statusBar(f);
  await header(f, 44, 'О приложении', false);

  const card = makeFrame(f, 'AboutCard', 0, 100, W, H-100-84);
  card.fills = [solid(C.white)];
  await txt(card, 'Викторина к 100-летию\nУсть-Алданского улуса.', 24, 24, W-48, 52, 14, C.text);
  await txt(card, 'Вопросы об истории, культуре и природе района.', 24, 84, W-48, 38, 12, C.text2);
  await txt(card, 'Разработчик: ...', 24, 132, W-48, 22, 11, C.text2);
  await txt(card, 'v1.0  •  2025', 24, 156, W-48, 20, 10, C.text2);
  await outlineBtn(card, 'Предложить вопрос', 24, 210, W-48, 48);
  await primaryBtn(card, 'Посетить сайт', 24, 270, W-48, 48);
  await navBar(f, 3);
}

// ── 1e — MainMenu / Профиль ───────────────────────────────────────────
async function buildProfile(page, x) {
  const f = makeFrame(page, '1e — MainMenu / Профиль', x, 0, W, H);
  makeGradient(f, 'Background', 0, 0, W, H, C.gradTop, C.gradBot);
  statusBar(f);
  await header(f, 44, 'Профиль', false);

  const card = makeFrame(f, 'ProfileCard', 0, 100, W, H-100-84);
  card.fills = [solid(C.white)];
  await txt(card, 'Профиль пользователя', 24, 28, W-48, 26, 15, C.text, true);
  await txt(card, 'Здесь будет информация о прогрессе.', 24, 62, W-48, 36, 12, C.text2);
  await navBar(f, 0);
}

// ── 2 — QuestionMap ───────────────────────────────────────────────────
async function buildQuestionMap(page, x) {
  const f = makeFrame(page, '2 — QuestionMap', x, 0, W, H);
  f.fills = [solid(C.bg)];
  statusBar(f);
  await header(f, 44, 'История', true, '0 / 15');

  const tW=100, tH=68, cols=3, gx=14, gy=14;
  const startX = (W - cols*tW - (cols-1)*gx)/2;
  const startY = 116;
  const stateColors = [C.correct, C.correct, C.wrong, C.secondary, C.secondary];

  for (let i=0; i<15; i++) {
    const col=i%cols, row=Math.floor(i/cols);
    const tx=startX+col*(tW+gx), ty=startY+row*(tH+gy);
    const color = i < stateColors.length ? stateColors[i] : {r:0.88,g:0.88,b:0.88};
    const textColor = i < stateColors.length ? C.white : C.text;
    const tile = makeFrame(f, `Tile_${i+1}`, tx, ty, tW, tH, 12);
    tile.fills = [solid(color)];
    await txt(tile, String(i+1), 0, 0, tW, tH, 17, textColor, true, 'CENTER');
  }
}

// ── 2b — QuestionWindow (попап) ───────────────────────────────────────
async function buildQuestionWindow(page, x) {
  const f = makeFrame(page, '2b — QuestionWindow', x, 0, W, H);
  f.fills = [solid(C.bg)];
  statusBar(f);
  await header(f, 44, 'История', true, '2 / 15');
  makeRect(f, 'Overlay', 0, 0, W, H, C.black, 0.55);

  const sx=Math.round(W*0.04), sy=Math.round(H*0.05);
  const sw=W-sx*2, shBottom=Math.round(H*0.88);
  const sh=shBottom-sy;
  const sheet = makeFrame(f, 'Sheet', sx, sy, sw, sh, 20);
  sheet.fills = [solid(C.white)];

  // BtnClose (верхний правый угол overlay)
  makeRect(f, 'BtnClose', sx+sw-4, shBottom-4, 44, 44, C.white, 0.9, 22);
  await txt(f, '✕', sx+sw-4, shBottom-4, 44, 44, 15, {r:0.4,g:0.4,b:0.4}, false, 'CENTER');

  let oy = 14;

  // Zone_Question: содержит Zone_Media + QuestionText
  const zqH = Math.round(sh * 0.44);
  const zq = makeFrame(sheet, 'Zone_Question', 0, oy, sw, zqH, 12);
  zq.fills = [solid({r:0.99,g:0.99,b:0.99})];
  zq.strokes = [solid({r:0.9,g:0.9,b:0.9})]; zq.strokeWeight = 1;

  // Zone_Media (скрытая зона картинки внутри Zone_Question)
  const zmH = 80;
  const zm = makeFrame(zq, 'Zone_Media [скрытая]', 0, 0, sw, zmH);
  zm.fills = [solid({r:0.14,g:0.14,b:0.14})];
  zm.opacity = 0.35;
  makeRect(zm, 'QuestionImage', 8, 6, sw-16, zmH-12, {r:0.3,g:0.3,b:0.3}, 0.6, 6);
  makeRect(zm, 'Spinner', sw/2-14, zmH/2-14, 28, 28, C.white, 0.25, 14);

  await txt(zq, 'В каком году был основан\nУсть-Алданский район?', 14, zmH+10, sw-28, zqH-zmH-20, 17, C.text, false, 'CENTER');
  oy += zqH + 12;

  // Zone_Answers
  const aH=38, aGap=6;
  const answers=['A: 1930','B: 1935','C: 1940','D: 1925'];
  const aColors=[C.correct,{r:0.93,g:0.93,b:0.93},{r:0.93,g:0.93,b:0.93},{r:0.93,g:0.93,b:0.93}];
  const aTxt=[C.white,C.text,C.text,C.text];
  const za = makeFrame(sheet, 'Zone_Answers', 0, oy, sw, 4*aH+3*aGap+8);
  za.fills = [];
  for (let i=0; i<4; i++) {
    const btn = makeFrame(za, `AnswerBtn_${i}`, 0, 4+i*(aH+aGap), sw, aH, 10);
    btn.fills = [solid(aColors[i])];
    btn.strokes = [solid({r:0.9,g:0.9,b:0.9})]; btn.strokeWeight = 1;
    await txt(btn, answers[i], 12, 0, sw-24, aH, 13, aTxt[i], false, 'LEFT');
  }
  oy += 4*(aH+aGap)+8+12;

  // Zone_Bottom (260px фиксированная)
  const zbH = sh - oy;
  const zb = makeFrame(sheet, 'Zone_Bottom', 0, oy, sw, zbH);
  zb.fills = [];
  makeRect(zb, 'ResultFeedback', 4, 4, sw-8, 32, {r:0.88,g:0.97,b:0.89}, 1, 8);
  await txt(zb, '✓  Правильно!', 14, 10, 180, 20, 12, C.correct, true);
  const bcW = Math.min(sw-32, 200);
  const cont = makeFrame(zb, 'BtnContinue', (sw-bcW)/2, 44, bcW, 52, 26);
  cont.fills = [solid(C.primary)];
  await txt(cont, 'Продолжить', 0, 0, bcW, 52, 14, C.white, true, 'CENTER');
}

// ── 2c — FactPopup ────────────────────────────────────────────────────
async function buildFactPopup(page, x) {
  await overlayPopup(page, '2c — FactPopup', x, 330, 240, async (card, cw, ch) => {
    await txt(card, 'Интересный факт', 0, 18, cw, 26, 15, C.text, true, 'CENTER');
    makeRect(card, 'Divider', 24, 52, cw-48, 1, {r:0.9,g:0.9,b:0.9});
    await txt(card, 'Усть-Алданский район основан в 1930 году. Административный центр — село Бордон.', 20, 62, cw-40, 70, 12, C.text2, false, 'LEFT');
    const btn = makeFrame(card, 'BtnOk', (cw-180)/2, 148, 180, 48, 24);
    btn.fills = [solid(C.primary)];
    await txt(btn, 'Понятно', 0, 0, 180, 48, 14, C.white, true, 'CENTER');
  });
}

// ── 2d — NoQuestionsPopup ─────────────────────────────────────────────
async function buildNoQuestionsPopup(page, x) {
  await overlayPopup(page, '2d — NoQuestionsPopup', x, 320, 230, async (card, cw, ch) => {
    await txt(card, '!', 0, 14, cw, 44, 36, C.secondary, true, 'CENTER');
    await txt(card, 'Нет вопросов', 0, 62, cw, 26, 16, C.text, true, 'CENTER');
    await txt(card, 'В этой категории пока нет вопросов', 12, 94, cw-24, 32, 11, C.text2, false, 'CENTER');
    const btn = makeFrame(card, 'BtnClose', (cw-180)/2, 140, 180, 48, 24);
    btn.fills = [solid(C.primary)];
    await txt(btn, 'Понятно', 0, 0, 180, 48, 14, C.white, true, 'CENTER');
  });
}

// ── 2e — ConfirmResetPopup ────────────────────────────────────────────
async function buildConfirmPopup(page, x) {
  await overlayPopup(page, '2e — ConfirmResetPopup', x, 330, 188, async (card, cw, ch) => {
    await txt(card, 'Вы уверены?', 0, 20, cw, 26, 16, C.text, true, 'CENTER');
    await txt(card, 'Весь прогресс будет удалён', 0, 52, cw, 22, 12, C.text2, false, 'CENTER');
    const bw=140, bh=44, gap=12;
    const totalW = bw*2+gap;
    const btnYes = makeFrame(card, 'BtnYes', (cw-totalW)/2, 108, bw, bh, 22);
    btnYes.fills = [solid(hex('E53935'))];
    await txt(btnYes, 'Да', 0, 0, bw, bh, 14, C.white, true, 'CENTER');
    const btnNo = makeFrame(card, 'BtnNo', (cw-totalW)/2+bw+gap, 108, bw, bh, 22);
    btnNo.fills = [solid(C.primary)];
    await txt(btnNo, 'Нет', 0, 0, bw, bh, 14, C.white, true, 'CENTER');
  });
}

// ── 2f — SuggestPanel (снизу) ─────────────────────────────────────────
async function buildSuggestPanel(page, x) {
  const f = makeFrame(page, '2f — SuggestPanel', x, 0, W, H);
  makeGradient(f, 'Background', 0, 0, W, H, C.gradTop, C.gradBot);
  makeRect(f, 'Overlay', 0, 0, W, H, C.black, 0.5);

  const sheetH = 480;
  const sheet = makeFrame(f, 'Sheet', 0, H-sheetH, W, sheetH, 20);
  sheet.fills = [solid(C.white)];
  await txt(sheet, 'Предложить вопрос', 0, 18, W, 26, 16, C.text, true, 'CENTER');
  makeRect(sheet, 'Divider', 24, 52, W-48, 1, {r:0.9,g:0.9,b:0.9});

  const fields=['Текст вопроса','Вариант A (правильный)','Вариант B','Вариант C','Вариант D'];
  for (let i=0; i<fields.length; i++) {
    const fy = 62 + i*58;
    makeRect(sheet, `Field_${i}`, 24, fy, W-48, 46, {r:0.96,g:0.96,b:0.96}, 1, 10);
    await txt(sheet, fields[i], 36, fy+13, W-72, 20, 11, {r:0.6,g:0.6,b:0.6});
  }
  const sendBtn = makeFrame(sheet, 'BtnSend', 24, 62+5*58+12, W-48, 52, 26);
  sendBtn.fills = [solid(C.primary)];
  await txt(sendBtn, 'Отправить', 0, 0, W-48, 52, 14, C.white, true, 'CENTER');
}

// ── 3 — Results ───────────────────────────────────────────────────────
async function buildResults(page, x) {
  const f = makeFrame(page, '3 — Results', x, 0, W, H);
  f.fills = [solid(C.bg)];
  statusBar(f);
  await header(f, 44, 'Результаты', false);

  await txt(f, 'Отлично!', 0, 114, W, 52, 32, C.primary, true, 'CENTER');
  ellipse(f, 'ScoreCircle', W/2, 258, 76, C.primary);
  await txt(f, '15/20', W/2-76, 230, 152, 56, 24, C.white, true, 'CENTER');
  for (let i=0; i<3; i++) makeRect(f, `Star_${i}`, W/2-60+i*54, 354, 44, 44, C.secondary, 1, 8);
  await txt(f, 'Вы ответили правильно на 15 из 20 вопросов', 24, 416, W-48, 46, 13, C.text, false, 'CENTER');
  await txt(f, 'Лучший результат: 18 / 20', 0, 468, W, 26, 12, C.text2, false, 'CENTER');
  await primaryBtn(f, 'Играть снова', 24, 516, W-48, 56);
  await outlineBtn(f, 'Главное меню', 24, 584, W-48, 50);
  await outlineBtn(f, 'Поделиться', 24, 646, W-48, 50);
}

// ── 4 — Roadmap ───────────────────────────────────────────────────────
async function buildRoadmap(page, x) {
  const f = makeFrame(page, '4 — Roadmap / Аркада', x, 0, W, H);
  f.fills = [solid(C.bg)];
  statusBar(f);

  const hdr = makeFrame(f, 'Header', 0, 44, W, 56);
  hdr.fills = [solid(C.primary)];
  makeRect(hdr, 'BtnBack', 10, 10, 36, 36, C.white, 0.2, 8);
  await txt(hdr, '‹', 18, 8, 20, 40, 26, C.white, true);
  await txt(hdr, 'Аркада', 56, 15, W-120, 26, 16, C.white, true, 'CENTER');
  makeRect(hdr, 'BtnReset', W-46, 10, 36, 36, C.white, 0.2, 8);
  await txt(hdr, '↺', W-46, 14, 36, 28, 16, C.white, true, 'CENTER');

  // Progress bar
  const pbY=110;
  makeRect(f, 'ProgressBg', 40, pbY, W-80, 8, {r:0.82,g:0.82,b:0.82}, 1, 4);
  makeRect(f, 'ProgressFill', 40, pbY, Math.round((W-80)*0.35), 8, C.primary, 1, 4);

  // Тайлы-змейка
  const NR=24;
  const nodes=[
    {x:60,y:140},{x:150,y:140},{x:240,y:140},{x:320,y:210},
    {x:240,y:280},{x:150,y:280},{x:60,y:280},{x:30,y:350},
    {x:60,y:420},{x:150,y:420},{x:240,y:420},{x:320,y:490},
    {x:240,y:560},{x:150,y:560},{x:60,y:560},
  ];
  for (let i=0; i<nodes.length-1; i++) {
    const a=nodes[i], b=nodes[i+1];
    const lx=Math.min(a.x,b.x)-NR/2, ly=Math.min(a.y,b.y)-NR/2;
    const lw=Math.max(Math.abs(b.x-a.x)+NR,6), lh=Math.max(Math.abs(b.y-a.y)+NR,6);
    makeRect(f, `Line_${i}`, lx, ly, lw, lh, {r:0.75,g:0.75,b:0.75}, 1, 3);
  }
  for (let i=0; i<nodes.length; i++) {
    const {x:nx,y:ny}=nodes[i];
    const color = i<4 ? C.correct : i===4 ? C.wrong : i<7 ? C.secondary : {r:0.86,g:0.86,b:0.86};
    const tc = i<7 ? C.white : C.text;
    const tile = makeFrame(f, `Tile_${i+1}`, nx-NR, ny-NR, NR*2, NR*2, 10);
    tile.fills = [solid(color)];
    await txt(tile, String(i+1), 0, NR-9, NR*2, 18, 12, tc, true, 'CENTER');
  }
}

// ── 4b — QuestionWindowFull (Roadmap) ────────────────────────────────
async function buildQuestionWindowFull(page, x) {
  const f = makeFrame(page, '4b — QuestionWindowFull', x, 0, W, H);
  f.fills = [solid({r:0.06,g:0.06,b:0.06})];
  statusBar(f);

  // Header
  const hdr = makeFrame(f, 'Header', 0, 44, W, 56);
  hdr.fills = [solid(C.primary)];
  makeRect(hdr, 'BtnBack', 8, 10, 36, 36, C.white, 0.2, 8);
  await txt(hdr, '‹', 16, 8, 20, 40, 26, C.white, true);
  await txt(hdr, 'История', 54, 15, W-160, 26, 15, C.white, true, 'CENTER');
  await txt(hdr, '5 / 20 ✓', W-94, 18, 82, 20, 10, C.white, false, 'RIGHT');

  // ImageZone
  const imgY=100, imgH=200;
  const imgZone = makeFrame(f, 'ImageZone', 0, imgY, W, imgH);
  imgZone.fills = [solid({r:0.14,g:0.14,b:0.14})];
  makeRect(imgZone, 'QuestionImage', 12, 10, W-24, imgH-20, {r:0.3,g:0.3,b:0.3}, 0.7, 8);
  makeRect(imgZone, 'Spinner', W/2-14, imgH/2-14, 28, 28, C.white, 0.2, 14);

  // ContentSheet (белый, скруглённый сверху)
  const csY=imgY+imgH, csH=H-csY;
  const cs = makeFrame(f, 'ContentSheet', 0, csY, W, csH+20, 20);
  cs.fills = [solid(C.white)]; cs.clipsContent = true;

  let oy=18;

  // QuestionCard
  const qcH=86;
  const qc = makeFrame(cs, 'QuestionCard', 14, oy, W-28, qcH, 10);
  qc.fills = [solid({r:0.96,g:0.96,b:0.96})];
  await txt(qc, 'В каком году был основан Усть-Алданский район?', 14, 12, W-56, qcH-24, 14, C.text, false, 'LEFT');
  oy += qcH+10;

  // Ответы
  const aH=38, aGap=8;
  const answers=['A: 1930','B: 1935','C: 1940','D: 1925'];
  const aColors=[C.correct,{r:0.93,g:0.93,b:0.93},{r:0.93,g:0.93,b:0.93},{r:0.93,g:0.93,b:0.93}];
  const aTxt=[C.white,C.text,C.text,C.text];
  for (let i=0; i<4; i++) {
    const btn = makeFrame(cs, `Answer_${i}`, 14, oy+i*(aH+aGap), W-28, aH, 10);
    btn.fills = [solid(aColors[i])];
    await txt(btn, answers[i], 12, 0, W-52, aH, 13, aTxt[i], false, 'LEFT');
  }
  oy += 4*(aH+aGap)+10;

  // Zone_Bottom
  makeRect(cs, 'FeedbackPanel', 14, oy, W-28, 34, {r:0.88,g:0.97,b:0.89}, 1, 8);
  await txt(cs, '✓  Правильно!', 26, oy+8, 180, 18, 12, C.correct, true);
  const cont = makeFrame(cs, 'BtnContinue', (W-200)/2, oy+46, 200, 52, 26);
  cont.fills = [solid(C.primary)];
  await txt(cont, 'Продолжить', 0, 0, 200, 52, 14, C.white, true, 'CENTER');
}

// ── Точка входа ───────────────────────────────────────────────────────
async function main() {
  await figma.loadFontAsync({ family:'Inter', style:'Regular' });
  await figma.loadFontAsync({ family:'Inter', style:'Bold' });

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

  await buildIntro(page, x);               x += step;
  await buildMainMenu(page, x);            x += step;
  await buildRecords(page, x);             x += step;
  await buildSettings(page, x);            x += step;
  await buildAbout(page, x);               x += step;
  await buildProfile(page, x);             x += step;
  await buildQuestionMap(page, x);         x += step;
  await buildQuestionWindow(page, x);      x += step;
  await buildFactPopup(page, x);           x += step;
  await buildNoQuestionsPopup(page, x);    x += step;
  await buildConfirmPopup(page, x);        x += step;
  await buildSuggestPanel(page, x);        x += step;
  await buildResults(page, x);             x += step;
  await buildRoadmap(page, x);             x += step;
  await buildQuestionWindowFull(page, x);

  figma.viewport.scrollAndZoomIntoView(page.children);
  figma.closePlugin('✅ UstAldan Quiz UI обновлён! 15 экранов на странице «UstAldan Quiz — UI»');
}

main().catch(err => figma.closePlugin('❌ Ошибка: ' + err.message));
