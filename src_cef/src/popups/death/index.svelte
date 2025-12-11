<script>
  import './main.css';
  import { executeClient } from 'api/rage';
  import { onMount, onDestroy } from 'svelte';

  // Popup входные данные от App.svelte / router
  export let popupData = null;
  // если App передаёт функцию обратного вызова (необязательно)
  export let popupFunc = null;

  // Основные состояния компонента
  let deathScreenData = {};
  let incomingData = {};
  let dropdownOpen = false;
  let selectedResp = null;
  let mainBoxHeight = 0;
  let containerRef = null;

  // Реактивные алиасы для шаблона
  $: places = incomingData?.places || [];
  $: isShown = !!incomingData?.isShown;
  $: isActive = !!deathScreenData?.active;

  // Нормализация popupData — поддерживаем старый формат {title,text}
  // и новый структурированный формат { isShown, places, deathScreenData, title?, text? }
  $: if (popupData) {
    if (popupData.deathScreenData || popupData.places || popupData.isShown !== undefined) {
      // новый формат пришёл
      deathScreenData = popupData.deathScreenData ?? popupData ?? {};
      incomingData = {
        places: popupData.places ?? [],
        isShown: popupData.isShown ?? true,
        title: popupData.title ?? '',
        text: popupData.text ?? ''
      };
    } else {
      // старый формат (title/text) — подсунем минимальную структуру
      incomingData = {
        places: incomingData?.places ?? [],
        isShown: true,
        title: popupData.title ?? '',
        text: popupData.text ?? ''
      };
      deathScreenData = popupData.deathScreenData ?? deathScreenData ?? {};
    }
  } else {
    // Если popupData сброшен/закрыт — чистим локальное состояние
    incomingData = {};
    deathScreenData = {};
    selectedResp = null;
    dropdownOpen = false;
  }

  // Форматирование даты/времени (ms -> dd.MM.yyyy / HH:mm)
  function formatDate(ms) {
    try {
      const d = new Date(ms);
      const day = String(d.getDate()).padStart(2, '0');
      const month = String(d.getMonth() + 1).padStart(2, '0');
      const year = d.getFullYear();
      return `${day}.${month}.${year}`;
    } catch {
      return '';
    }
  }
  function formatTime(ms) {
    try {
      const d = new Date(ms);
      const hh = String(d.getHours()).padStart(2, '0');
      const mm = String(d.getMinutes()).padStart(2, '0');
      return `${hh}:${mm}`;
    } catch {
      return '';
    }
  }

  // Возвращает путь к иконке места (placeholder если нет)
  function getImage(place) {
    if (!place) return 'icons/deathScreen/point.svg';
    let t = place.type === 'cayo_perico' ? place.type : place.type.split('_')[0];
    if (t === 'mansion') t = 'apartment';
    if (t === 'office') t = 'organization';
    return `icons/deathScreen/places/${t}.svg`;
  }

  // UI handlers
  function dropdownClick() {
    dropdownOpen = !dropdownOpen;
  }

 function selectResp(place) {
  selectedResp = place;
  dropdownOpen = false;

  // Лог для отладки — видно в CEF консоли
  console.log('[DeathPopup] selectResp', place);

  // Если нужно — сообщаем серверу сразу о выборе (альтернатива: при нажатии кнопки)
  // Здесь мы посылаем событие deathScreen.deathButton с action 'select' и типом места.
  // confirm.js должен слушать это событие и вызвать callRemote('deathScreen.deathButton', 'select', place.type)
  try {
    executeClient('deathScreen.deathButton', 'select', place?.type);
  } catch (e) {
    console.warn('[DeathPopup] executeClient deathScreen.deathButton failed', e);
  }
}

