const STORE_URL = ""; // Вставьте сюда ссылку на Steam / VK Play / другую страницу игры перед публикацией.

const header = document.querySelector('.site-header');
const menuToggle = document.querySelector('#menuToggle');
const nav = document.querySelector('#mainNav');
const progress = document.querySelector('#scrollProgress');
const modal = document.querySelector('#storeModal');
const modalClose = document.querySelector('.modal-close');
const modalOk = document.querySelector('.modal-ok');
const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

function updateScrollUI(){
  const y = window.scrollY || document.documentElement.scrollTop;
  const max = Math.max(1, document.documentElement.scrollHeight - window.innerHeight);
  header?.classList.toggle('scrolled', y > 28);
  if (progress) progress.style.width = `${Math.min(100, (y / max) * 100)}%`;
}
updateScrollUI();
window.addEventListener('scroll', updateScrollUI, {passive:true});

menuToggle?.addEventListener('click', () => {
  const open = header.classList.toggle('open');
  menuToggle.setAttribute('aria-expanded', String(open));
});
nav?.querySelectorAll('a').forEach(link => link.addEventListener('click', () => {
  header?.classList.remove('open');
  menuToggle?.setAttribute('aria-expanded', 'false');
}));

document.querySelectorAll('.js-store').forEach(button => button.addEventListener('click', () => {
  if (STORE_URL) window.open(STORE_URL, '_blank', 'noopener,noreferrer');
  else if (typeof modal?.showModal === 'function') modal.showModal();
}));
modalClose?.addEventListener('click', () => modal.close());
modalOk?.addEventListener('click', () => modal.close());
modal?.addEventListener('click', event => { if (event.target === modal) modal.close(); });

if ('IntersectionObserver' in window){
  const revealObserver = new IntersectionObserver(entries => {
    entries.forEach(entry => {
      if (entry.isIntersecting){
        entry.target.classList.add('is-visible');
        revealObserver.unobserve(entry.target);
      }
    });
  }, {threshold:.11, rootMargin:'0px 0px -35px'});
  document.querySelectorAll('.reveal').forEach(el => revealObserver.observe(el));
} else {
  document.querySelectorAll('.reveal').forEach(el => el.classList.add('is-visible'));
}

const sections = [...document.querySelectorAll('main section[id]')];
const navLinks = [...document.querySelectorAll('.main-nav a')];
if ('IntersectionObserver' in window){
  const navObserver = new IntersectionObserver(entries => {
    entries.forEach(entry => {
      if (!entry.isIntersecting) return;
      navLinks.forEach(link => link.classList.toggle('active', link.getAttribute('href') === `#${entry.target.id}`));
    });
  }, {rootMargin:'-40% 0px -50% 0px', threshold:0});
  sections.forEach(section => navObserver.observe(section));
}

const glow = document.querySelector('.cursor-glow');
if (!reduceMotion){
  window.addEventListener('pointermove', event => {
    if (!glow) return;
    glow.style.left = `${event.clientX}px`;
    glow.style.top = `${event.clientY}px`;
  }, {passive:true});

  const parallaxEls = [...document.querySelectorAll('.parallax')];
  window.addEventListener('pointermove', event => {
    const nx = (event.clientX / window.innerWidth - .5) * 2;
    const ny = (event.clientY / window.innerHeight - .5) * 2;
    parallaxEls.forEach(el => {
      const depth = Number(el.dataset.depth || 8);
      const base = el.classList.contains('hero-door') ? '' : '';
      el.style.translate = `${nx * depth * .45}px ${ny * depth * .32}px`;
    });
  }, {passive:true});
}

const retroTabs = [...document.querySelectorAll('.retro-tab')];
const retroPanels = [...document.querySelectorAll('.retro-panel')];
retroTabs.forEach(tab => tab.addEventListener('click', () => {
  retroTabs.forEach(item => {
    item.classList.remove('active');
    item.setAttribute('aria-selected', 'false');
  });
  retroPanels.forEach(panel => panel.classList.remove('active'));
  tab.classList.add('active');
  tab.setAttribute('aria-selected', 'true');
  document.querySelector(`.retro-panel[data-name="${tab.dataset.panel}"]`)?.classList.add('active');
}));

