<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

import { ConditionType, conditionTypeLabel, type ConditionDto } from '@/types'

const { t } = useI18n()

const props = defineProps<{
  condition: ConditionDto
  selectable?: boolean
  selected?: boolean
}>()

const emit = defineEmits<{
  select: [conditionId: string]
}>()

const displayText = computed(() => {
  if (props.condition.description) {
    return props.condition.description
  }
  switch (props.condition.type) {
    case ConditionType.WithSpecificPerson:
      return t('condition.withSpecificPerson', { name: props.condition.targetPersonName || t('condition.withSpecificPersonDefault') })
    case ConditionType.Alone:
      return t('condition.alone')
    case ConditionType.WithXPeople:
      return t('condition.withXPeople', { count: props.condition.minPeople ?? 2 })
    case ConditionType.MundaneAction:
      return t('condition.duringAction', { action: props.condition.action || t('condition.duringActionDefault') })
    default:
      return t('condition.custom')
  }
})
</script>

<template>
  <button
    type="button"
    class="condition-card"
    :class="{
      'condition-card--selectable': selectable,
      'condition-card--selected': selected,
    }"
    @click="selectable ? emit('select', condition.id) : null"
  >
    <span class="condition-card__type">{{ conditionTypeLabel(condition.type) }}</span>
    <span class="condition-card__description">
      {{ displayText }}
    </span>
  </button>
</template>

<style scoped>
.condition-card {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 0.875rem;
  display: grid;
  gap: 0.375rem;
  padding: 1rem;
  text-align: left;
  transition:
    border-color 0.15s ease,
    box-shadow 0.15s ease;
  width: 100%;
}

.condition-card--selectable {
  cursor: pointer;
}

.condition-card--selectable:hover {
  border-color: var(--primary);
}

.condition-card--selected {
  border-color: var(--primary);
  box-shadow: 0 0 0 3px var(--primary-ring);
}

.condition-card__type {
  color: var(--primary);
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
}

.condition-card__description {
  color: var(--text);
  font-size: 0.9375rem;
  margin: 0;
}
</style>