function deathButton(action) {
  console.log('[DeathPopup] deathButton', action, 'selectedResp=', selectedResp);

  // Если выбран конкретный spawn (selectedResp) — отправляем action + тип точки
  if (selectedResp && selectedResp.type) {
    try {
      executeClient('deathScreen.deathButton', action, selectedResp.type);
      return;
    } catch (e) {
      console.warn('[DeathPopup] executeClient deathScreen.deathButton failed', e);
    }
  }

  // Если нет выбранной точки — отправляем через numeric fallback на сервер
  // (confirm.js будет транслировать action -> numeric state)
  try {
    executeClient('deathScreen.deathButton', action);
  } catch (e) {
    console.warn('[DeathPopup] executeClient failed', e);
  }
}

  function resetSelectResp(event) {
    if (event && event.stopPropagation) event.stopPropagation();
    selectedResp = null;
  }

  

  // ResizeObserver для расчёта высоты фонового блока как в оригинале
  let ro = null;
  function updateMainBoxHeight() {
    if (containerRef) mainBoxHeight = Math.round(containerRef.offsetHeight * 1.9);
  }

  // Поддержка приёма данных от window.postMessage и CustomEvent (если нужно)
  function onMessageEvent(ev) {
    const d = ev?.data;
    if (!d) return;
    if (d.type === 'C2W:DeathScreen:SetData' && d.payload) {
      // payload формата из сервера
      const p = d.payload;
      // merge данные, не затирая selectedResp
      incomingData = { ...(incomingData || {}), ...(p || {}) };
      if (p.deathScreenData) deathScreenData = p.deathScreenData;
    } else if (d.type === 'W2C:DeathScreen:Data' && d.payload) {
      // альтернативный нейминг
      const p = d.payload;
      incomingData = { ...(incomingData || {}), ...(p.places ? { places: p.places } : {}) , isShown: p.isShown ?? incomingData.isShown };
      if (p.deathScreenData) deathScreenData = p.deathScreenData;
    }
  }
  function onCustomEvent(e) {
    if (e?.detail) {
      const p = e.detail;
      incomingData = { ...(incomingData || {}), ...(p.places ? { places: p.places } : {}), isShown: p.isShown ?? incomingData.isShown };
      if (p.deathScreenData) deathScreenData = p.deathScreenData;
    }
  }

  // Lifecycle
  onMount(() => {
    // Сообщаем клиенту, что popup готов (по аналогии с оригиналом)
    try { executeClient('W2C:DeathScreen:Ready'); } catch (e) { console.warn('executeClient W2C:DeathScreen:Ready failed', e); }

    // Resize observer + initial calc
    updateMainBoxHeight();
    ro = new ResizeObserver(updateMainBoxHeight);
    if (containerRef) ro.observe(containerRef);
    window.addEventListener('resize', updateMainBoxHeight);

    // Подписки на приходящие события (dev + серверные)
    window.addEventListener('message', onMessageEvent);
    window.addEventListener('C2W:DeathScreen:SetData', onCustomEvent);
    window.addEventListener('C2W:DeathScreen:SetDataMessage', onMessageEvent);

    // debug
    // console.log('PopupDeath mounted, incomingData:', incomingData, 'deathScreenData:', deathScreenData);
  });

  onDestroy(() => {
    try { executeClient('W2C:DeathScreen:Destroyed'); } catch (e) { /* ignore */ }

    window.removeEventListener('message', onMessageEvent);
    window.removeEventListener('C2W:DeathScreen:SetData', onCustomEvent);
    window.removeEventListener('C2W:DeathScreen:SetDataMessage', onMessageEvent);
    window.removeEventListener('resize', updateMainBoxHeight);
    if (ro && containerRef) ro.unobserve(containerRef);
    ro = null;
  });

  // (Опционально) helper для ручного теста из консоли:
  // window.router.setPopUp("PopupDeath", { isShown:true, places:[{type:'hospital',name:'Больница',hasOrganization:false}], deathScreenData:{ active:true, killerNone:false, killerName:'Test', killerId:1, weapon:'Pistol', distance:12, deathTime:Date.now() } });
</script>

