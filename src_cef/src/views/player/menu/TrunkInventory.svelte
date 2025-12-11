<script>
    import './4068.909ec3bf.css';
    import { translateText } from 'lang'
    import { itemsInfo } from 'json/itemsInfo.js'
    import { otherName } from './functions.js'
    import { getPng } from './getPng.js'
    import { executeClient } from 'api/rage'
    
    export let OtherInfo = { Id: 0, Name: "" }
    export let ItemsData = { other: [], inventory: [] }
    export let searchText = ""
    export let onExit
    export let handleMouseDown
    export let handleSlotMouseUp
    export let handleSlotMouseEnter
    export let handleSlotMouseLeave
    export let mainInventoryArea = false
    export let getItemSize
    export let cdn
    export let maxWeight = 40000;

    let activeItemId = null;
    let panel = "warehouse";
    let groupped = [];
    let warehouseWeight = 0;
    $: capacity = Number(maxWeight) || 0;
    let selectedAmount = {}; // ✅ ТЕПЕРЬ ПРИВЯЗАН К SqlId!

    // ✅ ГРУППИРОВКА
    $: {
        const items = ItemsData["other"] || [];
        const grouped = {};
        let totalWeight = 0;

        items.forEach(item => {
            if (!item || !item.ItemId || item.ItemId === 0) return;
            
            const info = itemsInfo[item.ItemId];
            if (!info) return;

            const key = item.ItemId;
            if (!grouped[key]) {
                grouped[key] = {
                    itemId: item.ItemId,
                    title: info.Name,
                    count: 0,
                    weight: 0
                };
            }

            grouped[key].count += item.Count || 1;
            grouped[key].weight += (info.Weight || 0) * (item.Count || 1);
            totalWeight += (info.Weight || 0) * (item.Count || 1);
        });

        groupped = Object.values(grouped);
        warehouseWeight = totalWeight;
    }

    $: filteredWarehouse = searchText && searchText.length 
        ? groupped.filter(item => item.title.toLowerCase().includes(searchText.toLowerCase()))
        : groupped;

    $: filteredItems = (() => {
        if (panel === "inventory") {
            return ItemsData["inventory"].filter(item => item && item.ItemId != 0);
        } else {
            return activeItemId 
                ? ItemsData["other"].filter(item => item.ItemId === activeItemId)
                : [];
        }
    })();

    function selectItem(item) {
        activeItemId = item.itemId;
    }

    function selectPanel(newPanel) {
        panel = newPanel;
        if (newPanel === "inventory") {
            activeItemId = null;
        }
    }

    function formatNumber(num) {
        let formatted = num.toFixed(3).replace(/\.?0+$/, '');
        if (num >= 1000) {
            formatted = formatted.replace(/\B(?=(\d{3})+(?!\d))/g, '.');
        }
        return formatted;
    }

    // ✅ ИСПРАВЛЕНО: Используем SqlId вместо Index
    function transferItem(item) {
        const arrayName = panel === "warehouse" ? "other" : "inventory";
        const sourceArray = ItemsData[arrayName];
        const realIndex = sourceArray.findIndex(i => i.SqlId === item.SqlId);
        
        if (realIndex === -1) {
            console.error("[TRANSFER ERROR] Item not found in source array", item);
            return;
        }

        const amount = selectedAmount[item.SqlId] || 1;
        
        console.log(`[TRANSFER] arrayName=${arrayName}, realIndex=${realIndex}, amount=${amount}, SqlId=${item.SqlId}`);
        
        executeClient("client.gamemenu.inventory.stack", arrayName, realIndex, 2, amount);
        
        // ✅ СБРАСЫВАЕМ СЧЁТЧИК ПОСЛЕ ПЕРЕНОСА
        selectedAmount[item.SqlId] = 1;
    }

    function changeCount(item, delta) {
        const currentAmount = selectedAmount[item.SqlId] || 1;
        const newAmount = Math.max(1, Math.min(item.Count, currentAmount + delta));
        selectedAmount[item.SqlId] = newAmount;
    }

    function setMinMax(item, type) {
        if (type === "min") {
            selectedAmount[item.SqlId] = 1;
        } else {
            selectedAmount[item.SqlId] = item.Count;
        }
    }

    function inputCount(item, event) {
        const value = parseInt(event.target.value) || 1;
        selectedAmount[item.SqlId] = Math.max(1, Math.min(item.Count, value));
    }

    // ✅ ЗАЩИТА ОТ СЛУЧАЙНОГО USE
    function handleTransferClick(event, item) {
        event.stopPropagation(); // ✅ Останавливаем всплытие события
        transferItem(item);
    }
