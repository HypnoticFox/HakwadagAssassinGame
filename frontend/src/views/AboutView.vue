<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'

import BackButton from '@/components/BackButton.vue'

useI18n()
const router = useRouter()
</script>

<template>
  <section class="page-section">
    <BackButton
      :label="$t('common.back')"
      @click="router.push('/')"
    />
    <h1>{{ $t('about.title') }}</h1>
    <p>{{ $t('about.description') }}</p>
  </section>
</template>