<!-- В HTML-разметке после <div добавлен data-v-5e274ee3 -->
<div data-v-5e274ee3 class="death-screen full-width full-height">
    <div data-v-5e274ee3 class="death-bg" style="height: {mainBoxHeight}px;"></div>

    <!-- Основной контейнер (замена Vue Container) -->
    <div data-v-5e274ee3 class="death-row full-width row-block" bind:this={containerRef}>
        <div data-v-5e274ee3 class="death-info column-block">
            <span data-v-5e274ee3 class="death-info__title">ВЫ РАНЕНЫ</span>

            <!-- Иконка линии -->
            <img data-v-5e274ee3 class="death-info__icon" src="https://cdn.majestic-files.com/public/master/static/icons/deathScreen/line.svg" alt="line" />

            <span data-v-5e274ee3 class="death-info__text">Вы получили серьёзные ранения, организм борется за жизнь из последних сил</span>

            <div data-v-5e274ee3 class="control-buttons row-block">
          <!--      {#if places.length > 0 && isShown}
                    <div data-v-5e274ee3 class="top">
                        <div
                            data-v-5e274ee3
                            class="main {dropdownOpen ? 'open' : ''} {selectedResp ? 'active' : ''}"
                            on:click={dropdownClick}
                        >
                            <div data-v-5e274ee3 class="main-item">
                                <img data-v-5e274ee3 src={selectedResp && selectedResp.type ? getImage(selectedResp) : 'icons/deathScreen/point.svg'} alt="place" />
                                {#if selectedResp && selectedResp.hasOrganization}
                                    <p data-v-5e274ee3>{selectedResp.type.split('_')[0]}{selectedResp.name ? ' | ' + selectedResp.name : ''}</p>
                                {:else if selectedResp}
                                    <p data-v-5e274ee3>{selectedResp.type}</p>
                                {:else}
                                    <p data-v-5e274ee3>Выберите точку возрождения</p>
                                {/if}
                            </div>

                            <div data-v-5e274ee3 class="svg-wrapper">
                                {#if selectedResp}
                                    <img data-v-5e274ee3 class="cross" src="icons/deathScreen/cross.svg" alt="reset" on:click|stopPropagation={resetSelectResp} />
                                {/if}
                                <img data-v-5e274ee3 class="arrow" src="icons/deathScreen/arrow.svg" alt="arrow" />
                            </div>
                        </div>

                        <div data-v-5e274ee3 class="dropdown {dropdownOpen ? '' : 'hidden'}">
                            <div data-v-5e274ee3 class="scrollable {places?.length >= 5 ? 'scroll' : ''}">
                                {#each places as p (p.type + (p.name || ''))}
                                    <div data-v-5e274ee3 class="item" on:click={() => selectResp(p)}>
                                        <img data-v-5e274ee3 src={getImage(p)} alt="place" />
                                        <p data-v-5e274ee3>
                                            {p.hasOrganization
                                                ? `${p.type.split('_')[0]}${p.name ? ' | ' + p.name : ''}`
                                                : p.type}
                                            {p.type !== 'hospital' ? ` | +2 мин` : ''}
                                        </p>
                                    </div>
                                {/each}
                            </div>
                        </div>
                    </div>
                {/if}-->

                <div data-v-5e274ee3 class="bottom">
                    <div data-v-5e274ee3 class="control-button" on:click={() => deathButton('callMedics')}>
                        Вызвать медиков
                    </div>
                    <div data-v-5e274ee3 class="control-button" on:click={() => deathButton('loseConsciousness')}>
                        Потерять сознание
                    </div>
                    {#if deathScreenData?.selfRevive}
                        <div data-v-5e274ee3 class="control-button" on:click={() => deathButton('selfRevive')}>
                            Самореанимация
                        </div>
                    {/if}
                </div>
            </div>
        </div>

        {#if !deathScreenData?.killerNone}
            <div data-v-5e274ee3 class="line"></div>

            <div data-v-5e274ee3 class="killer column-block">
                <span data-v-5e274ee3 class="killer__title">Убийца</span>
                <div data-v-5e274ee3 class="killer-data row-block">
                    <div data-v-5e274ee3 class="killer-login column-block justify-center">
                        <span data-v-5e274ee3 class="killer-login__text-login">{deathScreenData?.killerName || ''}</span>
                        <span data-v-5e274ee3 class="killer-login__text-id">#{deathScreenData?.killerId ?? ''}</span>
                    </div>
                </div>

                <div data-v-5e274ee3 class="kill-description column-block">
                    <div data-v-5e274ee3 class="kill-data row-block">
                        <span data-v-5e274ee3 class="kill-data__title">Оружие:</span>
                        <span data-v-5e274ee3 class="kill-data__value">{deathScreenData?.weapon || '-'}</span>
                    </div>

                    <div data-v-5e274ee3 class="kill-data row-block">
                        <span data-v-5e274ee3 class="kill-data__title">Дистанция:</span>
                        <span data-v-5e274ee3 class="kill-data__value">{deathScreenData?.distance ?? '-'}m</span>
                    </div>

                    {#if deathScreenData?.deathTime}
                        <div data-v-5e274ee3 class="kill-date">
                            <div data-v-5e274ee3 class="kill-data row-block">
                                <span data-v-5e274ee3 class="kill-data__title">Дата:</span>
                                <span data-v-5e274ee3 class="kill-data__value">{formatDate(deathScreenData.deathTime)}</span>
                            </div>
                            <div data-v-5e274ee3 class="kill-data row-block">
                                <span data-v-5e274ee3 class="kill-data__title">Время:</span>
                                <span data-v-5e274ee3 class="kill-data__value">{formatTime(deathScreenData.deathTime)}</span>
                            </div>
                        </div>
                    {/if}
                </div>
            </div>
        {/if}
    </div>
</div>

<style>
    /* Стили не обязательны — ваш CSS уже есть. Этот блок лишь гарантирует, что минимальная структура не поломает компиляцию. */
    :global(.hidden) { display: none; }
</style>