</script>

<div class="warehouse column-block black" data-v-21328df9>
    
    <!-- headerww -->
    <headerww class="full-width align-center" data-v-21328df9>
        <div class="headerww-content row-block full-height align-center full-width justify-between" data-v-21328df9>
            <div class="row-block align-center" data-v-21328df9>
                <div class="headerww-title row-block align-center" data-v-21328df9>
                    
                    <svg class="headerww-title__picture" data-v-21328df9 xmlns="http://www.w3.org/2000/svg" width="19.998" height="20" viewBox="0 0 19.998 20">
  <g id="Artworks" transform="translate(-0.001 -0.001)">
    <path id="Контур_9555" data-name="Контур 9555" d="M19.816,6.316,10.232.066a.435.435,0,0,0-.458,0L.19,6.316a.418.418,0,0,0,.458.7l1.018-.667v12.4A1.254,1.254,0,0,0,2.917,20H17.084a1.254,1.254,0,0,0,1.25-1.25V6.349c.171.083,1.076.779,1.25.733a.42.42,0,0,0,.232-.767ZM7.084,5.834h5.833a.417.417,0,0,1,0,.833H7.084a.417.417,0,0,1,0-.833Zm0,2.083h5.833a.417.417,0,0,1,0,.833H7.084a.417.417,0,0,1,0-.833Zm7.5,11.25h-.833v-2.5H10.417v2.5H9.584v-2.5H6.25v2.5H5.417v-7.5h9.167Zm-2.5-3.333H7.917V12.917a.417.417,0,0,1,.417-.417h3.333a.417.417,0,0,1,.417.417Z" transform="translate(0)" fill="#fbfbfb"/>
  </g>
</svg>
                    <span class="headerww-title__title" data-v-21328df9>{otherName[OtherInfo.Id]?.name || "Багажник"}</span>
                </div>
                <div class="weight row-block align-center" data-v-21328df9>
                    <span class="weight__value-used" data-v-21328df9>{formatNumber(warehouseWeight / 1000)}</span>
                    <span class="weight__value-total" data-v-21328df9> / {formatNumber(capacity / 1000)}</span>
                        <svg class="weight__picture" data-v-21328df9 xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="14" height="14" viewBox="0 0 14 14">
  <path d="M11.227,0H2.773A2.773,2.773,0,0,0,0,2.773v8.452A2.773,2.773,0,0,0,2.773,14h8.452A2.773,2.773,0,0,0,14,11.227V2.773A2.769,2.769,0,0,0,11.227,0ZM5.338,11.246,4.255,8.395,3.773,9.406v1.84H1.958V3.224H3.773V6.7L5.266,3.221H7.252L5.338,6.975l2.056,4.271Zm7.03-.962a2.834,2.834,0,0,1-2.25,1.107,2.242,2.242,0,0,1-2.045-1.118,5.778,5.778,0,0,1-.686-3.032A5.773,5.773,0,0,1,8.072,4.2,2.248,2.248,0,0,1,10.117,3.09a1.866,1.866,0,0,1,1.408.577,3.255,3.255,0,0,1,.782,1.493l-1.444.53c-.129-.656-.377-.987-.746-.987q-.865,0-.867,2.537t.867,2.537a.752.752,0,0,0,.651-.3V8.345H9.95V6.757h2.417v3.527Z"/>