const dayStates = [
  {time:'06:00', phase:'Открытие', heading:'Свет включается первым', description:'Поднять ставни, проверить остатки, включить компьютер и успеть сделать первый чай до ранних покупателей.', queue:'низкая', tempo:'тихий', light:'рассвет', bg:'linear-gradient(#d8ae6f,#f3d99b 57%,#9a6442)', sun:{left:'12%',top:'30%',size:'62px'}},
  {time:'09:30', phase:'Утренний поток', heading:'Район просыпается', description:'Батоны, сдача, короткие разговоры и знакомые лица. Очередь уже не даёт надолго отвлекаться.', queue:'средняя', tempo:'быстрый', light:'утро', bg:'linear-gradient(#7fb2d2,#d7dcb9 58%,#b78b59)', sun:{left:'32%',top:'18%',size:'68px'}},
  {time:'13:00', phase:'Поставка', heading:'Курьер уже у двери', description:'Новый товар приезжает как раз тогда, когда полки начинают пустеть. Нужно принять поставку и не потерять темп торговли.', queue:'высокая', tempo:'деловой', light:'день', bg:'linear-gradient(#6ba8d6,#b8d6dc 60%,#9d8059)', sun:{left:'54%',top:'11%',size:'72px'}},
  {time:'17:40', phase:'После работы', heading:'Вечер собирает всех', description:'Люди возвращаются домой, у окна задерживаются разговоры, а привычные заказы звучат один за другим.', queue:'высокая', tempo:'оживлённый', light:'закат', bg:'linear-gradient(#876783,#d78f64 56%,#703d2f)', sun:{left:'76%',top:'28%',size:'64px'}},
  {time:'20:00', phase:'Закрытие', heading:'Последний чек и тишина', description:'Подвести итог, проверить склад, закрыть день и оставить тёплый свет только на пару минут — до завтра.', queue:'нет', tempo:'тихий', light:'ночь', bg:'linear-gradient(#162238,#2c3850 58%,#30241f)', sun:{left:'87%',top:'56%',size:'48px'}}
];
const dayRange = document.querySelector('#dayRange');
const daySky = document.querySelector('#daySky');
const skySun = document.querySelector('.sky-sun');
function applyDayState(index){
  const state = dayStates[index] || dayStates[0];
  document.querySelector('#dayTime').textContent = state.time;
  document.querySelector('#dayPhase').textContent = state.phase;
  document.querySelector('#dayHeading').textContent = state.heading;
  document.querySelector('#dayDescription').textContent = state.description;
  document.querySelector('#metricQueue').textContent = state.queue;
  document.querySelector('#metricTempo').textContent = state.tempo;
  document.querySelector('#metricLight').textContent = state.light;
  if (daySky) daySky.style.background = state.bg;
  if (skySun){
    skySun.style.left = state.sun.left;
    skySun.style.top = state.sun.top;
    skySun.style.width = state.sun.size;
    skySun.style.height = state.sun.size;
    skySun.style.opacity = index === 4 ? '.46' : '1';
  }
}
dayRange?.addEventListener('input', event => applyDayState(Number(event.target.value)));
applyDayState(0);

const tvScreen = document.querySelector('#tvScreen');
const channelButtons = [...document.querySelectorAll('.channel')];
const tvMeta = document.querySelector('#tvMeta');
const channels = {
  news:{src:'assets/tv-news.webp', meta:'ЭФИР · 18:42'},
  weather:{src:'assets/tv-weather.webp', meta:'ПОГОДА · 18:45'},
  soda:{src:'assets/tv-soda.webp', meta:'РЕКЛАМА · 18:47'},
  rock:{src:'assets/tv-rock.webp', meta:'КОНЦЕРТ · 19:10'}
};
let tvFrame = 0;
let activeChannel = 'news';
let tvTimer;
function applyChannel(){
  if (!tvScreen) return;
  tvScreen.style.backgroundImage = `url("${channels[activeChannel].src}")`;
  if (tvMeta) tvMeta.textContent = channels[activeChannel].meta;
}
function drawTvFrame(){
  if (!tvScreen) return;
  const x = tvFrame % 4;
  const y = Math.floor(tvFrame / 4);
  tvScreen.style.backgroundPosition = `${x * 33.3333}% ${y * 100}%`;
  tvFrame = (tvFrame + 1) % 8;
}
function startTv(){ clearInterval(tvTimer); drawTvFrame(); tvTimer = setInterval(drawTvFrame, 720); }
applyChannel();
startTv();
channelButtons.forEach(button => button.addEventListener('click', () => {
  channelButtons.forEach(item => item.classList.remove('active'));
  button.classList.add('active');
  activeChannel = button.dataset.channel;
  tvFrame = 0;
  applyChannel();
  startTv();
}));

document.addEventListener('visibilitychange', () => {
  if (document.hidden) clearInterval(tvTimer);
  else if (!reduceMotion) startTv();
});
if (reduceMotion) clearInterval(tvTimer);
