<script>
  import { onMount } from 'svelte';
  import SVGComponent from './SVGComponent.svelte';
  import "./css/compas.css"
    let visible = true;

  // Props
  export let degree = 0;
  export let capture = false;
  export let miniGamesIsPlaying = false;
  // ✅ События из клиента
  window.compassStore = {
    setVisible: (value) => visible = value,
    setDegree: (value) => degree = value
  };
  // State
  let degreesWrapperWidth = 480;
  let degreesMarksWidth = 30;
  let degreesMarksMargin = 12;
  let degreesMarksCount = 24;
  let countSideElements = 7;
  let degrees = [];
  
  // Computed
  $: getCurrentDegree = getLetterMark(String(parseInt(degree))) || parseInt(degree);
  
  // Functions
  function getLetterMark(deg) {
    const marks = {
      '0': 'N',
      '45': 'NE',
      '90': 'E',
      '135': 'SE',
      '180': 'S',
      '225': 'SW',
      '270': 'W',
      '315': 'NW',
      '360': 'N'
    };
    return marks[deg] || null;
  }
  
  function updateCompass(deg) {
    const degreesElement = document.querySelector('.degrees');
    if (!degreesElement) return;
    
    degreesElement.classList.add('transition');
    
    const step = (degreesMarksWidth + degreesMarksMargin) / (360 / degreesMarksCount) * -1;
    const offset = (degreesWrapperWidth / 2) - (degreesMarksWidth / 2) - (countSideElements * (degreesMarksWidth + degreesMarksMargin));
    const translateX = deg * step + offset;
    
    if (parseInt(deg) === 0 || parseInt(deg) === 360) {
      degreesElement.classList.remove('transition');
      degreesElement.style.transform = `translateX(${translateX}px)`;
      degreesElement.classList.add('transition');
    } else {
      degreesElement.style.transform = `translateX(${translateX}px)`;
    }
    
    // Update darkened classes
    const allDegrees = document.querySelectorAll('.degree');
    const centerIndex = Math.round(Math.abs((translateX - offset) / (degreesMarksWidth + degreesMarksMargin))) + countSideElements;
    
    allDegrees.forEach(el => el.classList.remove('darkened'));
    
    if (allDegrees[centerIndex - 1]) allDegrees[centerIndex - 1].classList.add('darkened');
    if (allDegrees[centerIndex]) allDegrees[centerIndex].classList.add('darkened');
    if (allDegrees[centerIndex + 1]) allDegrees[centerIndex + 1].classList.add('darkened');
  }
  
  function resizeWindow() {
    const windowHeight = window.innerHeight;
    degreesWrapperWidth = (480 / 1080) * windowHeight;
    degreesMarksWidth = (30 / 1080) * windowHeight;
    degreesMarksMargin = (12 / 1080) * windowHeight;
  }
  
  // Lifecycle
  onMount(() => {
    resizeWindow();
    
    // Generate degrees
    for (let i = 0; i < degreesMarksCount; i++) {
      const deg = parseInt((360 / degreesMarksCount) * i);
      const letterMark = getLetterMark(String(deg));
      degrees.push({
        id: i,
        degree: deg,
        letterMark
      });
    }
    
    degrees = degrees; // trigger reactivity
    
    setTimeout(() => {
      updateCompass(degree);
    }, 0);
  });
  
  // Watchers
  $: if (degree !== undefined) {
    updateCompass(degree);
  }
</script>
{#if visible}

<div data-v-f24a0ae4 class="compass-wrapper">
  <div data-v-82d800f6 class="compass row-block align-center justify-center" class:capture class:miniGamesIsPlaying>
    <div data-v-82d800f6 class="back back-left"></div>
    <div data-v-82d800f6 class="back back-center"></div>
    <div data-v-82d800f6 class="back back-right"></div>
    
    <div data-v-82d800f6 class="arrow">
      <SVGComponent path="icons/main/hud/compass/arrow.svg" />
    </div>
    
    <div data-v-82d800f6 
      class="current-value-deg" 
      style="width: {degreesMarksWidth}px; left: calc(50% - {degreesMarksWidth}px / 2)"
    >
      {getCurrentDegree}
    </div>
    
    <div data-v-82d800f6 
      class="degrees-wrapper row-block align-center justify-start"
      style="width: {degreesWrapperWidth}px; left: calc(50% - {degreesWrapperWidth}px / 2)"
    >
      <div data-v-82d800f6 
        class="degrees row-block align-center justify-start"
        style="width: {degreesWrapperWidth}px"
      >
        <!-- Left side elements -->
        {#each Array(countSideElements) as _, i}
          {@const index = degrees.length - 1 - (countSideElements - i - 1)}
          {#if degrees[index]}
            <div data-v-82d800f6 
              class="degree column-block align-center justify-start"
              style="margin-right: {degreesMarksMargin}px"
            >
              <div data-v-82d800f6 class="mark" class:big={degrees[index].letterMark}></div>
              {#if degrees[index].letterMark}
                <div data-v-82d800f6 class="letterMark">{degrees[index].letterMark}</div>
              {:else}
                <div data-v-82d800f6 class="degreeValue">{degrees[index].degree}</div>
              {/if}
            </div>
          {/if}
        {/each}
        
        <!-- Main degrees -->
        {#each degrees as deg, i}
          <div data-v-82d800f6 
            class="degree column-block align-center justify-start"
            style="margin-right: {degreesMarksMargin}px"
          >
            <div data-v-82d800f6 class="mark" class:big={deg.letterMark}></div>
            {#if deg.letterMark}
              <div data-v-82d800f6 class="letterMark">{deg.letterMark}</div>
            {:else}
              <div data-v-82d800f6 class="degreeValue">{deg.degree}</div>
            {/if}
          </div>
        {/each}
        
        <!-- Right side elements -->
        {#each Array(countSideElements) as _, i}
          {#if degrees[i]}
            <div data-v-82d800f6 
              class="degree column-block align-center justify-start"
              style="margin-right: {degreesMarksMargin}px"
            >
              <div data-v-82d800f6 class="mark" class:big={degrees[i].letterMark}></div>
              {#if degrees[i].letterMark}
                <div data-v-82d800f6 class="letterMark">{degrees[i].letterMark}</div>
              {:else}
                <div data-v-82d800f6 class="degreeValue">{degrees[i].degree}</div>
              {/if}
            </div>
          {/if}
        {/each}
      </div>
    </div>
  </div>
</div>
{/if}