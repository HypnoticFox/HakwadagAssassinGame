<script setup lang="ts">
import { useI18n } from 'vue-i18n'

useI18n()
</script>

<template>
  <section class="page-section">
    <h1>{{ $t('about.title') }}</h1>
    <p>{{ $t('about.description') }}</p>
  </section>
</template>