</svg>
                </div>
            </div>
            <div class="searchbar align-center" data-v-21328df9>
                <svg class="searchbar__picture" data-v-21328df9 viewBox="0 0 16 16">
                    <circle cx="7" cy="7" r="5" fill="none" stroke="currentColor" stroke-width="2"/>
                    <line x1="11" y1="11" x2="15" y2="15" stroke="currentColor" stroke-width="2"/>
                </svg>
                <input type="text" bind:value={searchText} placeholder="Поиск..." class="searchbar__input full-width" data-v-21328df9>
            </div>
        </div>
        
        <nav class="row-block full-height align-center" data-v-21328df9>
            <div class="nav-item" 
                 class:selected={panel === "warehouse"}
                 on:click={() => selectPanel("warehouse")}
                 data-v-21328df9>
                Предметы
            </div>
            <div class="nav-item" 
                 class:selected={panel === "inventory"}
                 on:click={() => selectPanel("inventory")}
                 data-v-21328df9>
                Мой инвентарь
            </div>
        </nav>
        
        <div class="closew-block flex-block" on:click={onExit} data-v-21328df9>
            <svg data-v-21328df9 viewBox="0 0 16 16">
                <path stroke="currentColor" stroke-width="2" d="M2,2 L14,14 M14,2 L2,14"/>
            </svg>
        </div>
    </headerww>
    
    <main class="full-width full-height" data-v-21328df9>
        
        <!-- ЛЕВАЯ ПАНЕЛЬ -->
        <div class="items full-height full-width" data-v-21328df9>
            <div class="items-scrollable" data-v-21328df9>
                {#each filteredWarehouse as item (item.itemId)}
                    <div class="item column-block" 
                         class:selected={item.itemId === activeItemId}
                         on:click={() => selectItem(item)}
                         data-v-21328df9>
                        <span class="item__title" data-v-21328df9>{item.title}</span>
                        <div class="item-picture full-width" 
                             style="background-image: url(http://cdn.piecerp.ru/cloud/inventoryItems/items/{item.itemId}.png)"
                             data-v-21328df9>
                        </div>
                        <div class="row-block justify-between full-width" data-v-21328df9>
                            <div class="row-block align-end" data-v-21328df9>
                                <span class="item__value" data-v-21328df9>{item.count}</span>
                                <div class="item__measure" data-v-21328df9>шт.</div>
                            </div>
                            <div class="weight row-block align-center" data-v-21328df9>
                                <span class="weight__value" data-v-21328df9>{formatNumber(item.weight / 1000)}</span>
                                <svg class="weight__picture" data-v-21328df9 xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="14" height="14" viewBox="0 0 14 14">
  <path d="M11.227,0H2.773A2.773,2.773,0,0,0,0,2.773v8.452A2.773,2.773,0,0,0,2.773,14h8.452A2.773,2.773,0,0,0,14,11.227V2.773A2.769,2.769,0,0,0,11.227,0ZM5.338,11.246,4.255,8.395,3.773,9.406v1.84H1.958V3.224H3.773V6.7L5.266,3.221H7.252L5.338,6.975l2.056,4.271Zm7.03-.962a2.834,2.834,0,0,1-2.25,1.107,2.242,2.242,0,0,1-2.045-1.118,5.778,5.778,0,0,1-.686-3.032A5.773,5.773,0,0,1,8.072,4.2,2.248,2.248,0,0,1,10.117,3.09a1.866,1.866,0,0,1,1.408.577,3.255,3.255,0,0,1,.782,1.493l-1.444.53c-.129-.656-.377-.987-.746-.987q-.865,0-.867,2.537t.867,2.537a.752.752,0,0,0,.651-.3V8.345H9.95V6.757h2.417v3.527Z"/>
</svg>
                            </div>
                        </div>
                    </div>
                {/each}
            </div>
        </div>
        
        <!-- ✅ ПРАВАЯ ПАНЕЛЬ: ТОЛЬКО ВЫБРАННЫЕ ПРЕДМЕТЫ -->
        <div class="panel full-height align-center column-block" data-v-21328df9>
            <div class="warehouse-panel column-block full-width full-height" data-v-21328df9>
                <div class="warehouse-items column-block full-width full-height" data-v-21328df9>
                    
                    <div class="hidden-margin" data-v-21328df9>1</div>
                    
                   {#each filteredItems as item}
    <div class="item-cover" data-v-21328df9>
        <div class="warehouse-item row-block full-width" 
             data-v-21328df9
             on:mouseenter={(e) => handleSlotMouseEnter(e, ItemsData[panel === "warehouse" ? "other" : "inventory"].findIndex(i => i.SqlId === item.SqlId), panel === "warehouse" ? "other" : "inventory")}
             on:mouseleave={handleSlotMouseLeave}>
            
            <div class="warehouse-item-picture" data-v-21328df9>
                <div class="warehouse-item-picture__image full-height full-width" 
                     style="background-image: url({getPng(item, itemsInfo[item.ItemId])})"
                     data-v-21328df9>
                </div>
            </div>
            
            <div class="content column-block full-width" data-v-21328df9>
                <span class="content__title" data-v-21328df9>{itemsInfo[item.ItemId].Name}</span>
                
                <div class="top-right-icons" data-v-21328df9>
                    {#if item.gender != null}
                        <div class="gender" data-v-21328df9>
                            <img class="full-width full-height" 
                                 src="{cdn}/img/inventory/{item.gender ? 'female' : 'male'}.svg" 
                                 alt="" />
                        </div>
                    {/if}
                </div>
                
                <div class="content-cover column-block justify-between full-height" data-v-21328df9>
                    <div class="item-data" data-v-21328df9>
                        <span class="item-data__text" data-v-21328df9>{itemsInfo[item.ItemId].Description}</span>
                        <div class="item-data__row row-block" data-v-21328df9>
                            <div class="item-data__element gap-4" data-v-21328df9>
                                <span class="item-data__value" data-v-21328df9>{item.Count}</span>
                                <span class="item-data__title" data-v-21328df9>шт.</span>
                            </div>
                            <div class="item-data__element gap-4" data-v-21328df9>
                                <span class="item-data__value" data-v-21328df9>{formatNumber(itemsInfo[item.ItemId].Weight / 1000)}</span>
                                <svg class="item-data__weight-picture" data-v-21328df9 xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 14 14">
                                    <path d="M11.227,0H2.773A2.773,2.773,0,0,0,0,2.773v8.452A2.773,2.773,0,0,0,2.773,14h8.452A2.773,2.773,0,0,0,14,11.227V2.773A2.769,2.769,0,0,0,11.227,0ZM5.338,11.246,4.255,8.395,3.773,9.406v1.84H1.958V3.224H3.773V6.7L5.266,3.221H7.252L5.338,6.975l2.056,4.271Zm7.03-.962a2.834,2.834,0,0,1-2.25,1.107,2.242,2.242,0,0,1-2.045-1.118,5.778,5.778,0,0,1-.686-3.032A5.773,5.773,0,0,1,8.072,4.2,2.248,2.248,0,0,1,10.117,3.09a1.866,1.866,0,0,1,1.408.577,3.255,3.255,0,0,1,.782,1.493l-1.444.53c-.129-.656-.377-.987-.746-.987q-.865,0-.867,2.537t.867,2.537a.752.752,0,0,0,.651-.3V8.345H9.95V6.757h2.417v3.527Z"/>
                                </svg>
                            </div>
                        </div>
                        
                        {#if item.Count > 1}
                            <div class="selector" data-v-21328df9>
                                <input class="selector__input" 
                                       type="text" 
                                       value={selectedAmount[item.SqlId] || 1}
                                       on:input={(e) => inputCount(item, e)}
                                       on:click={(e) => e.stopPropagation()}
                                       maxlength="5" 
                                       data-v-21328df9>
                                <div class="selector__button-row" data-v-21328df9>
                                    <div class="selector__button" 
                                         on:click={(e) => { e.stopPropagation(); changeCount(item, -1); }}
                                         data-v-21328df9>
                                        <svg data-v-21328df9 viewBox="0 0 16 16">
                                            <line x1="4" y1="8" x2="12" y2="8" stroke="currentColor" stroke-width="2"/>
                                        </svg>
                                    </div>
                                    <div class="selector__button" 
                                         on:click={(e) => { e.stopPropagation(); changeCount(item, 1); }}
                                         data-v-21328df9>
                                        <svg data-v-21328df9 viewBox="0 0 16 16">
                                            <line x1="8" y1="4" x2="8" y2="12" stroke="currentColor" stroke-width="2"/>
                                            <line x1="4" y1="8" x2="12" y2="8" stroke="currentColor" stroke-width="2"/>
                                        </svg>
                                    </div>
                                </div>
                                <div class="selector__button-row" data-v-21328df9>
                                    <div class="selector__button" 
                                         on:click={(e) => { e.stopPropagation(); setMinMax(item, 'min'); }}
                                         data-v-21328df9>Min</div>
                                    <div class="selector__button" 
                                         on:click={(e) => { e.stopPropagation(); setMinMax(item, 'max'); }}
                                         data-v-21328df9>Max</div>
                                </div>
                            </div>
                        {/if}
                    </div>
                    
                    <div class="control-button row-block align-center" 
                         on:click={(e) => handleTransferClick(e, item)}
                         data-v-21328df9>
                        <svg class="control-button__picture" 
                             class:rotated={panel !== "warehouse"}
                             data-v-21328df9 viewBox="0 0 16 16">
                            <path fill="currentColor" d="M8,2 L8,14 M4,10 L8,14 L12,10"/>
                        </svg>
                        <span class="control-button__title" data-v-21328df9>
                            {panel === "warehouse" ? "Взять" : "Положить"}
                        </span>
                    </div>
                </div>
            </div>
        </div>
    </div>
{/each}
                    
                </div>
            </div>
        </div>
    </main>
    
</div>

<style>
    .warehouse {
        position: fixed;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        z-index: 9999;
    }

    .control-button__picture.rotated {
        transform: rotate(180deg);
    }

    .hidden-margin {
        opacity: 0;
        height: 1px;
        overflow: hidden;
    }
</style>