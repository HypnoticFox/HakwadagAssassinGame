<script setup lang="ts">
withDefaults(defineProps<{ inline?: boolean }>(), { inline: false })
</script>

<template>
  <div
    class="loading-screen"
    :class="{ 'loading-screen--inline': inline }"
    role="status"
  >
    <div class="spinner" />
  </div>
</template>

<style scoped>
.loading-screen {
  align-items: center;
  display: flex;
  justify-content: center;
  min-height: 60vh;
}

.loading-screen--inline {
  min-height: auto;
  padding: 1.5rem 0;
}

.spinner {
  animation: spin 0.8s linear infinite;
  border: 3px solid var(--border);
  border-radius: 50%;
  border-top-color: var(--primary);
  height: 2.5rem;
  width: 2.5rem;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}
</style>
